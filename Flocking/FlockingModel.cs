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
        private  List<Boid> flock = new List<Boid>();
		
		private int m_xRange = 200;
		private int m_yRange = 200;
		private int m_zRange = 200;
		
		public int Size {
			get {return flock.Count;}
			set {
				if( value < flock.Count ) {
					flock.RemoveRange( 0, flock.Count - value );
				} else while( value > flock.Count ) {
					AddBoid( "boid"+flock.Count);	
				}
			}
		}

		void AddBoid (string name)
		{
			Boid boid = new Boid (name, 3.0f, 0.05f);
			boid.Location = new Vector3 (m_xRange / 2f, m_yRange / 2f, m_zRange / 2f);
			flock.Add (boid);
		}
						
						
				
		public void Initialise (int num, int xRange, int yRange, int zRange)
		{
			m_xRange = xRange;
			m_yRange = yRange;
			m_zRange = zRange;
			
			//TODO: fill in the initial Flock array properly
  			for (int i = 0; i < num; i++) {
				AddBoid ("boid"+i );
  			}
		}

		public List<Boid> UpdateFlockPos ()
		{
    		foreach (Boid b in flock) {
      			b.MoveInSceneRelativeToFlock(flock);  // Passing the entire list of boids to each boid individually
    		}
			
			return flock;
		}
	}
}

