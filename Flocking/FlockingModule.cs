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
using OpenSim.Services.Interfaces;


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
		private bool m_active = false;
		private uint m_frame = 0;
		private int m_frameUpdateRate = 1;
		private int m_chatChannel = 118;
		private string m_boidPrim = "boid-prim";
		private FlockingCommandParser m_commandParser;
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
				
				//register handlers
				m_scene.EventManager.OnFrame += FlockUpdate;
			}
		}

		public void RegionLoaded (Scene scene)
		{
			if (m_enabled) {
				// who is the owner for the flock in this region
				m_owner = scene.RegionInfo.EstateSettings.EstateOwner;
				
				//register command handler
				m_commandParser = new FlockingCommandParser(this, scene, m_chatChannel);
				RegisterCommands ();
				
				// init view
				m_view = new FlockingView (scene);
				m_view.PostInitialize (m_owner);
				m_view.BoidPrim = m_boidPrim;
			}
		}
		
		public void RemoveRegion (Scene scene)
		{
			if (m_enabled) {
				m_scene.EventManager.OnFrame -= FlockUpdate;
				m_commandParser.Deregister();
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
			if (((m_frame++ % m_frameUpdateRate) != 0) || !m_active || !m_enabled) {
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
		
		
		private void BuildFlowField(Vector3 centre, int width, int depth, int height) {
			m_log.Info("building flow field");
			//build a flow field based on the scene
			FlowField field = new FlowField(m_scene, centre, width, depth, height);
			m_log.Info("built");
			//ask the view how big the boid prim is
			Vector3 scale = m_view.GetBoidSize();
				
			Vector3 startPos = m_scene.GetSceneObjectPart(m_view.BoidPrim).ParentGroup.AbsolutePosition;
			// init model
			m_log.Info("creating model");
			m_model = new FlockingModel (m_behaviour, startPos );
			// Generate initial flock values
			m_model.Initialise (m_flockSize, scale, field);
			m_log.Info("done");

		}
		
		#region Command Handling
		

		private void RegisterCommands ()
		{
			m_commandParser.AddCommand ("stop", "", "Stop all Flocking", HandleStopCmd);
			m_commandParser.AddCommand ("start", "", "Start Flocking", HandleStartCmd);
			m_commandParser.AddCommand ("size", "num", "Adjust the size of the flock ", HandleSetSizeCmd);
			m_commandParser.AddCommand ("stats", "", "show flocking stats", HandleShowStatsCmd);
			m_commandParser.AddCommand ("prim", "name", "set the prim used for each boid to that passed in", HandleSetPrimCmd);
			m_commandParser.AddCommand ("framerate", "num", "[debugging] only update boids every <num> frames", HandleSetFrameRateCmd);
			m_commandParser.AddCommand ("set", "name, value", "change the flock behaviour properties", HandleSetParameterCmd);
		}
		
		private bool ShouldHandleCmd ()
		{
			return m_scene.ConsoleScene () == m_scene;
		}
		
		public void HandleSetParameterCmd(string module, string[] args)
		{
			if (ShouldHandleCmd ()) {
				string name = args[1];
				string newVal = args[2];
				
				if( m_behaviour.IsValidParameter( name ) ) {
					m_behaviour.SetParameter(name, newVal);
				} else {
					m_commandParser.ShowResponse( name + "is not a valid flock parameter", args );
					m_commandParser.ShowResponse( "valid parameters are: " + m_behaviour.GetList(), args);
				}
			}
		}
		
		public void HandleStopCmd (string module, string[] args)
		{
			if (ShouldHandleCmd ()) {
				m_log.Info ("stop the flocking capability");
				m_active = false;
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
				BuildFlowField(new Vector3(128f, 128f, 128f), 200, 200, 200);
				m_active = true;
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
				string str = m_model.ToString();
				m_commandParser.ShowResponse (str, args);
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
