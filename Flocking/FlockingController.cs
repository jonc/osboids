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

	public class FlockingController
	{
		private static readonly ILog m_log = LogManager.GetLogger (System.Reflection.MethodBase.GetCurrentMethod ().DeclaringType);
		public object UI_SYNC = new object ();

		private Scene m_scene;
		private FlockingModel m_model;
		private FlockingView m_view;
		private int m_chatChannel;
		private UUID m_owner;
		private Dictionary<string, BoidCmdDelegate> m_commandMap = new Dictionary<string, BoidCmdDelegate> ();
		private Dictionary<string, BoidCmdDefn> m_syntaxMap = new Dictionary<string, BoidCmdDefn> ();
		private uint m_frame = 0;
		private int m_frameUpdateRate = 1;
		private Vector3 m_startPos = new Vector3 (128f, 128f, 128f);
		private int m_minX = 0;
		private int m_maxX = 256;
		private int m_minY = 0;
		private int m_maxY = 256;
		private int m_minZ = 0;
		private int m_maxZ = 256;


		
		public FlockingController (Scene scene, BoidBehaviour behaviour, int channel, string prim, int flockSize)
		{
			//make the view
			// who is the owner for the flock in this region
			UUID owner = scene.RegionInfo.EstateSettings.EstateOwner;
			m_view = new FlockingView (scene);
			m_view.PostInitialize (owner);
			m_view.BoidPrim = prim;

			//make the model			
			FlowField field = new FlowField( scene, m_minX, m_maxX, m_minY, m_maxY, m_minZ, m_maxZ);
			FlockingModel model = new FlockingModel(field, behaviour, m_startPos);
			Vector3 startPos = new Vector3(128f, 128f, 128f);//scene.GetSceneObjectPart (View.BoidPrim).ParentGroup.AbsolutePosition;
			model.StartPosition = startPos; // TODO: by default start from the prim

			m_model = model;
			m_scene = scene;
			m_chatChannel = channel;
			
			// who do we respond to in send messages
			m_owner = m_scene.RegionInfo.EstateSettings.EstateOwner;

			// register our event handlers
			m_scene.EventManager.OnFrame += FlockUpdate; // plug in to the game loop
			m_scene.EventManager.OnChatFromClient += ProcessChatCommand; //listen for commands sent from the client      			
			IScriptModuleComms commsMod = m_scene.RequestModuleInterface<IScriptModuleComms>();
      		commsMod.OnScriptCommand += ProcessScriptCommand; // listen to scripts
		}
		
		public void Start()
		{
			//ask the view how big the boid prim is
			Vector3 scale = View.GetBoidSize ();
				
			FlowField field = new FlowField( m_scene, m_minX, m_maxX, m_minY, m_maxY, m_minZ, m_maxZ);
			// init model
			m_log.Info ("creating model");
			// Generate initial flock values
			m_model.BoidSize = scale;
			m_model.Initialise (field);
			m_log.Info ("done");

		}

		
		public int FrameUpdateRate {
			get { return m_frameUpdateRate; }
			set { m_frameUpdateRate = value; } 
		}
		
		public FlockingModel Model {
			get { return m_model; }
		}
		
		public FlockingView View {
			get { return m_view; }
		}

		public void Deregister ()
		{
			m_scene.EventManager.OnChatFromClient -= ProcessChatCommand;
			IScriptModuleComms commsMod = m_scene.RequestModuleInterface<IScriptModuleComms>();
      		commsMod.OnScriptCommand -= ProcessScriptCommand;
			m_scene.EventManager.OnFrame -= FlockUpdate;
		}
		
		public void AddCommand (IRegionModuleBase module, FlockingCommand cmd)
		{
			cmd.Controller = this;
			string name = cmd.Name;
			string args = cmd.Params;
			string help = cmd.Description;
			CommandDelegate fn =cmd.Handle;
			
			string argStr = "";
			if (args.Trim ().Length > 0) {
				argStr = " <" + args + "> ";
			}
			m_commandMap.Add (name, new BoidCmdDelegate (fn));
			m_syntaxMap.Add (name, new BoidCmdDefn (name, args, help));
			// register this command with the console
			m_scene.AddCommand (module, "flock-" + name, "flock-" + name + argStr, help, fn);
		}


		#region handlers
		
		public void FlockUpdate ()
		{
			if (((m_frame++ % m_frameUpdateRate) != 0) || !m_model.Active) {
				return;
			}
			// work out where everyone has moved to
			// and tell the scene to render the new positions
			lock (UI_SYNC) {
				List<Boid > boids = m_model.UpdateFlockPos ();
				m_view.Render (boids);
			}
		}
		

		
		public void ProcessScriptCommand (UUID scriptId, string reqId, string module, string input, string key)
		{
			if (FlockingModule.NAME != module) {
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
						del (FlockingModule.NAME, args);
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

