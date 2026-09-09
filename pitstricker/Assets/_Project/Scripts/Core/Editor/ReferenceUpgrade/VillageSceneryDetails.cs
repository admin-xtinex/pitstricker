#if UNITY_EDITOR
using UnityEngine;

namespace PitStriker.EditorTools
{
    internal static class VillageSceneryDetails
    {
        internal static void Build(Transform parent, Material wood, Material plaster, Material stone, Material soil, Material mountain)
        {
            var joinery = new VillageMeshBuilder();
            // Door frame, recessed window mullions and a fascia give the facade a readable scale.
            foreach (float z in new[] { 6.8f, 8.2f, 10f, 11.8f, 13f })
            {
                joinery.Box(new Vector3(-4.87f, 1.65f, z), new Vector3(.09f, 1.2f, .035f));
                joinery.Box(new Vector3(-4.85f, 1.65f, z), new Vector3(.09f, .045f, .75f));
            }
            joinery.Box(new Vector3(-4.36f, 3.0f, 9.8f), new Vector3(.17f, .18f, 7.6f));
            for (int i=0;i<8;i++) joinery.Box(new Vector3(-4.8f,2.96f,6.3f+i),new Vector3(1.1f,.12f,.09f));
            joinery.Emit("House_Joinery",parent,wood);

            var gables = new VillageMeshBuilder();
            gables.Triangle(new Vector3(-9.45f,3.18f,6.29f),new Vector3(-7.2f,4.18f,6.29f),new Vector3(-4.95f,3.18f,6.29f),Color.white);
            gables.Triangle(new Vector3(-4.95f,3.18f,13.31f),new Vector3(-7.2f,4.18f,13.31f),new Vector3(-9.45f,3.18f,13.31f),Color.white);
            gables.Emit("House_Gables",parent,plaster);
            var steps = new VillageMeshBuilder();
            steps.Box(new Vector3(-4.0f,.055f,9.7f),new Vector3(.55f,.11f,1.4f));
            steps.Box(new Vector3(-4.25f,.12f,9.7f),new Vector3(.45f,.24f,1.25f));
            steps.Emit("House_VerandaSteps",parent,stone);

            // Small stones break up the verge transition, with a clear central shooting corridor.
            var r = new System.Random(1791); var gravel = new VillageMeshBuilder();
            for (int i=0;i<800;i++)
            {
                float z=-8+(float)r.NextDouble()*52;
                float x=(i%2==0?-1:1)*(1.6f+(float)r.NextDouble()*7);
                float s=.012f+(float)r.NextDouble()*.045f;
                Vector3 c=new Vector3(x,s*.15f,z);
                Vector3 top=c+Vector3.up*s*.55f;
                for(int j=0;j<5;j++)
                {
                    float a=j*Mathf.PI*2/5,b=(j+1)*Mathf.PI*2/5;
                    gravel.Triangle(c+new Vector3(Mathf.Cos(a)*s,0,Mathf.Sin(a)*s*.7f),top,
                        c+new Vector3(Mathf.Cos(b)*s,0,Mathf.Sin(b)*s*.7f),Color.white);
                }
            }
            gravel.Emit("Verge_Gravel",parent,stone);

            var surround = new VillageMeshBuilder();
            for(int x=-44;x<44;x+=2) for(int z=-16;z<76;z+=2)
            {
                if(x>=-8&&x<8&&z<42) continue;
                surround.Quad(new Vector3(x,-.06f,z),new Vector3(x,-.06f,z+2),new Vector3(x+2,-.06f,z+2),new Vector3(x+2,-.06f,z),Color.white,Color.white);
            }
            surround.Emit("Surrounding_Earth",parent,soil);
            // A single connected terrain avoids repeating conical silhouettes.
            var ridge = new VillageMeshBuilder();
            for(int x=-90;x<90;x+=3) for(int z=65;z<146;z+=3)
            {
                ridge.Quad(Point(x,z),Point(x,z+3),Point(x+3,z+3),Point(x+3,z),Color.white,Color.white);
            }
            ridge.Emit("Distant_Ridge",parent,mountain);
        }

        static Vector3 Point(float x,float z)
        {
            float h=18*Mathf.Exp(-Mathf.Pow((x+28)/24,2)-Mathf.Pow((z-101)/24,2));
            h+=24*Mathf.Exp(-Mathf.Pow((x-15)/29,2)-Mathf.Pow((z-116)/23,2));
            h+=15*Mathf.Exp(-Mathf.Pow((x-53)/22,2)-Mathf.Pow((z-103)/24,2));
            h*=.92f+.16f*Mathf.PerlinNoise(x*.07f+20,z*.07f);
            return new Vector3(x,h-.08f,z);
        }
    }
}
#endif
