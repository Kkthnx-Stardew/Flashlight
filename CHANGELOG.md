# Changelog

Versions follow [Semantic Versioning](https://semver.org/). Newest first.

## 0.1.0 (not released)

First build. It compiles against Stardew Valley 1.6.15 and SMAPI 4.5.2 with no warnings and passes its tests. It was run in the game on a throwaway farm with only this mod loaded: the items and shop entries load, the beam and the soft light around the farmer show at night in all four directions, loaded cells drain and are swapped in turn (Dry Cells and Battery Packs, in slot order), the charge survives saving, the slots take only one cell each, the flicker and the lit toolbar picture work, and the farmer is drawn holding the flashlight while it is selected, standing and walking. Multiplayer, Linux and long play are untested.

- Flashlight tool and Dry Cell battery, sold at Robin's Carpenter's Shop, with a Dry Cell recipe.
- Two battery slots on the flashlight that work like the bait slot of a fishing rod: right-click to load one cell per slot or take one out, and hover to see them.
- The game's Battery Pack can be loaded too. It lasts 12 in-game hours (three Dry Cells) and while it is in use the beam is 30 percent longer and brighter. Settings: `UseBatteryPacks`, `BatteryPackHours`, `BatteryPackBeamPercent`.
- A forward beam in the four facing directions and a soft light around the farmer (`AmbientGlowTiles`, 0 turns it off), so the ground around them is lit and not only what is ahead. The toolbar picture shows when it is on.
- The farmer is drawn holding the flashlight while it is the selected tool. It follows the hand through the walking frames, the beam starts at its lens, and from the front and back it is held in one hand and not in the middle of the body. `ShowInHand` turns it off.
- Batteries drain while the flashlight is on and it is dark where the farmer is. The next loaded cell is used automatically.
- A random flicker now and then, more often when the last cell is nearly flat, and a message when it goes out. It switches off when the farmer goes to bed.
- A charge bar under the flashlight's picture like the water bar of the watering can (green, yellow, orange, red as it falls) and a charge line in the tooltip.
- Under a tenth of a full load the flicker gets steadily more frequent, about every 12 seconds at 5 percent and every 3 seconds when nearly empty.
- Settings in Generic Mod Config Menu, and a toggle key that can list several buttons, such as a controller button.
- No beam during cutscenes and festivals.
- The lit flashlight is drawn over the unlit one instead of being swapped for a second item, so there is one flashlight item and a lit flag that is cleared whenever a day starts.
- The shop's stock rolls are written with a full stop in every language (a comma broke them on some systems), keys typed in a chat or text box are ignored, and a flashlight left on in the rain drains like one left on at night.
- One flashlight to a player (the shop stops selling a second, a second one cannot be picked up or moved into the backpack, and a spare that gets in some other way is inert: it cannot be lit and takes no cells), a daily limit on batteries with a random chance of a thin or empty shelf, a carry limit for the shop, and a low sell price for batteries so they cannot be bought and sold for profit.
- The Dry Cell is left out of the shipping collection (so Perfection does not ask for it), the fishing collection and random sales (so no merchant or cart sells it past the shop's limits).
- A `flashlight_report` console command prints the version, the settings that were changed and a local tally of nights used, hours lit and cells used, for bug reports and balance feedback. The tally is kept in `stats.json` in the mod folder and is never sent anywhere.
