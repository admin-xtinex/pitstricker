#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PitStriker.Gameplay;
using PitStriker.Visuals;

namespace PitStriker.EditorTools
{
    /// <summary>
    /// Generates a single continuous Unified Flat Ground mesh spanning the entire arena
    /// (X in [-10, 10], Z in [-10, 38]) with 3 exact circular apertures (r=0.50m, 36 segments)
    /// at Z=3.0, 16.5, 31.0. Eliminates all trenches, cliffs, square seams, floating slabs, and floating flags.
    /// Also installs deep 3D circular saucer pit cups (depth -0.18m) with dark red shading and in-pit basin numerals.
    /// </summary>
    public static class VillagePitMeshGenerator
    {
        private const string ScenePath = "Assets/_Project/Scenes/SC_Village_Graphics_Test.unity";
        private const string SoilMatPath = "Assets/_Project/Art/Environments/Village/Reference_Soil.mat";
        private const string PhysMatPath = "Assets/_Project/Physics/PM_Sand_Friction.physicMaterial";

        // Rebuild only on explicit request; compilation must not edit scenes or queue APKs.
        private static void AutoRunOnCompile()
        {
            EditorApplication.delayCall += () =>
            {
                bool needsRepair = !SessionState.GetBool("VillagePitMeshGenerator_v6_Ran", false) || HasInvisiblePit();
                if (needsRepair)
                {
                    SessionState.SetBool("VillagePitMeshGenerator_v6_Ran", true);
                    RebuildVillagePits();
                }
            };
        }

        private static bool HasInvisiblePit()
        {
            string[] pitNames = { "Pit_01_Round", "Pit_02_Round", "Pit_03_Round" };
            foreach (var pitName in pitNames)
            {
                GameObject pitObj = GameObject.Find(pitName);
                if (pitObj == null) return true;
                if (!pitObj.activeSelf) return true;
                MeshRenderer mr = pitObj.GetComponent<MeshRenderer>();
                if (mr == null || !mr.enabled) return true;
            }
            return false;
        }

        [MenuItem("Pit Striker/Rebuild Deep Earthen Pits (3D Depth)", false, 12)]
        public static void RebuildVillagePits()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[PIT GENERATOR] Could not open scene: {ScenePath}");
                return;
            }

            Material soilMat = AssetDatabase.LoadAssetAtPath<Material>(SoilMatPath);
            PhysicsMaterial physMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(PhysMatPath);

            // 1. Create or load dark red material for entire pit cavity
            string redMatPath = "Assets/_Project/Art/Environments/Village/M_Pit_DarkRed.mat";
            Material redMat = AssetDatabase.LoadAssetAtPath<Material>(redMatPath);
            if (redMat == null)
            {
                Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
                if (litShader == null) litShader = Shader.Find("Standard");
                redMat = new Material(litShader);
                redMat.name = "M_Pit_DarkRed";
                Color darkRed = new Color(0.48f, 0.05f, 0.06f, 1.0f);
                redMat.SetColor("_BaseColor", darkRed);
                redMat.SetColor("_Color", darkRed);
                redMat.SetFloat("_Smoothness", 0.32f);
                redMat.EnableKeyword("_EMISSION");
                redMat.SetColor("_EmissionColor", new Color(0.18f, 0.015f, 0.02f));
                AssetDatabase.CreateAsset(redMat, redMatPath);
                AssetDatabase.SaveAssets();
            }

            // 2. Generate and save Deep 3D circular dish mesh asset (depth -0.18m, radius 0.50m)
            Mesh deepDishMesh = CreateDeepPitMesh();
            string meshAssetPath = "Assets/_Project/Art/Environments/Village/Shallow_Pit_Saucer_Mesh.asset";
            AssetDatabase.CreateAsset(deepDishMesh, meshAssetPath);
            AssetDatabase.SaveAssets();

            // 3. Configure the Unified Continuous Flat Ground across the entire arena
            ConfigureUnifiedArenaGround(soilMat, physMat);

