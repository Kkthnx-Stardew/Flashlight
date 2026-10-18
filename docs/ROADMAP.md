# Roadmap

Nothing here is promised for a date. The order follows what players report, and the numbers stay open until they have been tried in real play. Feedback goes in the issue templates on GitHub and in the Nexus comments (see the README).

## Done

- The flashlight in the farmer's hand while it is the selected tool. The picture is placed from the measured hand of the arm the game draws in the current frame, drawn just under the arm so the fingers close over the handle. The beam starts at the lens. From the front and back it is held in the right hand, offset from the body.
- The game's Battery Pack as a better cell: 12 hours of light, a beam 30 percent longer and brighter.
- A local usage tally and a `flashlight_report` console command, so balance feedback can come with numbers.

## Next

### Check the balance against real play

The first release is the first test of the prices, the stock and the battery life. After the first reports, go through the levers in [BALANCE.md](BALANCE.md) and note each change in the changelog with the reason.

### Multiplayer

Other players see the game's ordinary round light for the beam, because the cone picture is only drawn on the machine that owns the flashlight. The held flashlight is drawn for every farmer who has one selected and lit, but the whole of multiplayer has not been tested. Sharing the cone is a possible next step.

### Translations

Every text is in `i18n/default.json`. Translations are welcome as pull requests, one file per language.

## Ideas, not planned

- A Content Patcher token or a small API so other mods can sell the flashlight or add a battery.
- A different beam colour with a different cell.
