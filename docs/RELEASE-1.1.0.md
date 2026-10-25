# Flashlight 1.1.0 release text

Published on 2026-10-25. The tag `v1.1.0` and the GitHub release are up, and on Nexus the zip went in as an update to the 1.0.0 file (so 1.0.0 moved to old files) with the page text from `NEXUS.bbcode`.

The text below is what was used. Anyone who played 1.0.0 keeps `BatteryHours` 2 and `BatteryPackHours` 4 in their saved `config.json`, which the notes say.

## GitHub release notes

Flashlight 1.1.0

- Other players now see your beam as a cone in multiplayer, flicker included. Before, they saw a plain round glow. Everyone in the game needs the mod for it.
- The beam is stopped by trees, boulders, buildings and walls and throws a shadow. `BeamShadows` turns it off.
- Batteries last two hours longer. A pair of Dry Cells lasts 4 in-game hours and a pair of Battery Packs 6. A config.json from 1.0.0 keeps the old 2 and 4, so set `BatteryHours` to 4 and `BatteryPackHours` to 6 or delete the file.
- Robin sends one letter about the flashlight a few days into a save, to a player who has none. `SendMail` turns it off.
- A beam is removed when its player leaves a multiplayer game.
- The shadows cost about 0.2 ms a frame while moving and almost nothing standing still, and make no garbage for the game to clear.

Tested with one host and one joining player on one computer. Three or more players, split screen and internet play are untested. The numbers are still a first pass, see docs/BALANCE.md.

## Nexus file description

Flashlight 1.1.0. Other players see your beam as a cone in multiplayer, trees and walls now throw shadows, batteries last 4 and 6 hours instead of 2 and 4, and Robin sends one letter about the flashlight. If you played 1.0.0, change the two pair life settings or delete config.json to get the new battery life.

## Nexus changelog line

1.1.0: multiplayer cone and flicker, beam shadows, longer battery life (4 and 6 hours), a letter from Robin.
