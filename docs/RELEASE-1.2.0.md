# Flashlight 1.2.0 release text

Published on 2026-10-25. The text below is what went into the GitHub release, the Nexus file description and the Nexus changelog line. The page text is `NEXUS.bbcode`.

## GitHub release notes

Flashlight 1.2.0

- Fixed a mouse click on a toolbar slot switching the light instead of selecting the slot while the flashlight was selected.
- Clint's Blacksmith can sell the flashlight and Dry Cells too. `SoldAtBlacksmith` is off by default and works alone or with Robin's. With both on, the daily Dry Cell stock is shared between the two shops.
- The letter about the flashlight is Robin's, Robin's with a line about Clint, or Clint's, depending on who sells it.
- The tooltip says why the battery slots are empty, since a pair is used up as soon as the light goes on.

Most of this came from feedback on Nexus. A config.json from 1.0.0 keeps the old 2 and 4 hour battery life, so set `BatteryHours` to 4 and `BatteryPackHours` to 6 or delete the file. Multiplayer is still only tested with one host and one joining player, see docs/BALANCE.md.

## Nexus file description

Flashlight 1.2.0. Fixes a toolbar click switching the light, adds Clint's Blacksmith as an optional shop with the Dry Cell stock shared between shops, makes the letter follow the shops, and explains empty battery slots in the tooltip.

## Nexus changelog line

1.2.0: toolbar click fix, optional Clint's shop, letter follows the shops, tooltip explains empty slots.
