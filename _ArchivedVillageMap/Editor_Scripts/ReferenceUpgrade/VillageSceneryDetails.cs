#if UNITY_EDITOR
using UnityEngine;

namespace PitStriker.EditorTools
{
    internal static class VillageSceneryDetails
    {
        internal static void Build(Transform parent, Material wood, Material plaster, Material stone, Material soil, Material mountain, Material foliage)
        {
            // Duplicate house geometry removed to restore the authentic Blender house and avoid floating gable/awning artifacts.


            // Small stones break up the verge transition, with a clear central shooting corridor.
            // Small stones break up the verge transition, with a clear central shooting corridor (widened for 75% fairway)
            var r = new System.Random(1791); var gravel = new VillageMeshBuilder();
            for (int i = 0; i < 800; i++)
            {
                float z = -8 + (float)r.NextDouble() * 52;
                float x = (i % 2 == 0 ? -1 : 1) * (3.0f + (float)r.NextDouble() * 11.5f);
                float s = .012f + (float)r.NextDouble() * .045f;
                Vector3 c = new Vector3(x, s * .15f, z);
                Vector3 top = c + Vector3.up * s * .55f;
                for (int j = 0; j < 5; j++)
                {
                    float a = j * Mathf.PI * 2 / 5, b = (j + 1) * Mathf.PI * 2 / 5;
                    gravel.Triangle(c + new Vector3(Mathf.Cos(a) * s, 0, Mathf.Sin(a) * s * .7f), top,
                        c + new Vector3(Mathf.Cos(b) * s, 0, Mathf.Sin(b) * s * .7f), Color.white);
                }
            }
            gravel.Emit("Verge_Gravel", parent, stone);

            // 1. Continuous Surrounding Earth underlaying all active terrain and verges
            var surround = new VillageMeshBuilder();
            for (int x = -130; x < 130; x += 5)
            {
                for (int z = -80; z < 165; z += 5)
                {
                    if (x >= -17.5f && x <= 17.5f && z >= -10f && z <= 41.5f) continue; // Skip widened playable course track
                    surround.Quad(
                        new Vector3(x, -0.06f, z),
                        new Vector3(x, -0.06f, z + 5),
                        new Vector3(x + 5, -0.06f, z + 5),
                        new Vector3(x + 5, -0.06f, z),
                        Color.white, Color.white);
                }
            }
            surround.Emit("Surrounding_Earth", parent, soil);

            // Rustic wooden signpost removed per user request
            // BuildRusticSignpost(parent, wood, stone, foliage);

            BuildPerimeter(parent, soil, foliage);
        }

        static void BuildRusticSignpost(Transform parent, Material wood, Material stone, Material foliage)
        {
            var timber = new VillageMeshBuilder();
            var flora = new VillageMeshBuilder();

            Vector3 basePos = new Vector3(-3.45f, 0f, -4.5f);
            float angle = 22f * Mathf.Deg2Rad;
            Vector3 forward = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
            Vector3 right = new Vector3(Mathf.Cos(angle), 0, -Mathf.Sin(angle));

            // Sturdy vertical timber posts
            float postSpacing = 0.65f;
            Vector3 leftPost = basePos - right * (postSpacing * 0.5f);
            Vector3 rightPost = basePos + right * (postSpacing * 0.5f);
            timber.Tube(leftPost, leftPost + Vector3.up * 1.25f, 0.055f, 0.048f, 8);
            timber.Tube(rightPost, rightPost + Vector3.up * 1.25f, 0.055f, 0.048f, 8);

            // 3 Horizontal weathered wood planks
            float plankW = 1.05f;
            float[] plankY = { 0.52f, 0.76f, 1.00f };
            for (int p = 0; p < 3; p++)
            {
                Vector3 pCenter = basePos + Vector3.up * plankY[p] + forward * 0.045f;
                // Add slight organic tilt
                float tilt = (p == 1) ? 0.015f : -0.010f;
                Vector3 pRight = right * (plankW * 0.5f) + Vector3.up * tilt;
                Vector3 pUp = Vector3.up * 0.11f;
                Vector3 pFwd = forward * 0.022f;

                timber.Box(pCenter, new Vector3(plankW, 0.21f, 0.045f));
            }

            // Cluster of river pebbles and stones at the base of the posts
            for (int s = 0; s < 7; s++)
            {
                float a = s * 1.1f;
                Vector3 sPos = basePos + new Vector3(Mathf.Cos(a) * 0.45f, 0.04f, Mathf.Sin(a) * 0.35f);
                timber.Box(sPos, new Vector3(0.12f, 0.08f, 0.10f));
            }

            // Clustered broadleaf rosettes around the sign base
            flora.Rosette(basePos - right * 0.45f + forward * 0.20f, 0.18f, 7, 0.03f, 0.95f);
            flora.Rosette(basePos + right * 0.50f + forward * 0.15f, 0.15f, 6, 0.02f, 0.90f);

            var signRoot = new GameObject("Rustic_Signpost_Hero");
            signRoot.transform.SetParent(parent, false);
            timber.Emit("Sign_Wood", signRoot.transform, wood);
            flora.Emit("Sign_Foliage", signRoot.transform, foliage);

            // Painted chalk-white lettering "PIT STRIKER" with royal crown insignia
            var textObj = new GameObject("Sign_PaintedText");
            textObj.transform.SetParent(signRoot.transform, false);
            textObj.transform.position = basePos + Vector3.up * 0.88f + forward * 0.08f;
            textObj.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            var tm = textObj.AddComponent<TextMesh>();
            tm.text = "PIT\nSTRIKER";
            tm.characterSize = 0.048f;
            tm.fontSize = 72;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = new Color(0.95f, 0.92f, 0.82f); // Chalk white / cream paint
        }

