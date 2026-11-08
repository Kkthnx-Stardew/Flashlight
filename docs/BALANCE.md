# Flashlight: balance design

The numbers below are the starting point. Almost every one is a config option, so they can be tuned after play-testing.

## What the game already has

Checked on the Stardew Valley Wiki and in the game's own code (1.6.15):

| Light | Reach | Notes |
| --- | --- | --- |
| Torch | about 3 tiles | Starter recipe (1 Wood, 2 Sap). Only lights while it is the held item (`Object.actionWhenBeingHeld`). |
| Small Glow Ring | 5 tiles | Found in Mines floors 1 to 39. Not sold. |
| Glow Ring and Glowstone Ring | 10 tiles | Found in Mines floor 40 and deeper and in the Skull Cavern. Not sold. |

Night starts at 8pm in spring and summer, 7pm in fall and 6pm in winter. The day ends at 2am. Ten in-game minutes pass in 7 real seconds, so a spring night (8pm to 2am) is about 4.2 real minutes. A new game starts with 500g (seen in the game).

The game's Battery Pack sells for 500g and is not sold in any shop. It comes from a Lightning Rod in a storm (up to two a strike) and from the late-game Solar Panel (one every seven days of sun), and it is an ingredient in the lamp-posts, the Crystalarium and the Iridium Sprinkler. This mod adds its own cheap battery, the Dry Cell, for everyday use, and lets the flashlight take a Battery Pack as the better cell.

## What the flashlight is for

- It keeps lighting while the player uses other tools. A torch has to be the selected item.
- It reaches further than a torch, but only forward.
- It costs upkeep, so it does not make the rings pointless.

## How it works

- **Battery slots.** The flashlight always has two slots, like the bait slot of a fishing rod, and it is not a setting. It needs both filled with the same kind of cell, as a real flashlight does: two Dry Cells or two Battery Packs. One cell alone does nothing and a Dry Cell next to a Battery Pack is refused and handed back. Right-click the flashlight in the backpack with cells on the cursor to load one into each slot, and right-click it with an empty hand to take one out. Hover it to see what is loaded.
- **Charge.** A matched pair is used up together the moment the flashlight needs light and gives a fixed time, 10 hours for Dry Cells. The flashlight keeps that charge and, when it runs out, takes the next matched pair if one is loaded. With no matching pair it goes out. A pair can be loaded while the light is already on.
- **Battery Pack.** A pair of Battery Packs goes in the slots instead of Dry Cells. A pair lasts 12 hours (two more than Dry Cells) and while it is in use the beam is 20 percent longer (about 12 tiles) and takes more of the darkness away. Packs are found, not bought, and it takes two, so a pair is 1,000g of items the game would sell for 12 hours of a slightly better beam. That is deliberately a poor trade, so packs are a treat and not the default. The charge bar is measured against the best pair in the flashlight, so a Dry Cell pair reads full and a pair of packs reads full.
- **Only a cone.** Like a real flashlight it lights what it points at and nothing around the farmer, so it does not replace a torch for work at close range or a glow ring that lights every way.
- **Switching.** A key (F, or a list such as `F, LeftShoulder` for a controller) or using the tool switches it on and off. The toolbar picture changes to a lit one while it is on.
- **Charge bar.** Under the picture is a bar like the water bar of the watering can. It shows how much of a full load (one fresh pair, running or loaded) is left, in steps: green above half, yellow above a quarter, orange above a tenth, red below that. The tooltip gives the same figure as a percentage.
- **Only in the dark.** The charge only drains where a light matters: indoors, in the mines, or outside after dark or in the rain. A flashlight left on in the sun costs nothing.
- **Random flicker.** Now and then, on average every 40 seconds of use, the beam stutters for a fraction of a second, like an old flashlight with a loose cell. Under a tenth of a full load the flickers come steadily closer together: about every 12 seconds at 5 percent and every 3 seconds when nearly empty, even if the random flicker is switched off, and a warning shows once.

