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
using System.Timers;
using System.Collections.Generic;
using OpenMetaverse;
using System.IO;
using Nini.Config;
using System.Threading;
using log4net;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Framework;
using OpenSim.Framework.Console;

namespace Flocking
{
	public class FlockingModule : INonSharedRegionModule
	{

		private static readonly ILog m_log = LogManager.GetLogger (System.Reflection.MethodBase.GetCurrentMethod ().DeclaringType);
		static object m_sync = new object();

		private Scene m_scene;
		private FlockingModel m_model;
		private FlockingView m_view;
		private bool m_enabled = false;
		private bool m_ready = false;
		private uint m_frame = 0;
		private int m_frameUpdateRate = 1;
		private int m_chatChannel = 118;
		private string m_boidPrim;
		private ChatCommandParser m_chatCommandParser;
		private BoidBehaviour m_behaviour;
		private int m_flockSize = 100;

		private UUID m_owner;

		#region IRegionModule Members



		public void Initialise (IConfigSource source)
		{
			//check if we are in the ini files
			//if so get some physical constants out of them and pass into the model
			IConfig config = source.Configs ["Boids"];
			if (config != null) {
				m_chatChannel = config.GetInt ("chat-channel", 118);
				m_boidPrim = config.GetString ("boid-prim", "boidPrim");
				m_flockSize = config.GetInt ("flock-size", 100);
				
				m_behaviour = new BoidBehaviour();
				m_behaviour.maxSpeed = config.GetFloat("max-speed", 3f);
				m_behaviour.maxForce = config.GetFloat("max-force", 0.25f);
				m_behaviour.neighbourDistance = config.GetFloat("neighbour-dist", 25f);
				m_behaviour.desiredSeparation = config.GetFloat("desired-separation", 20f);
				m_behaviour.tolerance = config.GetFloat("tolerance", 5f);
				m_behaviour.separationWeighting = config.GetFloat("separation-weighting", 1.5f);
				m_behaviour.alignmentWeighting = config.GetFloat("alignment-weighting", 1f);
				m_behaviour.cohesionWeighting = config.GetFloat("cohesion-weighting", 1f);
				m_behaviour.lookaheadDistance = config.GetFloat("lookahead-dist", 100f);

				// we're in the config - so turn on this module
				m_enabled = true;
			}
		}

		public void AddRegion (Scene scene)
		{
			//m_log.Info ("ADDING FLOCKING");
			m_scene = scene;
			if (m_enabled) {
				//register commands
				m_chatCommandParser = new ChatCommandParser(this, scene, m_chatChannel);
				RegisterCommands ();
				
				//register handlers
				m_scene.EventManager.OnFrame += FlockUpdate;
				m_scene.EventManager.OnChatFromClient += m_chatCommandParser.SimChatSent; //listen for commands sent from the client

				// init module
				m_model = new FlockingModel (m_behaviour);
				
				m_view = new FlockingView (m_scene);
				m_view.BoidPrim = m_boidPrim;
			}
		}

		public void RegionLoaded (Scene scene)
		{
			if (m_enabled) {
				
				//build a proper flow field based on the scene
				FlowField field = new FlowField(scene, new Vector3(128f, 128f, 128f), 200, 200, 200);
				
				//ask the view how big the boid is
				Vector3 scale = m_view.GetBoidSize();
//				m_log.Error( m_boidPrim + " = " + scale.ToString());
				
				// Generate initial flock values
				m_model.Initialise (m_flockSize, scale, field);
				
				// who is the owner for the flock in this region
				m_owner = m_scene.RegionInfo.EstateSettings.EstateOwner;
				m_view.PostInitialize (m_owner);

				// Mark Module Ready for duty
				m_ready = true;
			}
		}

		public void RemoveRegion (Scene scene)
		{
			if (m_enabled) {
				m_scene.EventManager.OnFrame -= FlockUpdate;
				m_scene.EventManager.OnChatFromClient -= m_chatCommandParser.SimChatSent;
			}
		}

		public string Name {
			get { return "FlockingModule"; }
		}

		public bool IsSharedModule {
			get { return false; }
		}

		#endregion
		
		#region EventHandlers
		
