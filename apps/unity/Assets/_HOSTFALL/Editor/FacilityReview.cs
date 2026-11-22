using System.IO;
using Hostfall.World;
using Hostfall.Player;
using Unity.Cinemachine;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Hostfall.Editor
{
    // Offscreen art review. No player save is read or written, no gameplay UI is driven.
    public static class FacilityReview
    {
        public static void CaptureWorld()
        {
            var brains=Object.FindObjectsByType<CinemachineBrain>(FindObjectsSortMode.None);
            var states=System.Array.ConvertAll(brains,b=>b.enabled);
            var camera=Object.FindAnyObjectByType<UnityEngine.Camera>();
            var position=camera.transform.position;var rotation=camera.transform.rotation;float fov=camera.fieldOfView;bool enabled=camera.enabled;
            bool post=camera.GetUniversalAdditionalCameraData().renderPostProcessing;
            try
            {
                foreach(var brain in brains)brain.enabled=false;camera.enabled=false;
                foreach(var text in Object.FindObjectsByType<Hostfall.Rendering.WorldTextDepth>(FindObjectsSortMode.None))if(text.enabled){text.enabled=false;text.enabled=true;}
                camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
                string dir=WorldGeometryWorkbench.Output+"/review";Directory.CreateDirectory(dir);
                Shot(camera,dir+"/spawn.png",new Vector3(0,1.64f,-5),new Vector3(0,1.5f,-1),78);
                Shot(camera,dir+"/logistics.png",new Vector3(1.5f,1.64f,64),new Vector3(6,1.7f,69),78);
                Shot(camera,dir+"/robotics.png",new Vector3(22,1.64f,83),new Vector3(34,3.6f,92),78);
                Shot(camera,dir+"/citadel.png",new Vector3(36,1.64f,20.5f),new Vector3(38,3.4f,27),78);
                Shot(camera,dir+"/dispatch.png",new Vector3(21,1.64f,114),new Vector3(28,4,121),78);
                Shot(camera,dir+"/nexus.png",new Vector3(0,1.64f,38),new Vector3(0,1.2f,53),78);
                Shot(camera,dir+"/archive.png",new Vector3(60,1.64f,75),new Vector3(71,1.8f,83),78);
                Shot(camera,dir+"/service-bay.png",new Vector3(48.5f,1.65f,78.2f),new Vector3(51.5f,1.4f,80),70);
                Shot(camera,dir+"/freight-service.png",new Vector3(-21.5f,1.65f,78),new Vector3(-24.4f,1.3f,76),70);
                Shot(camera,dir+"/robotics-west-stairs.png",new Vector3(18,1.64f,88.8f),new Vector3(21,2.8f,91.5f),75);
                Shot(camera,dir+"/dispatch-upper.png",new Vector3(20,5.36f,119.1f),new Vector3(36,4.5f,122.5f),78);
            }
            finally
            {
                camera.transform.SetPositionAndRotation(position,rotation);camera.fieldOfView=fov;camera.enabled=enabled;
                camera.GetUniversalAdditionalCameraData().renderPostProcessing=post;
                for(int i=0;i<brains.Length;i++)brains[i].enabled=states[i];
            }
        }
        public static void CaptureComputeVault()
        {
            EditorSceneManager.OpenScene(FacilityBuilder.ScenePath);
            foreach(var brain in Object.FindObjectsByType<CinemachineBrain>(FindObjectsSortMode.None)) brain.enabled=false;
            foreach(var text in Object.FindObjectsByType<Hostfall.Rendering.WorldTextDepth>(FindObjectsSortMode.None))
            { text.enabled=false; text.enabled=true; }
            var camera=Object.FindAnyObjectByType<UnityEngine.Camera>();camera.enabled=false;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            string directory=System.Environment.GetEnvironmentVariable("HOSTFALL_QA_DIR");
            if(string.IsNullOrEmpty(directory)) throw new System.InvalidOperationException("Set HOSTFALL_QA_DIR for review captures");
            Directory.CreateDirectory(directory);
            Shot(camera,directory+"/entrance.png",new Vector3(0,1.64f,16),new Vector3(4.6f,2.7f,23),72);
            Shot(camera,directory+"/east-stairs.png",new Vector3(4.6f,1.64f,16f),new Vector3(4.6f,3.5f,22.8f),70);
            Shot(camera,directory+"/observation.png",new Vector3(4.6f,4.84f,33.8f),new Vector3(4.6f,4.13f,35.6f),70);
            Shot(camera,directory+"/west-stairs.png",new Vector3(-4.6f,1.64f,16.7f),new Vector3(-4.6f,3.5f,24.3f),70);
            Debug.Log("HOSTFALL S02 visual review: "+directory);
        }
        public static void Capture()
        {
            EditorSceneManager.OpenScene(FacilityBuilder.ScenePath);
            foreach(var brain in Object.FindObjectsByType<CinemachineBrain>(FindObjectsSortMode.None)) brain.enabled=false;
            var camera=Object.FindAnyObjectByType<UnityEngine.Camera>(); camera.enabled=false;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            foreach(var animator in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
            { animator.Rebind(); animator.Play("Idle",0,.25f); animator.Update(0); }
            foreach(var mesh in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None)) mesh.updateWhenOffscreen=true;
            string directory=Path.GetFullPath("../../.artifacts/iteration-6/review"); Directory.CreateDirectory(directory);
            Shot(camera,directory+"/watcher.png",new Vector3(-.65f,1.38f,2.4f),new Vector3(-.4f,.65f,4.9f));
            Shot(camera,directory+"/sentry.png",new Vector3(-.5f,1.42f,8.25f),new Vector3(.7f,.65f,10.2f));
            var body=Object.FindAnyObjectByType<OperatorVisual>(); body.transform.parent.position=new Vector3(0,.035f,-1.5f);
            body.SetView(false,true);
            Shot(camera,directory+"/operator.png",new Vector3(1.4f,1.5f,-3.1f),new Vector3(0,1,-1.5f));
            body.SetView(true,true);
            var eye=body.transform.parent.GetComponent<PlayerMotor>().Eye;
            Shot(camera,directory+"/first-person-body.png",eye,eye+Quaternion.Euler(76,180,0)*Vector3.forward,72);
            Debug.Log("HOSTFALL review images: "+directory);
        }
        internal static void Shot(UnityEngine.Camera camera,string path,Vector3 eye,Vector3 target,float fov=58)
        {
            // PNG is an sRGB display image, rather than a raw linear HDR buffer.
            var texture=new RenderTexture(1440,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB); texture.Create();
            var previous=RenderTexture.active; camera.targetTexture=texture;
            camera.fieldOfView=fov; camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));
            try
            {
                camera.Render(); RenderTexture.active=texture;
                var image=new Texture2D(1440,900,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1440,900),0,0); image.Apply();
                File.WriteAllBytes(path,image.EncodeToPNG()); Object.DestroyImmediate(image);
            }
            finally { camera.targetTexture=null; RenderTexture.active=previous; texture.Release(); Object.DestroyImmediate(texture); }
        }
    }
}