            // 4. Configure each 3D Pit
            string[] pitNames = { "Pit_01_Round", "Pit_02_Round", "Pit_03_Round" };
            Vector3[] pitPositions = {
                new Vector3(0f, 0.0f, 3.0f),
                new Vector3(0f, 0.0f, 16.5f),
                new Vector3(0f, 0.0f, 31.0f)
            };

            for (int i = 0; i < pitNames.Length; i++)
            {
                string pitName = pitNames[i];
                GameObject pitObj = GameObject.Find(pitName);
                if (pitObj == null)
                {
                    pitObj = new GameObject(pitName);
                }

                pitObj.transform.position = pitPositions[i];
                pitObj.transform.rotation = Quaternion.identity;
                pitObj.transform.localScale = Vector3.one;

                // Remove obsolete flat cylinder / insets
                Transform oldInset = pitObj.transform.Find("Pit_Interior_Red");
                if (oldInset != null) UnityEngine.Object.DestroyImmediate(oldInset.gameObject);
                Transform oldRim = pitObj.transform.Find("Stylized_Ceramic_Rim");
                if (oldRim != null) UnityEngine.Object.DestroyImmediate(oldRim.gameObject);
                Transform oldBadge = pitObj.transform.Find("PitNumber_Badge");
                if (oldBadge != null) UnityEngine.Object.DestroyImmediate(oldBadge.gameObject);

                // Remove any floating air holograms or poles
                Transform oldVisualRoot = pitObj.transform.Find("Hologram_VisualRoot");
                if (oldVisualRoot != null) UnityEngine.Object.DestroyImmediate(oldVisualRoot.gameObject);
                Transform oldBeam = pitObj.transform.Find("Hologram_EmitterBeam");
                if (oldBeam != null) UnityEngine.Object.DestroyImmediate(oldBeam.gameObject);

                // Assign deep 3D circular dish mesh
                MeshFilter mf = pitObj.GetComponent<MeshFilter>();
                if (mf == null) mf = pitObj.AddComponent<MeshFilter>();
                mf.sharedMesh = deepDishMesh;

                // Assign whole pit dark red material to MeshRenderer
                MeshRenderer mr = pitObj.GetComponent<MeshRenderer>();
                if (mr == null) mr = pitObj.AddComponent<MeshRenderer>();
                pitObj.SetActive(true);
                mr.enabled = true;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                mr.receiveShadows = true;
                mr.sharedMaterials = new Material[] { redMat };

                // Update 3D Raised Beveled Lip Ring (100% circular, raised +0.015m)
                Transform existingLip = pitObj.transform.Find("BrightRed_PitLip");
                GameObject lipObj = existingLip != null ? existingLip.gameObject : new GameObject("BrightRed_PitLip");
                lipObj.transform.SetParent(pitObj.transform);
                lipObj.transform.localPosition = new Vector3(0f, 0.005f, 0f);
                lipObj.transform.localRotation = Quaternion.identity;

                MeshFilter lipMf = lipObj.GetComponent<MeshFilter>();
                if (lipMf == null) lipMf = lipObj.AddComponent<MeshFilter>();
                MeshRenderer lipMr = lipObj.GetComponent<MeshRenderer>();
                if (lipMr == null) lipMr = lipObj.AddComponent<MeshRenderer>();
                lipMr.sharedMaterial = redMat;
                lipMf.sharedMesh = GenerateRaisedLipRingMesh(0.48f, 0.54f, 0.015f);

                // In-Pit Floor Numeral Identifier (Lies flat on basin floor, ZERO air obstruction)
                HolographicPitProjection holo = pitObj.GetComponent<HolographicPitProjection>();
                if (holo == null) holo = pitObj.AddComponent<HolographicPitProjection>();
                holo.PitNumber = i + 1;
                holo.BuildInPitMarker();

                // Update MeshCollider for deep 3D cup rolling physics
                MeshCollider mc = pitObj.GetComponent<MeshCollider>();
                if (mc == null) mc = pitObj.AddComponent<MeshCollider>();
                mc.sharedMesh = deepDishMesh;
                if (physMat != null) mc.sharedMaterial = physMat;

                // Update SphereCollider Trigger covering the cup
                SphereCollider sc = pitObj.GetComponent<SphereCollider>();
                if (sc == null) sc = pitObj.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.center = new Vector3(0f, -0.08f, 0f);
                sc.radius = 0.52f;

                // Verify PitZone configuration
                PitZone pz = pitObj.GetComponent<PitZone>();
                if (pz == null) pz = pitObj.AddComponent<PitZone>();
                pz.SetPitNumber(i + 1);
                SerializedObject so = new SerializedObject(pz);
                SerializedProperty maxSpdProp = so.FindProperty("_maxCaptureSpeed");
                if (maxSpdProp != null) maxSpdProp.floatValue = 1.0f;
                SerializedProperty pitNumProp = so.FindProperty("_pitNumber");
                if (pitNumProp != null) pitNumProp.intValue = i + 1;
                so.ApplyModifiedProperties();

                EditorUtility.SetDirty(pitObj);
                Debug.Log($"<color=#00FFAA><b>[CIRCULAR 3D PIT]</b> {pitName} configured with 0.18m depth, in-pit numeral, and seamless ground opening!</color>");
            }

