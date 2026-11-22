using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace Hostfall.Editor
{
    // Small functional bays, rather than scattered props. Stable named root makes the pass repeatable.
    internal static class EnvironmentPass
    {
        private const string RootName="NORTHLINE / Service Bays";
        internal static void Apply()
        {
            PrepareServiceJunction();
            var old=GameObject.Find(RootName);if(old!=null)Object.DestroyImmediate(old);
            var root=new GameObject(RootName).transform;
            Bay("Cooling inspection",new Vector3(-22,0,30),90,true);
            Bay("Freight maintenance",new Vector3(-22,0,76),90,false);
            Bay("Sorting supplies",new Vector3(-22,0,87),90,false);
            Bay("Actuator service",new Vector3(49.8f,0,70),270,true);
            Bay("Robot diagnostics",new Vector3(49.8f,0,80),270,true);
            Bay("Security repair",new Vector3(52.8f,0,43),270,false);
            Bay("Archive cooling",new Vector3(85.8f,0,47),270,true);
            Bay("Dispatch maintenance",new Vector3(52.1f,0,119),270,false);
            // Light the actual ascent, not the whole chamber. Existing light/shadow levels elsewhere survive.
            Pool("Logistics ascent",new Vector3(5.5f,3.8f,69),new Vector3(4.2f,0,67.5f),new Color(1,.68f,.35f),8,9,root);
            Pool("Robotics west ascent",new Vector3(20.5f,7,90.4f),new Vector3(21,2,91.5f),new Color(.45f,.74f,1),10,10,root);
            Pool("Robotics east ascent",new Vector3(46.5f,7,90.4f),new Vector3(46,2,91.5f),new Color(.45f,.74f,1),10,10,root);
            Pool("Citadel ascent",new Vector3(37.4f,6,22.5f),new Vector3(38,2,22.5f),new Color(1,.66f,.32f),9,9,root);
            Pool("Dispatch ascent",new Vector3(20,6.4f,115.2f),new Vector3(20,1.5f,115.2f),new Color(.4f,.7f,1),9,10,root);
            RepairSurfaceUVs();
            // Same URP renderer. Clustered lighting removes the four-lights-per-large-floor limit.
            var data=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(FacilityBuilder.Content+"/Settings/FacilityRenderer.asset");
            if(data!=null){data.renderingMode=RenderingMode.ForwardPlus;EditorUtility.SetDirty(data);}

            void Bay(string name,Vector3 p,float yaw,bool cooling)
            {
                var facing=Quaternion.Euler(0,yaw,0)*Vector3.forward;
                var wall=Physics.RaycastAll(p+Vector3.up*1.8f,-facing,6).Where(h=>h.collider.name.Contains("Wall")||h.collider.name.Contains("wall")||h.collider.name.Contains("shell")).OrderBy(h=>h.distance).FirstOrDefault();
                if(wall.collider==null)throw new System.InvalidOperationException("No mounting wall for service bay "+name);
                p=wall.point+facing*.3f;p.y=0;
                var bay=new GameObject(name).transform;bay.SetParent(root,false);bay.position=p;bay.rotation=Quaternion.Euler(0,yaw,0);
                Kit("Prop_Crate4",new Vector3(-.55f,0,.15f),.8f);
                Kit("Prop_Barrel_Large",new Vector3(.55f,0,.1f),1.05f);
                Kit("Prop_Vent_Wide",new Vector3(0,2.25f,-.28f),1.3f);
                Place("Engineering/Maintenance cart",new Vector3(.15f,0,1.2f),null,180);
                if(cooling)Place("Engineering/Cooling valve",new Vector3(-.78f,1.25f,-.3f),null,0);
                Kit("Prop_Light_Small",new Vector3(0,2.7f,0),.9f);
                var panel=AssetDatabase.LoadAssetAtPath<GameObject>(FacilityBuilder.Content+"/Prefabs/Engineering/Service junction.prefab");
                if(panel!=null){var g=(GameObject)PrefabUtility.InstantiatePrefab(panel);g.transform.SetParent(bay,false);g.transform.localPosition=new Vector3(0,1.1f,-.19f);g.transform.localRotation=Quaternion.identity;Mount(g);}
                Pool(name+" task light",bay.TransformPoint(new Vector3(0,2.65f,.2f)),bay.TransformPoint(new Vector3(0,.4f,.8f)),cooling?new Color(.45f,.8f,.9f):new Color(1,.73f,.4f),7,5,bay);
                void Mount(GameObject go)
                {
                    var rs=go.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
                    float radius=Mathf.Abs(facing.x)*b.extents.x+Mathf.Abs(facing.z)*b.extents.z;
                    float back=Vector3.Dot(b.center-wall.point,facing)-radius;go.transform.position+=facing*(.01f-back);
                }
                void Kit(string id,Vector3 pos,float largest)=>Place("Kit/"+id,pos,largest,0);
                void Place(string id,Vector3 pos,float? largest,float angle)
                {
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(FacilityBuilder.Content+"/Prefabs/"+id+".prefab");if(prefab==null)throw new System.InvalidOperationException("Missing curated asset "+id);
                    var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.transform.SetParent(bay,false);go.transform.localPosition=pos;go.transform.localRotation=Quaternion.Euler(0,angle,0);
                    if(largest.HasValue){var rs=go.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);go.transform.localScale*=largest.Value/Mathf.Max(b.size.x,b.size.y,b.size.z);}
                    if(id.Contains("Cooling valve")||id.Contains("Vent")||id.Contains("Light"))Mount(go);
                    // Keep invisible parts disabled by the curated prefab out of any proxy bounds.
                    if(id.Contains("Cooling valve")||id.Contains("Vent"))return;
                    if(go.GetComponentsInChildren<Collider>().Length==0)
                    {ColliderProfileRepair.Fit(go.AddComponent<BoxCollider>());}
                }
            }
        }
        private static void PrepareServiceJunction()
        {
            const string modelPath="Assets/_HOSTFALL/Art/HostfallEngineering/ServiceJunction.fbx";
            string output=FacilityBuilder.Content+"/Prefabs/Engineering/Service junction.prefab";
            if(AssetDatabase.LoadAssetAtPath<GameObject>(output)!=null)return;
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);if(model==null)return;
            var root=new GameObject("Service junction");var child=Object.Instantiate(model,root.transform);
            var rs=child.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
            child.transform.localPosition-=new Vector3(b.center.x,b.min.y,b.center.z);
            root.transform.localScale=Vector3.one*(1.0515f/b.size.y);
            var materials=new Dictionary<string,Material>();
            foreach(var r in rs)r.sharedMaterials=r.sharedMaterials.Select(source=>
            {
                string n=source!=null?source.name:"enamel";if(materials.TryGetValue(n,out var cached))return cached;
                string path=FacilityBuilder.Content+"/Materials/Service junction "+(n.Contains("warning")?"brass":n.Contains("gasket")?"gasket":"enamel")+".mat";
                var m=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
                m.SetColor("_BaseColor",n.Contains("warning")?new Color(.65f,.32f,.065f):n.Contains("gasket")?new Color(.025f,.035f,.04f):new Color(.21f,.28f,.27f));m.SetFloat("_Metallic",.45f);m.SetFloat("_Smoothness",.35f);m.enableInstancing=true;EditorUtility.SetDirty(m);materials[n]=m;return m;
            }).ToArray();
            var c=root.AddComponent<BoxCollider>();c.center=new Vector3(0,b.size.y/2,0);c.size=b.size;
            PrefabUtility.SaveAsPrefabAsset(root,output);Object.DestroyImmediate(root);
        }
        private static void Pool(string name,Vector3 p,Vector3 target,Color color,float intensity,float range,Transform parent)
        {
            var light=new GameObject(name).AddComponent<Light>();light.transform.SetParent(parent,true);light.transform.position=p;light.transform.rotation=Quaternion.LookRotation(target-p);
            light.type=LightType.Spot;light.color=color;light.intensity=intensity;light.range=range;light.spotAngle=95;light.innerSpotAngle=65;light.shadows=LightShadows.None;
        }
        private static void RepairSurfaceUVs()
        {
            string folder=FacilityBuilder.Content+"/Meshes/WorldSurfaces";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            foreach(var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null||renderer.sharedMaterial==null||filter.sharedMesh==null)continue;
                if(filter.sharedMesh.name!="Cube"&&!filter.sharedMesh.name.StartsWith("Metric surface"))continue;
                string mat=renderer.sharedMaterial.name;if(mat!="Floor Panels"&&mat!="Concrete"&&mat!="Dark Metal")continue;
                if(!(filter.name.Contains("Floor")||filter.name.Contains("floor")||filter.name.Contains("Wall")||filter.name.Contains("Slab")||filter.name.Contains("Ceiling")))continue;
                Vector3 size=filter.transform.lossyScale;string key=$"{size.x:F3}_{size.y:F3}_{size.z:F3}".Replace(',','.');
                string path=folder+"/"+key+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(mesh==null)
                {
                    mesh=Object.Instantiate(filter.sharedMesh);mesh.name="Metric surface "+key;
                    var uv=new Vector2[mesh.vertexCount];var verts=mesh.vertices;var normals=mesh.normals;
                    for(int i=0;i<uv.Length;i++)
                    {var v=Vector3.Scale(verts[i],size);var n=normals[i];uv[i]=Mathf.Abs(n.y)>.5f?new Vector2(v.x,v.z):Mathf.Abs(n.x)>.5f?new Vector2(v.z,v.y):new Vector2(v.x,v.y);uv[i]/=2;}
                    mesh.uv=uv;mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,path);
                }
                filter.sharedMesh=mesh;
            }
        }
    }
}
