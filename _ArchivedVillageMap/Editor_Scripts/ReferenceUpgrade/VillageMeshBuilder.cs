#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PitStriker.EditorTools
{
    internal sealed class VillageMeshBuilder
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<int> triangles = new List<int>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<Color> colors = new List<Color>();
        internal int TriangleCount => triangles.Count / 3;

        internal void Triangle(Vector3 a, Vector3 b, Vector3 c, Color color)
        {
            int i = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(.5f, 1));
            colors.Add(color); colors.Add(color); colors.Add(color);
            triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
        }

        internal void Triangle(Vector3 a, Vector3 b, Vector3 c, Color ca, Color cb, Color cc)
        {
            int i = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(.5f, 1));
            colors.Add(ca); colors.Add(cb); colors.Add(cc);
            triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
        }

        internal void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color root, Color tip)
        {
            int i = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1));
            colors.Add(root); colors.Add(root); colors.Add(tip); colors.Add(tip);
            triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
            triangles.Add(i); triangles.Add(i + 2); triangles.Add(i + 3);
        }

        internal void Blade(Vector3 root, Vector3 direction, float height, float width, float bend, int segments, float shade)
        {
            Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
            for (int j = 0; j < segments; j++)
            {
                float t = j / (float)segments, q = (j + 1f) / segments;
                Vector3 a = root + Vector3.up * (height * t) + direction * (bend * t * t);
                Vector3 b = root + Vector3.up * (height * q) + direction * (bend * q * q);
                float wa = width * (1 - t * .75f) * .5f, wb = width * (1 - q * .75f) * .5f;
                Color ca = new Color(shade * Mathf.Lerp(.72f, 1, t), shade * Mathf.Lerp(.72f, 1, t), shade * Mathf.Lerp(.72f, 1, t), t);
                Color cb = new Color(shade * Mathf.Lerp(.72f, 1, q), shade * Mathf.Lerp(.72f, 1, q), shade * Mathf.Lerp(.72f, 1, q), q);
                if (j == segments - 1) Triangle(a - side * wa, a + side * wa, b, ca, ca, cb);
                else Quad(a - side * wa, a + side * wa, b + side * wb, b - side * wb, ca, cb);
            }
        }

        internal void Rosette(Vector3 root, float radius, int leafCount, float droop, float shade)
        {
            for (int k = 0; k < leafCount; k++)
            {
                float angle = k * Mathf.PI * 2f / leafCount + (k % 2 == 0 ? 0.12f : -0.12f);
                Vector3 d = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 side = Vector3.Cross(Vector3.up, d).normalized;
                float len = radius * (0.75f + 0.35f * Mathf.Sin(k * 1.7f));
                float w = len * 0.36f;
                Vector3 mid = root + d * (len * 0.5f) + Vector3.up * (len * 0.18f);
                Vector3 tip = root + d * len + Vector3.up * Mathf.Max(0.005f, len * 0.18f - droop);
                Color cRoot = new Color(shade * 0.70f, shade * 0.70f, shade * 0.70f, 0f);
                Color cMid = new Color(shade * 0.88f, shade * 0.88f, shade * 0.88f, 0.22f);
                Color cTip = new Color(shade, shade, shade, 0.40f);
                Quad(root - side * (w * 0.25f), root + side * (w * 0.25f), mid + side * w, mid - side * w, cRoot, cMid);
                Triangle(mid - side * w, mid + side * w, tip, cMid, cMid, cTip);
            }
        }

        internal void CreepingClump(Vector3 root, int count, float spread, float height, float shade)
        {
            for (int k = 0; k < count; k++)
            {
                float angle = k * Mathf.PI * 2f / count + (k * 0.73f);
                Vector3 d = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                float len = spread * (0.6f + 0.45f * Mathf.Sin(k * 2.3f));
                float h = height * (0.65f + 0.35f * Mathf.Cos(k * 1.9f));
                float w = 0.016f + 0.008f * Mathf.Sin(k);
                Blade(root + d * 0.015f, d, h, w, len * 0.75f, 2, shade * (0.85f + 0.15f * Mathf.Sin(k)));
            }
        }

        internal void Clover(Vector3 root, float radius, float shade)
        {
            for (int k = 0; k < 3; k++)
            {
                float angle = k * Mathf.PI * 2f / 3f + (k * 0.2f);
                Vector3 d = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 side = Vector3.Cross(Vector3.up, d).normalized;
                float r = radius * (0.8f + 0.2f * Mathf.Sin(k * 1.5f));
                Vector3 basePos = root + Vector3.up * 0.015f;
                Vector3 tip = basePos + d * r + Vector3.up * 0.018f;
                Vector3 left = basePos + d * (r * 0.5f) - side * (r * 0.45f) + Vector3.up * 0.012f;
                Vector3 right = basePos + d * (r * 0.5f) + side * (r * 0.45f) + Vector3.up * 0.012f;
                Color cBase = new Color(shade * 0.65f, shade * 0.65f, shade * 0.65f, 0.1f);
                Color cTip = new Color(shade * 0.95f, shade * 0.95f, shade * 0.95f, 0.35f);
                Triangle(basePos, left, tip, cBase, cTip, cTip);
                Triangle(basePos, tip, right, cBase, cTip, cTip);
            }
        }

        internal void WeedFlower(Vector3 root, float height, float shade)
        {
            Vector3 top = root + Vector3.up * height + new Vector3(0.015f, 0, 0.015f);
            Color stemRoot = new Color(shade * 0.6f, shade * 0.6f, shade * 0.6f, 0f);
            Color stemTop = new Color(shade * 0.9f, shade * 0.9f, shade * 0.9f, 0.5f);
            Quad(root - Vector3.right * 0.004f, root + Vector3.right * 0.004f, top + Vector3.right * 0.003f, top - Vector3.right * 0.003f, stemRoot, stemTop);
            float petalR = 0.018f;
            Color flowerColor = new Color(1.3f * shade, 1.25f * shade, 0.65f * shade, 0.6f);
            for (int p = 0; p < 4; p++)
            {
                float a = p * Mathf.PI * 0.5f + 0.2f;
                Vector3 pd = new Vector3(Mathf.Cos(a), 0.2f, Mathf.Sin(a)) * petalR;
                Vector3 ps = Vector3.Cross(Vector3.up, pd).normalized * (petalR * 0.4f);
                Triangle(top, top + pd - ps, top + pd + ps, flowerColor, flowerColor, flowerColor);
            }
        }

        internal void GrainStalk(Vector3 root, float height, float shade)
        {
            Vector3 top = root + Vector3.up * height + new Vector3(height * 0.08f, 0, height * 0.08f);
            Color stemRoot = new Color(shade * 0.62f, shade * 0.62f, shade * 0.62f, 0f);
            Color stemTop = new Color(shade * 0.95f, shade * 0.95f, shade * 0.95f, 0.45f);
            Quad(root - Vector3.right * 0.0035f, root + Vector3.right * 0.0035f, top + Vector3.right * 0.0025f, top - Vector3.right * 0.0025f, stemRoot, stemTop);
            int grains = 4;
            Color grainColor = new Color(1.25f * shade, 1.15f * shade, 0.70f * shade, 0.6f);
            for (int g = 0; g < grains; g++)
            {
                float gt = g / (float)grains;
                Vector3 gp = top - Vector3.up * (0.04f * gt);
                Vector3 gSide = (g % 2 == 0 ? Vector3.right : Vector3.left) * 0.012f + Vector3.up * 0.010f;
                Triangle(gp, gp + gSide, gp + Vector3.up * 0.015f, grainColor, grainColor, grainColor);
            }
        }

        internal void Leaf(Vector3 root, Vector3 direction, float length, float width, float droop, float shade)
        {
            direction.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, direction).normalized * width;
            Vector3 mid = root + direction * length * .45f + Vector3.up * .05f;
            Vector3 tip = root + direction * length - Vector3.up * droop;
            Color c = new Color(shade, shade, shade, .6f);
            Triangle(root, mid - side, tip, c); Triangle(root, tip, mid + side, c);
        }

        internal void QuadUV(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD, Color ca, Color cb)
        {
            int i = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            uv.Add(uvA); uv.Add(uvB); uv.Add(uvC); uv.Add(uvD);
            colors.Add(ca); colors.Add(ca); colors.Add(cb); colors.Add(cb);
            triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
            triangles.Add(i); triangles.Add(i + 2); triangles.Add(i + 3);
        }

        internal void FoliagePuff(Vector3 center, float radius, float shade, bool highDetail = true)
        {
            // Volumetric organic foliage cluster:
            // Multi-tiered outward-oriented curved cards with natural orientation and golden ratio spacing
            int count = highDetail ? 14 : 8;
            float golden = 2.39996f;
            for (int p = 0; p < count; p++)
            {
                float pitch = Mathf.Acos(1f - 2f * (p + 0.5f) / count) - Mathf.PI * 0.5f;
                float yaw = p * golden;
                Vector3 normal = new Vector3(Mathf.Cos(pitch) * Mathf.Cos(yaw), Mathf.Sin(pitch), Mathf.Cos(pitch) * Mathf.Sin(yaw));
                Vector3 u = Vector3.Cross(normal, Vector3.up).normalized;
                if (u.sqrMagnitude < 0.01f) u = Vector3.right;
                Vector3 v = Vector3.Cross(normal, u).normalized;

                float r = radius * (0.75f + 0.25f * Mathf.Sin(p * 2.3f));
                float s = shade * (0.82f + 0.18f * Mathf.Clamp01(normal.y * 0.5f + 0.5f));
                Color cRoot = new Color(s * 0.85f, s * 0.85f, s * 0.85f, 0.40f);
                Color cTip = new Color(s * 1.05f, s * 1.05f, s * 1.05f, 0.80f);

                // Offset card outward along the sphere surface
                Vector3 c = center + normal * (radius * 0.45f);
                Vector3 p0 = c - u * r - v * (r * 0.70f);
                Vector3 p1 = c - u * (r * 0.85f) + v * (r * 0.85f);
                Vector3 p2 = c + u * (r * 0.85f) + v * (r * 0.85f);
                Vector3 p3 = c + u * r - v * (r * 0.70f);
                Quad(p0, p1, p2, p3, cRoot, cTip);

                // Cross card for 3D leaf volume
                if (highDetail && p % 2 == 0)
                {
                    Vector3 diagU = (u + v).normalized;
                    Vector3 diagV = Vector3.Cross(normal, diagU).normalized;
                    Vector3 q0 = c - diagU * (r * 0.8f) - diagV * (r * 0.6f);
                    Vector3 q1 = c - diagU * (r * 0.7f) + diagV * (r * 0.8f);
                    Vector3 q2 = c + diagU * (r * 0.7f) + diagV * (r * 0.8f);
                    Vector3 q3 = c + diagU * (r * 0.8f) - diagV * (r * 0.6f);
                    Quad(q0, q1, q2, q3, cRoot, cTip);
                }
            }
        }

        internal void PalmFrond(Vector3 crown, Vector3 direction, float length, float width, float droop, float shade)
        {
            direction.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
            int segments = 8;
            Vector3 prevP = crown;
            float prevW = 0.08f;

            for (int j = 0; j < segments; j++)
            {
                float t0 = j / (float)segments;
                float t1 = (j + 1f) / segments;

                // Arching trajectory: rises up from crown, then gracefully cascades downward
                float arch0 = Mathf.Sin(t0 * Mathf.PI * 0.85f) * (length * 0.28f) - (t0 * t0) * droop;
                float arch1 = Mathf.Sin(t1 * Mathf.PI * 0.85f) * (length * 0.28f) - (t1 * t1) * droop;

                Vector3 p0 = crown + direction * (length * t0) + Vector3.up * arch0;
                Vector3 p1 = crown + direction * (length * t1) + Vector3.up * arch1;

                // Frond width profile (slender at base, swells in mid-frond, tapers at tip)
                float w0 = (Mathf.Sin(t0 * Mathf.PI * 0.95f) * 0.85f + 0.15f) * width;
                float w1 = (Mathf.Sin(t1 * Mathf.PI * 0.95f) * 0.85f + 0.15f) * width;

                // Natural downward droop of leaflets from the raised central spine (3D chevron arch)
                Vector3 droopOffset0 = Vector3.up * (w0 * 0.36f);
                Vector3 droopOffset1 = Vector3.up * (w1 * 0.36f);

                Vector3 left0 = p0 - side * (w0 * 0.5f) - droopOffset0;
                Vector3 left1 = p1 - side * (w1 * 0.5f) - droopOffset1;
                Vector3 right0 = p0 + side * (w0 * 0.5f) - droopOffset0;
                Vector3 right1 = p1 + side * (w1 * 0.5f) - droopOffset1;

                Color c0 = new Color(shade * (0.85f + 0.15f * t0), shade * (0.85f + 0.15f * t0), shade * (0.85f + 0.15f * t0), t0 * 0.75f);
                Color c1 = new Color(shade * (0.85f + 0.15f * t1), shade * (0.85f + 0.15f * t1), shade * (0.85f + 0.15f * t1), t1 * 0.75f);

                // Left leaflet wing: from outer tip (U=0) to central rachis (U=0.5)
                QuadUV(left0, p0, p1, left1, new Vector2(0f, t0), new Vector2(0.5f, t0), new Vector2(0.5f, t1), new Vector2(0f, t1), c0, c1);

                // Right leaflet wing: from central rachis (U=0.5) to outer tip (U=1.0)
                QuadUV(p0, right0, right1, p1, new Vector2(0.5f, t0), new Vector2(1f, t0), new Vector2(1f, t1), new Vector2(0.5f, t1), c0, c1);
            }
        }

        internal void CoconutCluster(Vector3 center, int count, float nutRadius)
        {
            Color nutColor = new Color(0.38f, 0.45f, 0.18f); // Golden-olive tropical coconut
            for (int k = 0; k < count; k++)
            {
                float angle = k * Mathf.PI * 2f / count + 0.3f;
                Vector3 d = new Vector3(Mathf.Cos(angle), -0.2f, Mathf.Sin(angle)).normalized;
                Vector3 pos = center + d * (nutRadius * 1.35f);
                Box(pos, Vector3.one * (nutRadius * 1.8f));
            }
        }

        internal void FlaredTrunk(Vector3 basePos, Vector3 forkPos, float radiusBase, float radiusTop, int sides = 8)
        {
            int heightSteps = 6;
            Vector3 prevCenter = basePos;
            Vector3 axis = (forkPos - basePos).normalized;
            Vector3 u = Vector3.Cross(axis, Vector3.up).normalized;
            if (u.sqrMagnitude < 0.01f) u = Vector3.right;
            Vector3 v = Vector3.Cross(axis, u).normalized;

            for (int step = 0; step < heightSteps; step++)
            {
                float t0 = step / (float)heightSteps;
                float t1 = (step + 1f) / heightSteps;

                Vector3 p0 = Vector3.Lerp(basePos, forkPos, t0);
                Vector3 p1 = Vector3.Lerp(basePos, forkPos, t1);

                // Flared root base: radius swells significantly at ground line
                float flare0 = Mathf.Lerp(radiusBase * 1.85f, radiusTop, Mathf.Pow(t0, 0.45f));
                float flare1 = Mathf.Lerp(radiusBase * 1.85f, radiusTop, Mathf.Pow(t1, 0.45f));

                for (int j = 0; j < sides; j++)
                {
                    float a0 = j * Mathf.PI * 2 / sides;
                    float a1 = (j + 1f) * Mathf.PI * 2 / sides;

                    // Buttress wave modulation near root base
                    float buttress0 = (1f - t0) * 0.22f * Mathf.Sin(a0 * 4f);
                    float buttress1 = (1f - t1) * 0.22f * Mathf.Sin(a1 * 4f);

                    float r0A = flare0 * (1f + buttress0);
                    float r0B = flare0 * (1f + (1f - t0) * 0.22f * Mathf.Sin(a1 * 4f));
                    float r1B = flare1 * (1f + buttress1);
                    float r1A = flare1 * (1f + (1f - t1) * 0.22f * Mathf.Sin(a0 * 4f));

                    Vector3 n0A = u * Mathf.Cos(a0) + v * Mathf.Sin(a0);
                    Vector3 n0B = u * Mathf.Cos(a1) + v * Mathf.Sin(a1);

                    Vector3 vA = p0 + n0A * r0A;
                    Vector3 vB = p0 + n0B * r0B;
                    Vector3 vC = p1 + n0B * r1B;
                    Vector3 vD = p1 + n0A * r1A;

                    Quad(vA, vB, vC, vD, Color.white, Color.white);
                }
            }
        }

        internal void Tube(Vector3 a, Vector3 b, float radiusA, float radiusB, int sides = 8)
        {
            Vector3 axis = (b - a).normalized;
            Vector3 u = Vector3.Cross(axis, Vector3.up).normalized;
            if (u.sqrMagnitude < .01f) u = Vector3.right;
            Vector3 v = Vector3.Cross(axis, u).normalized;
            for (int j = 0; j < sides; j++)
            {
                float t = j * Mathf.PI * 2 / sides, q = (j + 1f) * Mathf.PI * 2 / sides;
                Vector3 n = u * Mathf.Cos(t) + v * Mathf.Sin(t), m = u * Mathf.Cos(q) + v * Mathf.Sin(q);
                Quad(a + n * radiusA, a + m * radiusA, b + m * radiusB, b + n * radiusB, Color.white, Color.white);
            }
        }

        internal void Box(Vector3 center, Vector3 size)
        {
            Vector3 h = size * .5f;
            var p = new[] { center + new Vector3(-h.x,-h.y,-h.z), center + new Vector3(h.x,-h.y,-h.z),
                center + new Vector3(h.x,-h.y,h.z), center + new Vector3(-h.x,-h.y,h.z),
                center + new Vector3(-h.x,h.y,-h.z), center + new Vector3(h.x,h.y,-h.z),
                center + new Vector3(h.x,h.y,h.z), center + new Vector3(-h.x,h.y,h.z) };
            int[,] f = { {0,1,2,3}, {4,7,6,5}, {0,4,5,1}, {1,5,6,2}, {2,6,7,3}, {3,7,4,0} };
            for (int i = 0; i < 6; i++) Quad(p[f[i,0]], p[f[i,1]], p[f[i,2]], p[f[i,3]], Color.white, Color.white);
        }

        internal Mesh Build(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.SetUVs(0, uv); mesh.SetColors(colors);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            return mesh;
        }

        internal Renderer Emit(string name, Transform parent, Material material)
        {
            if (vertices.Count == 0) return null;
            var obj = new GameObject(name); obj.transform.SetParent(parent, false);
            obj.AddComponent<MeshFilter>().sharedMesh = VillageSurfaceLibrary.Save(Build(name), name);
            var renderer = obj.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true; renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            return renderer;
        }
    }
}
#endif
