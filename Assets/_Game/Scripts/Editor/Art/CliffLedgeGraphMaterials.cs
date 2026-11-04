using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Farm.EditorTools.Art
{
    // Puts the Shader Graph materials on the Cliff Ledge scene in place of the hand-written shader's materials.
    // Each code material "X" gets a graph material "XSG" (and "XOutlineSG" when it has an outline). A graph material
    // is created from its code material's values only when it doesn't exist yet, so tuning done on it is kept.
    internal static class CliffLedgeGraphMaterials
    {
        const string MatDir = "Assets/_Game/Art/Materials/";
        const string ShaderDir = "Assets/_Game/Art/Shaders/";

        public static void ApplyToOpenScene()
        {
            var renderers = new List<Renderer>();
            // Prefab instances already carry the graph materials from their prefab asset.
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                if (!PrefabUtility.IsPartOfPrefabInstance(r)) renderers.Add(r);
            Apply(renderers);
            AssetDatabase.SaveAssets();
        }

        // Swaps the materials on an object and its children, such as a prop before it is saved as a prefab.
        public static void ApplyTo(GameObject root) => Apply(root.GetComponentsInChildren<Renderer>(true));

        static void Apply(IList<Renderer> renderers)
        {
            var toonCode = Shader.Find("CliffLedge/Toon");
            var backdropCode = Shader.Find("CliffLedge/Backdrop");
            var swap = new Dictionary<Material, Material>();
            var outline = new Dictionary<Material, Material>();

            foreach (var r in renderers)
            foreach (var m in r.sharedMaterials)
            {
                if (m == null || swap.ContainsKey(m)) continue;
                if (m.shader == toonCode)
                {
                    swap[m] = m.GetFloat("_StreakWidth") > 0 ? RockFrom(m) : ToonFrom(m);
                    if (m.GetFloat("_OutlineWidth") > 0) outline[m] = OutlineFrom(m);
                }
                else if (m.shader == backdropCode) swap[m] = BackdropFrom(m);
            }

            foreach (var r in renderers)
            {
                var mats = new List<Material>(r.sharedMaterials);
                if (mats.Count == 0) continue;
                var codeLast = mats[mats.Count - 1];
                bool changed = false;
                for (int i = 0; i < mats.Count; i++)
                    if (mats[i] != null && swap.TryGetValue(mats[i], out var g)) { mats[i] = g; changed = true; }
                // An extra material slot redraws the last submesh, which is where the outlined material sits.
                if (codeLast != null && outline.TryGetValue(codeLast, out var o) && !mats.Contains(o)) { mats.Add(o); changed = true; }
                if (changed) r.sharedMaterials = mats.ToArray();
            }
        }

        static Material Get(Material code, string suffix, string graph, out bool created)
        {
            string path = MatDir + code.name + suffix + ".mat";
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderDir + graph + ".shadergraph");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            created = m == null;
            if (created) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            else if (m.shader != shader) m.shader = shader;
            return m;
        }

        static void Bands(Material code, Material m)
        {
            m.SetColor("_Base_Color", code.GetColor("_BaseColor"));
            m.SetColor("_Shade_Color", code.GetColor("_ShadeColor"));
            m.SetColor("_Highlight_Color", code.GetColor("_HighlightColor"));
            m.SetFloat("_Shade_Threshold", code.GetFloat("_ShadeThreshold"));
            m.SetFloat("_Highlight_Threshold", code.GetFloat("_HighlightThreshold"));
            m.SetFloat("_Receive_Shadows", code.GetFloat("_ShadowShade") > 0.5f ? 1f : 0f);
            m.SetFloat("_Fog_Amount", code.GetFloat("_FogAmount"));
        }

        static Material ToonFrom(Material code)
        {
            var m = Get(code, "SG", "CliffLedgeGrassSG", out bool created);
            if (!created) return m;
            Bands(code, m);
            m.SetFloat("_Patches", code.GetFloat("_PatchAmount") > 0 ? 1f : 0f);
            m.SetColor("_Patch_Color", code.GetColor("_PatchColor"));
            m.SetFloat("_Patch_Scale", code.GetFloat("_PatchScale"));
            m.SetFloat("_Patch_Amount", code.GetFloat("_PatchAmount"));
            m.SetFloat("_Patch_Layers", 3f);
            m.SetFloat("_Patch_Warp", code.GetFloat("_PatchWarpOn") > 0.5f && code.GetFloat("_PatchWarp") > 0 ? 1f : 0f);
            m.SetFloat("_Patch_Warp_Strength", code.GetFloat("_PatchWarp"));
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material RockFrom(Material code)
        {
            var m = Get(code, "SG", "CliffLedgeRockSG", out bool created);
            if (!created) return m;
            Bands(code, m);
            m.SetColor("_Streak_Color", code.GetColor("_StreakColor"));
            m.SetVector("_Stretch", code.GetVector("_StreakStretch"));
            m.SetFloat("_Line_Width", code.GetFloat("_StreakWidth"));
            m.SetFloat("_Levels", code.GetFloat("_StreakLevels"));
            m.SetFloat("_Warp_Strength", code.GetFloat("_StreakWarp"));
            m.SetFloat("_Warp_Size", code.GetFloat("_StreakWarpScale"));
            m.SetFloat("_Fill_Cutoff", code.GetFloat("_StreakFill"));
            m.SetFloat("_Contour_Line", code.GetFloat("_StreakOn"));
            m.SetFloat("_Domain_Warp", code.GetFloat("_StreakWarpOn"));
            m.SetFloat("_Layers", 3f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material OutlineFrom(Material code)
        {
            var m = Get(code, "OutlineSG", "CliffLedgeOutlineSG", out bool created);
            if (!created) return m;
            m.SetFloat("_Outline", 1f);
            m.SetFloat("_Outline_Width", code.GetFloat("_OutlineWidth"));
            m.SetColor("_Outline_Color", code.GetColor("_OutlineColor"));
            m.SetFloat("_Fog_Amount", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material BackdropFrom(Material code)
        {
            var m = Get(code, "SG", "CliffLedgeBackdropSG", out bool created);
            // The painted texture is regenerated on every build, so always point at the current one.
            m.SetTexture("_Backdrop_Texture", code.GetTexture("_BaseMap"));
            if (created)
            {
                m.SetColor("_Tint", code.GetColor("_BaseColor"));
                m.SetFloat("_Alpha_Cutoff", code.GetFloat("_Cutoff"));
            }
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
