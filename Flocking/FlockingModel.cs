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
		private FlockParameters m_parameters;		
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
		
		public FlockingModel( FlockParameters parameters ) {
			m_parameters = parameters;
		}

		void AddBoid (string name)
		{
			Boid boid = new Boid (name, this, m_flowField);
			
			// find an initial random location for this Boid
			// somewhere not within an obstacle
			int xInit = m_rnd.Next(Util.SCENE_SIZE);
			int yInit = m_rnd.Next(Util.SCENE_SIZE);
			int zInit = m_rnd.Next(Util.SCENE_SIZE);
			Vector3 location = new Vector3 (Convert.ToSingle(xInit), Convert.ToSingle(yInit), Convert.ToSingle(zInit));
			boid.Location = location + m_flowField.AdjustVelocity(location, Vector3.UnitZ, 5f);
			m_flock.Add (boid);
		}
						
		public float MaxSpeed {
			get {return m_parameters.maxSpeed;}
		}
				
		public float MaxForce {
			get {return m_parameters.maxForce;}
		}

		public float NeighbourDistance {
			get {return m_parameters.neighbourDistance;}
		}
				
		public float DesiredSeparation {
			get {return m_parameters.desiredSeparation;}
		}
				
		public float Tolerance {
			get {return m_parameters.tolerance;}
		}
				
		public float SeparationWeighting {
			get{ return m_parameters.separationWeighting; }
			set{ m_parameters.separationWeighting = value;}
		}
		
		public float AlignmentWeighting {
			get{ return m_parameters.alignmentWeighting; }
			set{ m_parameters.alignmentWeighting = value;}
		}
		
		public float CohesionWeighting {
			get{ return m_parameters.cohesionWeighting; }
			set{ m_parameters.cohesionWeighting = value;}
		}
		
		public float LookaheadDistance {
			get { return m_parameters.lookaheadDistance; }
			set { m_parameters.lookaheadDistance = value;}
		}
		

		public void Initialise (FlowField flowField)
		{
			m_flowField = flowField;			
  			for (int i = 0; i < m_parameters.flockSize; i++) {
				AddBoid ("boid"+i );
  			}
		}
		
		public List<Boid> GetNeighbours(Boid boid) {
			return m_flock.FindAll(delegate(Boid other) 
			{
				return (boid != other) && (Utils.GetDistanceTo (boid.Location, other.Location) < m_parameters.neighbourDistance);
			});
		}


		public List<Boid> UpdateFlockPos ()
		{
			m_flock.ForEach( delegate(Boid boid) {
				boid.MoveInSceneRelativeToFlock();	
			} );
			
			return m_flock;
		}
	}
}

