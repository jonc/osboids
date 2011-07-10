using System;
using OpenMetaverse;
using OpenSim.Region.Framework.Scenes;

namespace Flocking
{
	public class FlowMap
	{
		private Scene m_scene;
		private float[,,] m_flowMap = new float[256,256,256];
		
		public FlowMap (Scene scene)
		{
			m_scene = scene;
		}
		
		public int LengthX {
			get {return 256;}
		}
		public int LengthY {
			get {return 256;}
		}
		public int LengthZ {
			get {return 256;}
		}
		
		public void Initialise() {
			//fill in the boundaries
			for( int x = 0; x < 256; x++ ) {
				for( int y = 0; y < 256; y++ ) {
					m_flowMap[x,y,0] = 100f;
					m_flowMap[x,y,255] = 100f;
				}
			}
			for( int x = 0; x < 256; x++ ) {
				for( int z = 0; z < 256; z++ ) {
					m_flowMap[x,0,z] = 100f;
					m_flowMap[x,255,z] = 100f;
				}
			}
			for( int y = 0; y < 256; y++ ) {
				for( int z = 0; z < 256; z++ ) {
					m_flowMap[0,y,z] = 100f;
					m_flowMap[255,y,z] = 100f;
				}
			}
			
			//fill in the terrain
			for( int x = 0; x < 256; x++ ) {
				for( int y = 0; y < 256; y++ ) {
					int zMax = Convert.ToInt32(m_scene.GetGroundHeight( x, y ));
					for( int z = 1; z < zMax; z++ ) {
						m_flowMap[x,y,z] = 100f;
					}
				}
			}
			
			// fill in the things
			foreach( EntityBase entity in m_scene.GetEntities() ) {
				if( entity is SceneObjectGroup ) {
					SceneObjectGroup sog = (SceneObjectGroup)entity;
					
					//todo: ignore phantom
					float fmaxX, fminX, fmaxY, fminY, fmaxZ, fminZ;
					int maxX, minX, maxY, minY, maxZ, minZ;
					sog.GetAxisAlignedBoundingBoxRaw( out fminX, out fmaxX, out fminY, out fmaxY, out fminZ, out fmaxZ );
					
					minX = Convert.ToInt32(fminX);
					maxX = Convert.ToInt32(fmaxX);
					minY = Convert.ToInt32(fminY);
					maxY = Convert.ToInt32(fmaxX);
					minZ = Convert.ToInt32(fminZ);
					maxZ = Convert.ToInt32(fmaxZ);
					
					for( int x = minX; x < maxX; x++ ) {
						for( int y = minY; y < maxY; y++ ) {
							for( int z = minZ; z < maxZ; z++ ) {
								m_flowMap[x,y,z] = 100f;
							}
						}
					}
				}
			}
		}

		public bool WouldHitObstacle (Vector3 currPos, Vector3 targetPos)
		{
			bool retVal = false;
			//fail fast
			if( IsOutOfBounds(targetPos) ) {
				retVal = true;
			} else if( IsWithinObstacle(targetPos) ) {
				retVal = true;
			} else if( IntersectsObstacle (currPos, targetPos) ) {
				retVal = true;
			}
			
			return retVal;
		}
		
		public bool IsOutOfBounds(Vector3 targetPos) {
			bool retVal = false;
			if( targetPos.X < 5f ||
				targetPos.X > 250f ||
				targetPos.Y < 5f ||
				targetPos.Y > 250f ||
				targetPos.Z < 5f ||
				targetPos.Z > 250f ) {
				
				retVal = true;
			}
			
			return retVal;
		}

		public bool IntersectsObstacle (Vector3 currPos, Vector3 targetPos)
		{
			bool retVal = false;
			// Ray trace the Vector and fail as soon as we hit something
			Vector3 direction = targetPos - currPos;
			float length = direction.Length();
			// check every metre
			for( float i = 1f; i < length; i += 1f ) {
				Vector3 rayPos = currPos + ( direction * i );
				//give up if we go OOB on this ray
				if( IsOutOfBounds( rayPos ) ){ 
					retVal = true;
					break;
				}
				else if( IsWithinObstacle( rayPos ) ) {
					retVal = true;
					break;
				}
			}
			
			return retVal;
		}
		
		public bool IsWithinObstacle( Vector3 targetPos ) {
			return IsWithinObstacle(Convert.ToInt32(targetPos.X), Convert.ToInt32(targetPos.Y),Convert.ToInt32(targetPos.Z));
		}
		
		public bool IsWithinObstacle( int x, int y, int z ) {
			bool retVal = false;
			if( x > LengthX || y > LengthY || z > LengthZ ) {
				retVal = true;
			} else if( x < 0 || y < 0 || z < 0 ) {
				retVal = true;
			} else if (m_flowMap[x,y,z] > 50f) {
				retVal = true;
			}
			return retVal;	
		}
	}
	
	
}

