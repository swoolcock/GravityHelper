# GravityHelper
A Celeste mod for controlling gravity in realtime (yes, actually real GravityHelper).

### How does it work?
Madeline's `Speed` vector always represents the direction to the floor, regardless of gravity.
This means that while inverted, her `Speed.Y` will be the opposite sign of her actual velocity as displayed in CelesteTAS.
It was done this way to ensure that any tech will work on the ceiling since the game thinks it's the floor, and most third-party mods "just work".

The majority of hooks in GravityHelper (over 100!) are for entity interaction compatibility, or to ensure that collisions are done in
the correct direction and from the correct corner of the hitbox.

Implementing floor and corner correction were tricky, since it needs to check collisions in the opposite direction to movement
and nudge Madeline in the opposite direction she would normally go.

The biggest hurdle by far was implementing dash correction on upside-down jumpthrus
(both MaxHelpingHand's and GravityHelper's own).  It's possible that there are still slight inconsistencies with it,
and I welcome any TASers to submit issues to GitHub.

### What happens when gravity flips?
Any `Entity` that supports inverted gravity will have a tracked `GravityComponent` that indicates how GravityHelper should
perform the flip.  The default procedure is:

1) The `Entity`'s position is moved to the opposite side of the current hitbox (this means it could be stuck in the ceiling or floor).
2) The hitbox is moved such that it has the same absolute coordinates as it did prior to the flip (hopefully unsticking it).
3) `Sprite`s are moved and flipped to correspond to the new position and direction.
4) If the entity is an `Actor`, the Y component of the `Speed` property (if it exists) is inverted so that momentum is preserved.

A `GravityComponent` can be added to any entity to allow it to flip visually on demand, but movement will only be affected
for `Actor`s that provide an `UpdateSpeed` action.

Grabbing a `Holdable` while inverted will also invert that entity, even if you then release it.
Gravity for that entity will reset after a short period of time (default is 2 seconds), but is configurable with a
`BehaviorGravityController`.

Adding a `GravityListener` component to an entity will trigger an action when gravity changes for Madeline or another entity.
This can be filtered by type using `GravityListener(typeof(SomeEntity))` or for a specific entity with `GravityListener(someEntity)`.
There is a convenience `PlayerGravityListener` class that will specifically listen for changes to Madeline's gravity.

There is also a `TriggerComponent` component that acts in a similar way to `Trigger` entities but adds support for collisions
with any entity.  A `GravityTriggerComponent` is provided that will set the gravity on a supported entity.
This is currently only used by `GravityTrigger` and `GravityField`.

### How do I make my entities support GravityHelper?
To add support for GravityHelper, add a `GravityComponent` to your entity that implements the following actions.
Note that these will be executed in the order listed.

* `UpdatePosition`: Update the position of the entity.
  By default this will move the entity to the opposite side of the current `Collider`.
* `UpdateColliders`: Update the position and size of any colliders in your entity.
  By default this will move the current `Collider` such that `Top = -Bottom`, restoring the absolute bounds that were
  affected by `UpdatePosition`.  This also applies to `Holdable.PickupCollider`.
* `UpdateSpeed`: Required for `Actor`s that have a speed affected by gravity.
  Update the `Speed` property (or similar) to invert the `Y` component and multiply by `MomentumMultiplier`.
  Actors do not have a common interface for this property, so it needs to be done manually.
* `UpdateVisuals`: Update the position and scaling of any sprites etc. in your entity.  GravityHelper cannot safely guess this.

### Which Celeste actors are supported?
* `Player` (of course).
* `TheoCrystal`
* `Glider` (jellies)

### Which third party entities/mods are already supported?

#### Extended Variants
* "Dash trail all the time" is correctly supported while inverted.

#### FancyTileEntities
* `FancyFallingBlock`

#### FrostHelper
* `CustomSpring`

#### MaddyCrown
* Crowns are rendered in the correct place and upside-down.

#### MaxHelpingHand
* `UpsideDownJumpThru`

#### OutbackHelper
* `Portal`

#### SpeedrunTool
* Gravity is correctly maintained between save states.

#### Crowneline
* It just works, no need for additional hooks.

#### Cateline
* Added hooks to correctly invert the tail.  Also fixes the missing tail trail.

### What entities/triggers are provided?

All entities provided follow a common theme of blue = normal, red = inverted, purple = toggle.

#### GravityTrigger
An invisible trigger that will change a supported `Actor`'s gravity when it enters.
Can be configured to support Madeline and/or any holdable/non-holdable `Actor`.

#### SpawnGravityTrigger
An invisible trigger that will set Madeline's initial gravity if placed over a player spawn entity.
Will optionally apply this gravity setting when returning from a berry/cassette bubble,
since return bubbles will always set regular gravity when triggered.
This was the safest solution given that return bubbles do not work well while inverted.
When placing a spawn on the ceiling, nudge the player spawn entity 5 pixels upward to ensure Madeline is standing and doesn't fall upward.

