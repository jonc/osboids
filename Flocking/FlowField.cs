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
			
			m_startX = Math.Max( BUFFER, centre.X - width/2f);
			m_startY = Math.Max( BUFFER, centre.Y - depth/2f);
			m_startZ = Math.Max( BUFFER, centre.Z - height/2f);
			m_endX = Math.Min( Util.SCENE_SIZE - BUFFER, centre.X + width/2f);
			m_endY = Math.Min( Util.SCENE_SIZE - BUFFER, centre.Y + depth/2f);
			m_endZ = Math.Min( Util.SCENE_SIZE - BUFFER, centre.Z + height/2f);
			
			// build the flow field over the given bounds
			Initialize();			
		}
		
		/// <summary>
		/// build a flow field on the scene at the specified centre
		/// position in the scene and of extent given by width, depth and height.
		/// </summary>
		public void Initialize() {
			foreach( SceneObjectGroup sog in m_scene.Entities.GetAllByType<SceneObjectGroup>() ) {
				float offsetHeight;
				Vector3 size = sog.GetAxisAlignedBoundingBox( out offsetHeight );
				Vector3 pos = sog.AbsolutePosition;
				
				// color in the flow field with the strength at this pos due to 
				// this sog
				for( int x = 0; x < size.X; x++ ) {
					for( int y = 0; y < size.Y; y++ ) {
						for( int z = 0; z < size.Z; z++ ) {
						}
					}
				}
			}
		}

		public Vector3 AdjustVelocity (Vector3 loc, Vector3 vel, float lookAheadDist)
		{
			Vector3 normVel = Vector3.Normalize(vel);
			Vector3 inFront = loc + normVel * lookAheadDist;
			Vector3 adjustedDestintation = inFront + FieldStrength(inFront);
			Vector3 newVel = Vector3.Normalize(adjustedDestintation - loc) * Vector3.Mag(vel);
			return newVel;
		}

		public Vector3 FieldStrength (Vector3 inFront)
		{
			Vector3 retVal = Vector3.Zero;
			
			//keep us in bounds
			if( inFront.X > m_endX ) retVal.X -= inFront.X - m_endX;
			if( inFront.Y > m_endY ) retVal.Y -= inFront.Y - m_endY;
			if( inFront.Z > m_endZ ) retVal.Z -= inFront.Z - m_endZ;
			if( inFront.X < m_startX ) retVal.X += m_startX - inFront.X;
			if( inFront.Y < m_startY ) retVal.Y += m_startY - inFront.Y;
			if( inFront.Z < m_startZ ) retVal.Z += m_startZ - inFront.Z;
			
			//now get the field strength at the inbounds position
			Vector3 strength = LookUp( inFront + retVal);
			
			return retVal + strength;
		}

		public Vector3 LookUp (Vector3 par1)
		{
			return Vector3.Zero;
		}
	}
}

