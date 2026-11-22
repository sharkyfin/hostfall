using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Hostfall.Devices;
using Hostfall.Player;
using Hostfall.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Hostfall.Editor
{
    public static partial class Hostfall2Workbench
    {
        public static string Output
        {get{var root=new DirectoryInfo(Application.dataPath);while(root!=null&&!File.Exists(Path.Combine(root.FullName,"AGENTS.md")))root=root.Parent;var path=Path.Combine(root.FullName,".artifacts/hostfall2-20261117");Directory.CreateDirectory(path);return path;}}
        [Serializable]private class Station{public string path,id,scenario;public Vector3 position;public bool surface;public int renderers,colliders;}
        [Serializable]private class Door{public string id,path;public Vector3 position,size;public string condition;}
        [Serializable]private class Actor{public string id,type,path;public Vector3 position;}
        [Serializable]private class AuditData{public string scene,unity;public Vector3 spawn;public Station[] stations;public Door[] doors;public Actor[] actors;public int floors,renderers,colliders;}
        public const string PreviewScene="Assets/_HOSTFALL/Content/Scenes/Hostfall2Facility.unity";
        [MenuItem("HOSTFALL/2.0/Build First Loop QA")]
        public static void BuildFirstLoopQa()
        {
            WorkstationWorkshop.RequireCopy();UnityEditor.OSXStandalone.UserBuildSettings.architecture=UnityEditor.Build.OSArchitecture.x64;
            var target=UnityEditor.Build.NamedBuildTarget.Standalone;string previous=PlayerSettings.GetApplicationIdentifier(target);
            UnityEditor.Build.Reporting.BuildReport report;
            try
            {
                PlayerSettings.SetApplicationIdentifier(target,"com.northline.hostfall.computerqa");
                report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{PreviewScene},locationPathName="Builds/macOS/HOSTFALL2-FirstLoop-QA.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.Development});
            }
            finally{PlayerSettings.SetApplicationIdentifier(target,previous);}
            File.WriteAllText(Path.Combine(Output,"first-loop-qa-build.txt"),report.summary.result+" errors="+report.summary.totalErrors+" bytes="+report.summary.totalSize);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("First-loop QA build failed");
        }
        [MenuItem("HOSTFALL/2.0/Author First Loop Preview")]
        public static void AuthorFirstLoop()
        {
            WorkstationWorkshop.RequireCopy();
            EditorSceneManager.OpenScene(FacilityBuilder.ScenePath);
            File.Copy(FacilityBuilder.ScenePath,Path.Combine(Output,"scene-before-first-loop.unity"),true);
            var root=Object.FindAnyObjectByType<Hostfall.Core.FacilityRoot>();
            var rootFields=new SerializedObject(root);
            var oldMission=(Hostfall.Missions.IncidentMissionAsset)rootFields.FindProperty("mission").objectReferenceValue;
            const string missionPath="Assets/_HOSTFALL/Content/Hostfall2Mission.asset";
            var mission=AssetDatabase.LoadAssetAtPath<Hostfall.Missions.IncidentMissionAsset>(missionPath);
            if(mission==null){mission=Object.Instantiate(oldMission);AssetDatabase.CreateAsset(mission,missionPath);}
            mission.UseInteractiveComputers();EditorUtility.SetDirty(mission);
            rootFields.FindProperty("mission").objectReferenceValue=mission;rootFields.ApplyModifiedPropertiesWithoutUndo();
            var consoles=Object.FindObjectsByType<DoorTerminalConsole>(FindObjectsSortMode.None);
            var home=consoles.Single(c=>new SerializedObject(c).FindProperty("scenarioId").stringValue=="t00_home");
            var homeFields=new SerializedObject(home);homeFields.FindProperty("deviceId").enumValueIndex=Array.IndexOf(Enum.GetValues(typeof(Hostfall.Simulation.DeviceId)).Cast<Hostfall.Simulation.DeviceId>().ToArray(),Hostfall.Simulation.DeviceId.Home01);
            // Enum serialization uses underlying values, not display order.
            homeFields.FindProperty("deviceId").intValue=(int)Hostfall.Simulation.DeviceId.Home01;homeFields.ApplyModifiedPropertiesWithoutUndo();
            var screen=home.gameObject.AddComponent<ComputerScreen>();
            var temporary=new GameObject("Live screen header");temporary.transform.SetParent(home.transform,false);temporary.transform.rotation=Quaternion.Euler(0,180,0);
            var header=temporary.AddComponent<TextMesh>();screen.Configure(header,header,header);WorkstationWorkshop.ReplaceScreen(screen);
            var all=new List<(GameObject go,Hostfall.Simulation.DeviceId id,string scenario)>();
            foreach(var terminal in Object.FindObjectsByType<ComputerTerminal>(FindObjectsSortMode.None))all.Add((terminal.gameObject,terminal.Id,""));
            foreach(var terminal in consoles)all.Add((terminal.gameObject,terminal==home?Hostfall.Simulation.DeviceId.Home01:terminal.DeviceId,new SerializedObject(terminal).FindProperty("scenarioId").stringValue));
            foreach(var item in all)
            {
                string id=item.id switch
                {
                    Hostfall.Simulation.DeviceId.Home01=>"pc.home",Hostfall.Simulation.DeviceId.Ws07=>"pc.ventflow",
                    Hostfall.Simulation.DeviceId.Edge01=>"pc.buffer",Hostfall.Simulation.DeviceId.Core01=>"pc.netdef",
                    Hostfall.Simulation.DeviceId.Data01=>"pc.fragments",Hostfall.Simulation.DeviceId.Pwr01=>"pc.circuit",
                    Hostfall.Simulation.DeviceId.Qnt01=>"pc.signal",Hostfall.Simulation.DeviceId.Cargo01=>"pc.payload",
                    Hostfall.Simulation.DeviceId.Rbt01=>"pc.containment",Hostfall.Simulation.DeviceId.Sec07=>"pc.minefield",
                    Hostfall.Simulation.DeviceId.Neuro01=>"pc.detective",Hostfall.Simulation.DeviceId.Cloud09=>"pc.quorum",_=>"pc.integrity"
                };
                var surface=item.go.GetComponent<WorkstationSurface>();
                if(surface==null||!surface.Valid)throw new InvalidOperationException("Missing station surface: "+id);
                var station=item.go.AddComponent<ComputerStation>();station.Configure(id,surface);
                var oldComputer=item.go.GetComponent<ComputerTerminal>();if(oldComputer!=null)Object.DestroyImmediate(oldComputer);
                var oldConsole=item.go.GetComponent<DoorTerminalConsole>();if(oldConsole!=null)Object.DestroyImmediate(oldConsole);
                var oldScreen=item.go.GetComponent<ComputerScreen>();if(oldScreen!=null)Object.DestroyImmediate(oldScreen);
                foreach(var label in item.go.GetComponentsInChildren<TextMesh>())Object.DestroyImmediate(label.gameObject);
            }
            var motor=Object.FindAnyObjectByType<PlayerMotor>();motor.transform.SetPositionAndRotation(new Vector3(-2.6f,.08f,-14.3f),Quaternion.Euler(0,180,0));
            foreach(var door in Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None).Where(d=>d.DoorId=="door.soc.corridor"||d.DoorId=="door.corridor.server"))
            {
                var so=new SerializedObject(door);so.FindProperty("condition").enumValueIndex=(int)DoorAccessCondition.ManualUnlockOnly;
                so.FindProperty("lockedPromptText").stringValue="Шлюз без питания · проверьте VENTFLOW в операторской";
                so.FindProperty("autoOpenOnUnlock").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();
            }
            var source=Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None).Single(w=>w.Id=="watcher-r17");
            var robot=Object.Instantiate(source.gameObject,source.transform.parent);robot.name="Vent quarantine / R19";
            var watcher=robot.GetComponent<WatcherController>();var fields=new SerializedObject(watcher);
            fields.FindProperty("enemyId").stringValue="vent-r19";fields.FindProperty("patrolA").vector3Value=new Vector3(2.2f,0,-2.2f);fields.FindProperty("patrolB").vector3Value=new Vector3(-1,0,-5.2f);
            fields.FindProperty("xBounds").vector2Value=new Vector2(-3.8f,3.8f);fields.FindProperty("zBounds").vector2Value=new Vector2(-6.4f,.3f);fields.ApplyModifiedPropertiesWithoutUndo();
            robot.transform.SetPositionAndRotation(new Vector3(4.15f,0,-2.2f),Quaternion.Euler(0,270,0));
            var duct=new GameObject("Ventilation / vent.soc.alpha").transform;duct.SetParent(root.transform,false);duct.position=new Vector3(4.1f,0,-2.2f);
            var metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/_HOSTFALL/Content/Materials/Dark Metal.mat");
            if(metal==null)metal=source.GetComponentsInChildren<Renderer>().First().sharedMaterial;
            GameObject Box(string name,Vector3 position,Vector3 size,Transform parent)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=metal;return go;
            }
            Box("Duct roof",new Vector3(0,1.75f,0),new Vector3(1.5f,.12f,1.9f),duct);
            Box("Duct north side",new Vector3(0,.85f,.94f),new Vector3(1.5f,1.8f,.12f),duct);
            Box("Duct south side",new Vector3(0,.85f,-.94f),new Vector3(1.5f,1.8f,.12f),duct);
            var grate=new GameObject("Sliding vent grate").transform;grate.SetParent(duct,false);grate.localPosition=new Vector3(-.8f,0,0);
            for(int i=0;i<8;i++)Box("Grate horizontal "+i,new Vector3(0,.12f+i*.21f,0),new Vector3(.07f,.065f,1.8f),grate);
            for(int i=-1;i<=1;i+=2)Box("Grate rail",new Vector3(0,.87f,i*.88f),new Vector3(.1f,1.8f,.09f),grate);
            var release=duct.gameObject.AddComponent<VentReleaseController>();release.Configure(grate,watcher,robot.transform.position,new Vector3(2.5f,0,-2.2f));
            var lampGo=new GameObject("VENTFLOW / door supply lamp");lampGo.transform.SetParent(root.transform,false);lampGo.transform.position=new Vector3(1.3f,2.35f,.55f);
            var lamp=lampGo.AddComponent<Light>();lamp.type=LightType.Point;lamp.color=new Color(.18f,.9f,.55f);lamp.intensity=2;lamp.range=3;lamp.enabled=false;
            var world=root.gameObject.AddComponent<ComputerWorldController>();world.Configure(release,lamp);
            var pistol=Object.FindObjectsByType<PulseSidearmPickup>(FindObjectsSortMode.None).FirstOrDefault();
            if(pistol!=null)
            {
                var copy=Object.Instantiate(pistol.gameObject,pistol.transform.parent);copy.name="Shelter / CIRRUS emergency sidearm";copy.transform.position=new Vector3(2.6f,.93f,-16.4f);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),PreviewScene);RepairFirstVentClearance();
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(PreviewScene,true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=PreviewScene)).ToArray();
            File.WriteAllText(Path.Combine(Output,"first-loop-authoring.txt"),"Preview authored in isolated copy. 13 station IDs. Home spawn + physical surface; two manual gates; vent R19 + local supply lamp. Iteration1 acceptance pending.\n");
            Debug.Log("HOSTFALL 2 first-loop preview authored: "+PreviewScene);
        }
        [MenuItem("HOSTFALL/2.0/Repair First Vent Clearance")]
        public static void RepairFirstVentClearance()
        {
            WorkstationWorkshop.RequireCopy();EditorSceneManager.OpenScene(PreviewScene);
            var vent=Object.FindAnyObjectByType<VentReleaseController>();var delta=new Vector3(0,0,-5.6f-vent.transform.position.z);
            vent.transform.position+=delta;vent.Actor.transform.position+=delta;
            var fields=new SerializedObject(vent);fields.FindProperty("hiddenPosition").vector3Value+=delta;fields.FindProperty("exitPosition").vector3Value+=delta;fields.ApplyModifiedPropertiesWithoutUndo();
            var rack=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Single(t=>t.name=="Security Weapon Rack - SOC Security Post Rack");rack.position=new Vector3(4.8f,1.35f,-12);rack.rotation=Quaternion.Euler(0,-90,0);
            var crate=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Single(t=>t.name=="Prop_Crate3"&&(Vector3.Distance(t.position,new Vector3(4.15f,0,-5.75f))<.8f||Vector3.Distance(t.position,new Vector3(3.9f,0,-10.3f))<.2f));crate.position=new Vector3(3.9f,0,-10.3f);
            Physics.SyncTransforms();
            var obstacles=Physics.OverlapBox(new Vector3(4.1f,.85f,-5.6f),new Vector3(.7f,.8f,.88f),Quaternion.identity,Hostfall.Core.CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore)
                .Where(c=>!c.transform.IsChildOf(vent.transform)&&!c.transform.IsChildOf(vent.Actor.transform)).ToArray();
            if(obstacles.Length>0)throw new InvalidOperationException("Vent body still overlaps: "+string.Join(",",obstacles.Select(c=>c.name)));
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            File.WriteAllText(Path.Combine(Output,"vent-clearance-repair.txt"),"Moved duct from side-door route to south-east corner; original rack/crate retained in shelter. Interior overlap scan: 0. Passage CharacterController test pending.\n");
        }
        [MenuItem("HOSTFALL/2.0/Inspect Vent Placement")]
        public static void InspectVentPlacement()
        {
            WorkstationWorkshop.RequireCopy();EditorSceneManager.OpenScene(PreviewScene);Physics.SyncTransforms();
            var lines=new List<string>();
            foreach(float z in new[]{-5.6f,-5.2f,-4.8f,-2.2f})
            {
                lines.Add("CANDIDATE z="+z);
                foreach(var c in Physics.OverlapBox(new Vector3(4.1f,.85f,z),new Vector3(.75f,.84f,1.0f),Quaternion.identity,Hostfall.Core.CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore))
                    lines.Add(WorkstationWorkshop.PathOf(c.transform)+" "+c.bounds);
                lines.Add("EXIT "+string.Join(",",Physics.OverlapCapsule(new Vector3(2.5f,.35f,z),new Vector3(2.5f,1,z),.28f,Hostfall.Core.CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore).Select(c=>c.name)));
            }
            File.WriteAllLines(Path.Combine(Output,"vent-placement.txt"),lines);
        }
        [MenuItem("HOSTFALL/2.0/Audit Saved Facility")]
        public static void Audit()
        {
            WorkstationWorkshop.RequireCopy();EditorSceneManager.OpenScene(FacilityBuilder.ScenePath);var stations=new List<Station>();
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                var console=t.GetComponent<DoorTerminalConsole>();var terminal=t.GetComponent<ComputerTerminal>();if(console==null&&terminal==null)continue;
                stations.Add(new Station{path=WorkstationWorkshop.PathOf(t),id=(terminal!=null?terminal.Id:console.DeviceId).ToString(),scenario=console!=null?new SerializedObject(console).FindProperty("scenarioId").stringValue:"first-route",position=t.position,surface=t.GetComponent<WorkstationSurface>()!=null,renderers=t.GetComponentsInChildren<Renderer>().Length,colliders=t.GetComponentsInChildren<Collider>().Length});
            }
            var doors=Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None).Select(d=>new Door{id=d.DoorId,path=WorkstationWorkshop.PathOf(d.transform),position=d.transform.position,size=d.GetComponentsInChildren<Collider>().Select(c=>c.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;}).size,condition=new SerializedObject(d).FindProperty("condition").enumDisplayNames[new SerializedObject(d).FindProperty("condition").enumValueIndex]}).ToArray();
            var actors=Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None).Select(a=>new Actor{id=a.Id,type=a.GetType().Name,path=WorkstationWorkshop.PathOf(a.transform),position=a.transform.position}).ToArray();
            var all=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);var report=new AuditData{scene=FacilityBuilder.ScenePath,unity=Application.unityVersion,spawn=Object.FindAnyObjectByType<PlayerMotor>().transform.position,stations=stations.ToArray(),doors=doors,actors=actors,renderers=all.Length,colliders=Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Length,floors=all.Count(r=>r.name.Contains("Floor")||r.name.Contains("floor"))};
            File.WriteAllText(Path.Combine(Output,"scene-audit.json"),JsonUtility.ToJson(report,true));
            WorkstationWorkshop.Capture("hf2-home-before",new Vector3(0,1.7f,-13),new Vector3(-2.6f,1.3f,-16.5f));WorkstationWorkshop.Capture("hf2-soc-before",new Vector3(0,1.7f,-5),new Vector3(-2.65f,1.2f,-3.9f));
            foreach(string n in new[]{"hf2-home-before","hf2-soc-before"})File.Copy(Path.Combine(WorkstationWorkshop.Output,n+".png"),Path.Combine(Output,n+".png"),true);
            Debug.Log("HOSTFALL 2 audit: "+stations.Count+" computers, "+doors.Length+" doors, "+actors.Length+" watcher actors");
        }
    }
}
