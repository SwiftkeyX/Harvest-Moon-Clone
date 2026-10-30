# Farm

The farming life-sim's own vocabulary: the words tests, code and design discussions use for the game's rules.

## Time

**Day**:
One in-game day, from waking up to sleeping. The calendar counts days 1–30 within a season.
_Avoid_: Turn, tick

**Time of day**:
The in-game clock. Every day starts at 6:00 AM, and sleeping starts the next day at 6:00 AM again.
_Avoid_: Hour, tick

**Night**:
The change from one day to the next, when watered crops grow, every tile dries and, if a new season starts, out-of-season crops wither.
_Avoid_: Overnight update, day end

**Season**:
One of Spring, Summer, Fall and Winter, each 30 days long; after Winter comes Spring of the next year.
_Avoid_: Month

**Weather**:
Each day is either sunny or rainy. A rainy day waters every tilled tile; it never rains in winter.
_Avoid_: Climate

**Forecast**:
Tomorrow's weather, known a day ahead.
_Avoid_: Prediction

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

**Debris**:
A weed, rock or stump on a grass tile. Each kind is cleared back to grass only by its own tool: sickle, hammer or axe. Weeds can also sprout on grass overnight.
_Avoid_: Obstacle, junk

**Pond**:
A water tile where the watering can is refilled. It can't be tilled.
_Avoid_: Lake, well

## Crops

**Crop**:
A plant growing on a tile, defined by which seasons it grows in and how many watered nights each growth stage needs.
_Avoid_: Plant, seedling

**Mature**:
A crop that has grown through all its stages and is ready to harvest.
_Avoid_: Ripe, grown, done

**Harvest**:
Picking a mature crop by hand into the inventory. It costs no stamina, and fails if the inventory has no room.
_Avoid_: Pick up, collect, reap

**Regrow**:
A crop that stays planted after harvest and becomes mature again after a set number of watered nights.
_Avoid_: Respawn, reset

## Player

**Stamina**:
The player's energy for the day. Every tool swing costs some; a night's sleep restores it to full.
_Avoid_: Energy, health, HP

**Pass out**:
What happens when stamina reaches 0: the day ends at once, and the player wakes with half stamina.
_Avoid_: Faint, collapse, die

**Target tile**:
The tile directly in front of the player, in the direction they face. Tools, seed packets and harvesting act on it.
_Avoid_: Selected tile, cursor

**Hotbar**:
The row the player picks what to use from: the five tools, then each kind of seed packet they carry.
_Avoid_: Toolbar, quick slots

**Bed**:
Where the player sleeps to end the day.
_Avoid_: House

**Tool**:
Something the player swings at one tile: hoe, watering can, sickle, hammer or axe.
_Avoid_: Equipment, weapon

**Item**:
Something that can be held in the inventory, such as harvested produce. Each kind has a sell price and a stack limit.
_Avoid_: Object, thing, product

**Seed packet**:
An item bought at the seed shop. Sowing one plants its crop on the 3×3 square of tilled, empty tiles around the target and uses up the packet.
_Avoid_: Seed bag, seeds

**Inventory**:
The player's slots for carrying items; items of the same kind stack in one slot up to their limit.
_Avoid_: Bag, backpack, rucksack

## Money

**Money**:
What the player earns by shipping items and spends at shops. A new game starts with 500.
_Avoid_: Gold, coins, cash

**Shipping bin**:
The box on the farm where items are put to be sold. Everything in it sells overnight at each item's sell price, and the money is there in the morning.
_Avoid_: Sell box, chest

**Seed shop**:
Where seed packets are bought, from 9:00 AM until 5:00 PM. It only sells seeds for crops that grow in the current season.
_Avoid_: Store, market

**Ship**:
Putting items from the inventory into the shipping bin. Shipped items can't be taken back.
_Avoid_: Sell, deposit
