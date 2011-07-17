using System;
using OpenMetaverse;
using NUnit.Framework;

namespace Flocking
{
	[TestFixture()]
	public class VectorTest
	{
		[Test()]
		public void TestCase ()
		{
			Vector3 [,] field =  new Vector3[3,3];
			Vector2 start = new Vector2(0f, 0f);
			Assert.That( field[1,1].Z == 0 );
			
			field[1,0] = Vector3.UnitZ;
			
			
		}
	}
}

