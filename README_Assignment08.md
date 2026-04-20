# CS 426 - Assignment 8 - Tower of Sin

Group members: Evelyn Johnson, Mario Tinoco, Soham Hisabia

## Name: Mario Tinoco


## Name: Soham Hisabia
My main contribution to the beta release was on the boss side of the project, especially Lust, Pride, Sloth, and Mutant. In the alpha stage, these bosses did not work properly, so a big part of my work was fixing earlier issues and making them function more reliably in gameplay. I worked on correcting the boss animation controllers and improving the animation flow so idle, walking or chase, attack, special state, and death animations were connected more clearly and triggered at the right times. I also improved the encounter logic by tuning movement, aggro range, attack range, cooldowns, damage timing, and overall responsiveness so the bosses felt more polished and readable instead of inconsistent or unfinished.

On the visual side, I also worked on boss materials and shader related polish in Unity. This included applying and correcting textures, checking material assignments, and improving the overall presentation of the boss models so they looked more complete for the beta release. I worked with boss specific visual polish in URP, including texture setup and material adjustments, and focused on making the bosses easier to read during walking, attacking, phase changes, and death. Overall, my work helped move the boss encounters from a rough alpha state, where they were not functioning properly, into a more stable, playable, and polished beta state by improving both their gameplay behavior and their visual presentation.


## Name: Evelyn Johnson

For assignment 8, I followed each playtester through our game, noting down things I had noticed as well as the direct feedback they gave. I added notes on things that I believe should be fixed next, as well as relayed this information to my groupmates, assigning them specific deliverables that they should have done on the things they've worked on before the next playtesting. I completed the "Feedback from Alpha Release and Response" and "Beta Release" sections in the Design Doc.

I added all forms of writing to the game. I added a simple black screen + text animation that explained the basic premise and inspiration of the game, as well as set up narrator speech and on-screen text that acts as a sort of "tutorial", explaining more in depth who "he" was (the narrator), who the player is (a lost soul), and what the player's goal is, - which is to escape the tower and climb to the top. Some sample quotes from this are included below. I've also started contacting some people about Voice Acting for my bosses. The Envy boss sample quotes are below that I hope to add.

I also added a "Credits" menu that introduces our team, the idea and the origin a bit more, and crediting as needed. I also added a controls screen, but it is largely not functional right now. Just UI. And I implemented the "tutorial", which is essentially text appearing on the first entry of the Prison Scene, and the narrator's audio.

I've also made some more changes based on tester feedback:
- Reduced the amount of text in the tutorial scene
- Removed most of the lore-heavy introduction text so players can get into gameplay faster
- Added more visible “Press E” interaction prompts to doors and other interactable objects
- Fixed the issue where players could enter inactive/unlit portals by disabling the hidden teleporter collider on unused portals
- Increased the overall dungeon scale to 1.25x size, which made staircases easier to navigate
- Adjusted portal placements, loot spawn points, and zombie spawn locations after resizing the map
- Removed several zombie spawn points in the main hall and redistributed others to other locations to reduce overwhelming enemy clustering (I heard an "Oh god" when someone opened the door)
- Lowered the first boss difficulty by significantly reducing its health
- Reduced the first boss model size
- Reworked the boss health bar from a world-space object attached to the enemy into a static UI bar at the top of the screen
- Continued debugging death-state issues such as HP reset problems and scene recovery after dying
- Continued investigating camera clipping problems
- Hid the eyebat enemy, considering removing it all together

For the shaders, I have found almost all of the assets we are using for the game, and quite a few shaders exist. I've had to convert many of them via the Rendering Pipeline Converter, and edited them when necessary. I have changed the portal shaders a bit, changing the colors depending on the type of portal (prison portal, dungeon portal, boss portal), and edited the Shader Graphs/ShG_Portal_URP, adjusting the layered noise textures to fix the swirling portal effects, edge masked to make the sides/borders of the portals more crisp, and deactivated/activated portals depending on if they were chosen.

I also fixed quite a few errors and implemeneted an entirely unique boss, the only one that currently works without bugs or model issues on 4/17, the Envy Boss. Here's how the boss works:

  - In Phase 1, the boss teleports to a random valid location on the NavMesh every 10–15 seconds.
  - After teleporting, it waits 3 seconds, then plays its attack animation and attacks.
    - Phase 1 attack:
      - Finds the player once before attacking
      - Spawns the attack circle and corresponding audio
      - Deals damage if the player is inside the circle when the particle effect explodes
      - Waits a few seconds before the next single-circle attack
  - At 50% HP, the boss transitions to Phase 2.
    - Phase 2 attack:
      - Finds the player once before attacking
      - Spawns the attack circle and corresponding audio
      - Deals damage if the player is inside the circle when the particle effect explodes
      - Repeats this three times, with each new circle spawning within 1 second of the previous one
      - Waits a few seconds before the next round of attacks

I have also begun creating my second boss, Greed Boss. It's quite complicated, and I hope to have it completed by Assignment 9 to write about it!

Sample Narrator Quotes:
- "Ah, another sinful soul. Welcome. I am the one who gathers those who fall, to piece them back together. I am the one who guides lost sinners to their second chance. I am the one who watches, but never interferes. I am known as simply, the Keeper. If you truly wish it, you may yet escape your eternal torment by proving your redemption: climb the tower, overcome the weight of humanity’s greatest flaws, and cleanse your sins in the blood of the climb. Only then shall you leave this place."
- Cast your gaze to the right. Upon that desk lies a weathered tome—open its pages to bind your gathered relics and access your inventory. Beyond that door, the portal hungers for you—step through to face the first floor of the Tower. But heed this warning: should your soul shatter during the climb, your run is forfeit. You will be stripped of your triumphs and cast back to the bottom with nothing."
  
Sample Envy Boss Quotes:
- [On player entry] You dare challenge me? You? What do you have that I do not? You stumble, you fail, and you're stuck in this eternal tower, to be tormented all your life. I have never failed. Come forth. Let me show you what true perfection looks like.
- [Attacking] You think your suffering makes you interesting? Makes you likable? Makes you worthy? Please. Suffering is just incompetence with an audience. I know not suffering.
- [Attacking[ You beg for mercy? How pathetic. I simply take what I deserve. Starting with your last breath.
- [On entering phase 2] Why?! Why does it hurt...I-I am perfect. I do not hurt, I do not feel pain, I do not suffer. I will not let you win. I CANNOT LET YOU WIN.