#### GravityBadelineBoost
A `BadelineBoost` (Badeline orb) that supports going downward instead of upward, and can optionally change
Madeline's gravity on touch.  Gravity types can be defined per node.

#### GravityBooster
A `Booster` (bubble) that will change Madeline's gravity when she is released.

#### GravityBumper
A `Bumper` that will change Madeline's gravity upon touch.

#### BehaviorGravityController
A controller entity that manages:
* How long `Holdable`s stay inverted before resetting
* The gravity change cooldown on `GravitySpring`s
* The gravity change cooldown on `GravitySwitch`es
* Whether `Holdable`s will trigger `GravitySwitch`es
 
#### SoundGravityController
A controller entity that manages:
* The default sounds for gravity changes, if an entity supports it
* Defining a music param to change upon gravity flip

#### VisualGravityController
A controller entity that manages:
* Opacity of the background, arrows, and particles of `GravityField`s
* Opacity of `GravityLine`s, including the flash time

#### VvvvvvGravityController
A controller entity that manages:
* Enabling VVVVVV-mode, which allows Madeline to flip gravity at will by pressing jump while on the ground
* The sound to play when flipping with jump
* Whether or not to disable dash while VVVVVV mode is active (defaults to true)
* Whether or not to disable grab while VVVVVV mode is active (defaults to true)

#### GravityDreamBlock
A `DreamBlock` that will change Madeline's gravity upon exit.

#### GravityField
A subclass of `GravityTrigger` that will display seeker barrier-style particles and overlaid arrow decals.
Can be configured to support Madeline and/or any holdable/non-holdable `Actor`.

#### GravityLine
A line entity (position + one node) that will trigger a gravity change when Madeline (or a supported Actor) crosses it.
Similar to VVVVVV inversion planes.  Can be any orientation, even oblique.  Traversal checks are done by checking which side of the line the entity is on this frame rather than using collisions.
This means that it's theoretically "uncheesable" since you can't warp through it without triggering it, regardless of your speed.

#### GravityRefill
A `Refill` that provides a single 'charge' that causes the next dash to toggle gravity.
Has a cool sprite and animated indicator above Madeline's head.  Can be configured to provide more than one charge, but the entity looks the same.

#### GravitySpring
A `Spring` that will change Madeline's gravity upon touch.  Can be attached to the ceiling.  Will change gravity of other Actors (Theo etc.) if supported.

#### GravitySwitch
A `CoreModeToggle`-style switch that will change gravity upon touch.  Holdables will also trigger the switch if thrown/dropped (can be disabled).

#### UpsideDownJumpThru
A platform similar to `JumpthruPlatform` that allows `Actor`s to fall through it but not move upward.
Similar to the entity of the same name in MaxHelpingHand, and both are supported.

#### UpsideDownWatchTower
A `Lookout` (binoculars/watchtower) that is rendered upside-down and should be attached to the ceiling.

#### VvvvvvTrigger
A trigger that will enable or disable VVVVVV-mode. Requires that a VVVVVV controller exist with mode set to "trigger-based".

### How do controllers work?
There are currently four different kinds of controller. Behavior, Visual, Sound, and VVVVVV.
Most of the time you will only ever need zero or one of each of these types.

Controllers flagged as "persistent" act as a global default setting, and will be loaded at all times,
regardless of which room the controller is added to. You can customise individual rooms by adding a non-persistent controller
of a given type, which will set the defaults for just that room.

There must always be exactly zero or one persistent controllers in a map of a given type if you wish to use it.
Having two or more persistent controllers of the same type will result in undefined behaviour.
Additionally, you cannot have multiple controllers of the same type in a single room.

### What works?
Most things.

### What doesn't work?
https://github.com/swoolcock/GravityHelper/issues

### How can I help?
Make maps and playtest.  If you find anything not in the issue list that doesn't work as expected, please let me know.
I always welcome decent pull requests or even the occasional "me too" reply as long as it has additional information.

### Thanks to...
* VampireFlower for all their testing and TASing.  Without you GravityHelper would probably not exist.
* coloursofnoise for the initial proof-of-concept hooks.
* JaThePlayer for the original inspiration.
* Viv for the Badeline chaser hooks.
* max480 for putting up with my upside-down jumpthru complaints for months.
* Cruor/Vexatos for Ahorn/Lönn.
* 0x0ade and the Everest team for Everest.
* The Strawberry Jam team.
* The Celeste modding community for waiting patiently for years.
* Maddy/Noel/Kevin/Lena and all the EXOK team for being amazing people and creating a work of art enjoyed by so many people around the world.
* Anyone else I've missed.

### So... what now?
Go forth and make cool stuff! <3
