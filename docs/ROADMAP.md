# Roadmap

Nothing here is promised for a date. The order follows what players report, and the numbers stay open until they have been tried in real play. Feedback goes in the issue templates on GitHub and in the Nexus comments (see the README).

## Done

- The flashlight in the farmer's hand while it is the selected tool. The picture is placed from the measured hand of the arm the game draws in the current frame, drawn just under the arm so the fingers close over the handle. The beam starts at the lens. From the front and back it is held in the right hand, offset from the body.
- The game's Battery Pack as a better cell: a pair of them lasts 6 hours, with a beam 20 percent longer and a little brighter.
- A local usage tally and a `flashlight_report` console command, so balance feedback can come with numbers.

## Next

### Check the balance against real play

The first release is the first test of the prices, the stock and the battery life. After the first reports, go through the levers in [BALANCE.md](BALANCE.md) and note each change in the changelog with the reason.

### Multiplayer

The cone, its flicker and the clean-up of a beam when a player leaves work with one host and one joining player (see [BALANCE.md](BALANCE.md)). Still to test: three or more players, split screen, play over the internet with lag, a player joining while a beam is already lit, and other mods that change lights.

### Translations

Every text is in `i18n/default.json`. Translations are welcome as pull requests, one file per language.

## Ideas, not planned

- A Content Patcher token or a small API so other mods can sell the flashlight or add a battery.
- A different beam colour with a different cell.
