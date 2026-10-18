# Flashlight

A flashlight for Stardew Valley that casts a beam in the direction you face and runs on batteries. Buy it early, keep it in your backpack, and see at night without holding a torch.

This is an early build. It has been run in Stardew Valley 1.6.15 with SMAPI 4.5.2 on Windows and the beam, battery slots, shop and flicker work, but the art and the numbers are a first pass and it has not had real play-time yet.

## What it adds

- **Flashlight** (a tool): sold at Robin's Carpenter's Shop, one to a player. Press **F** (or use the tool) to switch it on and off. It keeps shining while you use other tools, which a torch cannot do. The toolbar picture shows when it is on, a charge bar under it works like the water bar of the watering can.
- **Battery slots**: the flashlight has two, like the bait slot on a fishing rod. Right-click the flashlight with Dry Cells on the cursor to load them, one to a slot, and right-click it with an empty hand to take one out. Hover it to see what is loaded.
- **Dry Cell** (a battery): sold at Robin's in limited numbers, or crafted from 1 Copper Bar and 1 Coal for 3 cells once you reach Mining level 2. Each lasts 4 in-game hours, and only while it is dark where you are.
- A forward cone of light, 7 tiles long, with a faint glow around you. Now and then it flickers like an old flashlight with a loose cell, and much more often once the charge falls under a tenth of a full load.

The balance reasoning, with the numbers it was checked against and how it is kept from being abused, is in [docs/BALANCE.md](docs/BALANCE.md).

## Settings

Edit `config.json` in the mod folder after the first run.

| Setting | Default | What it does |
| --- | --- | --- |
| `ToggleKey` | `F` | The button or buttons that switch the flashlight on and off, such as `F, LeftShoulder`. |
| `BeamLengthTiles` | `7` | How far the beam reaches (3 to 12). |
| `BatteryHours` | `4` | In-game hours one battery lasts (1 to 12). |
| `BatterySlots` | `2` | How many batteries a new flashlight holds (1 to 4). |
| `FlashlightPrice` | `600` | Shop price of the flashlight. |
| `BatteryPrice` | `60` | Shop price of one battery. |
| `SoldAtCarpenter` | `true` | Whether Robin sells them. Turn off if another mod handles shops. |
| `BatteryDailyStock` | `6` | How many batteries the shop sells to one player each day. |
| `MaxBatteriesCarried` | `6` | The shop stops selling batteries to someone carrying this many. |
| `BatteryOutOfStockPercent` | `15` | Chance in percent that Robin has no batteries on a day. |
| `BatteryLowStockPercent` | `25` | Chance in percent that Robin has half the stock on a day. |
| `FlickerSecondsBetween` | `40` | Average seconds of use between random flickers. 0 turns them off. |

## Install

1. Install [SMAPI](https://smapi.io/) 4.2.1 or newer.
2. Put the `Flashlight` folder in `Stardew Valley/Mods`.

Needs Stardew Valley 1.6.9 or newer.

## Known limits

- The battery slots, the charge bar and the tooltip line use four small Harmony patches on the game's own code. They only act on the flashlight. SMAPI lists the mod as one that changes game code for that reason.
- The settings can also be changed in game with Generic Mod Config Menu, when it is installed.
- In multiplayer other players see the game's ordinary round light, because the custom cone picture is only drawn on the machine that owns the flashlight. Multiplayer is untested.
- Shop days for Robin and the Mining level of the recipe are not checked against the rest of the game.

## Building

The mod needs the game's files to build, so it cannot be built on a GitHub runner. With the game installed:

```
dotnet build Flashlight/Flashlight.csproj
```

By default the build copies the mod into your `Mods` folder. Pass `-p:EnableModDeploy=false -p:EnableModZip=false` to build without that.

The tests cover the code that has no game dependencies, plus the manifest, translations and picture sizes:

```
dotnet test Flashlight.Tests
```

The pictures are drawn by `art/Generate-Art.ps1`, so they can be adjusted and regenerated:

```
powershell -ExecutionPolicy Bypass -File art/Generate-Art.ps1
```

See [docs/ROADMAP.md](docs/ROADMAP.md) for what is planned next.

## License

MIT, see [LICENSE.txt](LICENSE.txt). This is a fan-made mod and is not affiliated with ConcernedApe.
