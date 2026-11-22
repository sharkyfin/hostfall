using System;
using System.IO;
using System.Linq;
using Hostfall.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Hostfall.Editor
{
    public static class WeaponAtelierAuthor
    {
        private const string Art="Assets/_HOSTFALL/Art/HostfallCombat";
        private const string Output="Assets/_HOSTFALL/Resources/Combat";
        [MenuItem("HOSTFALL/Tools/Author Weapon Atelier")]
        public static void Apply()
        {
            if(!Application.dataPath.Contains("/.artifacts/"))throw new InvalidOperationException("Use the verification project first.");
            PrepareModels();
            EditorSceneManager.OpenScene(FacilityBuilder.ScenePath);
            foreach(var pickup in Object.FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None))Replace(pickup.transform,"ARC12");
            foreach(var pickup in Object.FindObjectsByType<PulseSidearmPickup>(FindObjectsSortMode.None))Replace(pickup.transform,"CIRRUS");
            foreach(var pickup in Object.FindObjectsByType<TitanbreakerPickup>(FindObjectsSortMode.None))Replace(pickup.transform.Find("W10 TITANBREAKER Inductor Model")??pickup.transform,"TITAN");
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
            Capture();Debug.Log("HOSTFALL Weapon Atelier authored");
        }
        internal static void PrepareModels()
        {
            Directory.CreateDirectory(Art+"/Materials");Directory.CreateDirectory(Output);AssetDatabase.Refresh();
            foreach(string path in Directory.GetFiles(Art+"/Textures","HF2_*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=path.Contains("Normal")?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.sRGBTexture=path.Contains("Surface");importer.wrapMode=TextureWrapMode.Repeat;importer.maxTextureSize=512;importer.SaveAndReimport();
            }
            foreach(string id in new[]{"ARC12","CIRRUS","TITAN"})
            {
                string path=Art+"/HF_"+id+".fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                importer.importAnimation=false;importer.importLights=false;importer.importCameras=false;importer.SaveAndReimport();
                var root=new GameObject(id);root.transform.localScale=Vector3.one*.85f;var geometry=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),root.transform);geometry.name="Authored geometry";
                foreach(var renderer in geometry.GetComponentsInChildren<Renderer>())
                    renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>Resolve(m.name)).ToArray();
                foreach(var t in geometry.GetComponentsInChildren<Transform>())
                {
                    string semantic=t.name.Split('.')[0];
                    if(new[]{"Body","Pump","Bolt","Magazine","Slide","GripRight","GripLeft","Muzzle","Ejection"}.Contains(semantic))t.name=semantic;
                    if(semantic=="GripRight"||semantic=="GripLeft")
                    {
                        bool right=semantic=="GripRight";
                        t.rotation=Quaternion.Euler(10,0,right?70:-70);
                        t.position+=Vector3.right*(right?.04f:id=="CIRRUS"?-.02f:-.065f);
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root,Output+"/"+id+".prefab");Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();
        }
        private static void Replace(Transform pickup,string id)
        {
            foreach(var child in pickup.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Output+"/"+id+".prefab"));
            model.name="Weapon display / "+id;model.transform.SetParent(pickup,false);model.transform.localRotation=Quaternion.Euler(0,90,0);
            var rs=model.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
            model.transform.position+=pickup.position-b.center;
            ColliderProfileRepair.Fit(pickup.GetComponent<BoxCollider>());
        }
        private static Material Resolve(string name)
        {
            string key=name.Replace(" (Instance)","");string path=Art+"/Materials/"+key+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            Color color=key.Contains("Ceramic")?new Color(.19f,.28f,.29f):key.Contains("Steel")?new Color(.38f,.44f,.46f):key.Contains("Copper")?new Color(.34f,.16f,.065f):key.Contains("Ivory")?new Color(.66f,.70f,.64f):key.Contains("Cyan")?new Color(.04f,.46f,.56f):key.Contains("Violet")?new Color(.24f,.075f,.46f):key.Contains("Rubber")?new Color(.035f,.041f,.045f):new Color(.055f,.073f,.085f);
            mat.SetColor("_BaseColor",color);mat.SetTexture("_BaseMap",Tex("HF2_Surface"));
            mat.SetTexture("_BumpMap",Tex("HF2_Normal"));mat.SetFloat("_BumpScale",.35f);mat.EnableKeyword("_NORMALMAP");
            bool metal=key.Contains("Steel")||key.Contains("Copper")||key.Contains("Graphite");
            mat.SetTexture("_MetallicGlossMap",Tex(metal?"HF2_MetalSmooth":"HF2_PaintSmooth"));mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.SetFloat("_Smoothness",key.Contains("Rubber")?.4f:.95f);mat.SetFloat("_Surface",0);mat.SetFloat("_ZWrite",1);mat.renderQueue=2000;
            if(key.Contains("Cyan")||key.Contains("Violet")){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*1.8f);}
            EditorUtility.SetDirty(mat);return mat;
        }
        private static Texture2D Tex(string name)=>AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/"+name+".png");
        [MenuItem("HOSTFALL/Tools/Build Weapon Atelier")]
        public static void BuildDesktop()
        {
            string result=Path.GetDirectoryName(WorldGeometryWorkbench.Output)+"/weapon-quality-20261114/desktop-result.txt";
            UnityEditor.OSXStandalone.UserBuildSettings.architecture=UnityEditor.Build.OSArchitecture.x64;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{FacilityBuilder.ScenePath},locationPathName="Builds/macOS/HOSTFALL-Arsenal.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.None});
            File.WriteAllText(result,report.summary.result+"\n"+report.summary.totalErrors+" errors\n"+report.summary.totalSize+" bytes");
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new InvalidOperationException("Weapon atelier build failed");
        }
        [MenuItem("HOSTFALL/Tools/Review Weapon Atelier")]
        public static void Capture()
        {
            var previous=EditorSceneManager.GetActiveScene();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            var camera=new GameObject("Weapon review camera").AddComponent<Camera>();
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camera.gameObject,scene);
            camera.backgroundColor=new Color(.035f,.048f,.065f);camera.clearFlags=CameraClearFlags.SolidColor;
            camera.nearClipPlane=.01f;camera.farClipPlane=10;camera.cullingMask=1<<30;
            void Light(string name,Vector3 p,Color color,float intensity)
            {
                var go=new GameObject(name);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);go.transform.position=p;
                var light=go.AddComponent<Light>();light.range=5;light.intensity=intensity;light.color=color;light.cullingMask=1<<30;
            }
            Light("Soft key",new Vector3(1,1,.5f),new Color(.85f,.92f,1),5);
            Light("Warm fill",new Vector3(-1,.35f,.4f),new Color(1,.8f,.65f),3);
            Light("Edge",new Vector3(0,.8f,-1),new Color(.45f,.75f,1),4);
            string dir=Path.GetDirectoryName(WorldGeometryWorkbench.Output)+"/weapon-quality-20261114/review";Directory.CreateDirectory(dir);
            try
            {
                foreach(string id in new[]{"ARC12","CIRRUS","TITAN"})
                {
                    var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Output+"/"+id+".prefab"));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(model,scene);
                    foreach(var t in model.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
                    float distance=id=="CIRRUS"?.58f:1.2f;
                    FacilityReview.Shot(camera,dir+"/"+id+"-three-quarter.png",new Vector3(distance,.38f,distance*.75f),new Vector3(0,.06f,.15f),44);
                    FacilityReview.Shot(camera,dir+"/"+id+"-side.png",new Vector3(distance,.06f,.15f),new Vector3(0,.06f,.15f),48);
                    Object.DestroyImmediate(model);
                }
            }
            finally{EditorSceneManager.CloseScene(scene,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);}
        }
    }
}
