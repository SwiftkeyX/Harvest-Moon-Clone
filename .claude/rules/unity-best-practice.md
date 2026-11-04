---
paths:
  - "Assets/**"
---

# Unity best practice

Team conventions for Unity work in this project. They apply to work done by hand in the Editor and to editor scripts that build scenes or assets.

## Prefabs

Anything that appears in a scene more than once, or that we may reuse, is a prefab. Don't create it as a loose GameObject in the scene.

- **Where:** save prefabs in `Assets/_Game/Art/Prefabs/`, each with its `.meta`.
- **Variants** cover recolours and sizes of one prop (e.g. `Bush_Yellow`, `Birch_Large`). Don't make a copy of the prefab.
- **Nested prefabs** cover assemblies built from reusable parts (e.g. `Fence` holds `Post` and `Rail`).
- **Clean root:** the root's pivot is where the prop meets the ground, and its scale is 1. Offsets, rotation and size go on the children.
- **Separate assets:** meshes and materials stay their own assets (`Art/Meshes`, `Art/Materials`), so they're never embedded in the prefab.
- **Placing:** use `PrefabUtility.InstantiatePrefab`, never `Object.Instantiate`, so the instance stays linked to its prefab. Never unpack.
- **Minimal overrides:** an instance changes only its root transform. If instances need a different material or shape, make a variant instead of an override.
- **Stable GUIDs:** builders save with `PrefabUtility.SaveAsPrefabAsset` to the same path on every run and then destroy the temporary object. Never delete and recreate a prefab, or scene references break.
- **Final materials:** a builder sets each prefab's final materials (e.g. the Shader Graph versions) before saving it, rather than swapping them on instances in the scene afterwards.
- **Scene layout:** group instances under tidy parent objects (`Bushes`, `Trees`, `Rocks`…).
- **Edit the prefab, not the instance:** to change something that is a prefab instance in a scene, edit the prefab asset in Prefab Mode or with `PrefabUtility.LoadPrefabContents`/`SaveAsPrefabAsset`, or edit the right variant. That way every copy gets the change. Only placement (the root's position, rotation and scale) is edited on the instance. If you changed an instance by mistake, apply the change to the prefab or revert it, so no stray overrides are left.
- **Generated prefabs:** if an editor builder creates the prefab (e.g. the Cliff Ledge props from `CliffLedgePrefabs.cs`), change the builder code. The next rebuild overwrites hand edits to the asset.

These can stay plain scene objects: terrain meshes, camera and canvas setups, lighting, and one-off pieces that appear only once.
