# Flashlight: balance design

The numbers below are the starting point. Every one of them is a config option, so they can be tuned after play-testing.

## What the game already has

Checked on the Stardew Valley Wiki and in the game's own code (1.6.15):

| Light | Reach | Notes |
| --- | --- | --- |
| Torch | about 3 tiles | Starter recipe (1 Wood, 2 Sap). Only lights while it is the held item (`Object.actionWhenBeingHeld`). |
| Small Glow Ring | 5 tiles | Found in Mines floors 1 to 39. Not sold. |
| Glow Ring | 10 tiles | Found in Mines floor 40 and deeper and in the Skull Cavern. Not sold. |

Night starts at 8pm in spring and summer, 7pm in fall and 6pm in winter. The day ends at 2am. Ten in-game minutes pass in 7 real seconds, so a spring night (8pm to 2am) is about 4.2 real minutes.

The vanilla Battery Pack is rare and sells for 500g, so it is not used. This mod adds its own battery item.

## What the flashlight is for

- It keeps lighting while the player uses other tools. A torch has to be the selected item.
- It reaches further than a torch, but only forward.
- It costs upkeep, so it does not make the rings pointless.

## Numbers

| Setting | Default | Why |
| --- | --- | --- |
| Beam length | 7 tiles | More reach than a Small Glow Ring, far less coverage than a Glow Ring. |
| Beam spread | 30 degrees each side of the facing | Narrow on purpose. |
| Glow around the farmer | about 2 tiles, faint | Enough to see your own feet. |
| Where it is sold | Robin's Carpenter's Shop | She already sells the lamp-post recipes (500g and 1,000g). Shop hours are not verified. |
| Flashlight price | 600g | Out of reach on day one, reachable by mid spring. |
| Battery price | 60g | A full spring night costs about 90g with the default battery life. |
| Battery life | 4 in-game hours (24 ticks of 10 minutes) | About 170 real seconds per battery. |
| Battery handling | Automatic | When the charge runs out the flashlight takes the next battery from the backpack. No loading menu. |
| Battery recipe | 1 Copper Bar + 1 Coal makes 3 batteries | Learned at Mining level 2. The level is a placeholder and not verified against the game's other recipes. |
| Low battery | Beam flickers for the last 10 minutes and a message appears | At zero the flashlight clicks off. |

## Controls and rules

- A key toggles the flashlight. It works from anywhere in the backpack, and using the tool also toggles it.
- It drains only while on. It is switched off when the farmer goes to bed.
- No infinite-battery option.

## Multiplayer

The light is added as a shared light, so other players see a light at the right place. They will see the game's ordinary round light, because the custom cone picture is only drawn on the machine that owns the flashlight. Cone sharing is a possible later step.

## Open questions

- Shop days for Robin's Carpenter's Shop.
- Mining level for the battery recipe against vanilla recipe levels.
- Beam position and size need checking in the real game.
