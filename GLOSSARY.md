# Farm

The farming life-sim's own vocabulary: the words tests, code and design discussions use for the game's rules and its look.

## Time

**Day**:
One in-game day, from waking up to sleeping. The calendar counts days 1–30 within a season.
_Avoid_: Turn, tick

**Time of day**:
The in-game clock. Every day starts at 6:00 AM and runs until 2:00 AM; one in-game hour takes 40 real seconds.
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
What happens when stamina reaches 0 or the player is still up at 2:00 AM: the day ends at once, and the player wakes with half stamina.
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

## Game

**Save**:
The game's progress on disk. It's written automatically every time a day ends and loaded when the game starts; F9 starts a new game.
_Avoid_: Checkpoint, slot

## Art

**Grass tones**:
The set of colours that grass ground is drawn in: dark grass, base grass and light grass, plus dry grass.
_Avoid_: Grass colours, green shades

**Grass shapes**:
The areas of dark and light grass. They follow the land: dark grass pools round trees, bushes and rocks and at the foot of slopes, light grass sits on ledge tops. Their edges are angular, like cut paper, and they stretch along the land.
_Avoid_: Patches, blobs, noise, land tone

**Dark grass**:
The cool blue-green grass round props and at the foot of slopes.
_Avoid_: Lush grass, shade, grass band

**Base grass**:
The middle grass tone, between dark grass and light grass.
_Avoid_: Normal green, grass colour

**Light grass**:
The warm yellow-green grass on open ground and ledge tops.
_Avoid_: Warm grass, highlight

**Break-up piece**:
A small chunky piece of dark or light grass just outside a grass shape, with a gap between, so the shape doesn't end on one clean line.
_Avoid_: Spot, satellite, speckle

**Grass shape map**:
A baked picture of the terrain from above that tells the shader where grass shapes go: near props, at slope feet, on ledge tops. Rebake it (Farm > Art > Bake Grass Shape Map) after moving props.
_Avoid_: Mask, splat map

**Dry grass**:
A small patch where the autumn grass leans towards straw colour.
_Avoid_: Dirt, sand, dead patch

**Rock streaks**:
The long, bent, mostly level strokes that cliffs are painted with: dark and mid grey-browns over a warm taupe rock, with a few cream patches. Shaded rock takes a softer, greyer mauve than shaded grass.
_Avoid_: Rock bands, layers, lines

**Leaf cards**:
The cloud of small painted leaf clusters that always face the camera and give a bush or tree its brushy outline. Every leafy blob has them.
_Avoid_: Billboards, sprites, particles

**Leafy core**:
The solid, darker ball inside the leaf cards that fills the gaps between them, so gaps read as depth.
_Avoid_: Inner mesh, blob

**Flower cards**:
How the decor flowers are drawn: a brushy tuft of leaf cards at the bottom, and one painted card per flower (stem, petals and middle) that always faces the camera.
_Avoid_: Flower sprites, billboards

**Tall grass**:
Clumps of upright painted grass blades round the foot of bushes, trees and rocks, so props sit in the grass rather than on it.
_Avoid_: Grass tufts, weeds

**Haze**:
The warm cream that the land fades into with distance, and the colour behind everything.
_Avoid_: Fog, sky colour

**Pixel look**:
The whole picture drawn at about a third of the screen's resolution and scaled up without smoothing, so it reads as chunky pixels.
_Avoid_: Low-res, retro filter

**Shadow**:
The darkening where sunlight doesn't reach, such as cliff shade, under trees or the side of a slope facing away from the sun. It moves with the light, unlike grass shapes.
_Avoid_: Shade, dark side
