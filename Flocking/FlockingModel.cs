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
	public enum FlockGoal
	{
    	Roost = 0x01,
    	Perch = 0x02,
    	Flock = 0x04,
	}
	
	public class FlockingModel
	{
        private List<Boid> m_flock = new List<Boid>();
		private FlowField m_flowField;
		private BoidBehaviour m_behaviour;		
		private int m_flockSize = 100;
		private Vector3 m_boidSize;
		private Vector3 m_startPos;
		private FlockGoal m_goal = FlockGoal.Flock;
		private bool m_active = false;
		private int m_minX = 0;
		private int m_maxX = 256;
		private int m_minY = 0;
		private int m_maxY = 256;
		private int m_minZ = 0;
		private int m_maxZ = 256;

		
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
		
		public FlockGoal Goal {
			get { return m_goal; }
			set { m_goal = value; }
		}

		public int MaxX {
			get {
				return this.m_maxX;
			}
			set {
				m_maxX = value;
			}
		}

		public int MaxY {
			get {
				return this.m_maxY;
			}
			set {
				m_maxY = value;
			}
		}

		public int MaxZ {
			get {
				return this.m_maxZ;
			}
			set {
				m_maxZ = value;
			}
		}

		public int MinX {
			get {
				return this.m_minX;
			}
			set {
				m_minX = value;
			}
		}

		public int MinY {
			get {
				return this.m_minY;
			}
			set {
				m_minY = value;
			}
		}

		public int MinZ {
			get {
				return this.m_minZ;
			}
			set {
				m_minZ = value;
			}
		}		
		public bool Active {
			get { return m_active; }
			set { m_active = value; }
		}
		
		public Vector3 StartPosition {
			get { return m_startPos; }
			set { m_startPos = value; }
		}
		
		public BoidBehaviour Behaviour {
			get { return m_behaviour; }
			set { m_behaviour = value; }
		}
		
		public Vector3 BoidSize {
			set { m_boidSize = value; }
		}
		
		
		public FlockingModel( FlowField field, BoidBehaviour behaviour, Vector3 startPos ) {
			m_flowField = field;
			m_behaviour = behaviour;
			m_startPos = startPos;
		}

		void AddBoid (string name)
		{
			Boid boid = new Boid (name, m_boidSize, m_behaviour);
			double d1 = ( Utils.RandomClass.NextDouble() - 0.5 ) * 20;
			double d2 = ( Utils.RandomClass.NextDouble() - 0.5 ) * 20;
			double d3 = ( Utils.RandomClass.NextDouble() - 0.5 ) * 20;
			boid.Location = m_startPos + new Vector3( (float)d1, (float)d2, (float)d3 );
			boid.Velocity = Vector3.UnitX;
			m_flock.Add (boid);
		}

		public bool ContainsPoint (Vector3 position)
		{
			return m_flowField.ContainsPoint( position );
		}
		
		
		public void Initialise (FlowField flowField)
		{
			m_flowField = flowField;
  			for (int i = 0; i < m_flockSize; i++) {
				AddBoid ("boid"+i );
				//UpdateFlockPos();
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
			if( m_active ) {
				m_flock.ForEach( delegate(Boid boid) {
					boid.MoveInSceneRelativeToFlock(GetNeighbours(boid), m_flowField);	
				} );
			}
			
			return m_flock;
		}
		
		
		public override string ToString ()
		{
			string retVal = "Num Boids: " + m_flockSize + Environment.NewLine
				+ m_behaviour.ToString() + Environment.NewLine
				+ m_flowField.ToString();
			
			return retVal;
		}
	}
}

