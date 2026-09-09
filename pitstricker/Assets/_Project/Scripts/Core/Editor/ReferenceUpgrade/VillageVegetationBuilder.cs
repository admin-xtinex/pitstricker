#if UNITY_EDITOR
using System;
using UnityEngine;

namespace PitStriker.EditorTools
{
    internal static class VillageVegetationBuilder
    {
        static float Range(System.Random r, float a, float b) => a + (b - a) * (float)r.NextDouble();
        static Vector3 Direction(float angle) => new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));

        internal static void Build(Transform parent, Material grass, Material canopy, Material bark)
        {
            // World coordinates agree with the imported scenery after Unity's FBX axis conversion.
            // Grass is batched in 8 m cells, rather than thousands of GameObjects.
            for (int side = -1; side <= 1; side += 2) for (int row = 0; row < 7; row++)
            {
                var r = new System.Random(1709 + row * 47 + side * 13);
                var high = new VillageMeshBuilder(); var low = new VillageMeshBuilder();
                for (int i = 0; i < 620; i++)
                {
                    float z = -9 + row * 8 + Range(r, 0, 8);
                    float edge = 2.6f + .42f * Mathf.Sin(z * .39f) + .22f * Mathf.Sin(z * 1.2f);
                    float outer = r.NextDouble() < .9 ? edge + 2.8f : 9f;
                    float x = side * Range(r, edge, outer);
                    // Gaps and islands prevent the continuous hedgerow look.
                    float patch = Mathf.PerlinNoise(x * .48f + 40, z * .48f + 40);
                    if (patch < .28f || (Mathf.Abs(x) > 6 && r.NextDouble() < .5)) continue;
                    Vector3 root = new Vector3(x, .015f, z);
                    float height = Range(r, .16f, .32f), shade = Range(r, .72f, 1);
                    for (int j = 0; j < 6; j++)
                    {
                        Vector3 d = Direction(Range(r, 0, Mathf.PI * 2));
                        Vector3 p = root + d * Range(r, 0, .05f);
                        float h = height * Range(r, .65f, 1.25f), width = Range(r, .012f, .022f);
                        high.Blade(p, d, h, width, h * .55f, 3, shade);
                        if (j % 3 == 0) low.Blade(p, d, h, width * 1.3f, h * .5f, 2, shade);
                    }
                }
                var tile = new GameObject("Meadow_" + side + "_" + row); tile.transform.SetParent(parent, false);
                var near = high.Emit(tile.name + "_Near", tile.transform, grass);
                var far = low.Emit(tile.name + "_Far", tile.transform, grass);
                var lod = tile.AddComponent<LODGroup>();
                lod.SetLODs(new[] { new LOD(.24f, new[] { near }), new LOD(.055f, new[] { far }) });
                lod.RecalculateBounds();
            }

            var random = new System.Random(2193);
            // Staggered side planting fills the empty horizon while leaving the lane visible.
            for (int i = 0; i < 22; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * Range(random, 11, 26);
                float z = Range(random, -6, 63);
                // Keep the house silhouette visible from the launch camera.
                if (x < 0 && z > 3 && z < 18) x -= 6;
                Broadleaf(parent, new Vector3(x, 0, z), Range(random, 3.8f, 7), i, canopy, bark);
            }
            for (int i = 0; i < 15; i++)
                Broadleaf(parent, new Vector3(-28 + i * 4.2f, 0, Range(random, 48, 64)), Range(random, 4, 8), i + 40, canopy, bark);

            Vector3[] palms = { new Vector3(-7,0,-5),new Vector3(8,0,-3),new Vector3(-10,0,18),new Vector3(9,0,19),
                new Vector3(-8,0,33),new Vector3(10,0,35),new Vector3(-13,0,47),new Vector3(5,0,49) };
            for (int i = 0; i < palms.Length; i++) Palm(parent, palms[i], 7 + i % 3, i, canopy, bark);
        }

        static void Broadleaf(Transform parent, Vector3 position, float height, int seed, Material foliage, Material bark)
        {
            var random = new System.Random(seed + 841);
            var trunk = new VillageMeshBuilder(); var leaves = new VillageMeshBuilder(); var distant = new VillageMeshBuilder();
            Vector3 fork = position + Vector3.up * height * .58f;
            trunk.Tube(position, fork, .20f, .10f, 9);
            for (int branch = 0; branch < 7; branch++)
            {
                Vector3 direction = Direction(branch * 2.4f);
                Vector3 end = fork + direction * Range(random, .8f, 1.8f) + Vector3.up * Range(random, .6f, 1.9f);
                trunk.Tube(fork, end, .08f, .015f, 6);
                for (int j = 0; j < 150; j++)
                {
                    Vector3 offset = new Vector3(Range(random,-1,1),Range(random,-.55f,.55f),Range(random,-1,1));
                    if (offset.sqrMagnitude > 1.2f) continue;
                    Vector3 origin = end + offset;
                    Vector3 d = Direction(Range(random,0,Mathf.PI*2)) + Vector3.up * Range(random,-.5f,.5f);
                    float length = Range(random,.23f,.43f), shade = Range(random,.62f,1);
                    leaves.Leaf(origin,d,length,.075f,.08f,shade);
                    if (j % 3 == 0) distant.Leaf(origin,d,length * 1.45f,.11f,.08f,shade);
                }
            }
            var obj = new GameObject("Broadleaf_" + seed); obj.transform.SetParent(parent, false);
            var wood = trunk.Emit(obj.name + "_Trunk", obj.transform, bark);
            var near = leaves.Emit(obj.name + "_Leaves", obj.transform, foliage);
            var far = distant.Emit(obj.name + "_Distant", obj.transform, foliage);
            var lod = obj.AddComponent<LODGroup>();
            lod.SetLODs(new[] { new LOD(.12f,new[] { wood,near }),new LOD(.025f,new[] { wood,far }) });
            lod.RecalculateBounds();
        }

        static void Palm(Transform parent, Vector3 position, float height, int seed, Material foliage, Material bark)
        {
            var trunk = new VillageMeshBuilder(); var fronds = new VillageMeshBuilder();
            Vector3 last = position;
            for (int i = 0; i < 12; i++)
            {
                float t = (i + 1) / 12f;
                Vector3 end = position + new Vector3(.35f*t*t,height*t,.2f*t*t);
                trunk.Tube(last,end,.16f*(1-i*.035f),.16f*(1-(i+1)*.035f),9); last=end;
            }
            for (int f = 0; f < 11; f++)
            {
                Vector3 d = Direction(f * Mathf.PI * 2 / 11 + seed * .3f), side = Vector3.Cross(Vector3.up,d);
                for (int j = 1; j < 17; j++)
                {
                    float t=j/18f;
                    Vector3 p=last+d*(2.6f*t)+Vector3.up*(.65f*Mathf.Sin(t*Mathf.PI)-.8f*t*t);
                    float length=.48f*Mathf.Sin(t*Mathf.PI)+.1f;
                    fronds.Leaf(p,side+d*.35f,length,.026f,.13f,.86f);
                    fronds.Leaf(p,-side+d*.35f,length,.026f,.13f,1);
                }
            }
            trunk.Emit("Palm_"+seed+"_Trunk",parent,bark);
            fronds.Emit("Palm_"+seed+"_Fronds",parent,foliage);
        }
    }
}
#endif
