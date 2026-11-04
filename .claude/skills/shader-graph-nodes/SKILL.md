---
name: shader-graph-nodes
description: Build and edit this project's Shader Graphs (Unity 6.3, Shader Graph 17.3) - write HLSL functions for Custom Function nodes, add or wire nodes, expose settings as material properties, add shadow keywords, tidy the layout, and check the result in the scene. Use when the user wants a new Shader Graph node or effect, wants a graph wired, organised or made tunable, asks why a graph edit doesn't show in the scene, or wants to move an effect from the HLSL toon shader into a graph.
---

# Shader Graph nodes

Effects are written as small HLSL functions and used in Shader Graph through **Custom Function** nodes (File mode). The graph wires them together and exposes the settings on the material. You write HLSL; you edit `.shadergraph` files only through `scripts/sg_graph.py`.

The user's `shader-graph-create-custom-node` skill does not apply here: it needs Shader Graph 17.5+ (this project has 17.3).

## Project files

Every Cliff Ledge material now uses a graph. Graph materials are named `<code material>SG.mat` (and `<code material>OutlineSG.mat`).

| File | Role |
|---|---|
| `Shaders/CliffLedgeNoise.hlsl` | All Custom Function code (streaks, contour, toon bands, patches, fog amount, outline offset, backdrop) |
| `Shaders/CliffLedgeRockSG.shadergraph` | Rocks and birch trunks: toon bands + stretched-FBM streaks (17 settings) |
| `Shaders/CliffLedgeGrassSG.shadergraph` | Everything else lit: toon bands + optional grass patches + fog amount (14 settings) |
| `Shaders/CliffLedgeOutlineSG.shadergraph` | Inverted-hull outline, used as an **extra material slot** (4 settings) |
| `Shaders/CliffLedgeBackdropSG.shadergraph` | Sky painting, **Sprite Unlit** target so it gets no fog (8 settings) |
| `Scripts/Editor/Art/CliffLedgeGraphMaterials.cs` | Called by the scene builder: swaps code materials for graph ones, creating a graph material from its code material only if it's missing (tuning is kept) |
| `Shaders/CliffLedgeToon.shader`, `CliffLedgeBackdrop.shader` | The original hand-written shaders; the builder still creates their materials as the source values |

All paths are under `Assets/_Game/Art/` except the editor script.

## Workflow

1. **Write the HLSL function** in the shared `.hlsl` file (rules below).
2. **Compile-check it** with a throwaway shader in `Assets/Temp/` (snippet below), then delete `Assets/Temp`.
3. **Save and close the graph window** if it is open. The window overwrites the file on its next save, and it may hold the user's unsaved work. Never edit the file while the window has unsaved changes.
4. **Back up the graph** to the scratchpad, then edit it with `sg_graph.py`. Run `python -I .claude/skills/shader-graph-nodes/scripts/sg_graph.py <graph> list` first to see properties, nodes, slots and what is wired.
5. **Reimport and check**: zero shader messages, and the expected property references exist (snippet below).
6. **Check in the scene**: capture with `Farm.EditorTools.Art.CliffLedgeSceneBuilder.Capture(path)` (capture twice; the first can miss new variants) and diff against a capture taken before the change. A change meant to be invisible should give 0 changed pixels.
7. **Reopen the graph** for the user with `AssetDatabase.OpenAsset` if they had it open.

## HLSL rules for Custom Function nodes

- Name functions `Name_float(...)`; the node's **Name** field is `Name` without `_float`.
- The node's inputs and outputs must be in **the same order and types** as the parameters. Outputs are `out` parameters, last.
- A node's `Boolean` input arrives as a float in HLSL: declare it `float` and test `> 0.5`.
- Name parameters after the user's property names (the user asked for this): property `Domain Warp` → parameter `DomainWarp`.
- Guard anything that needs the real renderer: `#if defined(SHADERGRAPH_PREVIEW)` with a fixed light direction for previews; otherwise `GetMainLight(TransformWorldToShadowCoord(posWS))`.
- Keep one shared file with an include guard; prefix helpers (`CL_Noise`, `CL_Fbm`) so they never clash with Unity's names.
- Keep each function one visible step (noise → mask → colour) so every node preview shows something meaningful.

## Making a graph tunable

