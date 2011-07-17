using System;
using System.Collections.Generic;

namespace Flocking
{
	public class FlockParameters
	{
//		public int flockSize;
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
		
		public FlockParameters() {
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
	}
}

