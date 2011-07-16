using System;
using NUnit.Framework;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Tests.Common;
using OpenSim.Tests.Common.Mock;


namespace Flocking
{
	[TestFixture()]
	public class FlowFieldTest
	{
		[Test()]
		public void TestEmptyFlowField ()
		{
            Scene scene = SceneSetupHelpers.SetupScene();
			
			Vector3 centre = new Vector3 (100f, 100f, 100f);
			FlowField field = new FlowField(scene, centre, 50, 50, 50);
			Vector3 strength = field.FieldStrength (centre);
			Assert.That( strength == Vector3.Zero);
		}
		
		[Test()]
		public void TestWeCanMoveFreely ()
		{
            Scene scene = SceneSetupHelpers.SetupScene();
			
			Vector3 centre = new Vector3 (100f, 100f, 100f);
			FlowField field = new FlowField(scene, centre, 50, 50, 50);
			Vector3 pos = new Vector3(100f, 100f,100f);
			Vector3 velocity = new Vector3(20f, 0f, 0f);
			Vector3 newVel = field.AdjustVelocity (pos, velocity, 10);
			Console.WriteLine( newVel );
			Assert.That(newVel == velocity);
			Vector3 newPos = pos+newVel;
			Assert.That( newPos.X < 150f);
		}
		
		[Test()]
		public void TestWeDontFallOfTheEdge ()
		{
           Scene scene = SceneSetupHelpers.SetupScene();
			
			Vector3 centre = new Vector3 (100f, 100f, 100f);
			FlowField field = new FlowField(scene, centre, 50, 50, 50);
			
			Vector3 pos = new Vector3(140f, 100f,100f);
			Vector3 velocity = new Vector3(20f, 0f, 0f);
			Vector3 newVel = field.AdjustVelocity (pos, velocity, 10);
			Console.WriteLine( newVel );
			Vector3 newPos = pos+newVel;
			Assert.That( newPos.X < 150f);
			Assert.That(velocity != newVel);
			
			pos = new Vector3(60f, 100f, 100f);
			velocity = new Vector3(-20f, 0f, 0f);
			newVel = field.AdjustVelocity(pos, velocity, 10);
			newPos = pos+newVel;
			Assert.That( newPos.X > 50f );
			Assert.That(velocity != newVel);
		}
		
		[Test()]
		public void TestWeCanCopeWithCorners ()
		{
           Scene scene = SceneSetupHelpers.SetupScene();
			
			Vector3 centre = new Vector3 (100f, 100f, 100f);
			FlowField field = new FlowField(scene, centre, 50, 50, 50);
			Vector3 pos = new Vector3(140f, 140f,140f);
			Vector3 velocity = new Vector3(20f, 20f, 20f); // going to hit the corner
			Vector3 newVel = field.AdjustVelocity (pos, velocity, 10);
			Console.WriteLine( newVel );
			Vector3 newPos = pos+newVel;
			Assert.That( newPos.X < 150f);
			Assert.That( newPos.Y < 150f);
			Assert.That( newPos.Z < 150f);
			Assert.That(velocity != newVel);
		}
		
		[Test()]
		[Ignore()]
		public void TestNonEmptyFlowField ()
		{
            Scene scene = SceneSetupHelpers.SetupScene();
			Vector3 centre = new Vector3 (100f, 100f, 100f);
			SceneObjectGroup sceneObjectGroup = AddSog (centre, new Vector3(10f,10f,10f));
			scene.AddNewSceneObject(sceneObjectGroup, false);

			FlowField field = new FlowField(scene, centre, 50, 50, 50);
			Vector3 strength = field.FieldStrength (centre);
			Assert.That( strength != Vector3.Zero);
			
 		}

		public static SceneObjectGroup AddSog (Vector3 position, Vector3 size)
		{
			UUID ownerId = new UUID("00000000-0000-0000-0000-000000000010");
            string part1Name = "part1";
            UUID part1Id = new UUID("00000000-0000-0000-0000-000000000001");
            string part2Name = "part2";
            UUID part2Id = new UUID("00000000-0000-0000-0000-000000000002");

            SceneObjectPart part1
                = new SceneObjectPart(ownerId, PrimitiveBaseShape.Default, position, Quaternion.Identity, Vector3.Zero) 
                    { Name = part1Name, UUID = part1Id };
			part1.Scale =size;
            SceneObjectGroup so = new SceneObjectGroup(part1);
 			
			return so;

		}
	}
}

