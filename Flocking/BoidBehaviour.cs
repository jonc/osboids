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

namespace Flocking
{
	public class BoidBehaviour
	{
		public float maxSpeed;
		public float maxForce;
		public float neighbourDistance;
		public float desiredSeparation;
		public float tolerance;
		public float separationWeighting;
		public float alignmentWeighting;
		public float cohesionWeighting;
		public float lookaheadDistance;
		private Dictionary<string, string> m_paramDescriptions = new Dictionary<string, string> ();

		public float AlignmentWeighting {
			get {
				return this.alignmentWeighting;
			}
			set {
				alignmentWeighting = value;
			}
		}

		public float CohesionWeighting {
			get {
				return this.cohesionWeighting;
			}
			set {
				cohesionWeighting = value;
			}
		}

		public float DesiredSeparation {
			get {
				return this.desiredSeparation;
			}
			set {
				desiredSeparation = value;
			}
		}

		public float LookaheadDistance {
			get {
				return this.lookaheadDistance;
			}
			set {
				lookaheadDistance = value;
			}
		}

		public float MaxForce {
			get {
				return this.maxForce;
			}
			set {
				maxForce = value;
			}
		}

		public float MaxSpeed {
			get {
				return this.maxSpeed;
			}
			set {
				maxSpeed = value;
			}
		}

		public float NeighbourDistance {
			get {
				return this.neighbourDistance;
			}
			set {
				neighbourDistance = value;
			}
		}

		public float SeparationWeighting {
			get {
				return this.separationWeighting;
			}
			set {
				separationWeighting = value;
			}
		}

		public float Tolerance {
			get {
				return this.tolerance;
			}
			set {
				tolerance = value;
			}
		}		
		public BoidBehaviour() {
			m_paramDescriptions.Add("max-speed", "max distance boid will travel per frame");
			m_paramDescriptions.Add("max-force", "max acceleration od decelleration boid can exert");
			m_paramDescriptions.Add("neighbour-distance", "boid will consider other boids within this distance as part of local flock");
			m_paramDescriptions.Add("desired-separation", "closest distance to other boids that our boid would like to get");
			m_paramDescriptions.Add("tolerance", "how close to the edges of objects or the scene should our boid get");
			m_paramDescriptions.Add("separation-weighting", "factor by which closeness to other boids should be favoured when updating position in flock");
			m_paramDescriptions.Add("alignment-weighting", "factor by which alignment with other boids should be favoured when updating position in flock");
			m_paramDescriptions.Add("cohesion-weighting", "factor by which keeping within the local flock should be favoured when updating position in flock");
			m_paramDescriptions.Add("lookahead-distance", "how far in front should the boid look for edges and boundaries");
		}

		public bool IsValidParameter (string name)
		{
			return m_paramDescriptions.ContainsKey (name);
		}

		public void SetParameter (string name, string newVal)
		{
			switch (name) {
			case "max-speed":
				maxSpeed = Convert.ToSingle(newVal);
				break;
			case "max-force":
				maxForce = Convert.ToSingle(newVal);
				break;
			case "neighbour-distance":
				neighbourDistance = Convert.ToSingle(newVal);
				break;
			case "desired-separation":
				desiredSeparation = Convert.ToSingle(newVal);
				break;
			case "tolerance":
				tolerance = Convert.ToSingle(newVal);
				break;
			case "separation-weighting":
				separationWeighting = Convert.ToSingle(newVal);
				break;
			case "alignment-weighting":
				alignmentWeighting = Convert.ToSingle(newVal);
				break;
			case "cohesion-weighting":
				cohesionWeighting = Convert.ToSingle(newVal);
				break;
			case "lookahead-distance":
				lookaheadDistance = Convert.ToSingle(newVal);
				break;
			}
		}

		public string GetList ()
		{
			string retVal = Environment.NewLine;
			foreach (string name in m_paramDescriptions.Keys) {
				retVal += name + " - " + m_paramDescriptions [name] + Environment.NewLine;
			}
			
			return retVal;
		}
		
		public override string ToString ()
		{
			return string.Format (
				"alignment-weighting  = {0}, " + Environment.NewLine +
				"cohesion-weighting   = {1}, " + Environment.NewLine +
				"desired-separation   = {2}, " + Environment.NewLine +
				"lookahead-distance   = {3}, " + Environment.NewLine +
				"max-force            = {4}, " + Environment.NewLine +
				"max-speed            = {5}, " + Environment.NewLine +
				"neighbour-distance   = {6}, " + Environment.NewLine +
				"separation-weighting = {7}, " + Environment.NewLine +
				"tolerance            = {8}", AlignmentWeighting, CohesionWeighting, DesiredSeparation, LookaheadDistance, MaxForce, MaxSpeed, NeighbourDistance, SeparationWeighting, Tolerance);
		}
	}
}

