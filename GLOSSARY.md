# Farm

The farming life-sim's own vocabulary: the words tests, code and design discussions use for the game's rules.

## Time

**Day**:
One in-game day, from waking up to sleeping. The calendar counts days 1–30 within a season.
_Avoid_: Turn, tick

**Night**:
The change from one day to the next, when watered crops grow, every tile dries and, if a new season starts, out-of-season crops wither.
_Avoid_: Overnight update, day end

**Season**:
One of Spring, Summer, Fall and Winter, each 30 days long; after Winter comes Spring of the next year.
_Avoid_: Month

## Farm

**Tile**:
One square of the farm, holding at most one crop.
_Avoid_: Cell, plot, square

**Tilled**:
A tile whose soil has been dug with the hoe, so seeds can be planted on it.
_Avoid_: Plowed, hoed

**Watered**:
A tilled tile that was watered today; it stays watered until the night.
_Avoid_: Wet, irrigated

**Withered**:
A tile whose crop died at a season change because the crop can't grow in the new season. Nothing can be planted on it until it is cleared.
_Avoid_: Dead, rotten

## Crops

**Crop**:
A plant growing on a tile, defined by which seasons it grows in and how many watered nights each growth stage needs.
_Avoid_: Plant, seedling

**Mature**:
A crop that has grown through all its stages and is ready to harvest.
_Avoid_: Ripe, grown, done
