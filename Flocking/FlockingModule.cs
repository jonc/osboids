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

		private Scene m_scene;
		
		private FlockingModel m_model;
		private FlockingView m_view;

		private bool m_enabled = false;
		private bool m_ready = false;
       	
		private uint m_frame = 0;
        private int m_frameUpdateRate = 1;


		#region IRegionModule Members



		public void Initialise (IConfigSource source)
		{
			//TODO:  check if we are in the ini files
			//TODO:  if so get some physical constants out of them and pass into the model
			m_enabled = true;
		}

		public void AddRegion (Scene scene)
		{
			m_log.Info("ADDING FLOCKING");
			m_scene = scene;
			if (m_enabled) {
				m_scene.EventManager.OnFrame += FlockUpdate;

				m_model = new FlockingModel();
				m_view = new FlockingView (m_scene);
				
				m_scene.AddCommand (this, "flocking", "I haz got a Flocking Module", "wotever" , null);
			}
		}

		public void RegionLoaded (Scene scene)
		{
			if (m_enabled) {
                // Generate initial flock values
                m_model.Initialise( 200, 255, 255, 255);
				m_view.PostInitialize();

                // Mark Module Ready for duty
                m_ready = true;
			}
		}

		public void RemoveRegion (Scene scene)
		{
			if (m_enabled) {
				m_scene.EventManager.OnFrame -= FlockUpdate;
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
		
		public void FlockUpdate()
        {
            if (((m_frame++ % m_frameUpdateRate) != 0) || !m_ready)
            {
                return;
            }
			
			//m_log.InfoFormat("update my boids");
			
			// work out where everyone has moved to
			// and tell the scene to render the new positions
            List<Boid> boids = m_model.UpdateFlockPos();
            m_view.Render(boids);
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
