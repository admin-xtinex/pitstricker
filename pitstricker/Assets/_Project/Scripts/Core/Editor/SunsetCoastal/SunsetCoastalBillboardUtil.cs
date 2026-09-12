#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PitStriker.EditorTools
{
    /// <summary>
    /// Shared helper for building the flat, alpha-cutout "2.5D card" quads used for
    /// hero-adjacent props, scenic dressing, and distant impostor layers (Phases 3 & 5).
    /// </summary>
    public static class SunsetCoastalBillboardUtil
    {
        private static Mesh _sharedQuad;
        private static Mesh _sharedGroundQuad;

        public static Mesh GetQuadMesh()
        {
            if (_sharedQuad != null) return _sharedQuad;

            var mesh = new Mesh { name = "SunsetCoastal_CardQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, 0f),
                new Vector3(0.5f, 0f, 0f),
                new Vector3(0.5f, 1f, 0f),
                new Vector3(-0.5f, 1f, 0f),
            };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            // "Pit Striker/Village Foliage" reads vertex COLOR.a as its wind-sway weight
            // (base=0, tip=1). Must be explicitly assigned — an unset COLOR stream is
            // undefined per-vertex data on the GPU, which can propagate NaNs into the
            // fragment output (magenta) even when _WindStrength is 0.
            mesh.colors = new[]
            {
                new Color(1, 1, 1, 0), new Color(1, 1, 1, 0),
                new Color(1, 1, 1, 1), new Color(1, 1, 1, 1)
            };
            mesh.tangents = new[]
            {
                new Vector4(1, 0, 0, -1), new Vector4(1, 0, 0, -1),
                new Vector4(1, 0, 0, -1), new Vector4(1, 0, 0, -1)
            };
            mesh.RecalculateBounds();
            _sharedQuad = mesh;
            return mesh;
        }

        /// <summary>
        /// A flat quad on the XZ plane (normal up), for decals laid directly on the sand.
        /// </summary>
        public static Mesh GetGroundQuadMesh()
        {
            if (_sharedGroundQuad != null) return _sharedGroundQuad;

            var mesh = new Mesh { name = "SunsetCoastal_GroundDecalQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0.5f),
            };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            mesh.tangents = new[]
            {
                new Vector4(1, 0, 0, -1), new Vector4(1, 0, 0, -1),
                new Vector4(1, 0, 0, -1), new Vector4(1, 0, 0, -1)
            };
            mesh.RecalculateBounds();
            _sharedGroundQuad = mesh;
            return mesh;
        }

        /// <summary>
        /// Spawns a flat ground decal (footprints, moss, foam, etc.) lying on the sand.
        /// </summary>
        public static GameObject SpawnGroundDecal(string name, Transform parent, Texture2D texture, Material material,
            Vector3 position, float worldSize, float yRotation = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);

            float aspect = texture != null && texture.height > 0 ? (float)texture.width / texture.height : 1f;
            go.transform.localScale = new Vector3(worldSize * aspect, 1f, worldSize);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = GetGroundQuadMesh();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return go;
        }

        /// <summary>
        /// Spawns a vertical billboard card sized to the texture's aspect ratio, pivoted
        /// at its base, facing along -forward by default (matches the lane's +Z view direction).
        /// An optional backward lean helps ground-sitting hero cards (shack, boat) read
        /// less like a flat cutout under the map's steep downward camera tilt.
        /// </summary>
        public static GameObject SpawnCard(string name, Transform parent, Texture2D texture, Material material,
            Vector3 position, float worldHeight, float yRotation = 0f, bool castShadows = false, bool isStatic = true, float backwardLeanDegrees = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f) * Quaternion.Euler(-backwardLeanDegrees, 0f, 0f);

            float aspect = texture != null && texture.height > 0 ? (float)texture.width / texture.height : 1f;
            go.transform.localScale = new Vector3(worldHeight * aspect, worldHeight, 1f);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = GetQuadMesh();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = castShadows;
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            if (isStatic) GameObjectUtility.SetStaticEditorFlags(go,
                StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic);

            return go;
        }
    }
}
#endif