## Numbers

| Setting | Default | Why |
| --- | --- | --- |
| Beam length | 10 tiles (12 with a Battery Pack) | The same reach as a Glow Ring and a Glowstone Ring, which the game gives a radius of 10. It is a narrow cone, so it covers far less ground than either ring. |
| Beam spread | 30 degrees each side of the facing | Narrow on purpose. |
| Battery slots | 2, fixed, a matched pair | A real flashlight takes two batteries that work together. Two Dry Cells is one pair, which is 10 in-game hours of light, so a full night uses about two thirds of a pair. |
| Battery life | A pair of Dry Cells lasts 10 in-game hours (60 ticks of 10 minutes) | About 7 real minutes a pair. It was 2 hours in 1.0.0, 4 in 1.1.0 and is 10 from 1.3.0, so one pair covers a night and most of the next. The price per cell went up with it, see the 1.3.0 note below. |
| Flashlight price | 600g | A new game starts with 500g, so it cannot be bought on day one, and it is the cost of a good early harvest. |
| Battery price | 30g a cell | A pair is 60g for 10 hours, which is 6g for each hour of light. A spring night (6 dark hours) costs about 36g, in the middle of the 30g to 45g a night that 1.1.0 and 1.2.0 charged. |
| Battery sell price | 6g | A fifth of the shop price, so buying cells to sell them again loses money. |
| Where it is sold | Robin's Carpenter's Shop, Clint's Blacksmith as an option | Robin already sells the lamp-post recipes (500g and 1,000g). Clint's is off by default. The game counts daily stock for each shop on its own, so with both on the daily stock is shared between them. Shop hours are not verified. |
| Battery Pack life | A pair of Battery Packs lasts 12 in-game hours | Two more hours than a pair of Dry Cells. The game sells a pack for 500g and uses it for lamp-posts and sprinklers, and the flashlight needs two, so burning a pair is a real choice and never a bargain. Lightning rod storms and a week of a solar panel cannot carry a season of nights. |
| Battery recipe | 1 Copper Bar + 1 Coal makes 12 Dry Cells | A Copper Bar is 5 Copper Ore and 1 Coal at a furnace (wiki), so a craft is 5 ore and 2 coal for 12 cells, which is 60 hours of light. Learned at Mining level 2, which is a placeholder and not verified. |

## Keeping it fair

| Way it could be abused | What stops it |
| --- | --- |
| Owning several flashlights to store charge in the spares | The shop only sells one to a player who has none. A second one cannot be picked up or moved into the backpack (the game is told it does not fit, so it stays where it was and nothing is lost). If one still gets in some other way, only the first in the backpack works. Spares cannot be lit and take no cells, so they cannot store extra light, and using one shows a message. |
| Stacking beams from several flashlights | There is one light per player, however many flashlights are carried. |
| Hoarding batteries so the upkeep never matters | The shop sells at most 8 a day and stops selling to a player carrying 8, which is 40 hours of light carried at most. Robin has none on about 15 of 100 days and half the stock on about 21 more (the roll is the same all day for every player, however often the shop is opened), and the shop is closed one day in seven. That averages about 5 cells a day, which is 25 hours of light a day, more than anyone lights in a normal day. |
| Buying cells and selling them back | Sell price is 6g against 30g to buy. |
| Crafting cells to sell them | A craft uses a Copper Bar (60g) and a Coal (15g), which sell for 75g, and its 12 cells sell for 72g, so it loses a little. Prices read from the game's own data. |
| Loading a whole stack of cells into a slot | A slot takes exactly one cell. |
| Putting anything else in the slots | Only Dry Cells and Battery Packs are accepted, and only a matching pair runs the light. |
| Burning cells in daylight | The charge does not drain outside by day. |
| Drain while time is frozen (menus, events) | Charge is spent on the game's own 10-minute clock, which stops then. |

