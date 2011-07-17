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
				NumParams = args.Split (", ".ToCharArray ()).Length;
			} else {
				NumParams = 0;
			}
		}
		
		public string GetSyntax ()
		{ 
			return m_name + " " + Args + " (" + Help + ")"; 
		}
	}

	public class ChatCommandParser
	{
		private static readonly ILog m_log = LogManager.GetLogger (System.Reflection.MethodBase.GetCurrentMethod ().DeclaringType);
		private string m_name;
		private Scene m_scene;
		private int m_chatChannel;
		private Dictionary<string, BoidCmdDelegate> m_commandMap = new Dictionary<string, BoidCmdDelegate> ();
		private Dictionary<string, BoidCmdDefn> m_syntaxMap = new Dictionary<string, BoidCmdDefn> ();
		
		public ChatCommandParser (IRegionModuleBase module, Scene scene, int channel)
		{
			m_name = module.Name;
			m_scene = scene;
			m_chatChannel = channel;
		}

		public void AddCommand (string cmd, string args, string help, CommandDelegate fn)
		{
			m_commandMap.Add (cmd, new BoidCmdDelegate (fn));
			m_syntaxMap.Add (cmd, new BoidCmdDefn (cmd, args, help));
		}
		
		public void SimChatSent (Object x, OSChatMessage msg)
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
						del (m_name, args);
					} else {
						// we don't understand this command
						// shouldn't happen
						m_log.ErrorFormat ("Unable to invoke command {0}", args [0]);
						RespondToMessage (msg, "Unable to invoke command " + args [0]);
					}

				} else {
					//	we recognise the command, but we got the call wrong
					RespondToMessage (msg, "wrong syntax:  " + defn.GetSyntax ());
				}
			} else {
				// this is not a command we recognise
				RespondToMessage (msg, args [0] + " is not a valid command for osboids");
			}
			
		}

		private bool CorrectSignature (string[] args, BoidCmdDefn defn)
		{
			// args contain cmd name at 0 and <ui> tagged in last pos
			return args.Length - 2 == defn.NumParams;
		}

		public void RespondToMessage (OSChatMessage msg, string message)
		{
			m_log.Debug ("sending response -> " + message);
			IClientAPI sender = msg.Sender;
			sender.SendChatMessage (message, (byte)ChatTypeEnum.Say, msg.Position, "osboids", msg.SenderUUID, (byte)ChatSourceType.Agent, (byte)ChatAudibleLevel.Fully);
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

