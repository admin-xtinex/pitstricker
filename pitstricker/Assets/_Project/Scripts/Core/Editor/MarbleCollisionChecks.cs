#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using PitStriker.Physics;
namespace PitStriker.EditorTools
{
    public static class MarbleCollisionChecks
    {
        public static string Run()
        {
            if(!Application.isPlaying) throw new InvalidOperationException("Collision checks require Play Mode callbacks.");
            var lines=new List<string>();
            Check("head-on",0,2,0,lines);
            Check("glancing",.20f,2,0,lines);
            Check("moving target",0,2,.6f,lines);
            Check("gentle contact",0,.35f,0,lines);
            return string.Join("\n",lines);
        }
        static void Check(string name,float offset,float incoming,float target,List<string> lines)
        {
            var scene=SceneManager.CreateScene("CollisionCheck_"+name,new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            var physics=scene.GetPhysicsScene();
            var a=Create(scene,new Vector3(-.5f,5,0)); var b=Create(scene,new Vector3(.1f,5,offset));
            var ar=a.GetComponent<Rigidbody>();var br=b.GetComponent<Rigidbody>();
            ar.linearVelocity=Vector3.right*incoming;br.linearVelocity=Vector3.right*target;
            int events=0;
            Action<MarbleController,MarbleController> hit=(x,y)=>{if((x==a&&y==b)||(x==b&&y==a))events++;};
            MarbleController.OnMarbleHitMarble+=hit;
            try
            {
                float initialEnergy=.5f*(incoming*incoming+target*target);
                for(int i=0;i<300;i++)physics.Simulate(.005f);
                float energy=.5f*(ar.linearVelocity.sqrMagnitude+br.linearVelocity.sqrMagnitude);
                float momentum=(ar.linearVelocity+br.linearVelocity).x;
                if(Mathf.Abs(momentum-incoming-target)>.03f)throw new Exception(name+": momentum changed.");
                if(energy>initialEnergy*1.02f+.001f)throw new Exception(name+": contact added energy.");
                if(events!=1)throw new Exception(name+": expected one collision notification; got "+events);
                if(br.linearVelocity.x<=target)throw new Exception(name+": target did not receive momentum.");
                if(name=="glancing" && Mathf.Abs(ar.linearVelocity.z)<.05f)throw new Exception("Glancing hit did not deflect.");
                lines.Add("PASS "+name+": energy="+energy.ToString("F3")+" <= "+initialEnergy.ToString("F3")+", events="+events);
            }
            finally
            {
                MarbleController.OnMarbleHitMarble-=hit;
                UnityEngine.Object.DestroyImmediate(a.gameObject);UnityEngine.Object.DestroyImmediate(b.gameObject);
                SceneManager.UnloadSceneAsync(scene);
            }
        }
        static MarbleController Create(Scene scene,Vector3 position)
        {
            var go=new GameObject("ValidationMarble");SceneManager.MoveGameObjectToScene(go,scene);go.transform.position=position;
            go.AddComponent<SphereCollider>().radius=.16f;
            var rb=go.AddComponent<Rigidbody>();var marble=go.AddComponent<MarbleController>();
            rb.useGravity=false;rb.linearDamping=0;rb.angularDamping=0;return marble;
        }
    }
}
#endif
