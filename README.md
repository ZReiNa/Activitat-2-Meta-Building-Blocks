# Midnight Routine


*Midnight Routine* is a mixed-reality defense game for Meta Quest, inspired by *Five Nights at Freddy’s*. Defeat animatronics appearing around your real space, manage your flashlight battery, and keep the stage monitor from reaching its final stage.

## Objective

Reach the target score by defeating animatronics with your flashlight. Manage your limited battery: it powers the flashlight and is also needed to reset the stage monitor. Energy orbs restore battery when grabbed.

You lose if a normal enemy’s timer runs out, you look at the rare purple enemy for too long, or the stage monitor completes its sequence.

## Controls

| Action | Control |
|---|---|
| Aim the flashlight | Point the right controller |
| Fire the flashlight | Hold the right controller’s trigger |
| Recharge | Grab an energy orb |
| Start or reset the stage monitor | Touch its button with your hand |
| Restart after a win or loss | Press **A** on the right controller or touch **Start** |

Raise your left wrist to check the battery and score.

## Gameplay

- **Animatronics** appear around the player. Their color changes as their individual timer runs down; defeat them before time expires.
- A **rare purple enemy** appears nearby. After a brief grace period, looking at it for too long causes a loss.
- The **stage monitor** cycles through three images. Reset it before the final stage ends by touching its button; resetting costs battery.
- **Energy orbs** can be grabbed to restore battery.
- Defeat the required number of enemies to win.

## Meta XR Building Blocks

- Camera Rig
- Passthrough
- Controller Tracking
- Interactions Rig
- Grab Interaction
- Ray Interaction
- Poke Interaction

## Mixed-Reality Features

The player sees their real surroundings through passthrough. Enemies appear around them, encouraging them to turn and look through the space. The rare enemy uses headset direction as an input: the player must look away to avoid losing.

The start menu and stage monitor are world-space panels that can be touched directly. Battery and score are displayed on the left controller’s wrist.

## Requirements

- Unity with Android Build Support
- Meta XR SDK packages compatible with the Unity project
- A Meta Quest headset with passthrough support
- Hand tracking enabled for touching the menu and stage-monitor buttons

## Known Issues

- Some spawned objects may be difficult to reach in a small play area. Spawning them closer to the player helps, but does not fully solve this.
- Enemy models may face the same direction rather than toward the player.
- The interface is still being refined; button placement can overlap text, and its visual design is basic.

## Project Member

**Saüc Pellejero Gálvez**
