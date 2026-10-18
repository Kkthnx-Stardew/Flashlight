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

The vanilla Battery Pack is rare and sells for 500g, so it is not used. This mod adds its own battery item, the Dry Cell.

## What the flashlight is for

- It keeps lighting while the player uses other tools. A torch has to be the selected item.
- It reaches further than a torch, but only forward.
- It costs upkeep, so it does not make the rings pointless.

## How it works

- **Battery slots.** The flashlight has two slots that work like the bait slot of a fishing rod. Right-click the flashlight in the backpack with Dry Cells on the cursor to load one into each slot; right-click it with an empty hand to take one out. Hover it to see what is loaded. Only Dry Cells fit, and only one to a slot, so it can never hold more than two.
- **Charge.** The flashlight keeps the charge of the cell in use. When that runs out it takes the next loaded cell. With nothing loaded it goes out.
- **Switching.** A key (F, or a list such as `F, LeftShoulder` for a controller) or using the tool switches it on and off. The toolbar picture changes to a lit one while it is on, and the farmer holds the flashlight in one hand while it is the selected tool.
- **Charge bar.** Under the picture is a bar like the water bar of the watering can. It shows how much of a full load (every slot holding a fresh cell) is left, in steps: green above half, yellow above a quarter, orange above a tenth, red below that. The tooltip gives the same figure as a percentage.
- **Only in the dark.** The charge only drains where a light matters: indoors, in the mines, or outside after dark. A flashlight left on in the sun costs nothing.
- **Random flicker.** Now and then, on average every 40 seconds of use, the beam stutters for a fraction of a second, like an old flashlight with a loose cell. Under a tenth of a full load the flickers come steadily closer together: about every 12 seconds at 5 percent and every 3 seconds when nearly empty, even if the random flicker is switched off, and a warning shows once.

## Numbers

| Setting | Default | Why |
| --- | --- | --- |
| Beam length | 7 tiles | More reach than a Small Glow Ring, far less coverage than a Glow Ring. |
| Beam spread | 30 degrees each side of the facing | Narrow on purpose. |
| Battery slots | 2 | Two cells is 8 in-game hours, a full night with some to spare, and no more. |
| Battery life | 4 in-game hours (24 ticks of 10 minutes) | About 170 real seconds per cell. |
| Flashlight price | 600g | A new game starts with 500g, so it cannot be bought on day one, and it is the cost of a good early harvest. |
| Battery price | 60g | 15g for each hour of light. A full spring night (6 hours, 1.5 cells) is about 90g, and only the dark hours count. |
| Battery sell price | 15g | Under a quarter of the shop price, so buying cells to sell them again loses money. |
| Where it is sold | Robin's Carpenter's Shop | She already sells the lamp-post recipes (500g and 1,000g). Shop hours are not verified. |
| Battery recipe | 1 Copper Bar + 1 Coal makes 3 Dry Cells | A Copper Bar is 5 Copper Ore and 1 Coal at a furnace (wiki), so a craft is 5 ore and 2 coal for 3 cells. Learned at Mining level 2, which is a placeholder and not verified. |

## Keeping it fair

| Way it could be abused | What stops it |
| --- | --- |
| Owning several flashlights to store charge in the spares | The shop only sells one to a player who has none, and only the first flashlight in the backpack works. A second one shows a message. |
| Stacking beams from several flashlights | There is one light per player, however many flashlights are carried. |
| Hoarding batteries so the upkeep never matters | The shop sells at most 6 a day and stops selling to a player carrying 6. Robin has none on about 15 of 100 days and half the stock on about 21 more (the roll is the same all day for every player, however often the shop is opened). That averages about 4.5 cells a day. |
| Buying cells and selling them back | Sell price is 15g against 60g to buy. |
| Crafting cells to sell them | A craft costs 5 Copper Ore and 2 Coal for 45g of cells at the sell price. |
| Loading a whole stack of cells into a slot | A slot takes exactly one cell. |
| Putting anything else in the slots | Only Dry Cells are accepted. |
| Burning cells in daylight | The charge does not drain outside by day. |
| Drain while time is frozen (menus, events) | Charge is spent on the game's own 10-minute clock, which stops then. |

Not blocked on purpose: batteries made by crafting are not limited, since each needs mined ore and coal.

## Multiplayer

The light is added as a shared light, so other players see a light at the right place. They will see the game's ordinary round light, because the custom cone picture is only drawn on the machine that owns the flashlight. Cone sharing is a possible later step. Multiplayer has not been tested.

## Open questions

- Shop days for Robin's Carpenter's Shop.
- Mining level for the battery recipe against vanilla recipe levels.
- Whether 600g and 60g feel right after real play. Every price is in `config.json`.
- Beam size and position need a play-test.
