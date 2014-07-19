Region Module -  ability to control flocks of prims within an OpenSim scene


To build from source

Add OpenSimBoids source tree under opensim/addon-modules

./runprebuild.sh against opensim root to build this module into the solution
then xbuild, or build within Visual Studio / Monodevelop to produce the binaries

OpenSimBoids has no external dependencies other than the dlls currently included in opensim.
The project generates a single dll - OpenSimBoids.Modules.dll which is copied into opensim/bin as part of the build step


Configuration

To become active, the module needs to be both referenced and enabled in the ini files. Otherwise it does nothing on startup

Entry is as follows and in addition various config parameters are available to control the flock dynamics.


	[Boids]
	
	enabled = true          ;removing the Boids group or setting enabled=false will switch off the module
	flock-size = 100        ;the number of Boids to flock
	max-speed = 3           ;how far each boid can travel per update
	max-force = 0.25        ;the maximum acceleration allowed to the current velocity of the boid
	neighbour-dist = 25	    ;max distance for other boids to be considered in the same flock as us
	desired-separation = 20 ;how far away from other boids we would like to stay
	tolerance = 5           ;how close to the edges of things can we get without being worried
	border-size = 5         ;how close to the edge of a region can we get?
	max-height = 256        ;how high are we allowed to flock
	boid-prim = fish01      ;By default the module will create a flock of plain wooden spheres, 
	                        ;however this can be overridden to the name of an existing prim that
	                        ;needs to already exist in the scene - i.e. be rezzed in the region.



Various runtime commands control the flocking module behaviour - described below. These can either be invoked
from the Console or in world by directing them to a chat channel. To specify which channel to use:

	chat-channel = 118 	the chat channel to listen for boid commands on



Runtime Commands

The following commands, which can either be issued on the Console, or via a chat channel in-world, control the behaviour
of the flock at runtime

	flock-stop or /118 stop in chat 	- stop all flocking and remove boids from the region
	flock-start or /118 start 		- start the flocking simulation
	flock-size <num> or /118 size <num>	- change the size of the flock
	flock-prim <name> or /118 prim <name>	- change the boid prim to that passed in - must be rezzed in the scene
	flock-framerate <num> or /118 framerate - only update the flock positions every <num> frames - only really useful
						- for photography and debugging boid behaviour


Boid prims

Any, currently rezzed in scene, object can be used as the boid prim. However fps is very much affected by the
complexity of the entity to use. It is easier to throw a single prim (or sculpty) around the scene than it is to
throw the constituent parts of a 200 linked prim dragon.

Tests show that <= 500 single prims can be flocked effectively - depending on system and network	
However maybe <= 300 simple linksets can perform as well.

I intend to allow inventory items and UUIDs to represent the boids - this is not written yet however.




Prebuilt binaries etc.. to follow 


Please Note 

This module is currently only tested against opensim master. If it is found to work against a stable release, 
then that behaviour ought to be considered as a bug - which I will attempt to fix in the next git push.


Status

probably made it to alpha by now ...


Next Steps 

I want to improve the ability of the boids to avoid obstacles within the scene. Current avoidance is pretty basic, and
only functions correctly about fifty percent of the time. Need to improve this without increasing computational cost.



Licence: all files released under a BSD licence
