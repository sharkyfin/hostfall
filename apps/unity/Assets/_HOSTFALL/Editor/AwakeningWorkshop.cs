using System;
using System.IO;
using System.Linq;
using Hostfall.Core;
using Hostfall.Devices;
using Hostfall.Player;
using Hostfall.World;
using Hostfall.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Hostfall.Editor
{
    public static class AwakeningWorkshop
    {
        public static string Output=>Path.GetFullPath(Application.dataPath+"/../../../.artifacts/awakening-20261116");
        private static Transform world;
        [MenuItem("HOSTFALL/Tools/Author Awakening Route")]
        public static void Apply()
        {
            WorkstationWorkshop.RequireCopy();EditorSceneManager.OpenScene(FacilityBuilder.ScenePath);world=Object.FindAnyObjectByType<WorkstationSurface>().transform.root;
            var previous=GameObject.Find("Awakening route dressing");if(previous!=null)Object.DestroyImmediate(previous);
            world=new GameObject("Awakening route dressing").transform;
            var metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/_HOSTFALL/Content/Materials/Metal.mat")??Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).First(r=>r.name=="Airlock lower panel").sharedMaterial;
            // The SOC's real north opening leads to WATCHER's corridor. It remains a genuine saved door.
            var old=Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None).FirstOrDefault(d=>d.DoorId=="door.soc.corridor");if(old!=null)Object.DestroyImmediate(old.gameObject);
            var gate=new GameObject("SOC corridor airlock").transform;gate.SetParent(world,false);gate.position=new Vector3(0,0,1);
            GameObject Box(string name,Transform parent,Vector3 p,Vector3 size,Material mat,bool collider=true)
            {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;if(!collider)Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
            for(int side=-1;side<=1;side+=2)Box("SOC portal jamb",gate,new Vector3(side*1.26f,1.4f,0),new Vector3(.12f,2.8f,.3f),metal);
            Box("SOC portal lintel",gate,new Vector3(0,2.85f,0),new Vector3(2.64f,.3f,.3f),metal);
            Object.DestroyImmediate(gate.gameObject);
            var sourceDoor=Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None).Single(d=>d.DoorId=="door.corridor.server");
            gate=Object.Instantiate(sourceDoor.gameObject,world).transform;gate.name="SOC corridor airlock";gate.position=new Vector3(0,0,1);
            var leaf=gate.Find("Sliding leaf");var door=gate.GetComponent<FacilityDoor>();door.Configure(leaf,DoorAccessCondition.WorkstationTerminalComplete,"Шлюз закрыт · восстановите доступ на WS-07","[E] Открыть дверь в коридор","[E] Закрыть дверь в операторскую",false,Vector3.left,2.65f,1.6f,"door.soc.corridor");
            Box("SOC portal upper seal",gate,new Vector3(0,2.84f,0),new Vector3(2.5f,.48f,.3f),metal);
            var paper=new Material(Shader.Find("Universal Render Pipeline/Lit"));paper.SetColor("_BaseColor",new Color(.72f,.69f,.57f));paper.SetFloat("_Smoothness",.12f);
            string path="Assets/_HOSTFALL/Content/Materials/Evacuation paper.mat";if(!File.Exists(path))AssetDatabase.CreateAsset(paper,path);else{Object.DestroyImmediate(paper);paper=AssetDatabase.LoadAssetAtPath<Material>(path);}
            var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/_HOSTFALL/Content/Fonts/NotoSans-Regular.ttf");
            void Note(string name,string text,Vector3 position,Vector3 front,float width=.7f)
            {
                var root=new GameObject(name).transform;root.SetParent(world,false);root.position=position;root.rotation=Quaternion.LookRotation(-front,Vector3.up);
                Box("Worn paper",root,Vector3.zero,new Vector3(width,.62f,.008f),paper,false);
                var go=new GameObject("Evacuation message");go.transform.SetParent(root,false);go.transform.localPosition=new Vector3(0,0,-.006f);
                var label=go.AddComponent<TextMesh>();label.font=font;label.fontSize=64;label.characterSize=.0105f;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.text=text;label.color=new Color(.16f,.07f,.045f);label.richText=false;go.GetComponent<MeshRenderer>().sharedMaterial=font.material;go.AddComponent<WorldTextDepth>().Configure(Shader.Find("HOSTFALL/World Text"));
                // Subtle tape, no floating collision or new light.
                Box("Upper tape",root,new Vector3(.05f,.31f,-.007f),new Vector3(.25f,.065f,.002f),paper,false);
            }
            Note("SOC / save the world","СПАСИ МИР\n\nНе дай Super\nзамкнуть контур.",new Vector3(-1.9f,1.7f,.87f),Vector3.back);
            Note("SOC / AI takeover","ИИ ВСЁ\nЗАХВАТИЛ\n\nДвери. Связь.\nОхрана.",new Vector3(1.9f,1.7f,.88f),Vector3.back);
            Note("Corridor / survivors","НЕ ДОВЕРЯЙ\nТИШИНЕ\n\nОхрана ещё здесь.",new Vector3(1.225f,1.7f,2.5f),Vector3.left,.6f);
            Note("Server / warning","СПАСИ МИР\n\nСначала изолируй\nвнешний контур.",new Vector3(-4.87f,1.65f,8.3f),Vector3.right);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
            Directory.CreateDirectory(Output);WorkstationWorkshop.Capture("soc-gate",new Vector3(-2,1.65f,-2),new Vector3(.5f,1.6f,1));WorkstationWorkshop.Capture("soc-note",new Vector3(-1.9f,1.65f,-1.4f),new Vector3(-1.9f,1.7f,.87f));Debug.Log("Awakening route authored; SOC gate closed, four wall notes");
        }
        [MenuItem("HOSTFALL/Tools/Build Awakening Profile")]
        public static void BuildProfile()=>Build(true);
        [MenuItem("HOSTFALL/Tools/Build Awakening Release")]
        public static void BuildRelease()=>Build(false);
        private static void Build(bool development)
        {
            WorkstationWorkshop.RequireCopy();UnityEditor.OSXStandalone.UserBuildSettings.architecture=UnityEditor.Build.OSArchitecture.x64;
            var target=UnityEditor.Build.NamedBuildTarget.Standalone;var original=PlayerSettings.GetApplicationIdentifier(target);if(development)PlayerSettings.SetApplicationIdentifier(target,"com.northline.hostfall.workstationqa");
            UnityEditor.Build.Reporting.BuildReport report;try{report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{FacilityBuilder.ScenePath},locationPathName="Builds/macOS/HOSTFALL-Awakening"+(development?"-Profile":"")+".app",target=BuildTarget.StandaloneOSX,options=development?BuildOptions.Development:BuildOptions.None});}finally{PlayerSettings.SetApplicationIdentifier(target,original);}
            File.WriteAllText(Path.Combine(Output,development?"profile-build.txt":"release-build.txt"),report.summary.result+" errors="+report.summary.totalErrors+" bytes="+report.summary.totalSize);if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Awakening build failed");
        }
    }
}
