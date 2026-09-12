#if UNITY_EDITOR
using System;
using UnityEngine;

namespace PitStriker.EditorTools
{
    internal static class VillageVegetationBuilder
    {
        static float Range(System.Random r, float a, float b) => a + (b - a) * (float)r.NextDouble();
        static Vector3 Direction(float angle) => new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));

        internal static void Build(Transform parent, Material grass, Material canopy, Material palmFoliage, Material bark)
        {
            // World coordinates agree with the imported scenery after Unity's FBX axis conversion.
            // Grass is batched in 8 m cells, rather than thousands of GameObjects.
            for (int side = -1; side <= 1; side += 2) for (int row = 0; row < 8; row++)
            {
                var r = new System.Random(1709 + row * 47 + side * 13);
                var high = new VillageMeshBuilder(); var low = new VillageMeshBuilder();
                for (int i = 0; i < 380; i++)
                {
                    float z = -9 + row * 8 + Range(r, 0, 8);
                    // 75% wider fairway: edge moved from 2.4m to 4.2m
                    float edge = 4.2f + .70f * Mathf.Sin(z * .39f) + .38f * Mathf.Sin(z * 1.2f);
                    float outer = r.NextDouble() < .85 ? edge + 5.5f : 16.5f;
                    float x = side * Range(r, edge, outer);
                    // Gaps and islands prevent the continuous hedgerow look.
                    float patch = Mathf.PerlinNoise(x * .48f + 40, z * .48f + 40);
                    if (patch < .26f || (Mathf.Abs(x) > 11.5f && r.NextDouble() < .45)) continue;
                    Vector3 root = new Vector3(x, .012f, z);
                    float height = Range(r, .08f, .19f), shade = Range(r, .75f, 1f);
                    double typeRoll = r.NextDouble();

                    // Distinct flora matching reference image: Rosettes, Creeping clumps, Clovers, Weed flowers, Grain stalks, and Multi-layered blades
                    if (typeRoll < .08)
                    {
                        // Broadleaf rosette weeds (plantain / wild dandelions)
                        float rad = Range(r, .055f, .13f);
                        int leaves = (int)Range(r, 5, 8);
                        high.Rosette(root, rad, leaves, Range(r, .01f, .03f), shade);
                        if (i % 2 == 0) low.Rosette(root, rad * 1.1f, Mathf.Max(4, leaves - 2), Range(r, .01f, .03f), shade);
                    }
                    else if (typeRoll < .22)
                    {
                        // Low creeping star-tufts along fairway verge
                        float spread = Range(r, .06f, .15f);
                        int count = (int)Range(r, 4, 7);
                        high.CreepingClump(root, count, spread, height * .8f, shade);
                        if (i % 2 == 0) low.CreepingClump(root, Mathf.Max(3, count - 2), spread * 1.15f, height * .8f, shade);
                    }
                    else if (typeRoll < .32)
                    {
                        // Wild clover / oxalis patches
                        float cRad = Range(r, .035f, .065f);
                        high.Clover(root, cRad, shade);
                        if (i % 2 == 0) low.Clover(root, cRad * 1.2f, shade);
                    }
                    else if (typeRoll < .39)
                    {
                        // Wild weed flowering stalks with golden blossom
                        float fH = Range(r, .10f, .20f);
                        high.WeedFlower(root, fH, shade);
                        if (i % 2 == 0) low.WeedFlower(root, fH, shade);
                    }
                    else if (typeRoll < .46)
                    {
                        // Tall slender wild grass grain/seedhead stalks
                        float gH = Range(r, .14f, .24f);
                        high.GrainStalk(root, gH, shade);
                        if (i % 2 == 0) low.GrainStalk(root, gH, shade);
                    }
                    else
                    {
                        // Multi-layered curved grass blades (short understory + arching tall blades)
                        int bladeCount = (int)Range(r, 4, 6);
                        for (int j = 0; j < bladeCount; j++)
                        {
                            Vector3 d = Direction(Range(r, 0, Mathf.PI * 2));
                            Vector3 p = root + d * Range(r, 0, .045f);
                            float layerScale = (j % 2 == 0) ? Range(r, .55f, .85f) : Range(r, .95f, 1.35f);
                            float h = height * layerScale, width = Range(r, .014f, .030f);
                            float bend = h * (j % 2 == 0 ? .35f : .65f);
                            high.Blade(p, d, h, width, bend, 3, shade * (0.88f + 0.12f * Mathf.Sin(j)));
                            if (j % 2 == 0) low.Blade(p, d, h, width * 1.35f, bend * .9f, 2, shade);
                        }
                    }
                }

                // Extra foreground hero cluster around the launch zone (side -1, row 0-1)
                if (side == -1 && (row == 0 || row == 1))
                {
                    var heroR = new System.Random(8841 + row * 29);
                    for (int h = 0; h < 45; h++)
                    {
                        float hx = Range(heroR, -7.35f, -3.3f);
                        float hz = Range(heroR, -4.5f, 3.5f);
                        Vector3 hRoot = new Vector3(hx, .012f, hz);
                        float hShade = Range(heroR, .8f, 1f);
                        double heroType = heroR.NextDouble();
                        if (heroType < .30)
                        {
                            high.Rosette(hRoot, Range(heroR, .07f, .15f), (int)Range(heroR, 6, 9), .02f, hShade);
                            if (h % 2 == 0) low.Rosette(hRoot, Range(heroR, .07f, .15f), 5, .02f, hShade);
                        }
                        else if (heroType < .55)
                        {
                            high.CreepingClump(hRoot, 6, Range(heroR, .08f, .16f), Range(heroR, .07f, .13f), hShade);
                            if (h % 2 == 0) low.CreepingClump(hRoot, 4, Range(heroR, .08f, .16f), Range(heroR, .07f, .13f), hShade);
                        }
                        else if (heroType < .72)
                        {
                            high.Clover(hRoot, Range(heroR, .04f, .075f), hShade);
                            if (h % 2 == 0) low.Clover(hRoot, Range(heroR, .04f, .075f), hShade);
                        }
                        else if (heroType < .86)
                        {
                            high.WeedFlower(hRoot, Range(heroR, .11f, .22f), hShade);
                        }
                        else
                        {
                            high.GrainStalk(hRoot, Range(heroR, .14f, .25f), hShade);
                        }
                    }
                }

                var tile = new GameObject("Meadow_" + side + "_" + row); tile.transform.SetParent(parent, false);
                var near = high.Emit(tile.name + "_Near", tile.transform, grass);
                var far = low.Emit(tile.name + "_Far", tile.transform, grass);
                var lod = tile.AddComponent<LODGroup>();
                lod.SetLODs(new[] { new LOD(.18f, new[] { near }), new LOD(.025f, new[] { far }) });
                lod.RecalculateBounds();
            }

            var random = new System.Random(2193);

            // =========================================================================
            // 1. GRAND HERO BANYAN TREE (Foreground right, matching Concept Reference Art!)
            // Shifted back to frame the top-right camera view with high overarching sunlit canopy
            // =========================================================================
            HeroBanyanTree(parent, new Vector3(8.4f, 0f, -3.2f), 11.8f, canopy, bark);

            // =========================================================================
            // 2. LAYERED TROPICAL BROADLEAF FOREST (Lush volumetric canopies framing the scene)
            // =========================================================================
            // Left flank perimeter trees (behind the house and stone wall)
            for (int i = 0; i < 24; i++)
            {
                float x = -Range(random, 16f, 44f);
                float z = Range(random, -4f, 68f);
                if (x > -23f && z > 2f && z < 20f) x -= 6f; // Preserve house visibility from launch camera
                float y = 0f;
                Broadleaf(parent, new Vector3(x, y, z), Range(random, 6.5f, 11.0f), i, canopy, bark);
            }
            // Right flank perimeter trees (deep tropical jungle backdrop)
            for (int i = 0; i < 24; i++)
            {
                float x = Range(random, 16f, 45f);
                float z = Range(random, -4f, 68f);
                float y = 0f;
                Broadleaf(parent, new Vector3(x, y, z), Range(random, 6.5f, 11.0f), i + 30, canopy, bark);
            }
            // Dense horizon mountain-framing canopy trees (continuous tropical rainforest horizon)
            for (int i = 0; i < 28; i++)
            {
                float x = Range(random, -42f, 42f);
                float z = Range(random, 50f, 72f);
                Broadleaf(parent, new Vector3(x, 0f, z), Range(random, 8.5f, 13.5f), i + 80, canopy, bark);
            }

            // =========================================================================
            // 3. AUTHENTIC CURVED COCONUT PALMS (Cascading feather fronds, coconut clusters, ringed bark)
            // Placed prominently so crowns rise high above house roof and frame the fairway
            // =========================================================================
            Vector3[] palms = {
                // Rising above and framing the authentic village house on left
                new Vector3(-9.8f, 0, 8.5f),
                new Vector3(-12.2f, 0, 14.5f),
                new Vector3(-8.8f, 0, 20.5f),
                new Vector3(-8.6f, 0, 0.8f),
                new Vector3(-14.5f, 0, 26.5f),
                new Vector3(-13.0f, 0, 34.0f),
                // Framing the right verge along the fairway
                new Vector3(9.2f, 0, 5.5f),
                new Vector3(10.5f, 0, 13.5f),
                new Vector3(11.2f, 0, 21.5f),
                new Vector3(11.5f, 0, 29.5f),
                new Vector3(13.8f, 0, 38.0f),
                // Framing the background paddock fence and distant mountains
                new Vector3(-7.5f, 0, 42.5f),
                new Vector3(7.5f, 0, 43.0f),
                new Vector3(-14.0f, 0, 48.5f),
                new Vector3(14.0f, 0, 49.0f),
                new Vector3(-20.0f, 0, 56.0f),
                new Vector3(20.0f, 0, 57.0f),
                new Vector3(0.0f, 0, 58.0f),
                new Vector3(-28.0f, 0, 62.0f),
                new Vector3(28.0f, 0, 63.0f)
            };
            for (int i = 0; i < palms.Length; i++)
            {
                float h = 10.2f + (i % 4) * 0.9f + (float)random.NextDouble() * 0.8f;
                Vector3 p = palms[i];
                p.y = 0f;
                Palm(parent, p, h, i, palmFoliage, bark);
            }
        }

        static void HeroBanyanTree(Transform parent, Vector3 position, float height, Material foliage, Material bark)
        {
            var trunk = new VillageMeshBuilder();
            var leaves = new VillageMeshBuilder();

            Vector3 fork = position + Vector3.up * (height * 0.45f);
            trunk.FlaredTrunk(position, fork, 0.40f, 0.22f, 10);

            // Boughs sweeping upward and arching out over the roadside
            Vector3[] boughDirs = {
                new Vector3(-1.08f, 0.85f, 0.30f),  // Sweeps out high toward fairway
                new Vector3(0.60f, 0.90f, -0.50f),  // Reaches back toward camera
                new Vector3(0.85f, 0.80f, 0.40f),   // Flanks right
                new Vector3(-0.35f, 1.05f, 0.80f),  // Reaches forward along course
                new Vector3(0.05f, 1.25f, 0.05f)    // Central vertical bough
            };

            for (int b = 0; b < boughDirs.Length; b++)
            {
                Vector3 boughEnd = fork + boughDirs[b] * (height * 0.35f);
                trunk.Tube(fork, boughEnd, 0.18f, 0.07f, 8);

                // Volumetric foliage masses
                leaves.FoliagePuff(boughEnd, height * 0.22f, 0.95f, true);
                leaves.FoliagePuff(boughEnd + boughDirs[b] * 0.7f + Vector3.up * 0.25f, height * 0.19f, 1.05f, true);

                for (int s = 0; s < 2; s++)
                {
                    float angle = s * 2.2f + b * 1.3f;
                    Vector3 subDir = new Vector3(Mathf.Cos(angle), 0.65f, Mathf.Sin(angle)).normalized;
                    Vector3 subEnd = boughEnd + subDir * (height * 0.18f);
                    trunk.Tube(boughEnd, subEnd, 0.07f, 0.025f, 6);
                    leaves.FoliagePuff(subEnd, height * 0.16f, 1.0f, true);
                }
            }

            // High central sunlit canopy dome
            leaves.FoliagePuff(fork + Vector3.up * (height * 0.42f), height * 0.28f, 1.15f, true);
            leaves.FoliagePuff(fork + Vector3.up * (height * 0.26f), height * 0.32f, 0.90f, true);

            var obj = new GameObject("Hero_Banyan_Tree");
            obj.transform.SetParent(parent, false);
            trunk.Emit("Hero_Trunk", obj.transform, bark);
            leaves.Emit("Hero_Canopy", obj.transform, foliage);
        }

        static void Broadleaf(Transform parent, Vector3 position, float height, int seed, Material foliage, Material bark)
        {
            var random = new System.Random(seed + 841);
            var trunk = new VillageMeshBuilder(); var leaves = new VillageMeshBuilder(); var distant = new VillageMeshBuilder();

            Vector3 fork = position + Vector3.up * (height * 0.48f) + new Vector3(Range(random, -0.3f, 0.3f), 0, Range(random, -0.3f, 0.3f));
            trunk.FlaredTrunk(position, fork, Range(random, 0.32f, 0.45f), Range(random, 0.18f, 0.25f), 8);

            // Stylized central canopy dome puffs directly above the fork
            float centralRadius = height * 0.34f;
            leaves.FoliagePuff(fork + Vector3.up * (height * 0.24f), centralRadius * 1.15f, 0.90f, true);
            distant.FoliagePuff(fork + Vector3.up * (height * 0.24f), centralRadius * 1.15f, 0.90f, false);

            leaves.FoliagePuff(fork + Vector3.up * (height * 0.42f), centralRadius * 0.95f, 1.08f, true);
            distant.FoliagePuff(fork + Vector3.up * (height * 0.42f), centralRadius * 0.95f, 1.08f, false);

            int branchCount = 4 + (seed % 2);
            for (int branch = 0; branch < branchCount; branch++)
            {
                float angle = branch * (Mathf.PI * 2f / branchCount) + (float)random.NextDouble() * 0.4f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), Range(random, 0.35f, 0.85f), Mathf.Sin(angle)).normalized;
                Vector3 end = fork + direction * Range(random, height * 0.22f, height * 0.38f);
                trunk.Tube(fork, end, Range(random, 0.12f, 0.16f), 0.05f, 6);

                float branchPuffRadius = Range(random, height * 0.18f, height * 0.26f);
                float shade = Range(random, 0.85f, 1.05f);
                leaves.FoliagePuff(end, branchPuffRadius, shade, true);
                distant.FoliagePuff(end, branchPuffRadius, shade, false);

                // Sub-puff
                Vector3 subPos = end + direction * 0.7f + Vector3.up * 0.25f;
                leaves.FoliagePuff(subPos, branchPuffRadius * 0.85f, shade * 1.10f, true);
                distant.FoliagePuff(subPos, branchPuffRadius * 0.85f, shade * 1.10f, false);
            }

            var obj = new GameObject("Broadleaf_" + seed); obj.transform.SetParent(parent, false);
            var wood = trunk.Emit(obj.name + "_Trunk", obj.transform, bark);
            var near = leaves.Emit(obj.name + "_Leaves", obj.transform, foliage);
            var far = distant.Emit(obj.name + "_Distant", obj.transform, foliage);
            var lod = obj.AddComponent<LODGroup>();
            lod.SetLODs(new[] { new LOD(0.04f, new[] { wood, near }), new LOD(0.001f, new[] { wood, far }) });
            lod.RecalculateBounds();
        }

        static void Palm(Transform parent, Vector3 position, float height, int seed, Material foliage, Material bark)
        {
            var random = new System.Random(seed + 907);
            var trunk = new VillageMeshBuilder(); var fronds = new VillageMeshBuilder();

            // Natural organic lean towards the open sky
            float leanAngle = (seed * 0.85f) % (Mathf.PI * 2f);
            Vector3 leanDir = new Vector3(Mathf.Cos(leanAngle), 0f, Mathf.Sin(leanAngle));
            float totalLean = Range(random, 0.65f, 1.45f);

            int trunkSegments = 14;
            Vector3 last = position;
            for (int i = 0; i < trunkSegments; i++)
            {
                float t = (i + 1) / (float)trunkSegments;
                // Curved trunk: lean increases with height squared
                Vector3 end = position + leanDir * (totalLean * t * t) + Vector3.up * (height * t);
                float r0 = Mathf.Lerp(0.24f, 0.12f, (float)i / trunkSegments);
                float r1 = Mathf.Lerp(0.24f, 0.12f, t);
                trunk.Tube(last, end, r0, r1, 8);
                last = end;
            }

            // Coconut cluster at crown base
            fronds.CoconutCluster(last - Vector3.up * 0.15f, 6, 0.14f);

            // Fibrous dry collar at the base of the crown
            fronds.FoliagePuff(last, 0.55f, 0.65f, false);

            // 18-20 Realistic Arching Feather Fronds in 3 tiers
            // Tier 1: Young upright fronds (5 fronds)
            int frondCount = 18;
            for (int f = 0; f < frondCount; f++)
            {
                float angle = f * (Mathf.PI * 2f / frondCount) + (float)random.NextDouble() * 0.25f;
                Vector3 d = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

                float frondLen;
                float frondDroop;
                float frondWidth;
                float frondShade;

                if (f % 3 == 0)
                {
                    // Upright young crown fronds
                    frondLen = Range(random, 3.2f, 3.8f);
                    frondDroop = Range(random, 0.6f, 0.9f);
                    frondWidth = 0.72f;
                    frondShade = 1.10f; // Sunlit golden-green
                }
                else if (f % 3 == 1)
                {
                    // Full arching mid fronds
                    frondLen = Range(random, 4.4f, 5.2f);
                    frondDroop = Range(random, 1.4f, 1.9f);
                    frondWidth = 0.88f;
                    frondShade = 0.98f;
                }
                else
                {
                    // Mature lower cascading fronds
                    frondLen = Range(random, 3.8f, 4.5f);
                    frondDroop = Range(random, 2.1f, 2.7f);
                    frondWidth = 0.80f;
                    frondShade = 0.82f; // Deep emerald
                }

                fronds.PalmFrond(last, d, frondLen, frondWidth, frondDroop, frondShade);
            }

            var obj = new GameObject("Palm_" + seed);
            obj.transform.SetParent(parent, false);
            trunk.Emit(obj.name + "_Trunk", obj.transform, bark);
            fronds.Emit(obj.name + "_Fronds", obj.transform, foliage);
        }
    }
}
#endif
