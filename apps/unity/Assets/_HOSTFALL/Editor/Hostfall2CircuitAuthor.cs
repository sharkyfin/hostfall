using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Hostfall.Core;
using Hostfall.Mapping;
using Hostfall.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Hostfall.Editor
{
    public static partial class Hostfall2Workbench
    {
        [MenuItem("HOSTFALL/2.0/Repair Maintenance Junction")]
        public static void RepairMaintenanceJunction()
        {
            WorkstationWorkshop.RequireCopy();EditorSceneManager.OpenScene(PreviewScene);
            var wall=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Single(t=>t.name=="Connector Wall North");
            if(Mathf.Abs(wall.localScale.x-9.8f)<.001f)return;
            if(Mathf.Abs(wall.localScale.x-12)> .001f||Vector3.Distance(wall.position,new Vector3(-10.75f,1.675f,-.25f))>.01f)
                throw new InvalidOperationException("Maintenance junction differs from inspected geometry; re-audit before repair");
            File.Copy(PreviewScene,Path.Combine(Output,"scene-before-maintenance-junction.unity"),true);
            WorkstationWorkshop.Capture("hf2-maintenance-junction-before",new Vector3(-17.2f,1.7f,2),new Vector3(-14,1.4f,-1.75f));
            File.Copy(Path.Combine(WorkstationWorkshop.Output,"hf2-maintenance-junction-before.png"),Path.Combine(Output,"hf2-maintenance-junction-before.png"),true);
            // The connector north wall must meet, rather than cross, the N/S bypass east wall.
            wall.position=new Vector3(-9.65f,wall.position.y,wall.position.z);wall.localScale=new Vector3(9.8f,wall.localScale.y,wall.localScale.z);
            Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            WorkstationWorkshop.Capture("hf2-maintenance-junction-repaired",new Vector3(-17.2f,1.7f,2),new Vector3(-14,1.4f,-1.75f));
            File.Copy(Path.Combine(WorkstationWorkshop.Output,"hf2-maintenance-junction-repaired.png"),Path.Combine(Output,"hf2-maintenance-junction-repaired.png"),true);
            File.WriteAllText(Path.Combine(Output,"maintenance-junction-repair.txt"),"Visible connector north wall and its collider trimmed from X=-16.75..-4.75 to -14.55..-4.75. Both-way CC acceptance remains required.");
        }
        [MenuItem("HOSTFALL/2.0/Repair Energy Shortcut Binding")]
        public static void RepairEnergyShortcutBinding()
        {
            WorkstationWorkshop.RequireCopy();EditorSceneManager.OpenScene(PreviewScene);var power=Object.FindAnyObjectByType<CircuitPowerController>();
            if(power==null)throw new InvalidOperationException("Author energy distribution first");
            if(power.ShortcutDoor.DoorId=="door.sec01.exec")return;
            File.Copy(PreviewScene,Path.Combine(Output,"scene-before-circuit-shortcut.unity"),true);
            var previous=new SerializedObject(power.ShortcutDoor);previous.FindProperty("autoOpenOnUnlock").boolValue=true;previous.ApplyModifiedPropertiesWithoutUndo();
            var shortcut=Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None).Single(d=>d.DoorId=="door.sec01.exec");
            var door=new SerializedObject(shortcut);door.FindProperty("condition").enumValueIndex=(int)DoorAccessCondition.ManualUnlockOnly;
            door.FindProperty("autoOpenOnUnlock").boolValue=false;door.FindProperty("lockedPromptText").stringValue="Обход обесточен · проверьте CIRCUIT BREAKER в энергоцентре";door.ApplyModifiedPropertiesWithoutUndo();
            var fields=new SerializedObject(power);fields.FindProperty("shortcutDoor").objectReferenceValue=shortcut;fields.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            File.WriteAllText(Path.Combine(Output,"circuit-shortcut-binding.txt"),"CIRCUIT shortcut powers the actual energy-to-SOC maintenance bypass, not the armory supply dead end. CC acceptance pending.");
        }
        [MenuItem("HOSTFALL/2.0/Author Energy Distribution")]
        public static void AuthorEnergyDistribution()
        {
            WorkstationWorkshop.RequireCopy();EditorSceneManager.OpenScene(PreviewScene);
            if(Object.FindAnyObjectByType<CircuitPowerController>()!=null){Debug.Log("Energy distribution already authored");RepairMaintenanceJunction();return;}
            File.Copy(PreviewScene,Path.Combine(Output,"scene-before-circuit.unity"),true);
            var sector=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Single(t=>t.name=="Sector 03 - Energy Center");
            var group=new GameObject("CIRCUIT BREAKER / local power distribution").transform;group.SetParent(sector,false);
            var doors=Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None);
            var main=doors.Single(d=>d.DoorId=="door.sec04.cryo");var shortcut=doors.Single(d=>d.DoorId=="door.sec01.exec");
            foreach(var door in new[]{main,shortcut})
            {
                var so=new SerializedObject(door);so.FindProperty("condition").enumValueIndex=(int)DoorAccessCondition.ManualUnlockOnly;
                so.FindProperty("autoOpenOnUnlock").boolValue=false;so.FindProperty("lockedPromptText").stringValue="Нет питания · проверьте схему CIRCUIT BREAKER в энергоцентре";so.ApplyModifiedPropertiesWithoutUndo();
            }
            var lights=sector.GetComponentsInChildren<Light>().Where(l=>l.name.StartsWith("Reactor HighBay")||l.name.StartsWith("Energy West Sconce")||l.name=="Energy South Sconce").ToArray();
            if(lights.Length==0)throw new InvalidOperationException("Missing existing energy route lights");
            var vents=sector.GetComponentsInChildren<Transform>().Where(t=>t.name=="Prop_Vent_Big").ToArray();
            if(vents.Length!=2)throw new InvalidOperationException("Expected two authored energy vents; got "+vents.Length);
            var metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/_HOSTFALL/Content/Materials/Dark Metal.mat");
            if(metal==null)throw new InvalidOperationException("Missing established dark metal palette");
            GameObject Plate(string name,Transform parent,Vector3 local,Vector3 scale)
            {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=local;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=metal;return go;}
            var shutters=new List<Transform>();
            foreach(var vent in vents)
            {
                for(int i=0;i<4;i++)
                {
                    var pivot=new GameObject("Powered ventilation louver").transform;pivot.SetParent(group,false);
                    pivot.position=vent.position+new Vector3(0,-.8f+i*.53f,-.46f);pivot.gameObject.AddComponent<MapGeometrySource>().Configure(false,true);
                    var panel=Plate("Visible induction shutter",pivot,Vector3.zero,new Vector3(2.12f,.49f,.06f));Object.DestroyImmediate(panel.GetComponent<Collider>());shutters.Add(pivot);
                }
            }
            var source=Object.FindAnyObjectByType<RailEyeTurretController>();if(source==null)throw new InvalidOperationException("Missing retained turret art source");
            var turret=Object.Instantiate(source.gameObject,group);turret.name="Local defensive induction turret";
            foreach(var behavior in turret.GetComponentsInChildren<MonoBehaviour>())Object.DestroyImmediate(behavior);
            turret.transform.SetPositionAndRotation(new Vector3(-11,5.6f,34),Quaternion.identity);
            var head=turret.GetComponentsInChildren<Transform>().Single(t=>t.name=="Turret Head");
            var lens=turret.GetComponentsInChildren<Transform>().Single(t=>t.name=="Rail Muzzle");
            var lamp=turret.GetComponentInChildren<Light>();lamp.color=new Color(.2f,.8f,1);lamp.intensity=2;lamp.range=8;
            Plate("Visible ceiling turret mount",group,new Vector3(-11,6.05f,34),new Vector3(.75f,.3f,.65f));
            var beamGo=new GameObject("Induction pulse");beamGo.transform.SetParent(group,false);var beam=beamGo.AddComponent<LineRenderer>();
            beam.positionCount=2;beam.startWidth=.035f;beam.endWidth=.012f;beam.useWorldSpace=true;beam.enabled=false;
            var beamMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_HOSTFALL/Content/Materials/Indicator Cyan.mat")??AssetDatabase.LoadAssetAtPath<Material>("Assets/_HOSTFALL/Content/Materials/Indicator Green.mat");
            if(beamMaterial==null)throw new InvalidOperationException("Missing established indicator material");beam.sharedMaterial=beamMaterial;
            group.gameObject.AddComponent<CircuitPowerController>().Configure(main,shortcut,lights,shutters.ToArray(),head,lens,lamp,beam);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            RepairMaintenanceJunction();
            File.WriteAllText(Path.Combine(Output,"circuit-authored.txt"),"Retained power lights: "+lights.Length+"; ventilation louvers: "+shutters.Count+"; main cryo/service bypass access; source turret art reused. Gameplay acceptance pending.");
            WorkstationWorkshop.Capture("hf2-energy-power-authored",new Vector3(-9,2,35.6f),new Vector3(-14,3,34.5f));
            File.Copy(Path.Combine(WorkstationWorkshop.Output,"hf2-energy-power-authored.png"),Path.Combine(Output,"hf2-energy-power-authored.png"),true);
        }
    }
}