            // Remove any legacy physical flags or poles in the scene
            string[] legacyProps = { "PitMarkerPole_1", "PitMarkerCloth_1", "PitNumber_1",
                                     "PitMarkerPole_2", "PitMarkerCloth_2", "PitNumber_2",
                                     "PitMarkerPole_3", "PitMarkerCloth_3", "PitNumber_3",
                                     "StylizedPitRim_Pit1", "StylizedPitRim_Pit2", "StylizedPitRim_Pit3" };
            foreach (var prop in legacyProps)
            {
                GameObject obj = GameObject.Find(prop);
                if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=#FFD700><b>[PIT UPGRADE COMPLETE]</b> All pits are 100% circular 3D cups with unified ground and unobstructed in-pit numerals.</color>");
        }

        /// <summary>
        /// Creates a single continuous Unified Arena Ground mesh covering the entire playing field
        /// (X: -10 to +10, Z: -10 to +38) at Y=0.0m flat.
        /// Destroys all separate banks, subfloors, and flank slabs to completely eliminate all trenches and cliff walls.
        /// </summary>
        public static void ConfigureUnifiedArenaGround(Material soilMat, PhysicsMaterial physMat)
        {
            GameObject arenaRoot = GameObject.Find("Arena_Sandbox");
            Transform parent = arenaRoot != null ? arenaRoot.transform : null;

            // 1. Destroy all obsolete separate bank and trench slabs that created cliff walls
            string[] obsoleteSlabs = {
                "Ground_Bank_Left", "Ground_Bank_Right", "Ground_Safety_Subfloor",
                "Ground_Center_Start", "Ground_Center_Bridge_1_2", "Ground_Center_Bridge_2_3", "Ground_Center_End",
                "Ground_Pit1_FlankL", "Ground_Pit1_FlankR",
                "Ground_Pit2_FlankL", "Ground_Pit2_FlankR",
                "Ground_Pit3_FlankL", "Ground_Pit3_FlankR"
            };

            foreach (var slabName in obsoleteSlabs)
            {
                GameObject slab = GameObject.Find(slabName);
                if (slab != null) UnityEngine.Object.DestroyImmediate(slab);
            }

            // 2. Generate Unified Full Arena Mesh with 3 Circular Pit Apertures (75% wider: X in [-17.5, 17.5])
            Mesh unifiedMesh = GenerateUnifiedGroundMesh();
            string meshAssetPath = "Assets/_Project/Art/Environments/Village/Fairway_Road_With_Pits_Mesh.asset";
            AssetDatabase.CreateAsset(unifiedMesh, meshAssetPath);
            AssetDatabase.SaveAssets();

            // 3. Configure the single continuous Ground_Center_Fairway object
            GameObject fairwayObj = GameObject.Find("Ground_Center_Fairway");
            if (fairwayObj == null)
            {
                fairwayObj = new GameObject("Ground_Center_Fairway");
                if (parent != null) fairwayObj.transform.SetParent(parent);
            }

            fairwayObj.transform.position = Vector3.zero;
            fairwayObj.transform.rotation = Quaternion.identity;
            fairwayObj.transform.localScale = Vector3.one;

            MeshFilter mf = fairwayObj.GetComponent<MeshFilter>();
            if (mf == null) mf = fairwayObj.AddComponent<MeshFilter>();
            mf.sharedMesh = unifiedMesh;

            MeshRenderer mr = fairwayObj.GetComponent<MeshRenderer>();
            if (mr == null) mr = fairwayObj.AddComponent<MeshRenderer>();
            mr.enabled = true;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;
            if (soilMat != null) mr.sharedMaterial = soilMat;

            MeshCollider mc = fairwayObj.GetComponent<MeshCollider>();
            if (mc == null) mc = fairwayObj.AddComponent<MeshCollider>();
            mc.sharedMesh = unifiedMesh;
            if (physMat != null) mc.sharedMaterial = physMat;

            BoxCollider bc = fairwayObj.GetComponent<BoxCollider>();
            if (bc != null) UnityEngine.Object.DestroyImmediate(bc);

            EditorUtility.SetDirty(fairwayObj);
        }

