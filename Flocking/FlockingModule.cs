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

		public static string NAME = "FlockingModule";

		private bool m_enabled = false;
		private int m_chatChannel = 118;
		private string m_boidPrim = "boid-prim";
		private FlockingController m_controller;
		private BoidBehaviour m_behaviour;
		private int m_flockSize = 100;

		#region IRegionModule Members

		public void Initialise (IConfigSource source)
		{
			//check if we are in the ini files
			//if so get some default values out of them and pass them onto the controller
			IConfig config = source.Configs ["Boids"];
			if (config != null) {
				m_chatChannel = config.GetInt ("chat-channel", 118);
				m_boidPrim = config.GetString ("boid-prim", "boidPrim");
				m_flockSize = config.GetInt ("flock-size", 100);
				
				m_behaviour = new BoidBehaviour ();
				m_behaviour.maxSpeed = config.GetFloat ("max-speed", 1f);
				m_behaviour.maxForce = config.GetFloat ("max-force", 0.25f);
				m_behaviour.neighbourDistance = config.GetFloat ("neighbour-dist", 25f);
				m_behaviour.desiredSeparation = config.GetFloat ("desired-separation", 20f);
				m_behaviour.tolerance = config.GetFloat ("tolerance", 5f);
				m_behaviour.separationWeighting = config.GetFloat ("separation-weighting", 1.5f);
				m_behaviour.alignmentWeighting = config.GetFloat ("alignment-weighting", 1f);
				m_behaviour.cohesionWeighting = config.GetFloat ("cohesion-weighting", 1f);
				m_behaviour.lookaheadDistance = config.GetFloat ("lookahead-dist", 100f);

				// we're in the config - so turn on this module
				m_enabled = true;
			}
		}

		public void AddRegion (Scene scene)
		{
		}

		public void RegionLoaded (Scene scene)
		{
			if (m_enabled) {
				//set up the boid module
				m_controller = new FlockingController (scene, m_behaviour, m_chatChannel, m_boidPrim, m_flockSize);
				RegisterCommand (new RoostCommand());
				RegisterCommand (new StopCommand());
				RegisterCommand (new StartCommand());
				RegisterCommand (new SetSizeCommand());
				RegisterCommand (new ShowStatsCommand());
				RegisterCommand (new SetPrimCommand());
				RegisterCommand (new SetPositionCommand());
				RegisterCommand (new SetBoundsCommand());
				RegisterCommand (new SetFrameRateCommand());
				RegisterCommand (new SetParameterCommand());
			}
		}
		
		public void RegisterCommand (FlockingCommand cmd)
		{
			m_controller.AddCommand( this, cmd);
		}


		
		public void RemoveRegion (Scene scene)
		{
			if (m_enabled) {
				m_controller.Deregister ();
			}
		}

		public string Name {
			get { return NAME; }
		}

		public bool IsSharedModule {
			get { return false; }
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
