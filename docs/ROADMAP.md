# Roadmap

Planned for after the first release. Nothing here is promised for a date, and the numbers are open until they have been tried in play.

## The flashlight in the farmer's hand

Shown in the hand of a farmer who has it selected, and following the hand as they walk.

The first build drew a small flashlight beside the farmer, but it stayed in one place while the arm swung, so it looked stuck on rather than held. It was taken out of 0.1.0 until it follows the hand properly. The last commit that has it is `d8aadc4`.

What is known so far:

- The game keeps the arms apart from the body. In its farmer sheet, the arm of each frame sits one cell, 96 pixels, to the right of the body cell, and the hand is the patch of skin colour at the bottom of it.
- The walking animations use a handful of frames: right and left use 6, 20, 11, 6, 21, 17; down uses 0, 1, 18, 1, 0, 2, 19, 2; up uses 12, 13, 22, 13, 12, 14, 23, 14.
- The picture should be placed from the measured hand position of the current frame, drawn just under the arm layer so the fingers close over the handle, and the beam should start at its lens.
- The measuring can be done once per frame and remembered, so it costs nothing while playing.

## The game's own batteries

Let the flashlight take the Battery Pack the game already has, as a better cell than the Dry Cell: a brighter light and a longer burn.

Things to settle first:

- How much brighter and longer. The Battery Pack is harder to get than a Dry Cell, so it should be worth the trouble without making the Dry Cell pointless.
- How a flashlight with one Dry Cell and one Battery Pack in its slots behaves. The charge bar and the charge model currently assume every cell is the same.
- The beam length and the drain have to stay fair at the top end, so the flashlight cannot replace a lantern or a glow ring later in the game.
- Whether the shop sells the Battery Pack. Probably not: it should stay something found.