        /// <summary>
        /// Generates a single continuous procedural mesh for the entire arena ground
        /// (75% wider: X in [-17.5, 17.5], Z in [-10, 38], Y=0.0m) with 3 exact circular holes (r=0.50m, 36 segments)
        /// at Z=3.0, 16.5, 31.0. All normals strictly point +Y (0, 1, 0) with zero inverted faces.
        /// </summary>
        public static Mesh GenerateUnifiedGroundMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "Unified_Arena_Ground_Mesh";

            List<Vector3> verts = new List<Vector3>();
            List<Vector3> norms = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            float uvScale = 0.5f;

            int AddVert(float x, float z)
            {
                int idx = verts.Count;
                verts.Add(new Vector3(x, 0.0f, z));
                norms.Add(Vector3.up);
                uvs.Add(new Vector2(x * uvScale, z * uvScale));
                return idx;
            }

            void AddQuad(int v_bl, int v_br, int v_tr, int v_tl)
            {
                // Top-down normal facing +Y:
                tris.Add(v_bl); tris.Add(v_tl); tris.Add(v_tr);
                tris.Add(v_bl); tris.Add(v_tr); tris.Add(v_br);
            }

            int[][] MakeGrid(float x0, float x1, float z0, float z1, int nx, int nz)
            {
                int[][] grid = new int[nz + 1][];
                for (int iz = 0; iz <= nz; iz++)
                {
                    grid[iz] = new int[nx + 1];
                    float z = Mathf.Lerp(z0, z1, (float)iz / nz);
                    for (int ix = 0; ix <= nx; ix++)
                    {
                        float x = Mathf.Lerp(x0, x1, (float)ix / nx);
                        grid[iz][ix] = AddVert(x, z);
                    }
                }

                for (int iz = 0; iz < nz; iz++)
                {
                    for (int ix = 0; ix < nx; ix++)
                    {
                        AddQuad(grid[iz][ix], grid[iz][ix + 1], grid[iz + 1][ix + 1], grid[iz + 1][ix]);
                    }
                }
                return grid;
            }

