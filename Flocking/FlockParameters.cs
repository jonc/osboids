using System;

namespace Flocking
{
	public struct FlockParameters
	{
		public int flockSize;
		public float maxSpeed;
		public float maxForce;
		public float neighbourDistance;
		public float desiredSeparation;
		public float tolerance;
		public float separationWeighting;
		public float alignmentWeighting;
		public float cohesionWeighting;
		public float lookaheadDistance;
	}
}

