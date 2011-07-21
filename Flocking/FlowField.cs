using System;
using OpenMetaverse;
using OpenSim.Region.Framework.Scenes;

namespace Flocking
{
	public class FlowField
	{
		private const int BUFFER = 5;
		private Scene m_scene;
		private float m_startX;
		private float m_startY;
		private float m_startZ;
		private float m_endX;
		private float m_endY;
		private float m_endZ;
		private UUID TERRAIN = UUID.Random ();
		private UUID EDGE = UUID.Random ();
		private UUID[,,] m_field = new UUID[256, 256, 256]; // field of the object at this position
		
		/// <summary>
		/// Initializes a new instance of the <see cref="Flocking.FlowField"/> class.
		/// </summary>
		/// <param name='scene'>
		/// Scene.
		/// </param>
		/// <param name='centre'>
		/// Centre.
		/// </param>
		/// <param name='width'>
		/// Width.
		/// </param>
		/// <param name='depth'>
		/// Depth.
		/// </param>
		/// <param name='height'>
		/// Height.
		/// </param>
		///
		public FlowField (Scene scene, Vector3 centre, int width, int depth, int height)
		{
			m_scene = scene;
			
			m_startX = Math.Max (BUFFER, centre.X - width / 2f);
			m_startY = Math.Max (BUFFER, centre.Y - depth / 2f);
			m_startZ = Math.Max (BUFFER, centre.Z - height / 2f);
			m_endX = Math.Min (Util.SCENE_SIZE - BUFFER, centre.X + width / 2f);
			m_endY = Math.Min (Util.SCENE_SIZE - BUFFER, centre.Y + depth / 2f);
			m_endZ = Math.Min (Util.SCENE_SIZE - BUFFER, centre.Z + height / 2f);
			
			// build the flow field over the given bounds
			Initialize ();			
		}
		
		/// <summary>
		/// build a flow field on the scene at the specified centre
		/// position in the scene and of extent given by width, depth and height.
		/// </summary>
		public void Initialize ()
		{
			
			//fill in the boundaries
			for (int x = 0; x < 256; x++) {
				for (int y = 0; y < 256; y++) {
					m_field [x, y, 0] = EDGE;
					m_field [x, y, 255] = EDGE;
				}
			}
			for (int x = 0; x < 256; x++) {
				for (int z = 0; z < 256; z++) {
					m_field [x, 0, z] = EDGE;
					m_field [x, 255, z] = EDGE;
				}
			}
			for (int y = 0; y < 256; y++) {
				for (int z = 0; z < 256; z++) {
					m_field [0, y, z] = EDGE;
					m_field [255, y, z] = EDGE;
				}
			}

			//fill in the terrain
			for (int x = 0; x < 256; x++) {
				for (int y = 0; y < 256; y++) {
					int zMax = Convert.ToInt32 (m_scene.GetGroundHeight (x, y));
					for (int z = 1; z < zMax; z++) {
						m_field [x, y, z] = TERRAIN;
					}
				}
			}
			foreach (SceneObjectGroup sog in m_scene.Entities.GetAllByType<SceneObjectGroup>()) {
				//todo: ignore phantom
				float fmaxX, fminX, fmaxY, fminY, fmaxZ, fminZ;
				int maxX, minX, maxY, minY, maxZ, minZ;
				sog.GetAxisAlignedBoundingBoxRaw (out fminX, out fmaxX, out fminY, out fmaxY, out fminZ, out fmaxZ);
					
				minX = Convert.ToInt32 (fminX);
				maxX = Convert.ToInt32 (fmaxX);
				minY = Convert.ToInt32 (fminY);
				maxY = Convert.ToInt32 (fmaxX);
				minZ = Convert.ToInt32 (fminZ);
				maxZ = Convert.ToInt32 (fmaxZ);
					
				for (int x = minX; x < maxX; x++) {
					for (int y = minY; y < maxY; y++) {
						for (int z = minZ; z < maxZ; z++) {
							m_field [x, y, z] = sog.UUID;
						}
					}
				}
			}
		}
		
		public Vector3 AdjustVelocity (Boid boid, float lookAheadDist)
		{
			Vector3 normVel = Vector3.Normalize (boid.Velocity);
			Vector3 loc = boid.Location;			
			Vector3 inFront = loc + normVel * lookAheadDist;
			
			Vector3 adjustedDestintation = inFront + FieldStrength (loc, boid.Size, inFront);
			Vector3 newVel = Vector3.Normalize (adjustedDestintation - loc) * Vector3.Mag (boid.Velocity);
			return newVel;
		}

		public Vector3 FieldStrength (Vector3 current, Vector3 size, Vector3 inFront)
		{
			Vector3 retVal = Vector3.Zero;
			float length = size.X/2;
			float width = size.Y/2;
			float height = size.Z/2;
			
			//keep us in bounds
			if (inFront.X > m_endX)
				retVal.X -= inFront.X - m_endX - length;
			if (inFront.Y > m_endY)
				retVal.Y -= inFront.Y - m_endY - width;
			if (inFront.Z > m_endZ)
				retVal.Z -= inFront.Z - m_endZ - height;
			if (inFront.X < m_startX)
				retVal.X += m_startX - inFront.X + length;
			if (inFront.Y < m_startY)
				retVal.Y += m_startY - inFront.Y + width;
			if (inFront.Z < m_startZ)
				retVal.Z += m_startZ - inFront.Z + height;
			
			//now get the field strength at the inbounds position
			UUID collider = LookUp (inFront + retVal);
			while (collider != UUID.Zero) {
				if (collider == TERRAIN) {
					// ground height at current and dest averaged
					float h1 = m_scene.GetGroundHeight (current.X, current.Y);
					float h2 = m_scene.GetGroundHeight (inFront.X, inFront.Y);
					float h = (h1 + h2) / 2;
					retVal.Z += h;
				} else if (collider == EDGE) {
					// we ain't ever going to hit these
				} else {
					//we have hit a SOG
					SceneObjectGroup sog = m_scene.GetSceneObjectPart (collider).ParentGroup;
					if (sog == null) {
						Console.WriteLine (collider);
					} else {
						float sogMinX, sogMinY, sogMinZ, sogMaxX, sogMaxY, sogMaxZ;
						sog.GetAxisAlignedBoundingBoxRaw (out sogMinX, out sogMaxX, out sogMinY, out sogMaxY, out sogMinZ, out sogMaxZ);
						//keep us out of the sog
						if (inFront.X > sogMinX)
							retVal.X -= inFront.X - sogMinX - length;
						if (inFront.Y > sogMinY)
							retVal.Y -= inFront.Y - sogMinY - width;
						if (inFront.Z > sogMinZ)
							retVal.Z -= inFront.Z - sogMinZ - height;
						if (inFront.X < sogMaxX)
							retVal.X += sogMaxX - inFront.X + length;
						if (inFront.Y < sogMaxY)
							retVal.Y += sogMaxY - inFront.Y + width;
						if (inFront.Z < sogMaxZ)
							retVal.Z += sogMaxZ - inFront.Z + height;
					}
				} 
				collider = LookUp (inFront + retVal);
				//inFront += retVal;
			} 
			
			return retVal;
		}

		public UUID LookUp (Vector3 loc)
		{
			return m_field [(int)loc.X, (int)loc.Y, (int)loc.Z];
		}
	}
}

