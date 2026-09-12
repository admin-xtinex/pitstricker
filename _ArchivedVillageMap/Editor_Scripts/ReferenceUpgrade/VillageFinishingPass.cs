#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace PitStriker.EditorTools
{
    internal static class VillageFinishingPass
    {
        internal static void Build(Transform parent, Material wood, Material foliage)
        {
            var chalk=VillageSurfaceLibrary.Material("Course_Chalk","Universal Render Pipeline/Unlit",new Color(.88f,.83f,.66f));
            var rings=new VillageMeshBuilder();
            var pits=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PitStriker.Gameplay.PitZone>(true)).OrderBy(p=>p.transform.position.z).ToArray();
            for(int index=0;index<pits.Length;index++)
            {
                Vector3 center=pits[index].transform.position; center.y=.022f;
                for(int j=0;j<64;j++)
                {
                    float a=j*Mathf.PI*2/64,b=(j+1)*Mathf.PI*2/64;
                    Vector3 u=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),v=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));
                    rings.Quad(center+u*.58f,center+v*.58f,center+v*.605f,center+u*.605f,Color.white,Color.white);
                }
            }
            rings.Emit("Course_Chalk_Rings",parent,chalk);
        }
        static void Label(Transform parent,string name,string value,Vector3 position,float scale,Color color)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=position;
            var text=go.AddComponent<TextMesh>();text.text=value;text.fontSize=64;text.characterSize=scale;
            text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=color;
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            go.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;
        }
    }
}
#endif