		public void FlockUpdate ()
		{
			if (((m_frame++ % m_frameUpdateRate) != 0) || !m_ready || !m_enabled) {
				return;
			}
			// work out where everyone has moved to
			// and tell the scene to render the new positions
			lock( m_sync ) {
				List<Boid > boids = m_model.UpdateFlockPos ();
				m_view.Render (boids);
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
			m_scene.AddCommand (this, "flock-" + cmd, "flock-" + cmd + argStr, help, fn);
			m_chatCommandParser.AddCommand(cmd, args, help, fn);
		}

		private void RegisterCommands ()
		{
			AddCommand ("stop", "", "Stop all Flocking", HandleStopCmd);
			AddCommand ("start", "", "Start Flocking", HandleStartCmd);
			AddCommand ("size", "num", "Adjust the size of the flock ", HandleSetSizeCmd);
			AddCommand ("stats", "", "show flocking stats", HandleShowStatsCmd);
			AddCommand ("prim", "name", "set the prim used for each boid to that passed in", HandleSetPrimCmd);
			AddCommand ("framerate", "num", "[debugging] only update boids every <num> frames", HandleSetFrameRateCmd);
			AddCommand ("set", "name, value", "change the flock dynamics", HandleSetParameterCmd);
		}
		
		private bool ShouldHandleCmd ()
		{
			return m_scene.ConsoleScene () == m_scene;
		}
		
		private bool IsInWorldCmd (ref string [] args)
		{
			bool retVal = false;
			
			if (args.Length > 0 && args [args.Length - 1].Equals ("<ui>")) {
				retVal = true;	
			}
			return retVal;
		}
		
		private void ShowResponse (string response, bool inWorld)
		{
			if (inWorld) {
					ScenePresence owner = m_scene.GetScenePresence(m_owner);
					m_chatCommandParser.SendMessage(owner, response);
			} else {
				MainConsole.Instance.Output (response);
			}
		}
		
		public void HandleSetParameterCmd(string module, string[] args)
		{
			if (ShouldHandleCmd ()) {
				string name = args[1];
				string newVal = args[2];
				
				if( m_behaviour.IsValidParameter( name ) ) {
					m_behaviour.SetParameter(name, newVal);
				} else {
					bool inWorld = IsInWorldCmd( ref args);
					ShowResponse( name + "is not a valid flock parameter", inWorld );
					ShowResponse( "valid parameters are: " + m_behaviour.GetList(), inWorld);
				}
			}
		}
		
		public void HandleStopCmd (string module, string[] args)
		{
			if (ShouldHandleCmd ()) {
				m_log.Info ("stop the flocking capability");
				m_enabled = false;
				m_view.Clear ();
			}
		}

		void HandleSetFrameRateCmd (string module, string[] args)
		{
			if (ShouldHandleCmd ()) {
				int frameRate = Convert.ToInt32( args[1] );
				m_frameUpdateRate = frameRate;
			}
		}

		public void HandleStartCmd (string module, string[] args)
		{
			if (ShouldHandleCmd ()) {
				m_log.Info ("start the flocking capability");
				m_enabled = true;
				FlockUpdate ();
			}
		}

		public void HandleSetSizeCmd (string module, string[] args)
		{
			if (ShouldHandleCmd ()) {
				lock( m_sync ) {
					int newSize = Convert.ToInt32(args[1]);
					m_model.Size = newSize;
					m_view.Clear();
				}
			}
		}
		
		public void HandleShowStatsCmd (string module, string[] args)
		{
			if (ShouldHandleCmd ()) {
				bool inWorld = IsInWorldCmd (ref args);
				string str = m_model.ToString();
				ShowResponse (str, inWorld);
			}
		}
		
		public void HandleSetPrimCmd (string module, string[] args)
		{
			if (ShouldHandleCmd ()) {
				string primName = args[1];
				lock(m_sync) {
					m_view.BoidPrim = primName;
					m_view.Clear();
				}
			}
		}

		#endregion



		#region IRegionModuleBase Members



		public void Close ()
		{
		}

		public Type ReplaceableInterface {
			get { return null; }
		}
		
		#endregion
	}
	
}
