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
            if (kind == "Wood" || kind == "Bark")
            {
                float rings = Mathf.Sin(u * Mathf.PI * 48 + Noise(u, v, 4) * 7);
                return .45f * broad + .2f * grain + .2f * rings + .15f * Noise(u, v, 128);
            }
            if (kind == "Plaster") return .65f * broad + .25f * grain + .1f * Noise(u, v, 128);
            if (kind == "Stone") return .55f * broad + .3f * Noise(u, v, 24) + .15f * grain;
            if (kind == "Roof") return .5f * broad + .35f * grain + .15f * Noise(u, v, 128);
            return .3f * broad + .3f * Noise(u, v, 24) + .25f * grain + .15f * Noise(u, v, 128);
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
