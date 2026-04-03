# CS 426 - Assignment 6 - Tower of Sin

Group members: Evelyn Johnson, Mario Tinoco, Soham Hisabia

## Name: Mario Tinoco

For Assignment 6 I created a basic boss enemy ai that uses a finite state machine to determine the boss movements.

The boss AI has 5 states; Idle, walk, jump, death, and attack. At the start of the boss fight the boss idles atop a ledge and waits for the player to get close the distnace. Once the AI detects that the player is "close enough" (specific distance can be tweaked in Unity) it triggers the jump state where the boss jumps from the ledge towards the player. When the boss is level with the player it transitions into a walking state where the boss starts walking towards the player. If the player is ever within the boss' attack radius the boss transitions into an attack state. In the futur I plan to make it so that if the player is ever "too far" from the player it once again transitions into the jump state to close the distance. The boss continues to cycle between walk and attack until either the player dies or the boss' health reaches 0. At this point the boss enters it's death state.

I animated the boss when it enters its 5 states using the in class turotial as a base line. The boss AI triggers the animator to perform the needed animations. Boss animations include idle, walking, jump, and attack.

In the future I plan to implement multiple different types of attacks to create variety in the game. Ideally there are 2-3 different attack types/animations per boss (ideally 7, one for each sin). The AI is also a bit wonky at times (sometimes wonders off) so I plan to find the cause of these bugs and find fixes for them.

## Name: Soham Hisabia

## Name: Evelyn Johnson

For Assignment 6, I created a zombie enemy AI that combines a finite state machine with probabilistic decision-making, and I later extended its movement with a grid-based A\* pathfinding system. I also created an Eyebat trap enemy that uses waypoint-based graph navigation and shortest-path search to follow the player indirectly through the level.

The FSM controls things like if the player is detected and within aggro range, the zombie begins walking towards them (pursuing), if the player is close enough, the zombie attacks, if the zombie gets too far from its spawnpoint, it walks back, or if its health becomes low, it may flee, block, or become enraged. I also made a Bayesian-style probabilistic decision system called `BayesianBrain`. It computes probabilities for blocking, enraging, fleeing, or just not doing anything and continuing to attack like normal. These probabilities are based on the zombie’s current health, sort of inspired by "fight/flight" mechanics, where lower HP = less likely to block/run away, and more likely to "enrage".

I had originally used NavMesh, but switched off of it to try using steering-based movement (seeking), and then finally just did an A* system. The zombie builds a local node grid between itself and its target, marks blocked nodes using obstacle checks, computes a path with A*, simplifies the resulting waypoint list, and then follows that path during pursuit, fleeing, and returning-to-spawn behavior. The zombie also only attacks if it "sees" the player (FOV, view distance, raycast). This makes it so that (1), the zombie can't start walking/targeting you through walls, which was annoying and didn't make sense, and (2), that introduced the possibility of entering a room with zombies through a different door, putting you "behind" the zombie, gaining you a "free hit" when it wasn't "aggro-ed" onto the player. In addition to the zombie, I also implemented an Eyebat trap enemy. This trap uses a waypoint graph rather than free movement. It patrols along a predefined network of waypoints and, when the player is detected, it finds the player’s closest waypoint and computes the shortest path through the graph to that location.

I animated both the player movements/animations and the zombie animations. For the zombie, the Animator is used to trigger an attack, block, roar (enrage), death and walking animation. The player has 2 slash type animations (basic, heavy), walk forward, run forward, walk backward, run backward, strafe right, strafe left, jump, and crouch animations. In the future, I plan to add some type of death animation, that when the player dies, it will 3rd person zoom out, play the player's death animation, display stats on their run (floor #, potential achievements completed, xp earned, etc), and auto load to the death realm after a few seconds. Similarly, I've tweaked the combat, such as the player/zombie animation speeds, HP, ATK dmg, crit chance, health potion drop chance + healing amount, etc, to try to make the combat hard enough, but not too difficult. The original settings I had made it almost impossible for the player to beat even one floor. Further tweaking is needed - but it's decent right now. I have to change the player's gravity though-I believe you can potentially jump to an entirely different floor on accident.

For physics/lights/textures, I implemented this in the prior assignment. Majority of objects that you'd assume would be movable in real life (books, glasses, chairs, small barrels) are moveable/pushable by the player. Larger objects like tables are not. There are many lights in the form of torches + the portals are lit up (particle system + lighting). And there are a ton of textures. I found pretty much all the models/animations used (except for the bosses), created the UI in Figma, and most of the current combat.

I pretty much just watched a ton of youtube videos. One on NavMesh, one or two on waypoints, and most on A\*. I didn't save most of them, but here are some:

- https://www.youtube.com/watch?v=ji-f-74zfIQ
- https://www.youtube.com/watch?v=UHnOW-OimLQ
- https://www.youtube.com/watch?v=alU04hvz6L4
- https://www.youtube.com/watch?v=tFpv4xFZrq8
