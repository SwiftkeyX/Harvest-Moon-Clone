using UnityEngine;

namespace Farm.EditorTools.Art
{
    // Props that fill the scene: bushes, tall grass, flowers, clusters and the plot border.
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

        private static void Bush(Transform parent, Vector3 position, float size, bool orange, int variant)
        {
            var bush = Group(orange ? "Bush Orange" : "Bush", parent);
            bush.position = OnGround(position);
            AddShade(position.x, position.z, 0.25f * size, 0.25f * size, 0.55f * size + 0.2f, 0.7f);

            var mat = orange ? Mat("BushOrange", Palette.BushOrange, 2f) : Mat("BushGreen", Palette.BushGreen, 2f);
            var big = SaveMesh(LowPolyMesh.Blob("Bush" + variant % 3, 3, 7, 0.5f, 0.2f, 40 + variant % 3));
            var small = SaveMesh(LowPolyMesh.Blob("Bush" + (variant + 1) % 3, 3, 7, 0.5f, 0.2f, 40 + (variant + 1) % 3));
            Spawn(bush, "Body", big, mat, new Vector3(0f, 0.2f * size, 0f), Quaternion.Euler(0f, variant * 50f, 0f),
                new Vector3(size, size * 0.75f, size));
            Spawn(bush, "Lobe", small, mat, new Vector3(0.38f * size, 0.14f * size, -0.1f * size), Quaternion.Euler(0f, variant * 80f, 0f),
                new Vector3(size * 0.65f, size * 0.5f, size * 0.65f));
        }

