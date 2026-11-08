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
        private static void Shot(UnityEngine.Camera camera,string path,Vector3 eye,Vector3 target,float fov=58)
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
