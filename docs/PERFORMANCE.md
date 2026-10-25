# Performance

Numbers are measured, not guessed. Anything changed for speed is timed before and after, and the result goes here.

## Beam shadows

The beam's shadows are worked out each frame the light is on and the picture can change: one pass over the lit pixels of the 128 by 128 cone, a straight line from the lens to each pixel through the tiles in between, then a soften pass and an upload of the picture.

How it is timed: a throwaway test mod calls the mask build 300 times for each of the four facings, on the real game with a real save, standing still and walking (the lens moves every frame). It reports milliseconds and bytes allocated per build. One frame at 60 frames a second is 16.7 ms.

Test place: Farm (198 terrain features, 97 resource clumps, 10 buildings). Town and Forest gave the same cost within 5 percent, so what is in the place hardly matters.

| Step | Walking, ms per frame | Garbage per frame | Standing still, ms per frame |
| --- | --- | --- | --- |
| First version | 0.349 | 125 KB | 0.349 |
| A delegate was made for every pixel's line, now made once | 0.305 | 0.3 KB | 0.305 |
| Softening is two passes of three instead of one of nine | 0.185 | 0.3 KB | 0.185 |
| The same picture is reused while nothing has moved, for up to 12 updates | 0.185 | 0.3 KB | 0.015 |

The 0.3 KB left is the test's own call overhead. The old version produced about 7.5 MB of garbage a second with the light on, which the game's collector had to clear. The softening change gives the same result as before: a test compares it with the plain nine-value average.

Walking costs about 1.1 percent of a frame and standing still about 0.1 percent.

## Other players' beams

Each machine draws the cones of the other players itself (see the multiplayer part of [BALANCE.md](BALANCE.md)). Timed on the host with the joining player's beam lit, calling the update 300 times:

| Case | ms per update | Garbage per update |
| --- | --- | --- |
| The beam has not moved or changed size | 0.013 | 0.02 KB |
| The picture has to be made again every time (worst case, one other player) | 0.158 | 0.02 KB |

It does nothing at all when no other player is in the game, and costs about the same again for each further player whose beam is in view and moving.

## Garbage across the whole game

Measured over 600 frames of the whole game, with the joining player in the same place. A bare run swings by about 1 KB a frame, so differences under that mean nothing.

| Case | Host, KB a frame | Joining player, KB a frame |
| --- | --- | --- |
| Nothing lit | 78.5 | 64.2 |
| Host's beam lit | 79.3 | 63.2 |
| Both beams lit | 80.4 | 63.4 |

The game itself makes about 64 to 78 KB a frame with no mod doing anything. A lit beam adds no more than about 1 KB, which is inside the run-to-run noise.