- **Material values win.** A Blackboard default only seeds new materials and the graph's own previews. Changing it never changes an existing material, so the scene does not change. Tuning happens on the material (live) or, for values typed into nodes, in the graph followed by **Save Asset** (recompiles).
- So: every value the user may want to tune becomes a **property** wired into the node, with a slider range where one makes sense (`g.property(name, 'f', default, slider=(lo, hi))`).
- Give properties **good defaults** (the current look's values), never black or zero, or new materials and previews look broken.
- Unwired node inputs that only act as fallbacks should still hold sensible values (Vector3 colours are linear: `srgb_to_linear`).
- For several looks from one graph (rock face vs shelf), suggest a parent material plus **Material Variants**, so shared settings are tuned in one place.

## Gotchas found the hard way

- **Unlit graphs get no shadow keywords.** Without `_MAIN_LIGHT_SHADOWS`, `_MAIN_LIGHT_SHADOWS_CASCADE` and `_SHADOWS_SOFT` (`g.shadow_keywords()`: global, multi-compile, not exposed), `GetMainLight` reports no shadows. Check with `shader.keywordSpace.keywordNames`.
- **No outline pass.** A graph can't add a second pass. Outlines are a separate material (`CliffLedgeOutlineSG`: Render Face Back, no shadow casting, vertex Position pushed out along the normal) added as an extra material slot. An extra slot redraws the **last** submesh, so it only works when the outlined material is the last one.
- **Unlit graphs always get fog.** The `CliffFogAmount` node pre-compensates the colour so Fog Amount 0–1 works, but it can't undo full fog (fog factor 0). For things beyond fog end (the backdrop at ~380 units), use the **Sprite Unlit** target, which has no fog (`g.set_target(sub_target='sprite_unlit')`).
- **New node types:** to get the exact JSON of a node, property or target you haven't used yet, build it with Shader Graph's own code via reflection (`GraphData.AddNode`, `MultiJson.Serialize`) and copy it; don't guess.
- **Check a conversion against the code shaders at the same moment**: swap the code materials back, capture, restore, capture. Comparing against an old capture mixes in unrelated scene changes.
- **Rounding.** Moving a literal (`3.0`) into a property can shift a few pixels at line edges. That is expected; the look is the same.
- **Builder rebuilds.** `Farm > Art > Cliff Ledge` (stage 3+) ends with `CliffLedgeGraphMaterials.ApplyToOpenScene()`. A new graph or a new code material needs a mapping there, or a rebuild puts the code material back.
- **Restoring after a test swap:** match material slots by index, not by material, when a renderer uses the same material on two slots.
- **Shell quoting.** Long `unity command script eval` strings with nested quotes break easily; write Python helpers to files and call them.

## Snippets (`unity command script eval "..." --result-only`)

Save and close an open graph window:

```csharp
foreach (var w in UnityEngine.Resources.FindObjectsOfTypeAll<UnityEditor.EditorWindow>()) {
  if (w.GetType().Name != "MaterialGraphEditWindow") continue;
  if (w.hasUnsavedChanges) w.GetType().GetMethod("SaveAsset", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, System.Type.EmptyTypes, null).Invoke(w, null);
  w.Close(); return "saved and closed"; }
return "not open";
```

Reimport a graph and report errors and properties:

```csharp
var p = "Assets/_Game/Art/Shaders/CliffLedgeRockSG.shadergraph";
UnityEditor.AssetDatabase.ImportAsset(p, UnityEditor.ImportAssetOptions.ForceUpdate);
var s = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>(p);
var r = "messages=" + UnityEditor.ShaderUtil.GetShaderMessages(s).Length;
for (int i = 0; i < s.GetPropertyCount(); i++) r += " " + s.GetPropertyName(i);
return r;
```

Compile-check HLSL: a minimal URP shader in `Assets/Temp/` that includes `Lighting.hlsl` and the `.hlsl` file, calls each function from `frag`, then `ShaderUtil.GetShaderMessages` on it. Delete `Assets/Temp` and its `.meta` afterwards.

## Editing a graph with sg_graph.py

```python
import sys; sys.path.insert(0, '.claude/skills/shader-graph-nodes/scripts')
from sg_graph import Graph
g = Graph.load(path)
grp = g.group('Streaks', -1060, -200)                      # labelled box; reuses one with the same title
node = g.custom_function('CliffContour', g.any_custom_source(),
    inputs=[('Noise', 'f'), ('ContourLine', 'b'), ('Levels', 'f', 3.0)], outputs=[('Mask', 'f')],
    x=-560, y=-140, group=grp)
prop = g.property('Levels', 'f', 3.0, slider=(1, 8))       # kinds: 'f', 'b', 'v2', 'v3', 'color' ('#rrggbb')
# Also: g.texture_property / texture_property_node, g.uv_node, g.sample_texture_node, g.add_block('SurfaceDescription.Alpha'),
# g.set_target(render_face='back', cast_shadows=False, sub_target='sprite_unlit'), and Graph.load(other).strip() to start a
# new graph from an existing one (keeps its target and keywords; save it under a new name).
g.wire(g.property_node(prop, -560, 230, grp), 0, node, 'Levels')
g.wire(g.custom('CliffStreakNoise'), 'Noise', node, 'Noise')
g.wire(node, 'Mask', g.custom('CliffToonColor'), 'Mask')   # g.block('SurfaceDescription.BaseColor') for the output
g.save()
```

Lay graphs out left to right (inputs → steps → colour → output block), one group per step, property nodes next to the input they feed. You cannot see the graph, so after a layout change ask the user to check for overlaps.
