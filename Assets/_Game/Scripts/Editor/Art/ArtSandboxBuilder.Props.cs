using UnityEngine;

namespace Farm.EditorTools.Art
{
    // Props and ground shapes. Variation comes from shapes and placement, never from gradients or noise.
    public static partial class ArtSandboxBuilder
    {
        private static void BuildPlotBorder()
        {
            var border = Group("Plot Border");
            var wood = Mat("WoodDark", Palette.WoodDark, 1.5f);
            var h = SaveMesh(LowPolyMesh.Box("BorderH", new Vector3(6.2f, 0.12f, 0.12f)));
            var v = SaveMesh(LowPolyMesh.Box("BorderV", new Vector3(0.12f, 0.12f, 2.96f)));
            Spawn(border, "Front", h, wood, new Vector3(0.5f, 0f, -1.54f));
            Spawn(border, "Back", h, wood, new Vector3(0.5f, 0f, 1.54f));
            Spawn(border, "Left", v, wood, new Vector3(-2.54f, 0f, 0f));
            Spawn(border, "Right", v, wood, new Vector3(3.54f, 0f, 0f));
        }

        // Large flat grass patches: irregular polygons with hard edges and no outline.
        private static void BuildPatches()
        {
            var patches = Group("Grass Patches");
            var dark = Mat("GrassDark", Palette.GrassDark, 0f);
            var light = Mat("GrassLight", Palette.GrassLight, 0f);
            // x, z, radius, stretch, dark?
            float[,] list =
            {
                { -1.0f, -3.4f, 1.5f, 1.7f, 1 }, { 8.6f, 1.6f, 2.2f, 1.2f, 0 }, { 1.5f, 5.2f, 2.3f, 1.4f, 0 },
                { -10.2f, -2.5f, 2.2f, 1.2f, 1 }, { -1.5f, 8.2f, 2.4f, 1.5f, 1 }, { 10.2f, 6.5f, 2.4f, 1.2f, 0 },
                { -3.5f, -3.9f, 1.3f, 1.3f, 0 }, { -9.5f, 8.0f, 2.2f, 1.3f, 0 }, { 5.0f, 9.5f, 2.0f, 1.5f, 1 },
            };
            for (int i = 0; i < list.GetLength(0); i++)
            {
                var mesh = SaveMesh(Patch("GrassPatch" + i, list[i, 2], list[i, 3], 8 + i % 3, 60 + i));
                Spawn(patches, "Patch", mesh, list[i, 4] > 0.5f ? dark : light, new Vector3(list[i, 0], 0.012f + 0.003f * i, list[i, 1]));
            }
        }

        private static Mesh Patch(string name, float radius, float stretch, int sides, int seed)
        {
            var random = new System.Random(seed);
            var m = new LowPolyMesh();
            var ring = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = (i + Range(random, -0.2f, 0.2f)) * Mathf.PI * 2f / sides;
                float r = radius * Range(random, 0.7f, 1.15f);
                ring[i] = new Vector3(Mathf.Cos(a) * r * stretch, 0f, Mathf.Sin(a) * r);
            }
            for (int i = 0; i < sides; i++)
                m.Tri(Vector3.zero, ring[(i + 1) % sides], ring[i]); // clockwise from above = faces up
            return m.Build(name, true);
        }

        private static void Bush(Transform parent, Vector3 position, float size, bool orange, int variant)
        {
            var bush = Group(orange ? "Bush Orange" : "Bush", parent);
            bush.position = position;
            var mat = orange ? Mat("BushOrange", Palette.BushOrange, 2f) : Mat("BushGreen", Palette.BushGreen, 2f);
            var big = SaveMesh(LowPolyMesh.Blob("Bush" + variant % 3, 3, 7, 0.5f, 0.2f, 40 + variant % 3));
            var small = SaveMesh(LowPolyMesh.Blob("Bush" + (variant + 1) % 3, 3, 7, 0.5f, 0.2f, 40 + (variant + 1) % 3));
            Spawn(bush, "Body", big, mat, new Vector3(0f, 0.2f * size, 0f), Quaternion.Euler(0f, variant * 50f, 0f),
                new Vector3(size, size * 0.75f, size));
            Spawn(bush, "Lobe", small, mat, new Vector3(0.38f * size, 0.14f * size, -0.1f * size), Quaternion.Euler(0f, variant * 80f, 0f),
                new Vector3(size * 0.65f, size * 0.5f, size * 0.65f));
        }

