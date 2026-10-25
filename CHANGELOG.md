# Changelog

Versions follow [Semantic Versioning](https://semver.org/). Newest first.

## Unreleased

- Fixed: with the flashlight selected, clicking another toolbar slot with the mouse switched the light on and off instead of selecting that slot. A click on the toolbar, the clock or any other on-screen menu now goes to the game and never toggles the light. Number keys already worked. Reported by mikezorz13 on Nexus.
- The tooltip now explains empty battery slots. A pair is used up as soon as the light is switched on, so a flashlight could show empty slots next to a full charge, which looked like a fault. With both slots empty it says either that a pair is in use or that there are no batteries. Raised by mikezorz13 on Nexus.
- The flashlight and Dry Cells can also be sold at Clint's Blacksmith. The new `SoldAtBlacksmith` setting (off by default) adds them there, alone or together with Robin's `SoldAtCarpenter`, so players with crowded shops can pick where it is sold. With both on, the daily Dry Cell stock is shared between the two shops (half each, rounded up), because the game counts a shop's daily stock for each shop on its own and two shops would otherwise sell twice the limit.
- The letter about the flashlight follows the shops. It is Robin's when she sells it, Robin's with a line about Clint when both do, and a short letter from Clint when only he does. It is still one letter for each player, and players who already had Robin's letter do not get another.

## 1.1.0 (2026-10-25)

Built against Stardew Valley 1.6.15 and SMAPI 4.5.2 with no warnings, and 140 tests pass. It was run in the game on a throwaway farm with only this mod loaded, and in a game with one host and one joining player on one computer. Real play and larger multiplayer games are untested.

- Other players now see your beam as a cone. Before, the game sent the light to them but not its picture, so they saw a plain round glow around you. Each machine now draws the cone for the other players' flashlights, facing the way they face, with the shadows worked out there. A player without the mod still sees the round glow.
- The flicker, including the steady flicker of a nearly flat battery, now shows to other players. The game holds back a quickly changing light size, so the owner also sends the true size in a way the game sends at once.
- A beam is removed when its player leaves the game, so a player who drops out with the flashlight on no longer leaves a beam behind that comes back when they rejoin.
- Multiplayer was tested with one host and one joining player on one computer, and the other cases are still untested, see docs/BALANCE.md.
- Batteries last two hours longer. A pair of Dry Cells lasts 4 in-game hours instead of 2, and a pair of Battery Packs 6 instead of 4, so a pair no longer runs out in the middle of an evening. This halves the upkeep (about 7.5g for an hour of light instead of 15g) and the shop's supply now covers about twice the hours, so it may make the flashlight too easy, which docs/BALANCE.md says how to pull back. The new numbers are the defaults. A `config.json` written by 1.0.0 keeps `BatteryHours` at 2 and `BatteryPackHours` at 4, so set them to 4 and 6 or delete the file to get them.
- Robin sends each player one letter about the flashlight, a few days into a save, to a player who has none. It names the prices, is sent once for each player in a save, and is only sent while Robin sells the flashlight. `SendMail` turns it off.
- The beam is stopped by tall things. Full-grown trees, boulders, buildings and walls throw a shadow behind them instead of being lit all the way through, and the side of a tree the beam cannot reach stays dark. Facing away from the screen the farmer no longer lights the back of their own head. `BeamShadows` turns it off. The shadows are cheap: about 0.2 ms a frame while walking and almost nothing standing still, with no garbage for the game to clear (see docs/PERFORMANCE.md).

## 1.0.0 (2026-10-18)

First release. The numbers are a first pass and real play will change them, see docs/BALANCE.md.

It compiles against Stardew Valley 1.6.15 and SMAPI 4.5.2 with no warnings and passes its tests. It was run in the game on a throwaway farm with only this mod loaded: the items and shop entries load, the cone shows at night in all four directions, loaded cells drain and are swapped in turn (Dry Cells and Battery Packs, in slot order), the charge survives saving, the slots take only one cell each, the flicker and the lit toolbar picture work, and the farmer is drawn holding the flashlight while it is selected, standing and walking. Multiplayer and long play are untested.

- Flashlight tool and Dry Cell battery, sold at Robin's Carpenter's Shop, with a Dry Cell recipe.
- Two battery slots on the flashlight, always two, that work like the bait slot of a fishing rod. It runs on a matching pair and needs both loaded: two Dry Cells or two Battery Packs, never mixed and never one alone. Right-click to load one cell to a slot or take one out, and hover to see them.
- The game's Battery Pack can be loaded as a pair. A pair lasts 4 in-game hours (a pair of Dry Cells lasts 2) and while it is in use the beam is 20 percent longer and a little brighter. Settings: `UseBatteryPacks`, `BatteryPackHours`, `BatteryPackBeamPercent`.
- A forward cone of light in the four facing directions, with nothing lit around the farmer, like a real flashlight. The toolbar picture shows when it is on.
- The farmer is drawn holding the flashlight while it is the selected tool. It follows the hand through the walking frames, the beam starts at its lens, and from the front and back it is held in one hand, off to the side, with the lens clear of the fingers. `ShowInHand` turns it off.
- Batteries drain while the flashlight is on and it is dark where the farmer is. A matched pair is used up together and the next matched pair is taken automatically. With no matching pair loaded it goes out.
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
