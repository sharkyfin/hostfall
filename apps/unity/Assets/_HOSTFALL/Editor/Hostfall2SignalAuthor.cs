using System;
using System.IO;
using System.Linq;
using Hostfall.Core;
using Hostfall.Devices;
using Hostfall.Mapping;
using Hostfall.Rendering;
using Hostfall.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Hostfall.Editor
{
    public static partial class Hostfall2Workbench
    {
        [MenuItem("HOSTFALL/2.0/Author Signal Barrier")]
        public static void AuthorSignalBarrier()
        {
            WorkstationWorkshop.RequireCopy();EditorSceneManager.OpenScene(PreviewScene);
            if(Object.FindAnyObjectByType<SignalBarrierController>()!=null){Debug.Log("Signal barrier already authored");return;}
            File.Copy(PreviewScene,Path.Combine(Output,"scene-before-signal.unity"),true);
            var door=Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None).Single(d=>d.DoorId=="door.sec05.logistics");
            var group=new GameObject("SIGNAL SYNC / timed field and north return").transform;group.SetParent(door.transform.parent,false);group.position=door.transform.position;
            var pc=Object.FindObjectsByType<ComputerStation>(FindObjectsSortMode.None).Single(c=>c.StationId=="pc.signal");
            var guard=Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None).Single(w=>w.Id=="quantum-watcher-r23");
            var so=new SerializedObject(door);so.FindProperty("condition").enumValueIndex=(int)DoorAccessCondition.ManualUnlockOnly;so.FindProperty("autoOpenOnUnlock").boolValue=false;
            so.FindProperty("lockedPromptText").stringValue="Поле активно · синхронизируйте SIGNAL SYNC";so.ApplyModifiedPropertiesWithoutUndo();
            const string fieldPath="Assets/_HOSTFALL/Content/Materials/Signal Energy Field.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(fieldPath);
            if(material==null){var shader=Shader.Find("HOSTFALL/Energy Field");if(shader==null)throw new InvalidOperationException("Import energy field shader first");material=new Material(shader);AssetDatabase.CreateAsset(material,fieldPath);}
            var plane=GameObject.CreatePrimitive(PrimitiveType.Quad);plane.name="Visible energy field / solid when active";plane.transform.SetParent(group,false);plane.transform.localPosition=new Vector3(0,1.97f,-.38f);plane.transform.localScale=new Vector3(3.9f,3.85f,1);
            Object.DestroyImmediate(plane.GetComponent<Collider>());var collider=plane.AddComponent<BoxCollider>();collider.size=new Vector3(1,1,.04f);
            var art=plane.GetComponent<Renderer>();art.sharedMaterial=material;art.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;plane.AddComponent<MapGeometrySource>().Configure(false,true);
            Light Lamp(string name,Vector3 position,Transform parent,float range)
            {var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;var light=go.AddComponent<Light>();light.type=LightType.Point;light.range=range;light.intensity=1.3f;light.color=new Color(.15f,.55f,1);light.shadows=LightShadows.None;return light;}
            var gateLamp=Lamp("Field status light",new Vector3(0,3.5f,-.8f),group,4);
            var heatLamp=Lamp("Phase equipment thermal light",new Vector3(.5f,1.6f,-.2f),pc.transform,2.5f);
            var dark=AssetDatabase.LoadAssetAtPath<Material>("Assets/_HOSTFALL/Content/Materials/Dark Metal.mat");
            var green=AssetDatabase.LoadAssetAtPath<Material>("Assets/_HOSTFALL/Content/Materials/Indicator Green.mat");
            GameObject Box(string name,Vector3 position,Vector3 scale,Material palette)
            {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(group,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=palette;return go;}
            Box("North emergency return enclosure",new Vector3(2.7f,1.25f,.36f),new Vector3(.4f,.6f,.16f),dark);
            Box("North emergency return actuator",new Vector3(2.7f,1.25f,.47f),new Vector3(.22f,.24f,.08f),green);
            var textGo=new GameObject("Field status / physical countdown");textGo.transform.SetParent(group,false);textGo.transform.localPosition=new Vector3(-3.55f,2.6f,-.45f);
            var text=textGo.AddComponent<TextMesh>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=54;text.characterSize=.026f;text.color=new Color(.5f,.82f,1);text.text="ПОЛЕ АКТИВНО";
            textGo.AddComponent<WorldTextDepth>().Configure(Shader.Find("HOSTFALL/World Text"));
            var arcs=new LineRenderer[3];
            for(int i=0;i<3;i++)
            {
                var spark=new GameObject("Reused phase overload arc "+i);spark.transform.SetParent(pc.transform,false);var line=arcs[i]=spark.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=3;
                line.SetPositions(new[]{new Vector3(-.35f+i*.22f,1.15f,.1f),new Vector3(-.3f+i*.22f,1.3f,.16f),new Vector3(-.2f+i*.22f,1.4f,.08f)});
                line.startWidth=.012f;line.endWidth=.004f;line.sharedMaterial=green;line.enabled=false;
            }
            group.gameObject.AddComponent<SignalBarrierController>().Configure(door,collider,art,heatLamp,gateLamp,pc.transform,guard,text,arcs);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            WorkstationWorkshop.Capture("hf2-signal-barrier-authored",new Vector3(1.2f,1.9f,57.8f),new Vector3(0,2,62));
            File.Copy(Path.Combine(WorkstationWorkshop.Output,"hf2-signal-barrier-authored.png"),Path.Combine(Output,"hf2-signal-barrier-authored.png"),true);
            File.WriteAllText(Path.Combine(Output,"signal-authored.txt"),"Timed field at existing logistics gate, retained metal leaf, solid quad collider, north emergency return, thermal lamp/arcs, existing QNT guard. UI/CC/restore acceptance pending.");
        }
    }
}
