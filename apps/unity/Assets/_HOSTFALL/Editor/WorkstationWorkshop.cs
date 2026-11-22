using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Hostfall.Devices;
using Hostfall.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Hostfall.Editor
{
    public static class WorkstationWorkshop
    {
        public static string Output
        {
            get
            {
                var directory=new DirectoryInfo(Application.dataPath);
                while(directory!=null&&!File.Exists(Path.Combine(directory.FullName,"AGENTS.md")))directory=directory.Parent;
                var path=Path.Combine(directory.FullName,".artifacts/workstation-20261115");Directory.CreateDirectory(path);return path;
            }
        }
        [MenuItem("HOSTFALL/Tools/Audit Workstation And Compute Seams")]
        public static void Audit()
        {
            RequireCopy();EditorSceneManager.OpenScene(FacilityBuilder.ScenePath);
            var lines=new List<string>();
            foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                var b=renderer.bounds;
                if(b.min.z>40||b.max.z<14||b.min.x>13||b.max.x< -10||!renderer.name.Contains("Wall")&&!renderer.name.Contains("Ceiling")&&!renderer.name.Contains("Door")&&!renderer.name.Contains("Portal"))continue;
                lines.Add(PathOf(renderer.transform)+" | centre="+b.center.ToString("F3")+" size="+b.size.ToString("F3"));
            }
            foreach(var screen in Object.FindObjectsByType<ComputerScreen>(FindObjectsSortMode.None))
            {
                var fields=new SerializedObject(screen);
                var header=fields.FindProperty("header").objectReferenceValue as TextMesh;
                lines.Add("SCREEN "+PathOf(screen.transform)+" root="+screen.transform.position+" header="+(header!=null?header.transform.position+" forward="+header.transform.forward:"null"));
                foreach(var r in screen.GetComponentsInChildren<Renderer>())lines.Add("PART "+PathOf(r.transform)+" | centre="+r.bounds.center.ToString("F3")+" size="+r.bounds.size.ToString("F3"));
            }
            File.WriteAllLines(Path.Combine(Output,"world-inventory.txt"),lines);
            Capture("compute-south-before",new Vector3(0,1.7f,27),new Vector3(0,3,15));
            Capture("compute-east-before",new Vector3(4,1.65f,27),new Vector3(8,1.65f,27));
            Capture("compute-upper-before",new Vector3(4.6f,4.85f,32),new Vector3(0,4.2f,16));
            Debug.Log("HOSTFALL workstation/seam audit written to "+Output);
        }
        private const string Art="Assets/_HOSTFALL/Art/WorkstationAtelier";
        [MenuItem("HOSTFALL/Tools/Author Physical Workstations")]
        public static void Apply()
        {
            RequireCopy();AssetDatabase.Refresh();File.Copy(Path.Combine(Output,"scene-before.unity"),FacilityBuilder.ScenePath,true);AssetDatabase.ImportAsset(FacilityBuilder.ScenePath,ImportAssetOptions.ForceSynchronousImport);EditorSceneManager.OpenScene(FacilityBuilder.ScenePath);
            Capture("compute-south-before",new Vector3(2,1.7f,24),new Vector3(0,4,15));
            RepairSeams();
            foreach(var screen in Object.FindObjectsByType<ComputerScreen>(FindObjectsSortMode.None))ReplaceScreen(screen);
            foreach(var item in Object.FindObjectsByType<DataCartridgeItem>(FindObjectsSortMode.None))ReplaceMedia(item.transform);
            foreach(var item in Object.FindObjectsByType<PhysicalMediaItem>(FindObjectsSortMode.None))ReplaceMedia(item.transform);
            foreach(var slot in Object.FindObjectsByType<DataCartridgeSlot>(FindObjectsSortMode.None))
            {
                var field=new SerializedObject(slot);var old=field.FindProperty("insertedCartridgeModel").objectReferenceValue as GameObject;
                if(old!=null){var p=old.transform.position;var q=old.transform.rotation;var model=Model("HF_ArchiveKey",old.transform.parent);model.transform.SetPositionAndRotation(p,q);field.FindProperty("insertedCartridgeModel").objectReferenceValue=model;field.ApplyModifiedPropertiesWithoutUndo();Object.DestroyImmediate(old);model.SetActive(false);}
            }
            if(Object.FindObjectsByType<DataCartridgeItem>(FindObjectsSortMode.None).Any(item=>item.GetComponent<Collider>()==null))throw new InvalidOperationException("Missing media interaction collider");
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
            Capture("compute-south-after",new Vector3(2,1.7f,24),new Vector3(0,4,15));
            Capture("compute-east-after",new Vector3(4,1.65f,27),new Vector3(8,1.65f,27));
            Capture("compute-upper-after",new Vector3(4.6f,4.85f,32),new Vector3(0,4.2f,16));
            Capture("workstation-data-after",new Vector3(.8f,1.65f,24.5f),new Vector3(0,1.2f,26));
            Debug.Log("Physical workstations and shared S02 seams authored");
        }
        public static void RepairSeams()
        {
            Transform Find(string name)=>Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Single(t=>t.name==name);
            var left=Find("Server North Wall Left");var right=Find("Server North Wall Right");
            left.position=new Vector3(-3.25f,1.5f,15);right.position=new Vector3(3.25f,1.5f,15);left.localScale=right.localScale=new Vector3(3.5f,3,.4f);
            Object.DestroyImmediate(Find("Server North Wall Lintel").gameObject);Object.DestroyImmediate(Find("Server-to-Sector02 Bulkhead").gameObject);
            foreach(var name in new[]{"East Wall Armory Header","West Wall Airlock Header"}){var h=Find(name);h.position=new Vector3(name.StartsWith("East")?7.05f:-7.05f,4.3f,26);h.localScale=new Vector3(.5f,2.2f,3);}
            var parent=left.parent;var material=left.GetComponent<Renderer>().sharedMaterial;
            void Wall(string name,Vector3 p,Vector3 size)
            {
                if(parent.Find(name)!=null)return;var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,true);go.transform.position=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;
            }
            Wall("S02 South Upper Left",new Vector3(-3.25f,4.2f,15),new Vector3(3.5f,2.4f,.4f));
            Wall("S02 South Upper Right",new Vector3(3.25f,4.2f,15),new Vector3(3.5f,2.4f,.4f));
            Wall("S02 South Upper Portal",new Vector3(0,4.35f,15),new Vector3(3,2.1f,.4f));
            foreach(string name in new[]{"Armory West Wall South","Armory West Wall North","Armory West Doorway Lintel"})
                foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name==name))Object.DestroyImmediate(t.gameObject);
        }
        private static GameObject Model(string name,Transform parent)
        {
            var importer=(ModelImporter)AssetImporter.GetAtPath(Art+"/"+name+".fbx");importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
            var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/"+name+".fbx"),parent);go.name=name;go.transform.localPosition=Vector3.zero;
            foreach(var renderer in go.GetComponentsInChildren<Renderer>())
            {renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>ResolveMaterial(m.name)).ToArray();renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
            return go;
        }
        private static Material ResolveMaterial(string name)
        {
            string key=name.Split('.')[0];Directory.CreateDirectory(Art+"/Materials");var path=Art+"/Materials/"+key+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material!=null)return material;
            material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var color=key.Contains("Enamel")?new Color(.17f,.24f,.25f):key.Contains("Metal")?new Color(.48f,.53f,.54f):key.Contains("Accent")?new Color(.12f,.62f,.67f):key.Contains("Contact")?new Color(.62f,.38f,.12f):new Color(.022f,.03f,.038f);
            material.SetColor("_BaseColor",color);material.SetFloat("_Metallic",key.Contains("Metal")||key.Contains("Contact")?.8f:.3f);material.SetFloat("_Smoothness",.4f);
            if(key.Contains("Accent")){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*.8f);}
            if(key.Contains("Screen")){Object.DestroyImmediate(material);material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",new Color(.008f,.018f,.025f));}
            AssetDatabase.CreateAsset(material,path);return material;
        }
        private static Transform Marker(GameObject model,string name)=>model.GetComponentsInChildren<Transform>().Single(t=>t.name.Split('.')[0]==name);
        internal static void ReplaceScreen(ComputerScreen screen)
        {
            var root=screen.transform;var computer=root.GetComponent<ComputerTerminal>();var door=root.GetComponent<DoorTerminalConsole>();
            var id=computer!=null?computer.Id:door.DeviceId;var fields=new SerializedObject(screen);var oldHeader=fields.FindProperty("header").objectReferenceValue as TextMesh;
            var front=-oldHeader.transform.forward;float desk=computer!=null?.83f:1f;
            var pedestal=root.GetComponentsInChildren<Renderer>().FirstOrDefault(r=>r.name=="Terminal Pedestal"||r.name=="Mac Desk Slab"||r.name=="Desktop"||r.name=="Desk Top"||r.name=="Win Desk Slab");if(pedestal!=null)desk=pedestal.bounds.max.y-root.position.y;
            TerminalOsFamily os=TerminalOsFamily.LinuxBash;if(computer==null&&TerminalScenarioCatalog.TryGetScenario(id.ToString(),out var scenario))os=scenario.OsFamily;
            string modelName=os==TerminalOsFamily.MacOsZsh?"HF_StudioConsole":os==TerminalOsFamily.WindowsPowerShell?"HF_OperationsPC":id==DeviceId.Edge01||id==DeviceId.Pwr01?"HF_FieldNotebook":"HF_OperationsPC";
            // Keep desks, curated dressing, lights, sockets, device colliders and stable IDs.
            foreach(var child in root.Cast<Transform>().ToArray())
            {
                string n=child.name;
                if(child.GetComponent<TextMesh>()!=null||n.StartsWith("Display")||n=="Live readouts"||n.Contains("Monitor")||n.Contains("Keyboard")||n.Contains("Keybed")||n.Contains("Mouse")||n.Contains("Tower")||n.Contains("Screen")||n.Contains("Badge")||n=="Computer"||n=="Stand"||n=="Display"||n=="Readouts"||n.StartsWith("HF_")||n.Contains("Status LED"))Object.DestroyImmediate(child.gameObject);
            }
            foreach(var child in root.GetComponentsInChildren<Transform>().Where(t=>t!=root&&(t.name=="Keyboard"||t.name=="Keycap"||t.name=="Mouse"||t.name=="Mouse pad"||t.name=="Monitor base")).ToArray())if(child!=null)Object.DestroyImmediate(child.gameObject);
            var model=Model(modelName,root);var tl=Marker(model,"DisplayTL");var tr=Marker(model,"DisplayTR");var bl=Marker(model,"DisplayBL");var br=Marker(model,"DisplayBR");
            var normal=-Vector3.Cross(tr.position-tl.position,tl.position-bl.position).normalized;var up=(tl.position-bl.position).normalized;
            model.transform.rotation=Quaternion.AngleAxis(Vector3.SignedAngle(Vector3.ProjectOnPlane(normal,Vector3.up).normalized,Vector3.ProjectOnPlane(front,Vector3.up).normalized,Vector3.up),Vector3.up)*model.transform.rotation;
            var bounds=model.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});model.transform.position+=root.position+Vector3.up*desk-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            var surface=root.GetComponent<WorkstationSurface>()??root.gameObject.AddComponent<WorkstationSurface>();surface.Configure(id,os,tl,tr,bl,br);
            var hitSurface=new GameObject("Screen interaction surface");hitSurface.transform.SetParent(root,true);hitSurface.transform.SetPositionAndRotation(surface.Centre,Quaternion.LookRotation(surface.Normal,surface.Up));var screenCollider=hitSurface.AddComponent<BoxCollider>();screenCollider.size=new Vector3(Vector3.Distance(tl.position,tr.position),Vector3.Distance(tl.position,bl.position),.035f);
            TextMesh Label(string name,float down,float fraction,int size)
            {
                var go=new GameObject(name);go.transform.SetParent(root,true);go.transform.SetPositionAndRotation(tl.position+(bl.position-tl.position)*down+(tr.position-tl.position)*.035f+surface.Normal*.008f,Quaternion.LookRotation(-surface.Normal,surface.Up));
                var text=go.AddComponent<TextMesh>();text.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/_HOSTFALL/Content/Fonts/RobotoMono-Regular.ttf");text.fontSize=size;text.characterSize=Vector3.Distance(tl.position,bl.position)*fraction*10/size;text.anchor=TextAnchor.UpperLeft;text.color=new Color(.65f,.8f,.8f);text.richText=false;
                go.AddComponent<Hostfall.Rendering.WorldTextDepth>().Configure(Shader.Find("HOSTFALL/World Text"));return text;
            }
            var header=Label("Live screen header",.04f,.055f,64);var body=Label("Live screen transcript",.16f,.047f,64);var footer=Label("Live screen prompt",.91f,.045f,64);screen.Configure(header,body,footer);header.text=id.Hostname().ToUpperInvariant();body.text="Канал готов";footer.text="";
            var screenRenderer=Marker(model,"DisplayGlass").GetComponent<Renderer>();
            if(computer!=null){var data=new SerializedObject(computer);data.FindProperty("screen").objectReferenceValue=screenRenderer;data.FindProperty("online").objectReferenceValue=screenRenderer.sharedMaterial;data.FindProperty("offline").objectReferenceValue=screenRenderer.sharedMaterial;data.FindProperty("readouts").objectReferenceValue=null;data.ApplyModifiedPropertiesWithoutUndo();}
            if(door!=null){var data=new SerializedObject(door);data.FindProperty("screenRenderer").objectReferenceValue=screenRenderer;data.FindProperty("screenStatusText").objectReferenceValue=null;data.FindProperty("screenBodyText").objectReferenceValue=null;data.FindProperty("statusLedRenderer").objectReferenceValue=null;data.ApplyModifiedPropertiesWithoutUndo();}
            var light=root.GetComponentInChildren<Light>();if(light!=null)
            {
                var button=Model("HF_ServicePanel",root);button.transform.rotation=model.transform.rotation;button.transform.position=root.position+Vector3.up*desk+(tr.position-tl.position).normalized*.70f+surface.Normal*.28f;
                var mount=new GameObject("Worklight switch / "+id);mount.transform.SetParent(root,true);mount.transform.SetPositionAndRotation(button.transform.position,button.transform.rotation);button.transform.SetParent(mount.transform,true);var panel=mount.AddComponent<ServiceButtonPanel>();panel.Configure("worklight."+id,light);var collider=mount.AddComponent<BoxCollider>();collider.center=new Vector3(0,.1f,0);collider.size=new Vector3(.18f,.22f,.09f);
            }
            Debug.Log("WORKSTATION "+id+" model="+modelName+" front="+surface.Normal+" screen="+Vector3.Distance(tl.position,tr.position)+"x"+Vector3.Distance(tl.position,bl.position));
        }
        private static void ReplaceMedia(Transform root)
        {
            var renderers=root.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return;var b=renderers.Select(r=>r.bounds).Aggregate((a,c)=>{a.Encapsulate(c);return a;});
            foreach(var child in root.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
            if(root.TryGetComponent<Renderer>(out var r))Object.DestroyImmediate(r);if(root.TryGetComponent<MeshFilter>(out var filter))Object.DestroyImmediate(filter);
            root.localScale=Vector3.one;var model=Model("HF_ArchiveKey",root);model.transform.position=new Vector3(b.center.x,b.min.y,b.center.z);if(root.GetComponent<PhysicalMediaItem>()!=null)model.transform.localScale*=.6f;
            if(!root.TryGetComponent<BoxCollider>(out var collider))collider=root.gameObject.AddComponent<BoxCollider>();ColliderProfileRepair.Fit(collider);
        }
        [MenuItem("HOSTFALL/Tools/Inspect Workstation Repair")]
        public static void InspectRepair()
        {
            var lines=new List<string>();
            foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                if(r.bounds.max.z>=14.8f&&r.bounds.min.z<=15.2f&&r.bounds.min.x<5&&r.bounds.max.x> -5)
                    lines.Add(PathOf(r.transform)+" p="+r.transform.position+" scale="+r.transform.localScale+" bounds="+r.bounds+" mesh="+(r.GetComponent<MeshFilter>()!=null?r.GetComponent<MeshFilter>().sharedMesh.bounds.ToString():""));
            File.WriteAllLines(Path.Combine(Output,"repair-details.txt"),lines);
        }
        [Serializable] private class RecoveryEnvelope{public string payload,sha256;}
        [MenuItem("HOSTFALL/Tools/Validate Recovery Candidates")]
        public static void ValidateRecovery()
        {
            RequireCopy();var lines=new List<string>();
            foreach(var path in Directory.GetFiles(Path.Combine(Output,"user-saves-after-initial-diagnostics"),"*v2.json*"))
            {
                try
                {
                    var envelope=JsonUtility.FromJson<RecoveryEnvelope>(File.ReadAllText(path));
                    using var hash=System.Security.Cryptography.SHA256.Create();
                    string sha=BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(envelope.payload))).Replace("-","").ToLowerInvariant();
                    if(sha!=envelope.sha256.ToLowerInvariant())throw new InvalidOperationException("Checksum mismatch");
                    var data=JsonUtility.FromJson<Hostfall.Persistence.GameSaveData>(envelope.payload);Hostfall.Persistence.GameSaveData.Validate(data);
                    var model=FacilitySimulation.Restore(data.simulation,MissionDefinition.FacilityCampaign);
                    var catalog=new Hostfall.Simulation.FieldCatalog(JsonUtility.FromJson<Hostfall.Simulation.FieldContentData>(Resources.Load<TextAsset>("FieldContent").text));
                    using var field=new Hostfall.Simulation.FieldSession(catalog,model);field.RestoreProgress(data.field);
                    lines.Add(Path.GetFileName(path)+" VALID progress="+model.Mission.CompletedObjectives+" position="+data.player.position);
                    if(model.Mission.CompletedObjectives==3&&path.Contains("090214")){data.simulation=model.Capture();data.field=field.CaptureProgress();new Hostfall.Persistence.WorldSaveStore(Path.Combine(Output,"recovered-campaign")).Write(data);}
                }
                catch(Exception e){lines.Add(Path.GetFileName(path)+" REJECTED "+e.GetType().Name+": "+e.Message);}
            }
            File.WriteAllLines(Path.Combine(Output,"recovery-validation.txt"),lines);Debug.Log("Recovery candidates validated");
        }
        [MenuItem("HOSTFALL/Tools/Build Workstation Profile")]
        public static void BuildProfile()=>Build("HOSTFALL-Workstation-Profile",true);
        [MenuItem("HOSTFALL/Tools/Build Workstation Release")]
        public static void BuildRelease()=>Build("HOSTFALL-Workstations",false);
        private static void Build(string name,bool development)
        {
            RequireCopy();UnityEditor.OSXStandalone.UserBuildSettings.architecture=UnityEditor.Build.OSArchitecture.x64;
            var originalIdentifier=PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone);
            if(development)PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone,"com.northline.hostfall.workstationqa");
            UnityEditor.Build.Reporting.BuildReport report;
            try{report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{FacilityBuilder.ScenePath},locationPathName="Builds/macOS/"+name+".app",target=BuildTarget.StandaloneOSX,options=development?BuildOptions.Development:BuildOptions.None});}
            finally{PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone,originalIdentifier);}
            File.WriteAllText(Path.Combine(Output,name+"-build.json"),JsonUtility.ToJson(new BuildReceipt{result=report.summary.result.ToString(),errors=report.summary.totalErrors,bytes=report.summary.totalSize,nativeOutput=PlayerSettings.defaultIsNativeResolution}));
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new InvalidOperationException("Workstation build failed");
        }
        [Serializable]private class BuildReceipt{public string result;public int errors;public ulong bytes;public bool nativeOutput;}
        public static void RequireCopy()
        {if(!Application.dataPath.Contains("/.artifacts/"))throw new InvalidOperationException("Inspect and repair the verification copy first.");}
        public static string PathOf(Transform item)=>item.parent==null?item.name:PathOf(item.parent)+"/"+item.name;
        public static void Capture(string name,Vector3 position,Vector3 target)
        {
            var go=new GameObject("Workbench camera");var camera=go.AddComponent<Camera>();camera.transform.position=position;camera.transform.LookAt(target);camera.fieldOfView=65;camera.nearClipPlane=.04f;
            var rt=RenderTexture.GetTemporary(1280,800,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var old=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var texture=new Texture2D(1280,800,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,800),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(Output,name+".png"),texture.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(texture);Object.DestroyImmediate(go);
        }
    }
}
