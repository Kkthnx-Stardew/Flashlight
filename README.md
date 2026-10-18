# Flashlight

A flashlight for Stardew Valley that casts a beam in the direction you face and runs on batteries. Buy it early, keep it in your backpack, and see at night without holding a torch.

This is an early build and has not been played in the game yet. The art and the numbers are a first pass.

## What it adds

- **Flashlight** (a tool): sold at Robin's Carpenter's Shop. Press **F** or use the tool to switch it on and off. It keeps shining while you use other tools, which a torch cannot do.
- **Dry Cell** (a battery): sold at Robin's, or crafted from 1 Copper Bar and 1 Coal for 3 cells once you reach Mining level 2. The flashlight takes a new one from your backpack when its charge runs out, so there is no menu to load it.
- A forward cone of light, 7 tiles long, with a faint glow around you.

The balance reasoning, with the numbers it was checked against, is in [docs/BALANCE.md](docs/BALANCE.md).

## Settings

Edit `config.json` in the mod folder after the first run.

| Setting | Default | What it does |
| --- | --- | --- |
| `ToggleKey` | `F` | The button that switches the flashlight on and off. |
| `BeamLengthTiles` | `7` | How far the beam reaches (3 to 12). |
| `BatteryHours` | `4` | In-game hours one battery lasts (1 to 12). |
| `FlashlightPrice` | `600` | Shop price of the flashlight. |
| `BatteryPrice` | `60` | Shop price of one battery. |
| `SoldAtCarpenter` | `true` | Whether Robin sells them. Turn off if another mod handles shops. |

## Install

1. Install [SMAPI](https://smapi.io/) 4.2.1 or newer.
2. Put the `Flashlight` folder in `Stardew Valley/Mods`.

Needs Stardew Valley 1.6.9 or newer.

## Known limits

- In multiplayer other players see the game's ordinary round light, because the custom cone picture is only drawn on the machine that owns the flashlight.
- The flashlight icon does not change when it is switched on. The "on" picture is in the sheet for later.
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

## License

MIT, see [LICENSE.txt](LICENSE.txt). This is a fan-made mod and is not affiliated with ConcernedApe.
