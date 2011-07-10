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
using OpenMetaverse;

namespace Flocking
{
	public class FlockingModel
	{
        private List<Boid> m_flock = new List<Boid>();
		private FlowMap m_flowMap;
		
		private Random m_rnd = new Random(Environment.TickCount);
		
		public int Size {
			get {return m_flock.Count;}
			set {
				if( value < m_flock.Count ) {
					m_flock.RemoveRange( 0, m_flock.Count - value );
				} else while( value > m_flock.Count ) {
					AddBoid( "boid"+m_flock.Count);	
				}
			}
		}

		void AddBoid (string name)
		{
			Boid boid = new Boid (name, 3.0f, 0.05f, m_flowMap);
			
			// find an initial random location for this Boid
			// somewhere not within an obstacle
			int xInit = m_rnd.Next(m_flowMap.LengthX);
			int yInit = m_rnd.Next(m_flowMap.LengthY);
			int zInit = m_rnd.Next(m_flowMap.LengthZ);
			
			while( m_flowMap.IsWithinObstacle( xInit, yInit, zInit ) ){
				xInit = m_rnd.Next(m_flowMap.LengthX);
				yInit = m_rnd.Next(m_flowMap.LengthY);
				zInit = m_rnd.Next(m_flowMap.LengthZ);
			}
				
			boid.Location = new Vector3 (Convert.ToSingle(xInit), Convert.ToSingle(yInit), Convert.ToSingle(zInit));
			m_flock.Add (boid);
		}
						
						
				
		public void Initialise (int num, FlowMap flowMap)
		{
			m_flowMap = flowMap;			
  			for (int i = 0; i < num; i++) {
				AddBoid ("boid"+i );
  			}
		}

		public List<Boid> UpdateFlockPos ()
		{
    		foreach (Boid b in m_flock) {
      			b.MoveInSceneRelativeToFlock(m_flock);  // Passing the entire list of boids to each boid individually
    		}
			
			return m_flock;
		}
	}
}

