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
		private UUID[,,] m_field = new UUID[256, 256, 256]; // field of objects at this position
		
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
		public FlowField (Scene scene, int minX, int maxX, int minY, int maxY, int minZ, int maxZ)
		{
			m_scene = scene;
			
			m_startX = Math.Max (BUFFER, minX);
			m_startY = Math.Max (BUFFER, minY);
			m_startZ = Math.Max (BUFFER, minZ);
			m_endX = Math.Min (Util.SCENE_SIZE - BUFFER, maxX);
			m_endY = Math.Min (Util.SCENE_SIZE - BUFFER, maxY);
			m_endZ = Math.Min (Util.SCENE_SIZE - BUFFER, maxZ);
			
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
				Vector3 pos = sog.AbsolutePosition;
					
				minX = Convert.ToInt32 (fminX + pos.X);
				maxX = Convert.ToInt32 (fmaxX + pos.X);
				minY = Convert.ToInt32 (fminY + pos.Y);
				maxY = Convert.ToInt32 (fmaxX + pos.Y);
				minZ = Convert.ToInt32 (fminZ + pos.Z);
				maxZ = Convert.ToInt32 (fmaxZ + pos.Z);
					
				for (int x = minX; x < maxX; x++) {
					for (int y = minY; y < maxY; y++) {
						for (int z = minZ; z < maxZ; z++) {
							if( inBounds(x,y,z) ) {
								m_field [x, y, z] = sog.UUID;
							} else {
								Console.WriteLine(sog.Name + " OOB at " + sog.AbsolutePosition + " -> " + x + " " + y + " " + z);
							}
						}
					}
				}
			}
		}

		public bool ContainsPoint (Vector3 p)
		{
			return p.X > m_startX && 
				p.X < m_endX &&
				p.Y > m_startY &&
				p.Y < m_endY &&
				p.Z > m_startZ &&
				p.Z < m_endZ;
		}

		private bool inBounds (int x, int y, int z)
		{
			return x >= 0 && x < 256 && y >= 0 && y < 256 && z >= 0;
		}
		
#if false
		public Vector3 AdjustVelocity (Boid boid, float lookAheadDist)
		{
			Vector3 normVel = Vector3.Normalize (boid.Velocity);
			Vector3 loc = boid.Location;			
			Vector3 inFront = loc + normVel * lookAheadDist;
			
			Vector3 adjustedDestintation = FieldStrength (loc, boid.Size, inFront);
			Vector3 newVel = Vector3.Normalize (adjustedDestintation - loc) * Vector3.Mag (boid.Velocity);
			
			float mOrigVel = Vector3.Mag(boid.Velocity);
			float mNewVel = Vector3.Mag(newVel);
			if( mNewVel != 0f && mNewVel > mOrigVel ) {
				newVel *= mOrigVel / mNewVel;
			}
			return newVel;
		}
#endif

		public Vector3 FieldStrength (Vector3 currentPos, Vector3 size, Vector3 targetPos)
		{
			float length = size.X/2;
			float width = size.Y/2;
			float height = size.Z/2;
			
			//keep us in bounds
			targetPos.X = Math.Min( targetPos.X, m_endX - length );
			targetPos.X = Math.Max(targetPos.X, m_startX + length);
			targetPos.Y = Math.Min( targetPos.Y, m_endY - width );
			targetPos.Y = Math.Max(targetPos.Y, m_startY + width);
			targetPos.Z = Math.Min( targetPos.Z, m_endZ - height );
			targetPos.Z = Math.Max(targetPos.Z, m_startZ + height);
			
			int count = 0;
			
			//now get the field strength at the inbounds position
			UUID collider = LookUp (targetPos);
			while (collider != UUID.Zero && count < 100) {
				count++;
				if (collider == TERRAIN) {
					// ground height at currentPos and dest averaged
					float h1 = m_scene.GetGroundHeight (currentPos.X, currentPos.Y);
					float h2 = m_scene.GetGroundHeight (targetPos.X, targetPos.Y);
					float h = (h1 + h2) / 2;
					targetPos.Z = h + height;
				} else if (collider == EDGE) {
					//keep us in bounds
					targetPos.X = Math.Min( targetPos.X, m_endX - length );
					targetPos.X = Math.Max(targetPos.X, m_startX + length);
					targetPos.Y = Math.Min( targetPos.Y, m_endY - width );
					targetPos.Y = Math.Max(targetPos.Y, m_startY + width);
					targetPos.Z = Math.Min( targetPos.Z, m_endZ - height );
					targetPos.Z = Math.Max(targetPos.Z, m_startZ + height);
				} else {
					//we have hit a SOG
					SceneObjectGroup sog = m_scene.GetSceneObjectPart(collider).ParentGroup;
					if (sog == null) {
						Console.WriteLine (collider);
					} else {
						float sogMinX, sogMinY, sogMinZ, sogMaxX, sogMaxY, sogMaxZ;
						sog.GetAxisAlignedBoundingBoxRaw (out sogMinX, out sogMaxX, out sogMinY, out sogMaxY, out sogMinZ, out sogMaxZ);
						Vector3 pos = sog.AbsolutePosition;
						//keep us out of the sog
						// adjust up/down first if necessary
						// then turn left or right
						if (targetPos.Z > sogMinZ + pos.Z)
							targetPos.Z = (sogMinZ + pos.Z) - height;
						if (targetPos.Z < sogMaxZ + pos.Z)
							targetPos.Z = (sogMaxZ + pos.Z)  + height;
						if (targetPos.X > sogMinX + pos.X)
							targetPos.X = (sogMinX + pos.X) - length;
						if (targetPos.Y > sogMinY + pos.Y)
							targetPos.Y = (sogMinY + pos.Y) - width;
						if (targetPos.X < sogMaxX + pos.X)
							targetPos.X  = (sogMaxX + pos.X) + length;
						if (targetPos.Y < sogMaxY + pos.Y)
							targetPos.Y = (sogMaxY + pos.Y)  + width;
					}
				} 
				
				// we what is at the new target position
				collider = LookUp (targetPos);
			} 
			
			return targetPos;
		}

		public UUID LookUp (Vector3 loc)
		{
			return m_field [(int)loc.X, (int)loc.Y, (int)loc.Z];
		}
		
		public override string ToString ()
		{
			return string.Format ("[FlowField]" + Environment.NewLine +
				"startX = {0}" + Environment.NewLine +
				"endX   = {1}" + Environment.NewLine +
				"startY = {2}" + Environment.NewLine +
				"endY   = {3}" + Environment.NewLine +
				"startZ = {4}" + Environment.NewLine +
				"endZ   = {5}", m_startX, m_endX, m_startY, m_endY, m_startZ, m_endZ);
		}
	}
}

