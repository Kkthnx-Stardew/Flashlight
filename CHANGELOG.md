# Changelog

Versions follow [Semantic Versioning](https://semver.org/). Newest first.

## 0.1.0 (not released)

First build. It compiles against Stardew Valley 1.6.15 and SMAPI 4.5.2 with no warnings and passes its tests. It was run in the game on a throwaway farm: the items and shop entries load, the beam shows in all four directions and follows the farmer through warps, loaded batteries drain and are swapped in turn, the charge survives saving, the slots take only one Dry Cell each, and the flicker and the lit toolbar picture work. Multiplayer, Linux and long play are untested.

- Flashlight tool and Dry Cell battery, sold at Robin's Carpenter's Shop, with a Dry Cell recipe.
- Two battery slots on the flashlight that work like the bait slot of a fishing rod: right-click to load one cell per slot or take one out, and hover to see them.
- A forward beam in the four facing directions, with a faint glow around the farmer. The toolbar picture shows when it is on.
- Batteries drain while the flashlight is on and it is dark where the farmer is; the next loaded cell is used automatically.
- A random flicker now and then, more often when the last cell is nearly flat, and a message when it goes out. It switches off when the farmer goes to bed.
- A charge bar under the flashlight's picture like the water bar of the watering can (green, yellow, orange, red as it falls) and a charge line in the tooltip.
- Under a tenth of a full load the flicker gets steadily more frequent, about every 12 seconds at 5 percent and every 3 seconds when nearly empty.
- Settings in Generic Mod Config Menu, and a toggle key that can list several buttons, such as a controller button.
- No beam during cutscenes and festivals.
- The lit flashlight is drawn over the unlit one instead of being swapped for a second item, so there is one flashlight item and a lit flag that is cleared whenever a day starts.
- The flashlight picture now sits in the rows above the charge bar, so it no longer touches the bar.
- The shop's stock rolls are written with a full stop in every language (a comma broke them on some systems), keys typed in a chat or text box are ignored, and a flashlight left on in the rain drains like one left on at night.
- One flashlight to a player (the shop stops selling a second, and a spare in the backpack is ignored), a daily limit on batteries with a random chance of a thin or empty shelf, a carry limit for the shop, and a low sell price for batteries so they cannot be bought and sold for profit.
