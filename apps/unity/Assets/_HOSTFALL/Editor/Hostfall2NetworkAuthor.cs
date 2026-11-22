using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Hostfall.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Hostfall.Editor
{
    public static partial class Hostfall2Workbench
    {
        [MenuItem("HOSTFALL/2.0/Repair Server Reserve Clearance")]
        public static void RepairServerReserveClearance()
        {
            WorkstationWorkshop.RequireCopy();EditorSceneManager.OpenScene(PreviewScene);var controller=Object.FindAnyObjectByType<ServerNetworkController>();
            if(controller==null)throw new InvalidOperationException("Author server network first");
            if(Vector3.Distance(controller.transform.position,new Vector3(3.65f,0,13))<.01f)return;
            File.Copy(PreviewScene,Path.Combine(Output,"scene-before-network-clearance.unity"),true);
            var rack=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Single(t=>t.name=="ServerRack"&&Vector3.Distance(t.position,new Vector3(3.1f,0,13))<.2f);
            Vector3? destination=null;
            foreach(var point in new[]{new Vector3(5.5f,0,18.5f),new Vector3(5.5f,0,21.5f),new Vector3(-5.5f,0,18.5f)})
                if(Physics.OverlapBox(point+Vector3.up*1.2f,new Vector3(.6f,1.17f,.6f),Quaternion.identity,Hostfall.Core.CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore).All(c=>c.bounds.max.y<.08f)){destination=point;break;}
            if(!destination.HasValue)throw new InvalidOperationException("No clear authored vault position for retained server rack");
            rack.position=destination.Value;controller.transform.SetPositionAndRotation(new Vector3(3.65f,0,13),Quaternion.Euler(0,90,0));
            var hidden=new[]{controller.transform.position,controller.transform.position};var exits=new[]{new Vector3(1.8f,0,12.1f),new Vector3(1.8f,0,13.6f)};
            var fields=new SerializedObject(controller);
            for(int i=0;i<2;i++)
            {
                fields.FindProperty("hidden").GetArrayElementAtIndex(i).vector3Value=hidden[i];fields.FindProperty("exits").GetArrayElementAtIndex(i).vector3Value=exits[i];
                controller.Responders[i].transform.SetPositionAndRotation(hidden[i],Quaternion.Euler(0,270,0));
            }
            fields.ApplyModifiedPropertiesWithoutUndo();Physics.SyncTransforms();
            foreach(var point in hidden.Concat(exits))
            {
                var blocked=Physics.OverlapCapsule(point+Vector3.up*.35f,point+Vector3.up,.28f,Hostfall.Core.CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore).Where(c=>!c.transform.IsChildOf(controller.transform)&&c.GetComponentInParent<IDamageable>()==null).ToArray();
                if(blocked.Length>0)throw new InvalidOperationException("Repaired reserve pose overlaps "+string.Join(",",blocked.Select(c=>c.name)));
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            File.WriteAllText(Path.Combine(Output,"network-clearance-repair.txt"),"Retained north-east ServerRack moved to "+destination+"; duct moved east of the desk bypass. CC round-trip acceptance remains required.");
            WorkstationWorkshop.Capture("hf2-server-network-clearance",new Vector3(.5f,1.7f,11),new Vector3(3.4f,1,13));
            File.Copy(Path.Combine(WorkstationWorkshop.Output,"hf2-server-network-clearance.png"),Path.Combine(Output,"hf2-server-network-clearance.png"),true);
        }
        [MenuItem("HOSTFALL/2.0/Author Server Network")]
        public static void AuthorServerNetwork()
        {
            WorkstationWorkshop.RequireCopy();EditorSceneManager.OpenScene(PreviewScene);
            if(Object.FindAnyObjectByType<ServerNetworkController>()!=null){Debug.Log("Server network already authored");return;}
            File.Copy(PreviewScene,Path.Combine(Output,"scene-before-network.unity"),true);
            var rack=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Single(t=>t.name=="Security Weapon Rack - Server Room Emergency Rack");
            rack.position=new Vector3(-1.85f,2.3f,14.72f);rack.rotation=Quaternion.Euler(0,180,0);
            var source=Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None).Single(w=>w.Id=="sentry-r18");
            var group=new GameObject("NETWORK DEFENSE / server relay and reserve").transform;group.SetParent(Object.FindAnyObjectByType<Hostfall.Core.FacilityRoot>().transform,false);
            group.position=new Vector3(1.8f,0,14.05f);
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/_HOSTFALL/Content/Materials/Dark Metal.mat")??source.GetComponentsInChildren<Renderer>().First().sharedMaterial;
            GameObject Box(string name,Vector3 local,Vector3 size,Transform parent)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=local;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;return go;
            }
            Box("Server duct roof",new Vector3(0,1.78f,0),new Vector3(1.3f,.12f,1.75f),group);
            Box("Server duct west",new Vector3(-.64f,.87f,0),new Vector3(.1f,1.8f,1.75f),group);
            Box("Server duct east",new Vector3(.64f,.87f,0),new Vector3(.1f,1.8f,1.75f),group);
            var grille=new GameObject("Server reserve grate").transform;grille.SetParent(group,false);grille.localPosition=new Vector3(0,0,-.87f);
            for(int i=0;i<8;i++)Box("Reserve grille horizontal",new Vector3(0,.14f+i*.21f,0),new Vector3(1.2f,.055f,.08f),grille);
            for(int i=-1;i<=1;i+=2)Box("Reserve grille upright",new Vector3(i*.59f,.88f,0),new Vector3(.055f,1.8f,.08f),grille);
            grille.gameObject.AddComponent<Hostfall.Mapping.MapGeometrySource>().Configure(false,true);
            var hidden=new[]{group.position,group.position};var exits=new[]{new Vector3(1.8f,0,12.1f),new Vector3(1.8f,0,11.1f)};
            var responders=new WatcherController[2];
            for(int i=0;i<2;i++)
            {
                var go=Object.Instantiate(source.gameObject,source.transform.parent);go.name="Server reserve / R"+(31+i);go.transform.SetPositionAndRotation(hidden[i],Quaternion.Euler(0,180,0));
                var actor=responders[i]=go.GetComponent<WatcherController>();var fields=new SerializedObject(actor);
                fields.FindProperty("enemyId").stringValue="network-response-r"+(31+i);fields.FindProperty("patrolA").vector3Value=new Vector3(1.8f,0,9.7f);
                fields.FindProperty("patrolB").vector3Value=new Vector3(1.8f,0,13.2f);fields.FindProperty("xBounds").vector2Value=new Vector2(-1.1f,2.2f);fields.FindProperty("zBounds").vector2Value=new Vector2(7.8f,14.1f);fields.ApplyModifiedPropertiesWithoutUndo();
            }
            var lampGo=new GameObject("Server / command link indicator");lampGo.transform.SetParent(group,false);lampGo.transform.localPosition=new Vector3(-.5f,1.45f,-.95f);
            var lamp=lampGo.AddComponent<Light>();lamp.type=LightType.Point;lamp.range=3;lamp.shadows=LightShadows.None;lamp.intensity=1.4f;lamp.color=new Color(1,.55f,.1f);
            var controller=group.gameObject.AddComponent<ServerNetworkController>();controller.Configure(new[]{source,Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None).Single(w=>w.Id=="watcher-r17")},responders,grille,hidden,exits,lamp);
            Physics.SyncTransforms();
            foreach(var point in hidden.Concat(exits))
            {
                var overlap=Physics.OverlapCapsule(point+Vector3.up*.35f,point+Vector3.up,.28f,Hostfall.Core.CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore).Where(c=>!c.transform.IsChildOf(group)&&c.GetComponentInParent<IDamageable>()==null).ToArray();
                if(overlap.Length>0)throw new InvalidOperationException("Reserve pose overlaps "+string.Join(",",overlap.Select(c=>c.name)));
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            WorkstationWorkshop.Capture("hf2-server-network-authored",new Vector3(.5f,1.7f,10.6f),new Vector3(1.8f,.9f,14));
            File.Copy(Path.Combine(WorkstationWorkshop.Output,"hf2-server-network-authored.png"),Path.Combine(Output,"hf2-server-network-authored.png"),true);
            RepairServerReserveClearance();
            Debug.Log("Server network authored; real passage/AI/save acceptance still required");
        }
        [MenuItem("HOSTFALL/2.0/Inspect Server Reserve Lanes")]
        public static void InspectServerReserveLanes()
        {
            WorkstationWorkshop.RequireCopy();EditorSceneManager.OpenScene(PreviewScene);Physics.SyncTransforms();var rows=new List<string>();
            foreach(var point in new[]{new Vector3(4.1f,0,9.1f),new Vector3(4.1f,0,11.4f),new Vector3(-4.1f,0,9.1f),new Vector3(-4.1f,0,12.5f)})
            {
                rows.Add("DUCT "+point);
                foreach(var hit in Physics.OverlapBox(point+Vector3.up*.85f,new Vector3(.75f,.85f,1.1f),Quaternion.identity,Hostfall.Core.CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore))rows.Add(WorkstationWorkshop.PathOf(hit.transform)+" "+hit.bounds);
                var exit=point+Vector3.right*(point.x>0?-1.7f:1.7f);
                rows.Add("EXIT "+exit+" "+string.Join(",",Physics.OverlapCapsule(exit+Vector3.up*.35f,exit+Vector3.up,.28f,Hostfall.Core.CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore).Select(c=>c.name)));
            }
            foreach(float x in new[]{1.8f,1.9f,2.1f,-1.8f,-1.9f})
            {
                var p=new Vector3(x,0,14.05f);var end=new Vector3(x,0,11.5f);
                rows.Add("NORTH DUCT "+p+" "+string.Join(",",Physics.OverlapBox(p+Vector3.up*.9f,new Vector3(.6f,.82f,.72f),Quaternion.identity,Hostfall.Core.CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore).Where(c=>c.GetComponentInParent<IDamageable>()==null).Select(c=>c.name)));
                rows.Add("NORTH LANE "+string.Join(",",Physics.CapsuleCastAll(p+Vector3.up*.35f,p+Vector3.up,.28f,Vector3.back,2.6f,Hostfall.Core.CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore).Where(h=>h.collider.GetComponentInParent<IDamageable>()==null).Select(h=>h.collider.name)));
            }
            File.WriteAllLines(Path.Combine(Output,"server-reserve-candidates.txt"),rows);
            WorkstationWorkshop.Capture("hf2-server-reserve-before",new Vector3(0,2,8.8f),new Vector3(4,1,11.5f));
            File.Copy(Path.Combine(WorkstationWorkshop.Output,"hf2-server-reserve-before.png"),Path.Combine(Output,"hf2-server-reserve-before.png"),true);
        }
    }
}
