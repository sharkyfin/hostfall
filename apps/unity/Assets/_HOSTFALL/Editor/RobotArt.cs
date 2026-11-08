using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace Hostfall.Editor
{
    internal static class RobotArt
    {
        private const string Source="Assets/_HOSTFALL/Art/QuaterniusRobots/";
        private const string Output=FacilityBuilder.Content+"/Prefabs/Robots/";
        internal static void Prepare()
        {
            Directory.CreateDirectory(Output); AssetDatabase.Refresh();
            foreach(string path in Directory.GetFiles(Source+"Textures","*.png"))
            {
                var t=(TextureImporter)AssetImporter.GetAtPath(path); t.maxTextureSize=1024;
                t.textureType=path.Contains("Normal")?TextureImporterType.NormalMap:TextureImporterType.Default;
                t.sRGBTexture=path.Contains("BaseColor")||path.Contains("Emissive"); t.SaveAndReimport();
            }
            foreach(string id in new[] { "Enemy_QuadShell", "Enemy_Trilobite" }) PrepareModel(id);
        }
        private static void PrepareModel(string id)
        {
            string path=Source+id+".fbx"; var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.importAnimation=true; importer.animationType=ModelImporterAnimationType.Generic; importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importCameras=false; importer.importLights=false; importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard; importer.SaveAndReimport();
            var clips=importer.defaultClipAnimations;
            foreach(var clip in clips) { clip.loopTime=clip.name.EndsWith("Idle")||clip.name.EndsWith("Walk")||clip.name.EndsWith("Run"); clip.lockRootPositionXZ=true; clip.lockRootHeightY=true; clip.lockRootRotation=true; }
            importer.clipAnimations=clips; importer.SaveAndReimport();
            var animations=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            Debug.Log("HOSTFALL robot "+id+" clips: "+string.Join(",",animations.Select(c=>c.name)));
            string controllerPath=Output+id+".controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)??AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine=controller.layers[0].stateMachine; foreach(var state in machine.states) machine.RemoveState(state.state);
            foreach(string name in new[] { "Idle", "Walk", "Run", "Attack", "TurnOff" })
            {
                var clip=animations.FirstOrDefault(c=>c.name==name||c.name.EndsWith("|"+name));
                if(clip==null) throw new System.InvalidOperationException("Missing robot clip "+id+" / "+name);
                var state=machine.AddState(name); state.motion=clip; if(name=="Idle") machine.defaultState=state;
            }
            var root=new GameObject(id); var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),root.transform); model.name="Animated geometry";
            var animator=model.GetComponent<Animator>()??model.AddComponent<Animator>(); animator.runtimeAnimatorController=controller;
            animator.applyRootMotion=false; animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var renderers=model.GetComponentsInChildren<Renderer>(); var bounds=renderers[0].bounds; foreach(var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            model.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            root.transform.localScale=Vector3.one*(.86f/Mathf.Max(bounds.size.x,bounds.size.z));
            var material=RobotMaterial(id=="Enemy_Trilobite"?"T_Enemies_Large":"T_Enemies");
            foreach(var r in renderers) r.sharedMaterials=r.sharedMaterials.Select(_=>material).ToArray();
            Debug.Log("HOSTFALL robot "+id+" bounds "+bounds.size+" scale "+root.transform.localScale);
            PrefabUtility.SaveAsPrefabAsset(root,Output+id+".prefab"); Object.DestroyImmediate(root); EditorUtility.SetDirty(controller);
        }
        private static Material RobotMaterial(string prefix)
        {
            string path=FacilityBuilder.Content+"/Materials/Robot "+prefix+".mat"; var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null) { m=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path); }
            Texture2D Tex(string suffix)=>AssetDatabase.LoadAssetAtPath<Texture2D>(Source+"Textures/"+prefix+suffix+".png");
            m.SetColor("_BaseColor",new Color(.75f,.8f,.73f)); m.SetFloat("_Surface",0); m.SetFloat("_ZWrite",1); m.renderQueue=2000;
            m.SetTexture("_BaseMap",Tex("_BaseColor")); m.SetTexture("_BumpMap",Tex("_Normal")); m.EnableKeyword("_NORMALMAP");
            m.SetTexture("_MetallicGlossMap",Tex("_MetalSmooth")); m.EnableKeyword("_METALLICSPECGLOSSMAP"); m.SetFloat("_Smoothness",.7f);
            m.SetTexture("_OcclusionMap",Tex("_Occlusion")); m.EnableKeyword("_OCCLUSIONMAP");
            m.SetTexture("_EmissionMap",Tex("_Emissive")); m.SetColor("_EmissionColor",new Color(.85f,.38f,.1f)*.7f); m.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(m); return m;
        }
        internal static Animator Place(string id,Transform parent)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Output+id+".prefab"));
            go.transform.SetParent(parent,false); return go.GetComponentInChildren<Animator>();
        }
    }
}