        private static void TallGrass(Transform parent, Vector3 position, int seed)
        {
            var tuft = Group("Tall Grass", parent);
            tuft.position = OnGround(position);
            var blade = SaveMesh(LowPolyMesh.Cylinder("TallBlade", 3, 0.05f, 0f, 0.6f));
            var matA = Mat("TallGrassA", Palette.GrassDark, 1f);
            var matB = Mat("TallGrassB", new Color(0.46f, 0.72f, 0.34f), 1f);
            var random = new System.Random(seed);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f + Range(random, -15f, 15f);
                Spawn(tuft, "Blade", blade, i % 2 == 0 ? matA : matB,
                    Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0f, Range(random, 0.03f, 0.1f)),
                    Quaternion.Euler(0f, angle, 0f) * Quaternion.Euler(Range(random, 8f, 24f), 0f, 0f),
                    new Vector3(1f, Range(random, 0.8f, 1.4f), 1f));
            }
        }

        private static void FlowerClump(Transform parent, Vector3 position, int seed, int count = 3)
        {
            var clump = Group("Flowers", parent);
            clump.position = OnGround(position);
            Flowers(clump, seed, count, 0.14f);
        }

        // Flowers on thin stems, scattered inside a radius.
        private static void Flowers(Transform at, int seed, int count, float radius)
        {
            var stem = SaveMesh(LowPolyMesh.Cylinder("FlowerStem", 3, 0.015f, 0.015f, 0.2f));
            var head = SaveMesh(LowPolyMesh.Blob("Flower", 2, 5, 0.07f, 0f, 1));
            var stemMat = Mat("CropLeaf", Palette.CropLeaf, 1.5f);
            var heads = new[] { Mat("FlowerPink", Palette.FlowerPink, 1f), Mat("FlowerYellow", Palette.FlowerYellow, 1f),
                                Mat("FlowerWhite", Palette.FlowerWhite, 1f) };
            var random = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                var p = new Vector3(Range(random, -radius, radius), 0f, Range(random, -radius * 0.6f, radius * 0.6f));
                float tall = Range(random, 0.9f, 1.3f);
                Spawn(at, "Stem", stem, stemMat, p, Quaternion.identity, new Vector3(1f, tall, 1f));
                Spawn(at, "Flower", head, heads[(seed + i) % 3], p + new Vector3(0f, 0.2f * tall, 0f), Quaternion.identity, Vector3.one * 1.2f);
            }
        }

        private static void FlowerBed(Transform parent, Vector3 position, int seed)
        {
            var bed = Group("Flower Bed", parent);
            bed.position = OnGround(position);
            Spawn(bed, "Soil", SaveMesh(LowPolyMesh.Box("BedSoil", new Vector3(0.8f, 0.1f, 0.38f))), Mat("Soil", Palette.Soil, 1f), Vector3.zero);
            var top = Group("Flowers", bed, new Vector3(0f, 0.08f, 0f));
            Flowers(top, seed, 6, 0.3f);
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
                var rockMat = Mat("Rock", Palette.Rock, 2f);
                Rock(parent, rockMat, position, 0.7f * scale, 30 + seed);
                Bush(parent, position + new Vector3(-1.0f * scale, 0f, -0.3f * scale), 0.8f * scale, true, seed);
                TallGrass(parent, position + new Vector3(0.7f * scale, 0f, -0.7f * scale), seed);
            }
        }

        private static void BuildDressing()
        {
            var dress = Group("Dressing");

            // Bushes along the outside (north side) of the fence.
            float[] fenceX = { -2.0f, -0.2f, 1.5f, 3.2f };
            for (int i = 0; i < fenceX.Length; i++)
                Bush(dress, new Vector3(fenceX[i], 0f, 2.7f), 0.85f + 0.2f * (i % 2), i % 2 == 1, i);

            // Tall grass and flowers on both sides of the path.
            var random = new System.Random(3);
            int n = 0;
            for (float z = -5.0f; z < 1.5f; z += 0.9f, n++)
            {
                if (n % 3 == 1) FlowerClump(dress, new Vector3(-7.2f - Range(random, 0f, 0.25f), 0f, z), n, 3);
                else TallGrass(dress, new Vector3(-7.15f - Range(random, 0f, 0.3f), 0f, z), n);
                if (n % 3 == 0) FlowerClump(dress, new Vector3(-5.25f + Range(random, 0f, 0.25f), 0f, z + 0.3f), n + 7, 3);
                else TallGrass(dress, new Vector3(-5.2f + Range(random, 0f, 0.3f), 0f, z + 0.3f), n + 7);
            }

            // House: flower beds under the windows, bushes at the corners.
            FlowerBed(dress, new Vector3(-7.15f, 0f, 2.05f), 5);
            FlowerBed(dress, new Vector3(-5.15f, 0f, 2.05f), 6);
            Bush(dress, new Vector3(-8.15f, 0f, 2.45f), 0.9f, false, 1);
            Bush(dress, new Vector3(-8.05f, 0f, 5.1f), 1.0f, true, 2);
            Bush(dress, new Vector3(-4.5f, 0f, 5.2f), 0.9f, false, 0);

            // Clusters around the edges.
            Cluster(dress, new Vector3(-11.8f, 0f, -1.2f), true, 1, 1.2f);
            Cluster(dress, new Vector3(11.4f, 0f, 4.2f), false, 2);
            Cluster(dress, new Vector3(-3.5f, 0f, 9.2f), false, 3);
            Cluster(dress, new Vector3(8.8f, 0f, 8.6f), true, 4);

            // Framing: big shapes cut by the screen edges.
            AutumnTree(dress, new Vector3(-8.0f, 0f, -5.2f), 15, 1.5f);
            AutumnTree(dress, new Vector3(9.0f, 0f, -4.6f), 16, 1.5f);
            Bush(dress, new Vector3(7.4f, 0f, -5.8f), 2.0f, true, 2);
            Pine(dress, new Vector3(3.2f, 0f, 10.6f), 1.6f);
            Pine(dress, new Vector3(-6.8f, 0f, 10.4f), 1.5f);
            Bush(dress, new Vector3(0.2f, 0f, 9.6f), 1.8f, false, 1);
        }
    }
}
