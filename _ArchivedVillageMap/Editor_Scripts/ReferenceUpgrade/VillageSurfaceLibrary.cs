#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace PitStriker.EditorTools
{
    // All generated maps come from the same height field: color is never used as height.
    internal static class VillageSurfaceLibrary
    {
        internal const string Folder = "Assets/_Project/Art/Environments/Village/ReferenceUpgrade";
        internal static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/_Project/Art/Environments/Village", "ReferenceUpgrade");
        }

        internal static T Save<T>(T value, string name) where T : UnityEngine.Object
        {
            string path = Folder + "/" + name + ".asset";
            var old = AssetDatabase.LoadAssetAtPath<T>(path);
            if (old)
            {
                EditorUtility.CopySerialized(value, old);
                UnityEngine.Object.DestroyImmediate(value);
                EditorUtility.SetDirty(old);
                return old;
            }
            AssetDatabase.CreateAsset(value, path);
            return value;
        }

        // Periodic interpolated noise keeps tile seams continuous in all maps.
        static float Hash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + 1709);
                h = (h ^ (h >> 13)) * 1274126177u;
                return (h & 0xffff) / 65535f;
            }
        }
        static float Noise(float u, float v, int frequency)
        {
            float x = u * frequency, y = v * frequency;
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float tx = x - ix, ty = y - iy;
            tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
            int ax = (ix % frequency + frequency) % frequency;
            int ay = (iy % frequency + frequency) % frequency;
            return Mathf.Lerp(Mathf.Lerp(Hash(ax, ay), Hash((ax + 1) % frequency, ay), tx),
                Mathf.Lerp(Hash(ax, (ay + 1) % frequency), Hash((ax + 1) % frequency, (ay + 1) % frequency), tx), ty);
        }
        static float Height(string kind, float u, float v)
        {
            float broad = Noise(u, v, 8), grain = Noise(u, v, 64);
            if (kind == "Bark")
            {
                float grooves = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 22 + Noise(u, v, 6) * 3.5f));
                float rings = Mathf.Sin(v * Mathf.PI * 36);
                return .38f * broad + .36f * grooves + .16f * rings + .10f * grain;
            }
            if (kind == "Wood")
            {
                float rings = Mathf.Sin(u * Mathf.PI * 48 + Noise(u, v, 4) * 7);
                return .45f * broad + .2f * grain + .2f * rings + .15f * Noise(u, v, 128);
            }
            if (kind == "Soil")
            {
                float ruts = 0.28f * (Mathf.Exp(-Mathf.Pow((u - 0.38f) * 14f, 2)) + Mathf.Exp(-Mathf.Pow((u - 0.62f) * 14f, 2)));
                return .38f * broad + .28f * Noise(u, v, 24) + .20f * grain + .14f * Noise(u, v, 128) - ruts;
            }
            if (kind == "Plaster") return .65f * broad + .25f * grain + .1f * Noise(u, v, 128);
            if (kind == "Stone") return .55f * broad + .3f * Noise(u, v, 24) + .15f * grain;
            if (kind == "Roof") return .5f * broad + .35f * grain + .15f * Noise(u, v, 128);
            return .3f * broad + .3f * Noise(u, v, 24) + .25f * grain + .15f * Noise(u, v, 128);
        }

        internal static Material Foliage(string kind, Color deep, Color sunlit)
        {
            EnsureFolder();
            int size = 512;
            var albedo = new Texture2D(size, size, TextureFormat.RGBA32, true, false);
            var colors = new Color[size * size];

            if (kind == "Palm")
            {
                // Authentic coconut palm feather frond:
                // Central rachis (woody stem) running up U = 0.5, with 28 pairs of angled, tapering leaflets
                for (int y = 0; y < size; y++)
                {
                    float v = y / (float)size;
                    float spineHalfWidth = Mathf.Lerp(0.022f, 0.006f, v);
                    float leafletLength = Mathf.Sin(v * Mathf.PI * 0.95f) * 0.44f;

                    for (int x = 0; x < size; x++)
                    {
                        float u = x / (float)size;
                        float distFromCenter = Mathf.Abs(u - 0.5f);
                        int i = y * size + x;

                        if (distFromCenter < spineHalfWidth)
                        {
                            // Central golden-green spine
                            float spineShade = 0.85f + 0.15f * Mathf.Sin(v * 40f);
                            Color spineCol = Color.Lerp(new Color(0.48f, 0.52f, 0.18f), new Color(0.68f, 0.72f, 0.28f), v) * spineShade;
                            colors[i] = new Color(spineCol.r, spineCol.g, spineCol.b, 1.0f);
                        }
                        else if (distFromCenter < leafletLength + spineHalfWidth)
                        {
                            // Leaflets branching at elegant ~35 degrees
                            float side = (u > 0.5f) ? 1f : -1f;
                            float leafletCoord = (v - distFromCenter * 0.55f) * 44f;
                            float leafletPhase = leafletCoord - Mathf.Floor(leafletCoord);
                            // Leaflet blade width vs gap between leaflets (82% blade, 18% slit)
                            if (leafletPhase < 0.82f && v > 0.03f && v < 0.97f)
                            {
                                float edgeFalloff = Mathf.Clamp01((0.82f - leafletPhase) * 16f) * Mathf.Clamp01(leafletPhase * 16f);
                                float tipDist = (leafletLength + spineHalfWidth - distFromCenter) / (leafletLength + 0.001f);
                                float tipFalloff = Mathf.Clamp01(tipDist * 10f);
                                float alpha = Mathf.Clamp01(edgeFalloff * tipFalloff);

                                float leafShade = 0.84f + 0.16f * Mathf.Sin(leafletCoord * Mathf.PI);
                                float sunMix = Mathf.Clamp01(v * 0.7f + (1f - distFromCenter * 2f) * 0.3f);
                                Color leafCol = Color.Lerp(deep, sunlit, sunMix) * leafShade;
                                colors[i] = new Color(leafCol.r, leafCol.g, leafCol.b, alpha);
                            }
                            else
                            {
                                colors[i] = Color.clear;
                            }
                        }
                        else
                        {
                            colors[i] = Color.clear;
                        }
                    }
                }
            }
            else
            {
                // Organic broadleaf canopy cluster:
                // Rich spray of 16 natural tropical leaves with distinct venation and organic silhouettes
                Vector2[] leafCenters = {
                    new Vector2(0.50f, 0.32f), new Vector2(0.35f, 0.42f), new Vector2(0.65f, 0.42f),
                    new Vector2(0.22f, 0.55f), new Vector2(0.78f, 0.55f), new Vector2(0.40f, 0.58f),
                    new Vector2(0.60f, 0.58f), new Vector2(0.28f, 0.70f), new Vector2(0.72f, 0.70f),
                    new Vector2(0.50f, 0.74f), new Vector2(0.16f, 0.40f), new Vector2(0.84f, 0.40f),
                    new Vector2(0.48f, 0.48f), new Vector2(0.36f, 0.82f), new Vector2(0.64f, 0.82f),
                    new Vector2(0.50f, 0.88f)
                };
                float[] leafAngles = { 90f, 125f, 55f, 140f, 40f, 105f, 75f, 120f, 60f, 90f, 160f, 20f, 95f, 110f, 70f, 90f };
                float[] leafRadii = { 0.16f, 0.15f, 0.15f, 0.14f, 0.14f, 0.15f, 0.15f, 0.13f, 0.13f, 0.14f, 0.12f, 0.12f, 0.14f, 0.12f, 0.12f, 0.11f };

                for (int y = 0; y < size; y++)
                {
                    float v = y / (float)size;
                    for (int x = 0; x < size; x++)
                    {
                        float u = x / (float)size;
                        int i = y * size + x;

                        float bestAlpha = 0f;
                        Color bestColor = Color.clear;

                        for (int k = 0; k < leafCenters.Length; k++)
                        {
                            Vector2 pos = new Vector2(u, v) - leafCenters[k];
                            float rad = leafAngles[k] * Mathf.Deg2Rad;
                            Vector2 rotated = new Vector2(pos.x * Mathf.Cos(rad) - pos.y * Mathf.Sin(rad), pos.x * Mathf.Sin(rad) + pos.y * Mathf.Cos(rad));

                            float normY = rotated.y / leafRadii[k];
                            if (normY >= -0.5f && normY <= 0.85f)
                            {
                                float widthAtY = Mathf.Sin((normY + 0.5f) / 1.35f * Mathf.PI) * (leafRadii[k] * 0.48f);
                                float distSide = Mathf.Abs(rotated.x);
                                if (distSide < widthAtY)
                                {
                                    float a = Mathf.Clamp01((widthAtY - distSide) * 45f);
                                    if (a > bestAlpha)
                                    {
                                        bestAlpha = a;
                                        float midrib = Mathf.Clamp01(1f - distSide * 75f);
                                        float lateralVeins = Mathf.Clamp01(Mathf.Sin((normY * 22f + distSide * 18f) * Mathf.PI) * 0.18f);
                                        float tone = Mathf.Clamp01(0.75f + normY * 0.25f + midrib * 0.22f + lateralVeins);
                                        bestColor = Color.Lerp(deep, sunlit, tone);
                                    }
                                }
                            }
                        }

                        colors[i] = new Color(bestColor.r, bestColor.g, bestColor.b, bestAlpha);
                    }
                }
            }

            albedo.SetPixels(colors);
            albedo.wrapMode = TextureWrapMode.Clamp;
            albedo.filterMode = FilterMode.Trilinear;
            albedo.anisoLevel = 8;
            albedo.Apply(true, false);

            albedo = Save(albedo, "Foliage_" + kind + "_Albedo");
            var material = Material("Foliage_" + kind, "Pit Striker/Village Foliage", Color.white);
            material.SetTexture("_BaseMap", albedo);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_AmbientFill", 0.55f);
            material.SetFloat("_Transmission", 0.45f);
            material.SetFloat("_Cutoff", 0.28f);
            material.SetFloat("_WindSpeed", kind == "Palm" ? 1.5f : 1.9f);
            material.SetFloat("_WindStrength", kind == "Palm" ? 0.045f : 0.06f);
            EditorUtility.SetDirty(material);
            return material;
        }

        internal static Material Surface(string kind, Color dark, Color light, float relief, bool ground = false)
        {
            EnsureFolder();
            int size = ground ? 512 : 256;
            var albedo = new Texture2D(size, size, TextureFormat.RGBA32, true, false);
            var normal = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
            var mask = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
            var colors = new Color[size * size]; var normals = new Color[size * size]; var masks = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size, e = 1f / size;
                float h = Height(kind, u, v);
                float dx = Height(kind, u + e, v) - Height(kind, u - e, v);
                float dy = Height(kind, u, v + e) - Height(kind, u, v - e);
                Vector3 n = new Vector3(-dx * relief, -dy * relief, 1).normalized;
                int i = y * size + x;
                // Low-contrast pigment variation is independent of the relief field.
                // Using height directly for pigment made plaster and soil look blotchy.
                float pigment = .5f + (Noise(u,v,4)-.5f)*.22f + (Noise(u,v,64)-.5f)*.18f;
                colors[i] = Color.Lerp(dark, light, pigment);
                normals[i] = new Color(n.x * .5f + .5f, n.y * .5f + .5f, n.z * .5f + .5f, 1);
                masks[i] = new Color(Mathf.Lerp(.8f, 1, h), Mathf.Lerp(.96f, .72f, h), 0, 1);
            }
            albedo.SetPixels(colors); normal.SetPixels(normals); mask.SetPixels(masks);
            foreach (var texture in new[] { albedo, normal, mask })
            {
                texture.wrapMode = TextureWrapMode.Repeat; texture.filterMode = FilterMode.Trilinear;
                texture.anisoLevel = 8; texture.Apply(true, false);
            }
            albedo = Save(albedo, kind + "_Albedo"); normal = Save(normal, kind + "_Normal"); mask = Save(mask, kind + "_Mask");
            var material = Material(kind, ground ? "Pit Striker/Village Ground" : "Pit Striker/Village Surface", Color.white);
            material.SetTexture("_BaseMap", albedo); material.SetTexture("_BumpMap", normal);
            material.SetTexture("_MaskMap", mask); material.SetFloat("_UseMask", 1);
            material.SetFloat("_AmbientFill", .45f);
            material.SetFloat("_BumpScale", .65f); material.SetFloat("_Smoothness", .15f);
            if (ground) material.SetFloat("_WorldScale", 1.25f);
            EditorUtility.SetDirty(material);
            return material;
        }

        internal static Material Material(string name, string shaderName, Color color)
        {
            EnsureFolder();
            Shader shader = Shader.Find(shaderName);
            if (!shader || !shader.isSupported) throw new InvalidOperationException("Missing or unsupported shader: " + shaderName);
            string path = Folder + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader; m.SetColor("_BaseColor", color); m.enableInstancing = true;
            EditorUtility.SetDirty(m); return m;
        }
    }
}
#endif
