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
                float wa = width * (1 - t) * .5f, wb = width * (1 - q) * .5f;
                Color ca = new Color(shade * Mathf.Lerp(.55f, 1, t), shade * Mathf.Lerp(.55f, 1, t), shade * Mathf.Lerp(.55f, 1, t), t);
                Color cb = new Color(shade, shade, shade, q);
                if (j == segments - 1) Triangle(a - side * wa, a + side * wa, b, cb);
                else Quad(a - side * wa, a + side * wa, b + side * wb, b - side * wb, ca, cb);
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
