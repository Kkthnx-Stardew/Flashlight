# Flashlight: balance design

The numbers below are the starting point. Almost every one is a config option, so they can be tuned after play-testing.

## What the game already has

Checked on the Stardew Valley Wiki and in the game's own code (1.6.15):

| Light | Reach | Notes |
| --- | --- | --- |
| Torch | about 3 tiles | Starter recipe (1 Wood, 2 Sap). Only lights while it is the held item (`Object.actionWhenBeingHeld`). |
| Small Glow Ring | 5 tiles | Found in Mines floors 1 to 39. Not sold. |
| Glow Ring | 10 tiles | Found in Mines floor 40 and deeper and in the Skull Cavern. Not sold. |

Night starts at 8pm in spring and summer, 7pm in fall and 6pm in winter. The day ends at 2am. Ten in-game minutes pass in 7 real seconds, so a spring night (8pm to 2am) is about 4.2 real minutes. A new game starts with 500g (seen in the game).

The game's Battery Pack sells for 500g and is not sold in any shop. It comes from a Lightning Rod in a storm (up to two a strike) and from the late-game Solar Panel (one every seven days of sun), and it is an ingredient in the lamp-posts, the Crystalarium and the Iridium Sprinkler. This mod adds its own cheap battery, the Dry Cell, for everyday use, and lets the flashlight take a Battery Pack as the better cell.

## What the flashlight is for

- It keeps lighting while the player uses other tools. A torch has to be the selected item.
- It reaches further than a torch, but only forward.
- It costs upkeep, so it does not make the rings pointless.

## How it works

- **Battery slots.** The flashlight always has two slots, like the bait slot of a fishing rod, and it is not a setting. It needs both filled with the same kind of cell, as a real flashlight does: two Dry Cells or two Battery Packs. One cell alone does nothing and a Dry Cell next to a Battery Pack is refused and handed back. Right-click the flashlight in the backpack with cells on the cursor to load one into each slot, and right-click it with an empty hand to take one out. Hover it to see what is loaded.
- **Charge.** A matched pair is used up together the moment the flashlight needs light and gives a fixed time, 2 hours for Dry Cells. The flashlight keeps that charge and, when it runs out, takes the next matched pair if one is loaded. With no matching pair it goes out. A pair can be loaded while the light is already on.
- **Battery Pack.** A pair of Battery Packs goes in the slots instead of Dry Cells. A pair lasts 4 hours (two more than Dry Cells) and while it is in use the beam is 20 percent longer (about 8.4 tiles) and takes more of the darkness away. Packs are found, not bought, and it takes two, so a pair is 1,000g of items the game would sell for 4 hours of a slightly better beam. That is deliberately a poor trade, so packs are a treat and not the default. The charge bar is measured against the best pair in the flashlight, so a Dry Cell pair reads full and a pair of packs reads full.
- **Only a cone.** Like a real flashlight it lights what it points at and nothing around the farmer, so it does not replace a torch for work at close range or a glow ring that lights every way.
- **Switching.** A key (F, or a list such as `F, LeftShoulder` for a controller) or using the tool switches it on and off. The toolbar picture changes to a lit one while it is on.
- **Charge bar.** Under the picture is a bar like the water bar of the watering can. It shows how much of a full load (one fresh pair, running or loaded) is left, in steps: green above half, yellow above a quarter, orange above a tenth, red below that. The tooltip gives the same figure as a percentage.
- **Only in the dark.** The charge only drains where a light matters: indoors, in the mines, or outside after dark or in the rain. A flashlight left on in the sun costs nothing.
- **Random flicker.** Now and then, on average every 40 seconds of use, the beam stutters for a fraction of a second, like an old flashlight with a loose cell. Under a tenth of a full load the flickers come steadily closer together: about every 12 seconds at 5 percent and every 3 seconds when nearly empty, even if the random flicker is switched off, and a warning shows once.

## Numbers

| Setting | Default | Why |
| --- | --- | --- |
| Beam length | 7 tiles (9 with a Battery Pack) | More reach than a Small Glow Ring, far less coverage than a Glow Ring. |
| Beam spread | 30 degrees each side of the facing | Narrow on purpose. |
| Battery slots | 2, fixed, a matched pair | A real flashlight takes two batteries that work together. Two Dry Cells is one pair, which is 2 in-game hours of light, so a full night takes several pairs and the bag matters. |
| Battery life | A pair of Dry Cells lasts 2 in-game hours (12 ticks of 10 minutes) | About 85 real seconds a pair. Short on purpose, so the flashlight is something you feed and not something you set and forget. |
| Flashlight price | 600g | A new game starts with 500g, so it cannot be bought on day one, and it is the cost of a good early harvest. |
| Battery price | 15g a cell | A pair is 30g for 2 hours, which is 15g for each hour of light. A full spring night (6 hours, 3 pairs) is about 90g, and only the dark hours count. |
| Battery sell price | 3g | A fifth of the shop price, so buying cells to sell them again loses money. |
| Where it is sold | Robin's Carpenter's Shop | She already sells the lamp-post recipes (500g and 1,000g). Shop hours are not verified. |
| Battery Pack life | A pair of Battery Packs lasts 4 in-game hours | Two more hours than a pair of Dry Cells. The game sells a pack for 500g and uses it for lamp-posts and sprinklers, and the flashlight needs two, so burning a pair is a real choice and never a bargain. Lightning rod storms and a week of a solar panel cannot carry a season of nights. |
| Battery recipe | 1 Copper Bar + 1 Coal makes 12 Dry Cells | A Copper Bar is 5 Copper Ore and 1 Coal at a furnace (wiki), so a craft is 5 ore and 2 coal for 12 cells, which is 12 hours of light. Learned at Mining level 2, which is a placeholder and not verified. |