        static Color MountainColor(float y)
        {
            float t = Mathf.Clamp01(y / 18f);
            return Color.Lerp(new Color(0.24f, 0.32f, 0.25f), new Color(0.18f, 0.26f, 0.28f), t);
        }

        static Vector3 MountainPoint(float x, float z) => new Vector3(x, SampleTerrainElevation(x, z), z);

        public static float SampleWorldElevation(float x, float z)
        {
            return SampleTerrainElevation(x, z);
        }

        public static float SampleTerrainElevation(float x, float z)
        {
            if (z < 78f || z > 168f || x < -140f || x > 140f) return -0.06f;

            // Single gentle mountain ridge on the right flank matching original reference village_overview.png
            float h = 0f;
            if (x > 6f)
            {
                h = 17f * Mathf.Exp(-Mathf.Pow((x - 45f) / 32f, 2) - Mathf.Pow((z - 128f) / 28f, 2));
                h *= 0.90f + 0.15f * Mathf.PerlinNoise(x * 0.05f + 20f, z * 0.05f);
            }

            float edgeX = Mathf.Min(x - (-130f), 130f - x);
            float wx = Mathf.Clamp01(edgeX / 16f);
            float wzFront = Mathf.Clamp01((z - 78f) / 18f);
            float wzBack = Mathf.Clamp01((166f - z) / 16f);

            return Mathf.Lerp(-0.06f, Mathf.Max(-0.06f, h - 0.06f), wx * wzFront * wzBack);
        }

        static Vector3 BorderPoint(float angle, float scale, float height)
        {
            float x = Mathf.Sin(angle), z = Mathf.Cos(angle);
            if (Mathf.Abs(x) < 1e-6f) x = 0; if (Mathf.Abs(z) < 1e-6f) z = 0;
            x = Mathf.Sign(x) * Mathf.Pow(Mathf.Abs(x), .3f) * 16.5f * scale;
            z = 14.5f + Mathf.Sign(z) * Mathf.Pow(Mathf.Abs(z), .3f) * 27.5f * scale;
            return new Vector3(x, height, z);
        }

        static float BermHeight(float angle)
        {
            return .32f + .12f * Mathf.Sin(angle * 5) + .08f * Mathf.Sin(angle * 11);
        }

        static void BuildPerimeter(Transform parent, Material soil, Material foliage)
        {
            // Ground skirt underlaps the entire landscape out to 280m on every side
            var skirt = new VillageMeshBuilder();
            for (int x = -280; x < 280; x += 24)
            {
                for (int z = -280; z < 280; z += 24)
                {
                    skirt.Quad(
                        new Vector3(x, -0.12f, z),
                        new Vector3(x, -0.12f, z + 24),
                        new Vector3(x + 24, -0.12f, z + 24),
                        new Vector3(x + 24, -0.12f, z),
                        Color.white, Color.white);
                }
            }
            skirt.Emit("Horizon_Ground_Skirt", parent, soil);

            var berm = new VillageMeshBuilder();
            var planting = new VillageMeshBuilder();
            float[] scales = { 1, 1.1f, 1.23f, 1.32f };
            for (int i = 0; i < 128; i++)
            {
                float a = i * Mathf.PI * 2 / 128, b = (i + 1) * Mathf.PI * 2 / 128;
                for (int j = 0; j < 3; j++)
                {
                    float ha = j == 0 ? -.04f : BermHeight(a) * (j == 1 ? 1 : .45f);
                    float hb = j == 0 ? -.04f : BermHeight(b) * (j == 1 ? 1 : .45f);
                    float hc = j == 2 ? -.04f : BermHeight(b) * (j == 0 ? 1 : .45f);
                    float hd = j == 2 ? -.04f : BermHeight(a) * (j == 0 ? 1 : .45f);
                    berm.Quad(BorderPoint(a, scales[j], ha), BorderPoint(a, scales[j + 1], hd),
                        BorderPoint(b, scales[j + 1], hc), BorderPoint(b, scales[j], hb), Color.white, Color.white);
                }
                Vector3 root = BorderPoint(a, 1.1f, BermHeight(a) + .015f);
                for (int leaf = 0; leaf < 12; leaf++)
                {
                    float direction = leaf * 2.4f + i;
                    Vector3 d = new Vector3(Mathf.Cos(direction), .65f, Mathf.Sin(direction));
                    planting.Leaf(root, d, .5f + .15f * Mathf.Sin(i + leaf), .10f, .08f, .82f);
                }
            }
            berm.Emit("Rounded_Perimeter_Berm", parent, soil);
            planting.Emit("Corner_And_Edge_Planting", parent, foliage);
        }
    }
}
#endif
