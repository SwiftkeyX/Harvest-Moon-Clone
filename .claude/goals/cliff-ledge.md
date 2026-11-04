# Goal: Cliff Ledge art scene

Build a new Unity scene from scratch that recreates the art style and layout of the
reference image `.claude/references/cliff-ledge.png`: a pixelated, high-angle view of
an autumn cliff ledge.

## Setup
- You start on `feature/art-look-pass` with uncommitted work. Commit it there first.
  Never stash it or throw it away. Leave `.claude/references/` and `.claude/goals/` out
  of that commit.
- Commit `.claude/references/cliff-ledge.png` and `.claude/goals/cliff-ledge.md`
  alone to `main`.
- Branch `feature/art-cliff-ledge` off `main`.
- Start from zero. Don't use or copy any existing terrain, grass, cliff, foliage,
  shader, material or art-tool script. Build everything new, with names starting with
  `CliffLedge`, in the matching `Assets/_Game` folders.
- New scene: `Assets/_Game/Scenes/CliffLedge.unity`. Follow CLAUDE.md for `.meta` files
  and the user's save.

## Work in this order. Take a Game view capture after each stage.
1. **Graybox:** camera, ledge, cliff and background planes in plain grey. Get the
   composition right before adding any colour.
2. **Terrain:** a grass plateau curving from top-left to centre-right. It ends at a
   sheer cliff dropping to the right, with a lower rock shelf along the bottom.
3. **Shaders and rendering, only in `CliffLedge.unity`:**
   - Make a new CliffLedge renderer and add it to the renderer lists of both the PC
     and Mobile pipeline assets. Only this scene's camera uses it; the default
     renderer stays the same.
   - Keep fog and lighting in this scene's own settings.
   - Pixelated look: render at low resolution and scale up with point filtering.
   - Flat toon shading with 2-3 colour bands and no specular.
   - Cliff material: pale grey with dark brown streaks that follow the rock layers.
   - Warm peach fog that fades the distant valley and mountains to sketch lines.
4. **Decoration:**
   - One dark-green pine.
   - Yellow birches with white trunks.
   - Clumps of round olive and yellow bushes.
   - A few orange-red leaf shapes and one mauve boulder.
   - Small yellow flowers and white pebbles.
   - A red-brown wooden fence at the bottom-left.
   - A placeholder character in a red coat with a navy hood, standing mid-ledge.

## Done when ALL of these are true
- Captures from `CliffLedge.unity` and the reference, placed side by side, match on:
  camera angle, where the cliff edge falls, the left/right split between land and sky,
  the main colours (grass, cliff, sky, foliage), visible pixel size, and every prop above.
- A capture of `FarmView.unity` looks the same as before this work: not pixelated,
  no peach fog.
- The comparison image and a short list of anything still off are in the scratchpad.
- `unity recompile --strict` reports 0 errors and 0 warnings, and the EditMode tests pass.
- The user's save file is unchanged.

Give up and report back if you're still not converging after 8 compare-and-fix rounds.
Make no other commits and don't push.
