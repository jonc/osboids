/*
 * Copyright (c) Contributors, https://github.com/jonc/osboids
 * https://github.com/JakDaniels/OpenSimBirds
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
using System.Timers;
using System.Net;
using System.IO;
using System.Text;
using System.Collections.Generic;
using OpenMetaverse;
using Nini.Config;
using System.Threading;
using log4net;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using Mono.Addins;

[assembly: Addin("OpenSimBirds", "0.2")]
[assembly: AddinDependency("OpenSim.Region.Framework", OpenSim.VersionInfo.VersionNumber)]

namespace Flocking
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "OpenSimBirds")]
    public class FlockingModule : INonSharedRegionModule
    {
        #region Fields
        private static readonly ILog m_log = LogManager.GetLogger (System.Reflection.MethodBase.GetCurrentMethod ().DeclaringType);

        public string m_name = "OpenSimBirds";
        private Scene m_scene;
        private ICommandConsole m_console;
		private FlockingModel m_model;
		private FlockingView m_view;
		private bool m_enabled = false;
		private bool m_ready = false;
		private uint m_frame = 0;
		private int m_frameUpdateRate = 1;
		private int m_chatChannel = 118;
		private string m_birdPrim;
		private int m_flockSize = 100;
		private float m_maxSpeed;
		private float m_maxForce;
		private float m_neighbourDistance;
		private float m_desiredSeparation;
		private float m_tolerance;
        private float m_borderSize;
        private int m_maxHeight;
        static object m_sync = new object();

        public IConfigSource m_config;

		private UUID m_owner;
        #endregion

        #region IRegionModuleBase implementation

        public string Name { get { return m_name; } }
        public Type ReplaceableInterface { get { return null; } }
        public bool IsSharedModule { get { return false; } }

        public void Initialise (IConfigSource source)
		{
            m_config = source;
		}

		public void AddRegion (Scene scene)
		{
            m_log.InfoFormat("[{0}]: Adding region '{1}' to this module", m_name, scene.RegionInfo.RegionName);
            IConfig cnf = m_config.Configs[scene.RegionInfo.RegionName];

            if (cnf == null)
            {
                m_log.InfoFormat("[{0}]: No region section [{1}] found in configuration. Birds in this region are set to Disabled", m_name, scene.RegionInfo.RegionName);
                m_enabled = false;
                return;
            }

            m_enabled = cnf.GetBoolean("BirdsEnabled", false);

            if (m_enabled)
            {
                m_chatChannel = cnf.GetInt("BirdsChatChannel", 118);
                m_birdPrim = cnf.GetString("BirdsPrim", "birdPrim");
                m_flockSize = cnf.GetInt("BirdsFlockSize", 100);
                m_maxSpeed = cnf.GetFloat("BirdsMaxSpeed", 3f);
                m_maxForce = cnf.GetFloat("BirdsMaxForce", 0.25f);
                m_neighbourDistance = cnf.GetFloat("BirdsNeighbourDistance", 25f);
                m_desiredSeparation = cnf.GetFloat("BirdsDesiredSeparation", 20f);
                m_tolerance = cnf.GetFloat("BirdsTolerance", 5f);
                m_borderSize = cnf.GetFloat("BirdsRegionBorderSize", 5f);
                m_maxHeight = cnf.GetInt("BirdsMaxHeight", 256);
                m_frameUpdateRate = cnf.GetInt("BirdsUpdateEveryNFrames", 1);

                m_log.InfoFormat("[{0}] Enabled on channel {1} with Flock Size {2}", m_name, m_chatChannel, m_flockSize);

                m_scene = scene;
                m_console = MainConsole.Instance;

                //register commands with the scene
                RegisterCommands();

                //register handlers
                m_scene.EventManager.OnFrame += FlockUpdate;
                m_scene.EventManager.OnChatFromClient += SimChatSent; //listen for commands sent from the client

                // init module
                m_model = new FlockingModel(m_name, m_maxSpeed, m_maxForce, m_neighbourDistance, m_desiredSeparation, m_tolerance, m_borderSize);
                m_view = new FlockingView(m_name, m_scene);
                m_view.BirdPrim = m_birdPrim;
                m_frame = 0;

                FlockInitialise();

             }
		}

		public void RegionLoaded (Scene scene)
		{
            //m_scene = scene;
            if (m_enabled) {
                // Mark Module Ready for duty
				m_ready = true;
			}            
		}

		public void RemoveRegion (Scene scene)
		{
            m_log.InfoFormat("[{0}]: Removing region '{1}' from this module", m_name, scene.RegionInfo.RegionName);
            if (m_enabled) {
                m_view.Clear();
                m_ready = false;
				scene.EventManager.OnFrame -= FlockUpdate;
				scene.EventManager.OnChatFromClient -= SimChatSent;
			}
		}

        public void Close()
        {
            if (m_enabled)
            {
                m_ready = false;
                m_scene.EventManager.OnFrame -= FlockUpdate;
                m_scene.EventManager.OnChatFromClient -= SimChatSent;
            }
        }

		#endregion

        #region Helpers

        public void FlockInitialise()
        {
            //make a flow map for this scene
            FlowMap flowMap = new FlowMap(m_scene, m_maxHeight, m_borderSize);
            flowMap.Initialise();

            // Generate initial flock values
            m_model.Initialise(m_flockSize, flowMap);

            // who is the owner for the flock in this region
            m_owner = m_scene.RegionInfo.EstateSettings.EstateOwner;
            m_view.PostInitialize(m_owner);
        }

        #endregion

        #region EventHandlers

        public void FlockUpdate ()
		{
			if (((m_frame++ % m_frameUpdateRate) != 0) || !m_ready || !m_enabled) {
				return;
			}
			
			//m_log.InfoFormat("update my birds");
			
			// work out where everyone has moved to
			// and tell the scene to render the new positions
			lock( m_sync ) {
				List<Bird > birds = m_model.UpdateFlockPos ();
				m_view.Render (birds);
			}
		}
		
		protected void SimChatSent (Object x, OSChatMessage msg)
		{
			if (msg.Channel != m_chatChannel)
				return; // not for us

			// try and parse a valid cmd from this msg
			string cmd = msg.Message.ToLower ();
			
			//stick ui in the args so we know to respond in world
			//bit of a hack - but lets us use CommandDelegate inWorld
			string[] args = (cmd + " <ui>").Split (" ".ToCharArray ());
			
			if (cmd.StartsWith ("stop")) {
				HandleStopCmd (m_name, args);
			} else if (cmd.StartsWith ("start")) {
				HandleStartCmd (m_name, args);
            } else if (cmd.StartsWith("enable")) {
                HandleEnableCmd(m_name, args);
            } else if (cmd.StartsWith("disable")) {
                HandleDisableCmd(m_name, args);
			} else if (cmd.StartsWith ("size")) {
				HandleSetSizeCmd (m_name, args);
			} else if (cmd.StartsWith ("stats")) {
				HandleShowStatsCmd (m_name, args);
			} else if (cmd.StartsWith ("prim")) {
				HandleSetPrimCmd (m_name, args);
			} else if (cmd.StartsWith ("framerate")) {
				HandleSetFrameRateCmd (m_name, args);
			}
			
		}

		#endregion
		
		#region Command Handling
		
		private void AddCommand (string cmd, string args, string help, CommandDelegate fn)
		{
			string argStr = "";
			if (args.Trim ().Length > 0) {
				argStr = " <" + args + "> ";
			}
            m_log.InfoFormat("[{0}]: Adding command {1} - {2} to region '{3}'", m_name, "birds-" + cmd + argStr, help, m_scene.RegionInfo.RegionName);
            //m_scene.AddCommand (this, "birds-" + cmd, "birds-" + cmd + argStr, help, fn);
            m_console.Commands.AddCommand(m_name, false, "birds-" + cmd, "birds-" + cmd + argStr, help, fn);
        }

		private void RegisterCommands ()
		{
			AddCommand ("stop", "", "Stop Birds Flocking", HandleStopCmd);
			AddCommand ("start", "", "Start Birds Flocking", HandleStartCmd);
            AddCommand ("enable", "", "Enable Birds Flocking", HandleEnableCmd);
            AddCommand ("disable", "", "Disable Birds Flocking", HandleDisableCmd);
			AddCommand ("size", "num", "Adjust the size of the flock ", HandleSetSizeCmd);
			AddCommand ("stats", "", "show flocking stats", HandleShowStatsCmd);
			AddCommand ("prim", "name", "set the prim used for each bird to that passed in", HandleSetPrimCmd);
			AddCommand ("framerate", "num", "[debugging] only update birds every <num> frames", HandleSetFrameRateCmd);
		}
		
		private bool ShouldHandleCmd ()
		{
            if (!(m_console.ConsoleScene == null || m_console.ConsoleScene == m_scene)) {
                return false;
            }
            return true;
    	}
		
		private bool IsInWorldCmd (ref string [] args)
		{
			if (args.Length > 0 && args [args.Length - 1].Equals ("<ui>")) {
                m_log.InfoFormat("[{0}]: Inworld command detected in region {1}", m_name, m_scene.RegionInfo.RegionName);
                return true;	
			}
            return false;
		}
		
		private void ShowResponse (string response, bool inWorld)
		{
			if (inWorld) {
				IClientAPI ownerAPI = null;
				if (m_scene.TryGetClient (m_owner, out ownerAPI)) {
					ownerAPI.SendBlueBoxMessage (m_owner, "Birds", response);
				}
			} else {
				MainConsole.Instance.Output (response);
			}
		}

        public void HandleDisableCmd(string module, string[] args)
        {
            if (m_ready && ShouldHandleCmd ()) {
                m_log.InfoFormat("[{0}]: Bird flocking is disabled in region {1}.", m_name, m_scene.RegionInfo.RegionName);
                m_enabled = false;
                m_ready = false;
                m_view.Clear();
            }
        }

        public void HandleEnableCmd(string module, string[] args)
        {
            if (!m_ready && ShouldHandleCmd())
            {
                m_log.InfoFormat("[{0}]: Bird flocking is enabled in region {1}.", m_name, m_scene.RegionInfo.RegionName);
                m_enabled = true;
                m_ready = true;
            }
        }
		
		public void HandleStopCmd (string module, string[] args)
		{
            if (m_enabled && m_ready && ShouldHandleCmd())
            {
                m_log.InfoFormat("[{0}]: Bird flocking is stopped in region {1}.", m_name, m_scene.RegionInfo.RegionName);
				m_enabled = false;
			}
		}

        public void HandleStartCmd(string module, string[] args)
        {
            if (!m_enabled && m_ready && ShouldHandleCmd())
            {
                m_log.InfoFormat("[{0}]: Bird flocking is started in region {1}.", m_name, m_scene.RegionInfo.RegionName);
                m_enabled = true;
                FlockUpdate();
            }
        }

		void HandleSetFrameRateCmd (string module, string[] args)
		{
			if (ShouldHandleCmd ()) {
				int frameRate = Convert.ToInt32( args[1] );
				m_frameUpdateRate = frameRate;
                m_log.InfoFormat("[{0}]: Bird updates set to every {1} frames in region {2}.", m_name, frameRate, m_scene.RegionInfo.RegionName);
			}
		}

		public void HandleSetSizeCmd (string module, string[] args)
		{
			if (ShouldHandleCmd ()) {
				lock( m_sync ) {
					int newSize = Convert.ToInt32(args[1]);
					m_model.Size = newSize;
                    m_log.InfoFormat("[{0}]: Bird flock size is set to {1} in region {2}.", m_name, newSize, m_scene.RegionInfo.RegionName);
					m_view.Clear();
				}
			}
		}
		
		public void HandleShowStatsCmd (string module, string[] args)
		{
			if (ShouldHandleCmd ()) {
				bool inWorld = IsInWorldCmd (ref args);
				ShowResponse ("Num Birds = " + m_model.Size, inWorld);
			}
		}
		
		public void HandleSetPrimCmd (string module, string[] args)
		{
			if (ShouldHandleCmd ()) {
				string primName = args[1];
				lock(m_sync) {
					m_view.BirdPrim = primName;
                    m_log.InfoFormat("[{0}]: Bird prim is set to {1} in region {2}.", m_name, primName, m_scene.RegionInfo.RegionName);
					m_view.Clear();
				}
			}
		}

		#endregion

	}
	
}
