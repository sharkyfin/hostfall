using System;
using System.IO;
using System.Linq;
using Hostfall.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Hostfall.Editor
{
    public static class CombatWorldUpgrade
    {
        private const string Art="Assets/_HOSTFALL/Art/HostfallCombat";
        private const string Output="Assets/_HOSTFALL/Resources/Combat";
        [MenuItem("HOSTFALL/Tools/Upgrade Combat Workshop")]
        public static void ApplyAndSave()
        {
            if(!Application.dataPath.Contains("/.artifacts/"))throw new InvalidOperationException("Use the isolated verification project first.");
            EditorSceneManager.OpenScene(FacilityBuilder.ScenePath);
            PrepareModels();RepairUpperShell();UpgradeRobots();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();Capture();Debug.Log("HOSTFALL combat workshop saved");
        }
        private static void PrepareModels()
        {
            Directory.CreateDirectory(Output);Directory.CreateDirectory(Art+"/Materials");AssetDatabase.Refresh();
            bool atelier=File.Exists(Art+"/Textures/HF2_Surface.png");
            if(atelier)WeaponAtelierAuthor.PrepareModels();
            foreach(var id in atelier?new[]{"MANTIS6"}:new[]{"ARC12","CIRRUS","TITAN","MANTIS6"})
            {
                string path=Art+"/HF_"+id+".fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                importer.importAnimation=false;importer.importLights=false;importer.importCameras=false;importer.SaveAndReimport();
                var root=new GameObject(id);var geometry=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),root.transform);geometry.name="Authored geometry";
                foreach(var r in geometry.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>MaterialFor(m!=null?m.name:"HF Combat graphite")).ToArray();
                var rs=root.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs.Skip(1))bounds.Encapsulate(r.bounds);
                geometry.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                if(id!="MANTIS6")geometry.transform.localPosition+=new Vector3(0,-.16f,.25f);
                PrefabUtility.SaveAsPrefabAsset(root,Output+"/"+id+".prefab");Object.DestroyImmediate(root);
            }
        }
        private static Material MaterialFor(string source)
        {
            string key=source.Replace(" (Instance)","");string path=Art+"/Materials/"+key+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat!=null)return mat;
            mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=key};
            Color c=key.Contains("ceramic")?new Color(.22f,.29f,.3f):key.Contains("steel")?new Color(.32f,.39f,.4f):key.Contains("copper")?new Color(.48f,.22f,.1f):key.Contains("bone")?new Color(.62f,.61f,.5f):key.Contains("cyan")?new Color(.12f,.6f,.68f):key.Contains("red")?new Color(.65f,.04f,.025f):key.Contains("violet")?new Color(.4f,.1f,.6f):new Color(.045f,.065f,.075f);
            mat.SetColor("_BaseColor",c);mat.SetFloat("_Metallic",key.Contains("rubber")?0:.65f);mat.SetFloat("_Smoothness",.48f);
            if(key.Contains("cyan")||key.Contains("red")||key.Contains("violet")){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",c*1.8f);}
            AssetDatabase.CreateAsset(mat,path);return mat;
        }
        public static void RepairUpperShell()
        {
            var all=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach(var t in all.Where(t=>t.name=="S07 Foyer North Wall L"||t.name=="S07 Foyer North Wall R"||t.name=="S07 Foyer North Wall Lintel").ToArray())Object.DestroyImmediate(t.gameObject);
            var robotics=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name=="Sector 06 - Robotics Plant");
            if(robotics.Find("SEC07 Aperture left infill")==null)
            {
                Box(robotics,"SEC07 Aperture left infill",new Vector3(27.6125f,2,64.75f),new Vector3(1.225f,4,.5f),"Concrete");
                Box(robotics,"SEC07 Aperture right infill",new Vector3(32.3875f,2,64.75f),new Vector3(1.225f,4,.5f),"Concrete");
                Box(robotics,"SEC07 Aperture head infill",new Vector3(30,3.9125f,64.75f),new Vector3(3.55f,.175f,.5f),"Dark Metal");
            }
            var sector=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name=="Sector 02 - Compute Vault");
            foreach(var t in sector.Cast<Transform>().Where(t=>t.name.StartsWith("TopCables_Straight_Hanging")).ToArray())
            {
                float z=t.position.z;Object.DestroyImmediate(t.gameObject);
                for(int i=0;i<3;i++)Box(sector,"S02 Cable conduit",new Vector3(0,5.12f-i*.065f,z),new Vector3(6.4f,.045f,.045f),"Dark Metal");
                foreach(float x in new[]{-2.7f,2.7f})Box(sector,"S02 Conduit bracket",new Vector3(x,5.24f,z),new Vector3(.06f,.24f,.1f),"Industrial Metal");
            }
            if(sector.Find("S02 Upper perimeter east")==null)
            {
                Box(sector,"S02 Upper perimeter east",new Vector3(6.7f,5.23f,26.5f),new Vector3(.32f,.26f,23.2f),"Dark Metal");
                Box(sector,"S02 Upper perimeter west",new Vector3(-6.7f,5.23f,26.5f),new Vector3(.32f,.26f,23.2f),"Dark Metal");
            }
        }
        private static GameObject Box(Transform p,string name,Vector3 pos,Vector3 size,string material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(p,false);go.transform.position=pos;go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(FacilityBuilder.Content+"/Materials/"+material+".mat");return go;
        }
        private static void UpgradeRobots()
        {
            foreach(var crawler in Object.FindObjectsByType<TrackedSentryController>(FindObjectsSortMode.None))
            {
                foreach(var child in crawler.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Output+"/MANTIS6.prefab"));model.transform.SetParent(crawler.transform,false);
                var sensor=model.GetComponentsInChildren<Transform>().First(t=>t.name=="Sensor");
                var muzzle=model.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name.Contains("Emitter"))??sensor;
                bool reactor=crawler.transform.position.x<0;
                crawler.Configure(null,model.transform,muzzle,reactor?new Vector3(-21,0,24):new Vector3(0,0,76),reactor?new Vector3(-12,0,32):new Vector3(0,0,86),reactor?"sentry-crawler-e06":"crawler-logistics-r35");
                crawler.name=reactor?"MANTIS-6 Reactor Guardian":"MANTIS-6 Freight Guardian";
                var col=crawler.GetComponent<BoxCollider>();col.center=new Vector3(0,.85f,0);col.size=new Vector3(3.35f,1.7f,3.4f);
                crawler.transform.rotation=Quaternion.LookRotation(reactor?Vector3.right:Vector3.back);
                Physics.SyncTransforms();
                FindClearPose(crawler.transform,reactor?new Vector2(-23,-9):new Vector2(-3,3),reactor?new Vector2(21,35):new Vector2(67,88),new Vector3(1.73f,.82f,1.82f));
            }
            var template=Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None).First(w=>w.Id=="sentry-r18");
            var specs=new[]{(new Vector3(-19,0,12),new Vector3(-17,0,12),new Vector3(-20,0,15),new Vector2(-21,-15),new Vector2(9,16)),(new Vector3(-18,0,72),new Vector3(-20,0,69),new Vector3(-14,0,74),new Vector2(-22,-12),new Vector2(66,78)),(new Vector3(47,0,82),new Vector3(45,0,80),new Vector3(49,0,84),new Vector2(43,50),new Vector2(79,88)),(new Vector3(60,0,73),new Vector3(58,0,72),new Vector3(64,0,76),new Vector2(56,67),new Vector2(69,80))};
            int index=0;
            foreach(var spec in specs)
            {
                string id="aux-watch-"+(++index).ToString("00");if(Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None).Any(w=>w.Id==id))continue;
                var go=Object.Instantiate(template.gameObject,template.transform.parent);go.name="Patrol "+id;go.transform.position=spec.Item1;
                var so=new SerializedObject(go.GetComponent<WatcherController>());so.FindProperty("enemyId").stringValue=id;so.FindProperty("patrolA").vector3Value=spec.Item2;so.FindProperty("patrolB").vector3Value=spec.Item3;so.FindProperty("xBounds").vector2Value=spec.Item4;so.FindProperty("zBounds").vector2Value=spec.Item5;so.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach(var actor in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IDamageable>().Where(a=>a is not Hostfall.Player.PlayerHealth&&a is not WatcherController&&a is not TrackedSentryController))
            {
                var t=((MonoBehaviour)actor).transform;var p=t.position;
                Vector3 entrance=p.x<-8?new Vector3(0,p.y,p.z):p.x>52?new Vector3(53,p.y,p.z):new Vector3(p.x,p.y,p.z-8);
                t.rotation=Quaternion.LookRotation(entrance-p);
            }
            foreach(var w in Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None))
            {
                Physics.SyncTransforms();
                var setup=new SerializedObject(w);
                if(w.Id.StartsWith("aux-watch-"))FindClearPose(w.transform,setup.FindProperty("xBounds").vector2Value,setup.FindProperty("zBounds").vector2Value,new Vector3(.28f,.48f,.28f));
                var p=w.transform.position;Vector3 entrance=p.z<18?new Vector3(0,0,-4):p.x<0?new Vector3(0,0,p.z):new Vector3(p.x,0,p.z-8);
                var direction=entrance-p;direction.y=0;if(direction.sqrMagnitude>.1f)w.transform.rotation=Quaternion.LookRotation(direction);
                var body=w.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Body")??w.transform;
                if(body.Find("ArmorL0")!=null)continue;
                for(int i=0;i<4;i++)
                {
                    var plate=Box(w.transform,"Armor"+(i%2==0?"L":"R")+i,w.transform.TransformPoint(new Vector3(i%2==0?-.31f:.31f,.65f,i<2?-.17f:.17f)),new Vector3(.07f,.19f,.22f),"Industrial Metal");
                    plate.transform.rotation=w.transform.rotation;Object.DestroyImmediate(plate.GetComponent<Collider>());plate.transform.SetParent(body,true);
                }
            }
            RobotArmorAuthor.WithReadableSources(()=>{foreach(var watcher in Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None))RobotArmorAuthor.Fit(watcher);});
        }
        private static void FindClearPose(Transform actor,Vector2 xb,Vector2 zb,Vector3 half)
        {
            Vector3 original=actor.position,best=original;float nearest=float.PositiveInfinity;
            for(float x=xb.x;x<=xb.y;x+=.5f)for(float z=zb.x;z<=zb.y;z+=.5f)
            {
                var p=new Vector3(x,original.y,z);
                if(!Physics.Raycast(p+Vector3.up*.3f,Vector3.down,.6f,Hostfall.Core.CollisionLayers.WorldMask))continue;
                var hits=Physics.OverlapBox(p+Vector3.up*(half.y+.06f),half,actor.rotation,Hostfall.Core.CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore);
                if(hits.Any(c=>!c.transform.IsChildOf(actor)))continue;
                float distance=(p-original).sqrMagnitude;if(distance<nearest){best=p;nearest=distance;}
            }
            if(float.IsPositiveInfinity(nearest))throw new InvalidOperationException("No clear robot spawn for "+actor.name);
            actor.position=best;Physics.SyncTransforms();
        }
        [MenuItem("HOSTFALL/Tools/Build Combat Desktop")]
        public static void BuildDesktop()
        {
            string result=Path.GetDirectoryName(WorldGeometryWorkbench.Output)+"/combat-rework-20261114/desktop-result.txt";
            UnityEditor.OSXStandalone.UserBuildSettings.architecture=UnityEditor.Build.OSArchitecture.x64;
            try
            {
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{FacilityBuilder.ScenePath},locationPathName="Builds/macOS/HOSTFALL-Combat.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.None});
                File.WriteAllText(result,report.summary.result+"\n"+report.summary.totalErrors+" errors\n"+report.summary.totalSize+" bytes");
                if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new InvalidOperationException("Combat build failed");
            }
            catch(Exception error){File.WriteAllText(result,"FAILED: "+error);throw;}
        }
        [MenuItem("HOSTFALL/Tools/Review Combat Workshop")]
        public static void Capture()
        {
            string dir=WorldGeometryWorkbench.Output+"/combat";Directory.CreateDirectory(dir);
            File.WriteAllLines(dir+"/upper-bounds.txt",Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.bounds.center.z>15&&r.bounds.center.z<38&&r.bounds.min.x>-8&&r.bounds.max.x<8&&r.bounds.max.y>4.5f).Select(r=>r.transform.parent.name+" / "+r.name+" "+r.bounds));
            var brains=Object.FindObjectsByType<Unity.Cinemachine.CinemachineBrain>(FindObjectsSortMode.None);var states=brains.Select(b=>b.enabled).ToArray();
            var camera=Object.FindAnyObjectByType<Camera>();var position=camera.transform.position;var rotation=camera.transform.rotation;float fov=camera.fieldOfView;
            try
            {
                foreach(var b in brains)b.enabled=false;
                FacilityReview.Shot(camera,dir+"/s02-upper.png",new Vector3(4.6f,4.84f,26),new Vector3(-1,5,29),78);
                FacilityReview.Shot(camera,dir+"/s06-door.png",new Vector3(35,1.7f,72),new Vector3(30,3,65),78);
                var watcher=Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None).First(w=>w.Id=="sentry-r18");
                FacilityReview.Shot(camera,dir+"/watcher-facing.png",watcher.transform.position+watcher.transform.forward*2.8f+Vector3.up*1.25f,watcher.transform.position+Vector3.up*.65f,60);
                var boss=Object.FindObjectsByType<TrackedSentryController>(FindObjectsSortMode.None).First(b=>b.transform.position.x<0);
                FacilityReview.Shot(camera,dir+"/mantis.png",boss.transform.position+new Vector3(2,2.5f,-4),boss.transform.position+Vector3.up,65);
                foreach(string id in new[]{"ARC12","CIRRUS","TITAN"})
                {
                    var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Output+"/"+id+".prefab"));go.transform.position=new Vector3(0,1,-14);
                    var light=new GameObject("Review light");light.transform.position=new Vector3(1,3,-12);var l=light.AddComponent<Light>();l.range=6;l.intensity=6;
                    FacilityReview.Shot(camera,dir+"/"+id+".png",new Vector3(1.2f,1.6f,-12.8f),new Vector3(0,1.1f,-14),45);Object.DestroyImmediate(go);Object.DestroyImmediate(light);
                }
            }
            finally{camera.fieldOfView=fov;camera.transform.SetPositionAndRotation(position,rotation);for(int i=0;i<brains.Length;i++)brains[i].enabled=states[i];}
        }
    }
}
