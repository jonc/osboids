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
using log4net;
using OpenMetaverse;
using OpenSim.Region.Framework.Scenes;

namespace Flocking
{
	public abstract class FlockingCommand
	{
		protected static readonly ILog m_log = LogManager.GetLogger (System.Reflection.MethodBase.GetCurrentMethod ().DeclaringType);
		protected FlockingController m_controller;
		protected string m_name;
		protected string m_args;
		protected string m_description;
		
		public FlockingCommand( string name, string args, string description ) {
			m_name = name;
			m_args = args;
			m_description = description;
		}
				
		public void Handle (string module, string[] args) {
			if( ShouldHandleCmd() ) {
				Invoke( args );
			}
		}
		
		private bool ShouldHandleCmd ()
		{
			return View.Scene.ConsoleScene () == View.Scene;
		}
		
		public abstract void Invoke( string[] args );
		
		public string Name {
			get{ return m_name;}
		}
		
		public string Params {
			get{ return m_args; }
		}

		public string Description {
			get{ return m_description;}
		}
		
		public FlockingController Controller {
			get{ return m_controller; }
			set{ m_controller = value; }
		}
		
		public FlockingModel Model {
			get { return Controller.Model; }
		}
		
		public FlockingView View {
			get { return Controller.View; }
		}
	}
	
	public class RoostCommand : FlockingCommand {
		
		public RoostCommand() : base( "roost", "", "return all the boids to the start position and remove them from the scene") {}

		public override void Invoke (string[] args) {
			if( Model.Active ) {
				Model.Goal = FlockGoal.Roost;
			} else {
				Controller.ShowResponse ("Flock is not active, roost has no effect", args);					
			}
		}		
	}	
	
	public class StopCommand : FlockingCommand {
		
		public StopCommand() : base("stop", "", "stop all Flocking"){}
		
		public override void Invoke (string[] args) {
			m_log.Info ("stopping the flocking activity");
			Model.Active = false;
			View.Clear ();
		}
	}
	
	public class SetPositionCommand : FlockingCommand {
		public SetPositionCommand() : base("position", "x,y,z", "location that the boids will start flocking from") {}
		
		public override void Invoke (string[] args)
		{
			float x = Convert.ToSingle (args [1]);
			float y = Convert.ToSingle (args [2]);
			float z = Convert.ToSingle (args [3]);
			Vector3 startPos = new Vector3 (x, y, z);
			if (Model.ContainsPoint (startPos)) {
				Model.StartPosition = startPos;
			} else {
				Controller.ShowResponse (startPos + "is not within the flow field", args);					
			}
		}
	}
	
	public class SetParameterCommand : FlockingCommand {
		public SetParameterCommand() : base("set", "name, value", "change the flock behaviour properties"){}
		
		public override void Invoke (string[] args)
		{
			string name = args [1];
			string newVal = args [2];
			
			BoidBehaviour behaviour = Model.Behaviour;
			
			if (behaviour.IsValidParameter (name)) {
				behaviour.SetParameter (name, newVal);
			} else {
				Controller.ShowResponse (name + "is not a valid flock parameter", args);
				Controller.ShowResponse ("valid parameters are: " + behaviour.GetList (), args);
			}
		}
	}
	
	public class SetFrameRateCommand : FlockingCommand {
		public SetFrameRateCommand() : base("framerate", "num", "[debugging] only update boids every <num> frames") {}
		
		public override void Invoke (string[] args)
		{
			int frameRate = Convert.ToInt32 (args [1]);
			Controller.FrameUpdateRate = frameRate;
		}
	}
	
	public class SetBoundsCommand : FlockingCommand {
		public SetBoundsCommand() : base("bounds", "xMin,xMax,yMin,yMax,zMin,zMax", "Bounds of the 3D space that the flock will be confined to") {}
		
		public override void Invoke (string[] args)
		{
			//TODO: 
		}
	}
	
	public class SetSizeCommand : FlockingCommand {
		public SetSizeCommand() : base("size", "num", "Adjust the size of the flock ") {}
		
		public override void Invoke (string [] args)
		{
			lock (Controller.UI_SYNC) {
				int newSize = Convert.ToInt32 (args [1]);
				Model.Size = newSize;
				View.Clear ();
			}
		}
	}
	
	public class SetPrimCommand : FlockingCommand {
		public SetPrimCommand() : base("prim", "name", "set the prim used for each boid to that passed in") {}
		
		public override void Invoke (string[] args)
		{
			string primName = args [1];
			lock (Controller.UI_SYNC) {
				View.BoidPrim = primName;
				View.Clear ();
			}
		}
	}
	
	public class ShowStatsCommand : FlockingCommand {
		public ShowStatsCommand() : base("stats", "", "show flocking stats") {}
		
		public override void Invoke (string[] args)
		{
			string str = Model.ToString ();
			Controller.ShowResponse (str, args);
		}
	}
	
	public class StartCommand : FlockingCommand {
		public StartCommand() : base("start", "", "Start Flocking") {}
		
		public override void Invoke(string[] args) {
			if( Model.Active ) { 
				Controller.ShowResponse("Already active, restarting", args);
				Model.Active = false;
				View.Clear();
			}
			
			m_log.Info ("start the flocking capability");
			Model.Goal = FlockGoal.Flock;
			Controller.Start ();
			Model.Active = true;
			//m_module.FlockUpdate ();
		}		
	}
}