        // A chunky clump of a few big blades.
        private static void TallGrass(Transform parent, Vector3 position, int seed)
        {
            var tuft = Group("Tall Grass", parent);
            tuft.position = position;
            var blade = SaveMesh(LowPolyMesh.Cylinder("TallBlade", 3, 0.11f, 0f, 0.8f));
            var matA = Mat("TallGrassA", Palette.GrassDark, 1f);
            var matB = Mat("TallGrassB", new Color(0.30f, 0.58f, 0.28f), 1f);
            for (int i = 0; i < 5; i++)
            {
                float angle = i * 72f + seed * 20f;
                Spawn(tuft, "Blade", blade, i % 2 == 0 ? matA : matB,
                    Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0f, 0.08f),
                    Quaternion.Euler(0f, angle, 0f) * Quaternion.Euler(14f, 0f, 0f),
                    new Vector3(1f, 0.8f + 0.2f * (i % 3), 1f));
            }
        }

        private static void FlowerClump(Transform parent, Vector3 position, int seed, int count = 4)
        {
            var clump = Group("Flowers", parent);
            clump.position = position;
            Flowers(clump, seed, count, 0.25f);
        }

        private static void Flowers(Transform at, int seed, int count, float radius)
        {
            var stem = SaveMesh(LowPolyMesh.Cylinder("FlowerStem", 3, 0.03f, 0.03f, 0.3f));
            var head = SaveMesh(LowPolyMesh.Blob("Flower", 2, 5, 0.15f, 0f, 1));
            var stemMat = Mat("CropLeaf", Palette.CropLeaf, 1.5f);
            var heads = new[] { Mat("FlowerPink", Palette.FlowerPink, 1f), Mat("FlowerYellow", Palette.FlowerYellow, 1f),
                                Mat("FlowerWhite", Palette.FlowerWhite, 1f) };
            var random = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count + Range(random, -0.4f, 0.4f);
                var p = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius * 0.7f);
                float tall = 0.9f + 0.15f * (i % 3);
                Spawn(at, "Stem", stem, stemMat, p, Quaternion.identity, new Vector3(1f, tall, 1f));
                Spawn(at, "Flower", head, heads[(seed + i) % 3], p + new Vector3(0f, 0.3f * tall, 0f), Quaternion.identity, new Vector3(1f, 0.7f, 1f));
            }
        }

        private static void FlowerBed(Transform parent, Vector3 position, int seed)
        {
            var bed = Group("Flower Bed", parent);
            bed.position = position;
            Spawn(bed, "Soil", SaveMesh(LowPolyMesh.Box("BedSoil", new Vector3(0.8f, 0.1f, 0.38f))), Mat("Soil", Palette.Soil, 1f), Vector3.zero);
            var top = Group("Flowers", bed, new Vector3(0f, 0.08f, 0f));
            Flowers(top, seed, 5, 0.28f);
        }

        // One big, one medium and one small item grouped together.
        private static void Cluster(Transform parent, Vector3 position, bool treeKind, int seed, float scale = 1f)
        {
            if (treeKind)
            {
                AutumnTree(parent, position, seed, 1.1f * scale);
                Bush(parent, position + new Vector3(1.3f * scale, 0f, -0.6f * scale), 0.9f * scale, seed % 2 == 0, seed);
                FlowerClump(parent, position + new Vector3(0.5f * scale, 0f, -1.4f * scale), seed, 4);
            }
            else
            {
                Rock(parent, Mat("Rock", Palette.Rock, 2f), position, 0.7f * scale, 30 + seed);
                Bush(parent, position + new Vector3(-1.0f * scale, 0f, -0.3f * scale), 0.8f * scale, true, seed);
                TallGrass(parent, position + new Vector3(0.7f * scale, 0f, -0.7f * scale), seed);
            }
        }

        private static void BuildDressing()
        {
            var dress = Group("Dressing");

            // Bushes along the north side of the fence.
            float[] fenceX = { -2.0f, -0.2f, 1.5f, 3.2f };
            for (int i = 0; i < fenceX.Length; i++)
                Bush(dress, new Vector3(fenceX[i], 0f, 2.7f), 0.85f + 0.2f * (i % 2), i % 2 == 1, i);

            // A few big tall-grass clumps along the path.
            float[] left = { -4.2f, -1.6f, 0.7f };
            float[] right = { -3.0f, -0.6f, 1.2f };
            for (int i = 0; i < 3; i++)
            {
                TallGrass(dress, new Vector3(-7.3f, 0f, left[i]), i);
                TallGrass(dress, new Vector3(-5.1f, 0f, right[i]), i + 3);
            }
            FlowerClump(dress, new Vector3(-7.4f, 0f, -3.0f), 1, 4);

            // House: flower beds under the windows, bushes at the corners.
            FlowerBed(dress, new Vector3(-7.15f, 0f, 2.05f), 5);
            FlowerBed(dress, new Vector3(-5.25f, 0f, 2.05f), 6);
            Bush(dress, new Vector3(-8.15f, 0f, 2.45f), 0.9f, false, 1);
            Bush(dress, new Vector3(-8.05f, 0f, 5.1f), 1.0f, true, 2);
            Bush(dress, new Vector3(-4.5f, 0f, 5.2f), 0.9f, false, 0);

            // Clusters around the edges.
            Cluster(dress, new Vector3(-11.8f, 0f, -1.2f), true, 1, 1.2f);
            Cluster(dress, new Vector3(11.4f, 0f, 4.2f), false, 2);
            Cluster(dress, new Vector3(-3.5f, 0f, 9.2f), false, 3);
            Cluster(dress, new Vector3(8.8f, 0f, 8.6f), true, 4);
            FlowerClump(dress, new Vector3(2.0f, 0f, 4.0f), 7, 5);
            FlowerClump(dress, new Vector3(9.5f, 0f, -0.2f), 8, 4);

            // Framing: big shapes cut by the screen edges.
            AutumnTree(dress, new Vector3(-8.0f, 0f, -5.2f), 15, 1.3f);
            AutumnTree(dress, new Vector3(9.0f, 0f, -4.6f), 16, 1.3f);
            Bush(dress, new Vector3(7.4f, 0f, -5.8f), 1.7f, true, 2);
            Pine(dress, new Vector3(3.2f, 0f, 10.6f), 1.6f);
            Pine(dress, new Vector3(-6.8f, 0f, 10.4f), 1.5f);
            Bush(dress, new Vector3(0.2f, 0f, 9.6f), 1.8f, false, 1);
        }
    }
}
