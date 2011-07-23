/*
 * Copyright (c) Contributors, https://github.com/jonc/osboids
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
using System;
using System.Collections.Generic;
using log4net;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Framework.Console;
using OpenSim.Region.Framework.Interfaces;

namespace Flocking
{
	public delegate void BoidCmdDelegate (string module,string[] args);
	
	public class BoidCmdDefn
	{
		public string Help = "";
		public string Args = "";
		public int NumParams = 0;
		string m_name;

		public BoidCmdDefn (string name, string args, string help)
		{
			Help = help;
			Args = args;
			m_name = name;
			
			if (args.Trim ().Length > 0) {
				NumParams = args.Split (",".ToCharArray ()).Length;
			} else {
				NumParams = 0;
			}
		}
		
		public string GetSyntax ()
		{ 
			return m_name + " " + Args + " (" + Help + ")"; 
		}
	}

	public class FlockingCommandParser
	{
		private static readonly ILog m_log = LogManager.GetLogger (System.Reflection.MethodBase.GetCurrentMethod ().DeclaringType);
		private IRegionModuleBase m_module;
		private Scene m_scene;
		private int m_chatChannel;
		private UUID m_owner;
		private Dictionary<string, BoidCmdDelegate> m_commandMap = new Dictionary<string, BoidCmdDelegate> ();
		private Dictionary<string, BoidCmdDefn> m_syntaxMap = new Dictionary<string, BoidCmdDefn> ();

		
		public FlockingCommandParser (IRegionModuleBase module, Scene scene, int channel)
		{
			m_module = module;
			m_scene = scene;
			m_chatChannel = channel;
			
			// who do we respond to in send messages
			m_owner = scene.RegionInfo.EstateSettings.EstateOwner;

			// register our event handlers
			m_scene.EventManager.OnChatFromClient += ProcessChatCommand; //listen for commands sent from the client
      			
			IScriptModuleComms commsMod = scene.RequestModuleInterface<IScriptModuleComms>();
      		commsMod.OnScriptCommand += ProcessScriptCommand;
		}

		public void Deregister ()
		{
			m_scene.EventManager.OnChatFromClient -= ProcessChatCommand;
			IScriptModuleComms commsMod = m_scene.RequestModuleInterface<IScriptModuleComms>();
      		commsMod.OnScriptCommand -= ProcessScriptCommand;
		}
		
		public void AddCommand (string cmd, string args, string help, CommandDelegate fn)
		{
			string argStr = "";
			if (args.Trim ().Length > 0) {
				argStr = " <" + args + "> ";
			}
			m_commandMap.Add (cmd, new BoidCmdDelegate (fn));
			m_syntaxMap.Add (cmd, new BoidCmdDefn (cmd, args, help));
			// register this command with the console
			m_scene.AddCommand (m_module, "flock-" + cmd, "flock-" + cmd + argStr, help, fn);
		}


		#region handlers
		
		public void ProcessScriptCommand (UUID scriptId, string reqId, string module, string input, string key)
		{
			if (m_module.Name != module) {
				return;
			}

			string[] tokens = (input+"|<script>").Split ( new char[] { '|' }, StringSplitOptions.None);

			string command = tokens [0];
			m_log.Debug("Input was " + input + ", command is " + command);
			BoidCmdDefn defn = null;
			if (m_syntaxMap.TryGetValue (command, out defn)) {
				if (CorrectSignature (tokens, defn)) {
					
					// we got the signature of the command right
					BoidCmdDelegate del = null;
					if (m_commandMap.TryGetValue (command, out del)) {
						m_log.Info("command ok - executing");
						del (module, tokens);
					} else {
						// we don't understand this command
						// shouldn't happen
						m_log.ErrorFormat ("Unable to invoke command {0}", command);
					}
				} else {
					m_log.Error(" signature wrong for " + command);
				}
			} else {
				m_log.Error("no command for " + command);
			}
		}
		
		void RespondToScript (UUID scriptId, int msgNum, string response)
		{
			IScriptModuleComms commsMod = m_scene.RequestModuleInterface<IScriptModuleComms>();
			if( commsMod != null ) {
				commsMod.DispatchReply (scriptId, msgNum, response, "");
			} else {
				Console.WriteLine("No script comms");
			}
		}
		
		public void ProcessChatCommand (Object x, OSChatMessage msg)
		{
			if (m_scene.ConsoleScene () != m_scene || msg.Channel != m_chatChannel)
				return; // not for us

			// try and parse a valid cmd from this msg
			string cmd = msg.Message.ToLower ();
			
			//stick ui in the args so we know to respond in world
			//bit of a hack - but lets us use CommandDelegate inWorld
			string[] args = (cmd + " <ui>").Split (" ".ToCharArray ());
			
			BoidCmdDefn defn = null;
			if (m_syntaxMap.TryGetValue (args [0], out defn)) {
				if (CorrectSignature (args, defn)) {
					
					// we got the signature of the command right
					BoidCmdDelegate del = null;
					if (m_commandMap.TryGetValue (args [0], out del)) {
						del (m_module.Name, args);
					} else {
						// we don't understand this command
						// shouldn't happen
						m_log.ErrorFormat ("Unable to invoke command {0}", args [0]);
						RespondToChat (msg, "Unable to invoke command " + args [0]);
					}

				} else {
					//	we recognise the command, but we got the call wrong
					RespondToChat (msg, "wrong syntax:  " + defn.GetSyntax ());
				}
			} else {
				// this is not a command we recognise
				RespondToChat (msg, args [0] + " is not a valid command for osboids");
			}
			
		}
		
		public void RespondToChat (OSChatMessage msg, string message)
		{
			m_log.Debug ("sending response -> " + message);
			IClientAPI sender = msg.Sender;
			sender.SendChatMessage (message, (byte)ChatTypeEnum.Say, msg.Position, "osboids", msg.SenderUUID, (byte)ChatSourceType.Agent, (byte)ChatAudibleLevel.Fully);
		}


		#endregion

		private bool CorrectSignature (string[] args, BoidCmdDefn defn)
		{
			// args contain cmd name at 0 and <ui> tagged in last pos
			return args.Length - 2 == defn.NumParams;
		}
		
		public void ShowResponse (string response, string [] cmd)
		{
			bool inWorld = IsInWorldCmd (cmd);
			if (inWorld) {
					ScenePresence owner = m_scene.GetScenePresence(m_owner);
					SendMessage(owner, response);
			} else {
				MainConsole.Instance.Output (response);
			}
		}

		private bool IsInWorldCmd (string [] args)
		{
			bool retVal = false;
			
			if (args.Length > 0 && args [args.Length - 1].Equals ("<ui>")) {
				retVal = true;	
			}
			return retVal;
		}


		public void SendMessage (ScenePresence recipient, string message)
		{
			IClientAPI ownerAPI = recipient.ControllingClient;
			ownerAPI.SendChatMessage (message, 
				(byte)ChatTypeEnum.Say, 
				recipient.AbsolutePosition, 
				"osboids", 
				recipient.UUID, 
				(byte)ChatSourceType.Agent, 
				(byte)ChatAudibleLevel.Fully
			);
		}
	}
}