Not blocked on purpose: batteries made by crafting are not limited, since each needs mined ore and coal.

## Multiplayer

The light is added as a shared light, so other players see a light at the right place, at the size and colour the owner sends. The game does not send a light's picture, though, so on its own every other player would see the plain round lantern glow. Each machine with the mod therefore draws the cone itself for the flashlights of the other players in the same place, facing the way their farmer faces, with the shadows worked out on that machine.

The flicker needed one more step. The game holds back a new radius until the old one has settled, so a flicker that changes the size every few updates never arrived (measured: the owner's beam changed size 30 times in 15 seconds and the other player's copy received none). The owner now also sends the true beam size in the light's texture index, which the game sends at once and ignores when the number is not one of its own pictures, and the other machine draws the cone at that size. A player without the mod keeps the lantern glow and the slow radius.

When a player leaves the game, the game removes the light of a ring but not one added by a mod, so the host removes the beams of a player who dropped and every player clears their own old beam when a day starts.

Here is what was tested, with a host and one joining player on one computer over a local game. The joining player's copy of the old build showed the host's beam as the lantern glow, and the new build shows the cone in two directions with shadows from trees. It also works the other way, with the joining player's flashlight seen on the host's screen. The flicker reaches the other screen too (the cone changed size 10 times for the 30 changes the owner made, since the game sends changes about 20 times a second), and the beam of a player who closed their game is removed on the host. Not tested yet are three or more players, split screen, play over the internet with lag, a player joining while a beam is already lit, and other mods that change lights.

## Supply against use

`docs/balance_sim.py` plays a year (112 days) with the shop open six days in seven and a player who buys up to the carry limit each open day, reloads from the bag, and needs the same hours of light every day. Crafting and Battery Packs are left out. Each cell is five hours of light, because a pair is used together for 10 hours. Run `python docs/balance_sim.py 8 8` to try other limits (daily stock, then carry limit).

Share of days with too little light, and what the cells cost a day at 30g, with the shop limits at 16 and 16 (the 1.3.0 defaults) and at 8 and 8 (the defaults from the version after it):

| Hours of light a day | Short at 16 and 16 | Short at 8 and 8 | Cells bought a day (8 and 8) | Cost a day (8 and 8) |
| --- | --- | --- | --- | --- |
| 4 | 0% | 0% | 0.8 | 24g |
| 6 | 0% | 0% | 1.2 | 36g |
| 8 | 0% | 0% | 1.6 | 47g |
| 12 | 0% | 0.6% | 2.4 | 71g |
| 16 | 0% | 4.3% | 3.1 | 93g |
| 20 | 0.2% | 7.0% | 3.8 | 113g |
| 30 | 1.8% | 34.4% | 4.8 | 142g |

Reading it: with the 1.3.0 batteries a pair covers 10 hours, so 16 cells a day (80 hours) was far more than anyone lights, and the shop limits shaped nothing. At 8 and 8 a normal night (4 to 6 hours) is still always covered and costs 24 to 36g, and only a player who runs the flashlight most of the day, such as through long Mines trips, sometimes finds the shelf short and then crafts cells (12 from a Copper Bar and a Coal) or spends Battery Packs. A day is 20 hours long from 6am to 2am, so 20 hours is the most anyone can light. The simulation is a model with a player who lights the same hours every day, not a measurement of real play.

## What would change the numbers

The defaults are a first pass. Players can paste the output of the `flashlight_report` console command into feedback, which shows their changed settings and a local tally of nights used, hours lit, cells used and how often the flashlight ran out. The levers and what would move them:

| What players report | Lever |
| --- | --- |
| Most nights end with spare cells and the shop is never empty | Lower `BatteryDailyStock` or raise `BatteryOutOfStockPercent`. The target is a flashlight that costs thought on a bad shop day, not every day. |
| The flashlight runs out mid night more than about one night in four | Raise `BatteryHours` or `BatteryDailyStock`. Upkeep should be a cost, not a wall. |
| The 600g price is reached before spring ends and feels free | Raise `FlashlightPrice`. If it feels out of reach, check against when players first have 600g. |
| Beam or glow is called too strong next to glow rings | Lower `BeamLengthTiles`. From 1.3.0 the beam reaches as far as a Glow Ring on purpose, so only the cone shape and the upkeep keep rings better in the Mines. |
| Battery Packs make the Dry Cell pointless | Lower `BatteryPackHours` or `BatteryPackBeamPercent`. A pack should be an event, not the default. |
| Flicker is annoying | Raise `FlickerSecondsBetween`, or set it to 0. The low-charge warning flicker stays. |

Each change goes into the changelog with the report that caused it, so the history of the balance can be followed.

## Open questions

- Shop days for Robin's Carpenter's Shop.
- Mining level for the battery recipe against vanilla recipe levels.
- Whether 600g and 30g feel right after real play. Every price is in `config.json`.
- Beam size and position need a play-test.

## 1.3.0 changes

Three numbers moved together, so the upkeep did not fall by accident.

| What | Was | Now | Why |
| --- | --- | --- | --- |
| Beam length | 7 tiles | 10 tiles (12 with a Battery Pack) | The game gives a Glow Ring and a Glowstone Ring a light radius of 10 (read from the game's code), with the colour (0, 30, 150), which the beam already used. Reach now matches. Measured at night in the same spot, the centre of the beam added 10 to 20 percent more light than the ring from 1 to 5 tiles out and 30 to 60 percent more at 7 to 9 tiles, because the cone puts its light in a narrow strip. Dimming the light colour made no visible difference in the same test, so the brightness is set by the picture and the length, not the colour value. |
| Dry Cell pair life | 4 hours | 10 hours | Six hours longer, as asked. A normal night of 6 dark hours now uses about two thirds of a pair. |
| Battery Pack pair life | 6 hours | 12 hours | Six hours longer, and still two hours more than Dry Cells. |
| Dry Cell price | 15g | 30g | Hours per cell went up by 2.5 times. At 15g the cost of an hour of light would have dropped from 7.5g to 3g, so a night would cost about 18g. At 30g an hour is 6g and a night about 36g, in the middle of the 30g to 45g a night the last two versions charged. At 38g an hour would match the old 7.5g exactly, but a pair at 76g is a bigger single purchase for a player who has just bought a 600g flashlight. |
| Dry Cell sell price | 3g | 6g | Kept at a fifth of the shop price, so buying to resell still loses money. |

Not changed in 1.3.0, but flagged. The daily stock (16) and the carry limit (16) covered 80 hours of light a day for each player, far more than anyone lights in a day, so the shop limits no longer shaped play. The next version lowers both to 8, see below.

A config.json from an older version has its values moved to these numbers only where they still equal the old defaults (7 tiles, 4 and 6 hours, 15g), and a value a player changed is kept. The yearly shop simulation in docs/balance_sim.py was rerun for these numbers afterwards, see Supply against use.

## Shop limits lowered (after 1.3.0)

| What | Was | Now | Why |
| --- | --- | --- | --- |
| Dry Cells sold a day (`BatteryDailyStock`) | 16 | 8 | With 10 hour pairs, 16 cells was 80 hours of light a day, and the simulation never ran short even at 20 hours a day. At 8 it is 40 hours, still far more than a normal night, and a long Mines day can sometimes find the shelf short. |
| Most Dry Cells carried (`MaxBatteriesCarried`) | 16 | 8 | The same reasoning, so a player cannot stockpile more than 40 hours from the shop. |

Nothing else moved, so the price per hour of light stays at 6g. A `config.json` that still has 16 for either value is moved to 8 once, and a value the player changed is kept. Both can be set back to 16 in `config.json` or in Generic Mod Config Menu.
