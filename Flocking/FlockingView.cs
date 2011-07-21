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
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using Utils = OpenSim.Framework.Util;

namespace Flocking
{
	public class FlockingView
	{
		private static readonly ILog m_log = LogManager.GetLogger (System.Reflection.MethodBase.GetCurrentMethod ().DeclaringType);

		private Scene m_scene;
		private UUID m_owner;
		private String m_boidPrim;
		
		private Dictionary<string, SceneObjectGroup> m_sogMap = new Dictionary<string, SceneObjectGroup> ();
				
		public FlockingView (Scene scene)
		{
			m_scene = scene;	
		}
		
		public void PostInitialize (UUID owner)
		{
			m_owner = owner;
		}
		
		public String BoidPrim {
			set{ m_boidPrim = value;}
		}

		public Vector3 GetBoidSize ()
		{
		 	float offsetHeight;
			return findByName(m_boidPrim).GetAxisAlignedBoundingBox( out offsetHeight );
		}

		public void Clear ()
		{
            //trash everything we have
			List<string> current = new List<string> (m_sogMap.Keys);
            current.ForEach (delegate(string name) {
                 RemoveSOGFromScene(name);
            });
            m_sogMap.Clear();
 		}

		public void Render (List<Boid> boids)
		{
			boids.ForEach(delegate( Boid boid ) {
					DrawBoid (boid);
			});
		}
		
		private void DrawBoid (Boid boid)
		{
			SceneObjectPart existing = m_scene.GetSceneObjectPart (boid.Id);

			SceneObjectGroup sog;
			if (existing == null) {
				//m_log.Error( "didnt find " + boid.Id );
				SceneObjectGroup group = findByName (m_boidPrim);
				sog = CopyPrim (group, boid.Id);
				m_sogMap [boid.Id] = sog;
				m_scene.AddNewSceneObject (sog, false);
			} else {
				sog = existing.ParentGroup;
			}
			
			Quaternion rotation = CalcRotationToEndpoint (sog, boid.Location);
			//sog.UpdateGroupRotationPR( boid.Location, rotation);
			sog.UpdateGroupPosition( boid.Location );
			sog.UpdateGroupRotationR( rotation );
		}
		
		private static Quaternion CalcRotationToEndpoint (SceneObjectGroup sog, Vector3 ev)
		{
			//llSetRot(llRotBetween(<1,0,0>,llVecNorm(targetPosition - llGetPos())));
			// boid wil fly x forwards and Z up
			Vector3 sv = sog.AbsolutePosition;
			
			Vector3 currDirVec = Vector3.UnitX;
			Vector3 desiredDirVec = Vector3.Subtract (ev, sv);
			desiredDirVec.Normalize ();
			
			return Vector3.RotationBetween (currDirVec, desiredDirVec);
		}
		
		private SceneObjectGroup CopyPrim (SceneObjectGroup prim, string name)
		{
			SceneObjectGroup copy = prim.Copy (true);
			copy.Name = name;
			copy.DetachFromBackup ();
			return copy;
		}
		
		private SceneObjectGroup findByName (string name)
		{
//			SceneObjectGroup retVal = (SceneObjectGroup)m_scene.Entities.Find (delegate( EntityBase e ) {
			//	return e.Name == name;
			//});
			
			SceneObjectGroup retVal = null;
			
			SceneObjectPart sop = m_scene.GetSceneObjectPart(name);
			
			// can't find it so make a default one
			if (sop == null) {
				m_log.Error("no " + name);
				retVal = MakeDefaultPrim (name);
			} else {
				retVal = sop.ParentGroup;
			}
			

			return retVal;
		}

		private SceneObjectGroup MakeDefaultPrim (string name)
		{
			PrimitiveBaseShape shape = PrimitiveBaseShape.CreateSphere ();
			shape.Scale = new Vector3 (0.5f, 0.5f, 0.5f);

			SceneObjectGroup prim = new SceneObjectGroup (m_owner, new Vector3 (128f, 128f, 25f), shape);
			prim.Name = name;
			prim.DetachFromBackup ();
			m_scene.AddNewSceneObject (prim, false);

			return prim;
		}

        private void RemoveSOGFromScene(string sogName)
        {
            SceneObjectGroup sog = m_sogMap[sogName];
            m_scene.DeleteSceneObject(sog, false);
			//sog.SendGroupFullUpdate();

        }


	}
}