## Keeping it fair

| Way it could be abused | What stops it |
| --- | --- |
| Owning several flashlights to store charge in the spares | The shop only sells one to a player who has none. A second one cannot be picked up or moved into the backpack (the game is told it does not fit, so it stays where it was and nothing is lost). If one still gets in some other way, only the first in the backpack works. Spares cannot be lit and take no cells, so they cannot store extra light, and using one shows a message. |
| Stacking beams from several flashlights | There is one light per player, however many flashlights are carried. |
| Hoarding batteries so the upkeep never matters | The shop sells at most 16 a day and stops selling to a player carrying 16. Robin has none on about 15 of 100 days and half the stock on about 21 more (the roll is the same all day for every player, however often the shop is opened). That averages about 11 cells a day, which is 11 hours of light, with the shop closed one day in seven. |
| Buying cells and selling them back | Sell price is 3g against 15g to buy. |
| Crafting cells to sell them | A craft costs 5 Copper Ore and 2 Coal for 36g of cells at the sell price. |
| Loading a whole stack of cells into a slot | A slot takes exactly one cell. |
| Putting anything else in the slots | Only Dry Cells and Battery Packs are accepted, and only a matching pair runs the light. |
| Burning cells in daylight | The charge does not drain outside by day. |
| Drain while time is frozen (menus, events) | Charge is spent on the game's own 10-minute clock, which stops then. |

Not blocked on purpose: batteries made by crafting are not limited, since each needs mined ore and coal.

## Multiplayer

The light is added as a shared light, so other players see a light at the right place. They will see the game's ordinary round light, because the custom cone picture is only drawn on the machine that owns the flashlight. Cone sharing is a possible later step. Multiplayer has not been tested.

## Supply against use

`docs/balance_sim.py` plays a year (112 days) with the shop open six days in seven and a player who buys up to the carry limit each open day, reloads from the bag, and needs the same hours of light every day. Crafting and Battery Packs are left out. Each cell is one hour of light, because a pair is used together for 2 hours.

| Hours of light a day | Days with too little light | Cells bought a day | Cost a day |
| --- | --- | --- | --- |
| 3 | 0% | 3.0 | 45g |
| 4 | 0% | 4.0 | 59g |
| 6 | 2% | 5.9 | 88g |
| 8 | 7% | 7.4 | 112g |
| 10 | 34% | 8.4 | 126g |
| 12 | 36% | 9.2 | 138g |

Reading it: a player who lights a normal night (4 to 6 hours) is covered by the shop and pays about 60 to 90g a day. A player who runs the flashlight all day, such as through long Mines trips, runs short on one day in three at 10 hours, so they need to craft cells (12 from a Copper Bar and a Coal) or spend Battery Packs. That is the intended pressure, and the first thing to check against real play.

## What would change the numbers

The defaults are a first pass. Players can paste the output of the `flashlight_report` console command into feedback, which shows their changed settings and a local tally of nights used, hours lit, cells used and how often the flashlight ran out. The levers and what would move them:

| What players report | Lever |
| --- | --- |
| Most nights end with spare cells and the shop is never empty | Lower `BatteryDailyStock` or raise `BatteryOutOfStockPercent`. The target is a flashlight that costs thought on a bad shop day, not every day. |
| The flashlight runs out mid night more than about one night in four | Raise `BatteryHours` or `BatteryDailyStock`. Upkeep should be a cost, not a wall. |
| The 600g price is reached before spring ends and feels free | Raise `FlashlightPrice`. If it feels out of reach, check against when players first have 600g. |
| Beam or glow is called too strong next to glow rings | Lower `BeamLengthTiles`. Rings should stay better in the Mines. |
| Battery Packs make the Dry Cell pointless | Lower `BatteryPackHours` or `BatteryPackBeamPercent`. A pack should be an event, not the default. |
| Flicker is annoying | Raise `FlickerSecondsBetween`, or set it to 0. The low-charge warning flicker stays. |

Each change goes into the changelog with the report that caused it, so the history of the balance can be followed.

## Open questions

- Shop days for Robin's Carpenter's Shop.
- Mining level for the battery recipe against vanilla recipe levels.
- Whether 600g and 30g feel right after real play. Every price is in `config.json`.
- Beam size and position need a play-test.
