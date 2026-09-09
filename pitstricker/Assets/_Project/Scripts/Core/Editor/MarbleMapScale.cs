#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace PitStriker.EditorTools
{
    public static class MarbleMapScale
    {
        public const float MarbleRadius=.16f;
        public const float PitRadius=.34f;
        public static void Apply(Scene scene)
        {
            var roots=scene.GetRootGameObjects();
            foreach(var marble in roots.SelectMany(r=>r.GetComponentsInChildren<PitStriker.Physics.MarbleController>(true)))
            {
                var col=marble.GetComponent<SphereCollider>();
                float radius=col.radius*Mathf.Abs(marble.transform.lossyScale.x);
                if(radius>0) marble.transform.localScale*=MarbleRadius/radius;
                var pos=marble.transform.position; pos.y=MarbleRadius+.015f; marble.transform.position=pos;
            }
        }
    }
}
#endif
