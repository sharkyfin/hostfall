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
    public static partial class Hostfall2Workbench
    {
        [MenuItem("HOSTFALL/2.0/Sync Payload Map Sources")]
        public static void SyncPayloadMapSources()
        {
            WorkstationWorkshop.RequireCopy();
            if(EditorSceneManager.GetActiveScene().path!=PreviewScene)EditorSceneManager.OpenScene(PreviewScene);
            var mechanism=Object.FindAnyObjectByType<LogisticsMechanisms>();
            if(mechanism==null)throw new InvalidOperationException("Author logistics before map sources");
            var group=mechanism.transform;
            void Tag(Transform t,bool floor,bool moving,string id="",string label="")
            {
                var source=t.GetComponent<Hostfall.Mapping.MapGeometrySource>()??t.gameObject.AddComponent<Hostfall.Mapping.MapGeometrySource>();
                source.Configure(floor,moving,id,label);EditorUtility.SetDirty(source);
            }
            foreach(var t in group.GetComponentsInChildren<Transform>())
            {
                if(t.name.StartsWith("Mezzanine / ")&&!t.name.Contains("railing"))Tag(t,true,false,"logistics.gallery","Грузовая галерея");
                if(t.name=="Bridge deck")Tag(t,true,false,"logistics.bridge","Технологический мост");
                if(t.name=="Bridge landing")Tag(t,true,false,"logistics.landing","Северная площадка");
                if(t.name.StartsWith("Payload / moving Quaternius crate"))Tag(t,false,true);
            }
            Tag(mechanism.Lift,true,true,"logistics.lift","Грузовой подъёмник");Tag(mechanism.Bridge,false,true);
            var oldFloor=group.parent.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Mezzanine Slab");oldFloor.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
        [MenuItem("HOSTFALL/2.0/Author Payload Mechanisms")]
        public static void AuthorPayloadMechanisms()
        {
            WorkstationWorkshop.RequireCopy();EditorSceneManager.OpenScene(PreviewScene);
            if(Object.FindAnyObjectByType<LogisticsMechanisms>()!=null){Debug.Log("Payload mechanisms already authored");return;}
            File.Copy(PreviewScene,Path.Combine(Output,"scene-before-payload.unity"),true);
            var sector=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Single(t=>t.name=="Sector 05 - Logistics Center");
            var group=new GameObject("PAYLOAD / logistics mechanisms").transform;group.SetParent(sector,false);
            var floor=sector.GetComponentsInChildren<Transform>().Single(t=>t.name=="Mezzanine Slab");
            var material=floor.GetComponent<Renderer>().sharedMaterial;
            GameObject Box(string name,Vector3 position,Vector3 size,Transform parent)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,true);go.transform.position=position;go.transform.localScale=size;
                go.GetComponent<Renderer>().sharedMaterial=material;return go;
            }
            // Preserve the authored slab but replace its collision/render surface with four actual pieces around the lift.
            var bounds=floor.GetComponent<Collider>().bounds;floor.GetComponent<Collider>().enabled=false;floor.GetComponent<Renderer>().enabled=false;
            void Slab(string name,float minX,float maxX,float minZ,float maxZ)
            { if(maxX>minX&&maxZ>minZ)Box(name,new Vector3((minX+maxX)/2,bounds.center.y,(minZ+maxZ)/2),new Vector3(maxX-minX,bounds.size.y,maxZ-minZ),group); }
            Slab("Mezzanine / west of lift",bounds.min.x,10.6f,bounds.min.z,bounds.max.z);
            Slab("Mezzanine / east of lift",13.4f,bounds.max.x,bounds.min.z,bounds.max.z);
            Slab("Mezzanine / south of lift",10.6f,13.4f,bounds.min.z,67.1f);
            Slab("Mezzanine / north of lift",10.6f,13.4f,69.9f,bounds.max.z);
            var lift=sector.GetComponentsInChildren<Transform>().Single(t=>t.name=="Freight Lift Base");lift.SetParent(group,true);
            var lower=new Vector3(12,.1f,68.5f);lift.position=lower;
            // A real hinged bridge connects the north mezzanine to a separate landing; it has physical depth and rails.
            var bridge=new GameObject("Payload / hinged service bridge").transform;bridge.SetParent(group,false);bridge.position=new Vector3(8.5f,1,79);
            var deck=Box("Bridge deck",new Vector3(8.5f,.925f,81),new Vector3(3,.15f,4),bridge);
            for(int sign=-1;sign<=1;sign+=2)
            {
                Box("Bridge rail",new Vector3(8.5f+sign*1.47f,1.95f,81),new Vector3(.07f,.08f,4),bridge);
                for(int n=0;n<3;n++)Box("Bridge upright",new Vector3(8.5f+sign*1.47f,1.48f,79.4f+n*1.6f),new Vector3(.07f,1,.07f),bridge);
            }
            foreach(var rail in sector.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Platform_Rails")&&Vector3.Distance(t.position,new Vector3(9.1f,1,78.95f))<.4f).ToArray())rail.gameObject.SetActive(false);
            foreach(var range in new[]{new Vector2(4.75f,7),new Vector2(10,13.45f)})
                Box("Mezzanine / split north railing",new Vector3((range.x+range.y)/2,1.95f,78.95f),new Vector3(range.y-range.x,.08f,.08f),group);
            Box("Bridge landing",new Vector3(8.5f,.5f,84),new Vector3(3,1,2),group);
            bridge.localRotation=Quaternion.Euler(-90,0,0);
            var belt=Box("Payload / conveyor",new Vector3(-5,.4f,88),new Vector3(6,.8f,1.7f),group);
            var start=new Vector3(-7,.8f,88);var end=new Vector3(-3,.8f,88);
            var source=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name=="Prop_Crate3");
            var cargo=new Transform[2];
            for(int i=0;i<cargo.Length;i++)
            {
                cargo[i]=Object.Instantiate(source,group);cargo[i].name="Payload / moving Quaternius crate "+i;
                var render=cargo[i].GetComponentsInChildren<Renderer>();var size=render[0].bounds;foreach(var r in render)size.Encapsulate(r.bounds);
                cargo[i].localScale*=.65f/Mathf.Max(size.size.x,size.size.y,size.size.z);cargo[i].position=Vector3.Lerp(start,end,i/(float)cargo.Length);
            }
            var beacon=new GameObject("Payload / alarm beacon");beacon.transform.SetParent(group,false);beacon.transform.position=new Vector3(8.5f,3.1f,72);
            var light=beacon.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.12f,.05f);light.range=9;light.shadows=LightShadows.None;light.enabled=false;
            var adapter=group.gameObject.AddComponent<LogisticsMechanisms>();adapter.Configure(lift,bridge,cargo,light,lower,bridge.position,bridge.position,start,end);
            SyncPayloadMapSources();
            WorkstationWorkshop.Capture("hf2-payload-mechanisms",new Vector3(2,3.1f,74),new Vector3(10,1.3f,79));
            File.Copy(Path.Combine(WorkstationWorkshop.Output,"hf2-payload-mechanisms.png"),Path.Combine(Output,"hf2-payload-mechanisms.png"),true);
            Debug.Log("Payload mechanisms authored; passage, lift travel and restore QA remain mandatory");
        }
    }
}
