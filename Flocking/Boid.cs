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
using log4net;
using OpenMetaverse;
using Utils = OpenSim.Framework.Util;

namespace Flocking
{
	public class Boid
	{
		private static readonly ILog m_log = LogManager.GetLogger (System.Reflection.MethodBase.GetCurrentMethod ().DeclaringType);
		private string m_id;
		
		private Vector3 m_loc;
		private Vector3 m_vel;
		private Vector3 m_acc;
		private Random m_rndnums = new Random (Environment.TickCount);
		
		private FlockingModel m_model;
		private FlowField m_flowField;

		
		/// <summary>
		/// Initializes a new instance of the <see cref="Flocking.Boid"/> class.
		/// </summary>
		/// <param name='l'>
		/// L. the initial position of this boid
		/// </param>
		/// <param name='ms'>
		/// Ms. max speed this boid can attain
		/// </param>
		/// <param name='mf'>
		/// Mf. max force / acceleration this boid can extert
		/// </param>
		public Boid (string id, FlockingModel model, FlowField flowField)
		{
			m_id = id;
			m_acc = Vector3.Zero;
			m_vel = new Vector3 (m_rndnums.Next (-1, 1), m_rndnums.Next (-1, 1), m_rndnums.Next (-1, 1));
			m_model = model;
			m_flowField = flowField;
		}
		
		public Vector3 Location {
			get { return m_loc;}
			set { m_loc = value; }
		}

		public Vector3 Velocity {
			get { return m_vel;}
		}

		public String Id {
			get {return m_id;}
		}
		
		/// <summary>
		/// Moves our boid in the scene relative to the rest of the flock.
		/// </summary>
		/// <param name='boids'>
		/// Boids. all the other chaps in the scene
		/// </param>
		public void MoveInSceneRelativeToFlock ()
		{
			List<Boid> neighbours = m_model.GetNeighbours(this);
			// we would like to stay with our mates
			Flock (neighbours);

			// our first priority is to not hurt ourselves
			// so adjust where we would like to go to avoid hitting things
			AvoidObstacles ();
			
			// then we want to avoid any threats
			//		this not implemented yet
			
			
			// ok so we worked our where we want to go, so ...
			UpdatePositionInScene ();
			
		}
		
		/// <summary>
		/// Move within our local flock
		/// We accumulate a new acceleration each time based on three rules
		/// these are:
		/// our separation from our closest neighbours,
		/// our desire to keep travelling within the local flock,
		/// our desire to move towards the flock centre
		/// 
		/// </summary>
		void Flock (List<Boid> neighbours)
		{
	
			// calc the force vectors on this boid 		
			Vector3 sep = Separate (neighbours);   // Separation
			Vector3 ali = Align (neighbours);      // Alignment
			Vector3 coh = Cohesion (neighbours);   // Cohesion
			
			// Arbitrarily weight these forces
			sep *= m_model.SeparationWeighting; 
			ali *= m_model.AlignmentWeighting;
			coh *= m_model.CohesionWeighting;
			
			// Add the force vectors to the current acceleration of the boid
			m_acc += sep;
			m_acc += ali;
			m_acc += coh;
		}
		

		/// <summary>
		/// Method to update our location within the scene.
		/// update our location in the world based on our 
		/// current location, velocity and acceleration
		/// taking into account our max speed
		/// 
		/// </summary> 
		void UpdatePositionInScene ()
		{
			// Update velocity
			m_vel += m_acc;
			// Limit speed
			m_vel = Util.Limit (m_vel, m_model.MaxSpeed);
			m_loc += m_vel;
			// Reset accelertion to 0 each cycle
			m_acc *= 0.0f;
		}
		
		/// <summary>
		/// Seek the specified target. Move into that flock
		/// Accelerate us towards where we want to go
		/// </summary>
		/// <param name='target'>
		/// Target. the position within the flock we would like to achieve
		/// </param>
		void Seek (Vector3 target)
		{
			m_acc += Steer (target, false);
		}
		
		/// <summary>
		/// Arrive the specified target. Slow us down, as we are almost there
		/// </summary>
		/// <param name='target'>
		/// Target. the flock we would like to think ourselves part of
		/// </param>
		void arrive (Vector3 target)
		{
			m_acc += Steer (target, true);
		}