            void MakePitBox(float cx, float cz, float halfSize, float rPit, int segments)
            {
                int[] innerIdx = new int[segments];
                int[] outerIdx = new int[segments];

                for (int s = 0; s < segments; s++)
                {
                    float angle = s * 2.0f * Mathf.PI / segments;
                    float cosA = Mathf.Cos(angle);
                    float sinA = Mathf.Sin(angle);

                    // Inner circle vertex
                    float ix = cx + rPit * cosA;
                    float iz = cz + rPit * sinA;
                    innerIdx[s] = AddVert(ix, iz);

                    // Outer box boundary
                    float tx = halfSize / Mathf.Max(Mathf.Abs(cosA), 0.0001f);
                    float tz = halfSize / Mathf.Max(Mathf.Abs(sinA), 0.0001f);
                    float t = Mathf.Min(tx, tz);
                    float ox = cx + t * cosA;
                    float oz = cz + t * sinA;
                    outerIdx[s] = AddVert(ox, oz);
                }

                for (int s = 0; s < segments; s++)
                {
                    int nxt = (s + 1) % segments;
                    tris.Add(innerIdx[s]);
                    tris.Add(outerIdx[nxt]);
                    tris.Add(outerIdx[s]);

                    tris.Add(innerIdx[s]);
                    tris.Add(innerIdx[nxt]);
                    tris.Add(outerIdx[nxt]);
                }
            }

            // Build the full unified arena ground (75% wider: X in [-17.5, 17.5]):
            // Section 0: Z in [-10.0, 1.5]
            MakeGrid(-17.5f, 17.5f, -10.0f, 1.5f, 32, 10);

            // Pit 1 Zone: Z in [1.5, 4.5] (Center Z=3.0, halfSize=1.5)
            MakeGrid(-17.5f, -1.5f, 1.5f, 4.5f, 14, 3);
            MakeGrid(1.5f, 17.5f, 1.5f, 4.5f, 14, 3);
            MakePitBox(0.0f, 3.0f, 1.5f, 0.50f, 36);

            // Bridge 1-2: Z in [4.5, 15.0]
            MakeGrid(-17.5f, 17.5f, 4.5f, 15.0f, 32, 10);

            // Pit 2 Zone: Z in [15.0, 18.0] (Center Z=16.5, halfSize=1.5)
            MakeGrid(-17.5f, -1.5f, 15.0f, 18.0f, 14, 3);
            MakeGrid(1.5f, 17.5f, 15.0f, 18.0f, 14, 3);
            MakePitBox(0.0f, 16.5f, 1.5f, 0.50f, 36);

            // Bridge 2-3: Z in [18.0, 29.5]
            MakeGrid(-17.5f, 17.5f, 18.0f, 29.5f, 32, 10);

            // Pit 3 Zone: Z in [29.5, 32.5] (Center Z=31.0, halfSize=1.5)
            MakeGrid(-17.5f, -1.5f, 29.5f, 32.5f, 14, 3);
            MakeGrid(1.5f, 17.5f, 29.5f, 32.5f, 14, 3);
            MakePitBox(0.0f, 31.0f, 1.5f, 0.50f, 36);

            // Section End: Z in [32.5, 41.5] (extends past back boundary fence at Z=39.50)
            MakeGrid(-17.5f, 17.5f, 32.5f, 41.5f, 32, 10);

            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Generates a deep 3D circular saucer pit cup mesh (radius 0.50m, depth -0.18m).
        /// Features smooth concave sidewalls, recessed basin floor, and inward-curving normals.
        /// </summary>
        private static Mesh CreateDeepPitMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "Deep_Pit_Saucer_Mesh";

            float rimRadius = 0.50f;
            float maxDepth = 0.18f;
            int segments = 36;
            int rings = 8;

