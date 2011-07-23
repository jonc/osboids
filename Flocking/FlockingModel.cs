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
using Utils = OpenSim.Framework.Util;

namespace Flocking
{
	public class FlockingModel
	{
        private List<Boid> m_flock = new List<Boid>();
		private FlowField m_flowField;
		private BoidBehaviour m_behaviour;		
		private Random m_rnd = new Random(Environment.TickCount);
		private int m_flockSize;
		private Vector3 m_boidSize;
		private Vector3 m_startPos;
		
		public int Size {
			get {return m_flockSize;}
			set {
				if( value < m_flock.Count ) {
					m_flock.RemoveRange( 0, m_flock.Count - value );
				} else while( value > m_flock.Count ) {
					AddBoid( "boid"+m_flock.Count);	
				}
			}
		}
		
		public FlockingModel( BoidBehaviour behaviour, Vector3 startPos ) {
			m_behaviour = behaviour;
			m_startPos = startPos;
		}

		void AddBoid (string name)
		{
			Boid boid = new Boid (name, m_boidSize, m_behaviour);
			
			boid.Location = m_startPos;
			boid.Velocity = Vector3.UnitX;
			m_flock.Add (boid);
		}
		
		
		public void Initialise (int flockSize, Vector3 boidSize, FlowField flowField)
		{
			m_flowField = flowField;
			m_flockSize = flockSize;
			m_boidSize = boidSize;
  			for (int i = 0; i < m_flockSize; i++) {
				AddBoid ("boid"+i );
				UpdateFlockPos();
  			}
		}
		
		public List<Boid> GetNeighbours(Boid boid) {
			return m_flock.FindAll(delegate(Boid other) 
			{
				return (boid != other) && (Utils.GetDistanceTo (boid.Location, other.Location) < m_behaviour.neighbourDistance);
			});
		}


		public List<Boid> UpdateFlockPos ()
		{
			m_flock.ForEach( delegate(Boid boid) {
				boid.MoveInSceneRelativeToFlock(GetNeighbours(boid), m_flowField);	
			} );
			
			return m_flock;
		}
		
		
		public override string ToString ()
		{
			string retVal = "Num Boids: " + m_flockSize + Environment.NewLine
				+ m_behaviour.ToString();
			
			return retVal;
		}
	}
}