		/// A method that calculates a steering vector towards a target
		/// Takes a second argument, if true, it slows down as it approaches the target
		Vector3 Steer (Vector3 target, bool slowdown)
		{
			Vector3 steer = Vector3.Zero;  // The steering vector
			Vector3 desired = target - m_loc;  // A vector pointing from the location to the target
			float distance = desired.Length (); // Distance from the target is the magnitude of the vector
			// If the distance is greater than 0, calc steering (otherwise return zero vector)
			if (distance > 0) {
				// Normalize desired
				desired.Normalize ();
				// Two options for desired vector magnitude (1 -- based on distance, 2 -- maxspeed)
				if ((slowdown) && (distance < m_model.LookaheadDistance )) { 
					desired *= (m_model.MaxSpeed * (distance / m_model.LookaheadDistance)); // This damping is somewhat arbitrary
				} else { 
					desired *= m_model.MaxSpeed;
				}
				// Steering = Desired minus Velocity
				steer = desired - m_vel;
				//steer.limit(maxforce);  // Limit to maximum steering force
				steer = Util.Limit (steer, m_model.MaxForce);
			} 
			return steer;
		}

		
		/// <summary>
		/// navigate away from whatever it is we are too close to
		/// </summary>
		void AvoidObstacles ()
		{
			//look tolerance metres ahead
			m_acc += m_flowField.AdjustVelocity( m_loc, m_vel, m_model.Tolerance ); 
		}

		
		/// <summary>
		/// Separate ourselves from the specified boids.
		/// keeps us a respectable distance from our closest neighbours whilst still 
		/// being part of our local flock
		/// </summary>
		/// <param name='neighbours'>
		/// Boids. all the boids in the scene
		/// </param>
		Vector3 Separate (List<Boid> neighbours)
		{
			// For every boid in the system, check if it's too close
			float desired = m_model.DesiredSeparation;
			//who are we too close to at the moment
			List<Boid> tooCloseNeighbours = neighbours.FindAll( delegate(Boid neighbour) {
				// Is the distance is less than an arbitrary amount
				return Utils.GetDistanceTo (m_loc, neighbour.Location) < desired;
			});
			
			// move a bit away from them
			Vector3 steer = Vector3.Zero;
			tooCloseNeighbours.ForEach( delegate(Boid neighbour) {
				// Calculate vector pointing away from neighbor
				Vector3 diff = m_loc - neighbour.Location;
				steer += Utils.GetNormalizedVector(diff) / (float)Utils.GetDistanceTo (m_loc, neighbour.Location);			
			});
			
			if( steer.Length () > 0 ) {
				// Average -- divide by how many
				steer /= (float)tooCloseNeighbours.Count;
				// Implement Reynolds: Steering = Desired - Velocity
				steer.Normalize ();
				steer *= m_model.MaxSpeed;
				steer -= m_vel;
				//don't go too fast;
				steer = Util.Limit (steer, m_model.MaxForce);
			} 
			return steer;
		}

		/// <summary>
		/// Align our boid within the flock.
		/// For every nearby boid in the system, calculate the average velocity
		/// and move us towards that - this keeps us moving with the flock.
		/// </summary>
		/// <param name='boids'>
		/// Boids. all the boids in the scene - we only really care about those in the neighbourdist
		/// </param>
		Vector3 Align (List<Boid> boids)
		{
			Vector3 steer = Vector3.Zero;
			
			boids.ForEach( delegate( Boid other ) {
				steer += other.Velocity;
			});
				
			int count = boids.Count;
			
			if (count > 0) {
				steer /= (float)count;
			}

			// As long as the vector is greater than 0
			if (steer.Length () > 0) {
				// Implement Reynolds: Steering = Desired - Velocity
				steer.Normalize ();
				steer *= m_model.MaxSpeed;
				steer -= m_vel;
				//steer.limit(maxforce);
				steer = Util.Limit (steer, m_model.MaxForce);
				
			}
			return steer;
		}

		/// <summary>
		/// MAintain the cohesion of our local flock
		/// For the average location (i.e. center) of all nearby boids, calculate our steering vector towards that location
		/// </summary>
		/// <param name='neighbours'>
		/// Boids. the boids in the scene
		/// </param>
		Vector3 Cohesion (List<Boid> neighbours)
		{
    
			Vector3 sum = Vector3.Zero;   // Start with empty vector to accumulate all locations
			
			neighbours.ForEach( delegate(Boid other) {
				sum += other.Location; // Add location
			});	
			
			int count = neighbours.Count;
			if (count > 0) {
				sum /= (float)count;
				return Steer (sum, false);  // Steer towards the location
			}
			return sum;
		}
	}
}