            List<Vector3> verts = new List<Vector3>();
            List<Vector3> norms = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            int cIdx = verts.Count;
            verts.Add(new Vector3(0.0f, -maxDepth, 0.0f));
            norms.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f, 0.5f));

            List<int[]> ringIndices = new List<int[]>();
            for (int r = 1; r <= rings; r++)
            {
                float frac = (float)r / rings;
                float curRadius = rimRadius * frac;
                float curY = -maxDepth * Mathf.Cos(frac * Mathf.PI * 0.5f);

                int[] ring = new int[segments];
                for (int s = 0; s < segments; s++)
                {
                    float angle = s * 2.0f * Mathf.PI / segments;
                    float cosA = Mathf.Cos(angle);
                    float sinA = Mathf.Sin(angle);

                    float x = curRadius * cosA;
                    float z = curRadius * sinA;
                    int idx = verts.Count;
                    verts.Add(new Vector3(x, curY, z));
                    uvs.Add(new Vector2(0.5f + 0.5f * frac * cosA, 0.5f + 0.5f * frac * sinA));

                    float slope = (maxDepth * Mathf.PI * 0.5f / rimRadius) * Mathf.Sin(frac * Mathf.PI * 0.5f);
                    float nx = -cosA * slope;
                    float nz = -sinA * slope;
                    float ny = 1.0f;
                    Vector3 n = new Vector3(nx, ny, nz).normalized;
                    norms.Add(n);
                    ring[s] = idx;
                }
                ringIndices.Add(ring);
            }

            for (int s = 0; s < segments; s++)
            {
                int nxt = (s + 1) % segments;
                tris.Add(cIdx);
                tris.Add(ringIndices[0][nxt]);
                tris.Add(ringIndices[0][s]);
            }

            for (int r = 0; r < rings - 1; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int nxt = (s + 1) % segments;
                    int v_in0 = ringIndices[r][s];
                    int v_in1 = ringIndices[r][nxt];
                    int v_out0 = ringIndices[r + 1][s];
                    int v_out1 = ringIndices[r + 1][nxt];

                    tris.Add(v_in0);
                    tris.Add(v_out1);
                    tris.Add(v_out0);

                    tris.Add(v_in0);
                    tris.Add(v_in1);
                    tris.Add(v_out1);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Generates a smooth circular raised 3D lip ring (inner 0.48m, outer 0.54m, height 0.015m)
        /// that frames the circular pit edge cleanly.
        /// </summary>
        private static Mesh GenerateRaisedLipRingMesh(float rIn, float rOut, float height)
        {
            Mesh mesh = new Mesh();
            mesh.name = "Raised_PitLip_Mesh";

            int segments = 36;
            float rMid = (rIn + rOut) * 0.5f;

            List<Vector3> verts = new List<Vector3>();
            List<Vector3> norms = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            int[] inIdx = new int[segments];
            int[] midIdx = new int[segments];
            int[] outIdx = new int[segments];

            for (int s = 0; s < segments; s++)
            {
                float a = s * 2.0f * Mathf.PI / segments;
                float cosA = Mathf.Cos(a);
                float sinA = Mathf.Sin(a);

                inIdx[s] = verts.Count;
                verts.Add(new Vector3(rIn * cosA, 0.0f, rIn * sinA));
                norms.Add(new Vector3(-cosA * 0.5f, 0.866f, -sinA * 0.5f));
                uvs.Add(new Vector2(0.0f, (float)s / segments));

                midIdx[s] = verts.Count;
                verts.Add(new Vector3(rMid * cosA, height, rMid * sinA));
                norms.Add(Vector3.up);
                uvs.Add(new Vector2(0.5f, (float)s / segments));

                outIdx[s] = verts.Count;
                verts.Add(new Vector3(rOut * cosA, 0.0f, rOut * sinA));
                norms.Add(new Vector3(cosA * 0.5f, 0.866f, sinA * 0.5f));
                uvs.Add(new Vector2(1.0f, (float)s / segments));
            }

            for (int s = 0; s < segments; s++)
            {
                int nxt = (s + 1) % segments;
                tris.Add(inIdx[s]); tris.Add(midIdx[nxt]); tris.Add(midIdx[s]);
                tris.Add(inIdx[s]); tris.Add(inIdx[nxt]); tris.Add(midIdx[nxt]);

                tris.Add(midIdx[s]); tris.Add(outIdx[nxt]); tris.Add(outIdx[s]);
                tris.Add(midIdx[s]); tris.Add(midIdx[nxt]); tris.Add(outIdx[nxt]);
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
#endif
