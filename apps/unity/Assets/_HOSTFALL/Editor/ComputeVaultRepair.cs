using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Hostfall.Editor
{
    // Only the S02 circulation structure is replaced. Devices, doors and puzzle IDs survive.
    public static class ComputeVaultRepair
    {
        private const string StructureName = "S02 Traversal Structure";
        private const string RampPath = FacilityBuilder.Content + "/Meshes/S02StairRamp.asset";
        public static void Apply()
        {
            var sector = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .SingleOrDefault(t => t.name == "Sector 02 - Compute Vault");
            if (sector == null) return;
            SeparateSecurityCorridor();
            var catwalk = sector.Find("Catwalk Mezzanine (Y=3.2)");
            if (catwalk == null) return;
            foreach (var child in catwalk.Cast<Transform>().ToArray())
                if (child.name == StructureName || child.name.StartsWith("Platform_") || child.name == "Column_MetalSupport")
                    Object.DestroyImmediate(child.gameObject);
            var structure = new GameObject(StructureName).transform;
            structure.SetParent(catwalk, false);
            var metal = Mat("Industrial Metal");
            var dark = Mat("Dark Metal");
            Deck("East Gallery", 4.6f, 21.7f, 36.5f);
            Deck("West Gallery", -4.6f, 22.5f, 31.5f);
            Stairs("East Stairs", 4.6f, 16.5f);
            Stairs("West Stairs", -4.6f, 17.3f);
            // Clear bridge portals; rail lengths use explicit world endpoints.
            Rail("East Outer", new Vector3(5.7f,3.2f,21.7f), new Vector3(5.7f,3.2f,36.5f));
            Rail("East Inner South", new Vector3(3.5f,3.2f,21.7f), new Vector3(3.5f,3.2f,25.9f));
            Rail("East Inner North", new Vector3(3.5f,3.2f,28.1f), new Vector3(3.5f,3.2f,36.5f));
            Rail("East End", new Vector3(3.5f,3.2f,36.5f), new Vector3(5.7f,3.2f,36.5f));
            Rail("West Outer", new Vector3(-5.7f,3.2f,22.5f), new Vector3(-5.7f,3.2f,31.5f));
            Rail("West Inner South", new Vector3(-3.5f,3.2f,22.5f), new Vector3(-3.5f,3.2f,25.9f));
            Rail("West Inner North", new Vector3(-3.5f,3.2f,28.1f), new Vector3(-3.5f,3.2f,31.5f));
            Rail("West End", new Vector3(-5.7f,3.2f,31.5f), new Vector3(-3.5f,3.2f,31.5f));
            // Keep the physical pickup on the desk, with a readable label and local light.
            var cartridge = catwalk.Find("Data Cartridge NL-CART-044");
            if (cartridge != null) cartridge.position = new Vector3(4.6f,4.13f,35.6f);
            foreach (var text in catwalk.GetComponentsInChildren<TextMesh>())
                if (text.text == "SEC-02 MEZZANINE // OBSERVATION") text.text = "ПОСТ НАБЛЮДЕНИЯ · NL-CART-044";
            // Four 12m cable sections previously overlapped along Z. Keep them over the central aisle.
            foreach (var cable in sector.Cast<Transform>().Where(t => t.name.StartsWith("TopCables_Straight_Hanging")))
            {
                cable.localRotation = Quaternion.Euler(0,90,0);
                FitWorldBounds(cable, new Vector3(6.4f,.25f,.35f));
                cable.position = new Vector3(0,5.05f,cable.position.z);
            }

            void Deck(string name, float x, float start, float end)
            {
                Box(name+" Slab", new Vector3(x,3.12f,(start+end)/2), new Vector3(2.2f,.16f,end-start), metal);
                for (float z=start+.7f; z<end; z+=3.5f)
                    foreach (float side in new[]{-1f,1f})
                        Box(name+" Support", new Vector3(x+side*.96f,1.52f,z), new Vector3(.16f,3.04f,.16f), dark);
            }
            void Stairs(string name, float x, float start)
            {
                const float run=5.2f;
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(FacilityBuilder.Content+"/Prefabs/Kit/Platform_Stairs_4.prefab");
                var stairs=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                stairs.name=name;
                stairs.transform.SetParent(structure,false);
                // Source mesh rises towards -Z. Both routes rise northwards, towards their decks.
                stairs.transform.SetPositionAndRotation(new Vector3(x,0,start+run/2),Quaternion.Euler(0,180,0));
                FitWorldBounds(stairs.transform,new Vector3(1.8f,3.2f,run));
                foreach(var collider in stairs.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
                var surface=new GameObject(name+" Walk Surface");
                surface.transform.SetParent(structure,false);
                surface.transform.position=new Vector3(x,0,start+run/2);
                surface.AddComponent<MeshCollider>().sharedMesh=RampMesh();
                Rail(name+" Left",new Vector3(x-.95f,0,start),new Vector3(x-.95f,3.2f,start+run));
                Rail(name+" Right",new Vector3(x+.95f,0,start),new Vector3(x+.95f,3.2f,start+run));
            }
            void Rail(string name, Vector3 from, Vector3 to)
            {
                Beam(name+" Handrail",from+Vector3.up*1.05f,to+Vector3.up*1.05f,.065f,metal);
                Beam(name+" Midrail",from+Vector3.up*.52f,to+Vector3.up*.52f,.045f,dark);
                int spans=Mathf.CeilToInt(Vector3.Distance(from,to)/2);
                for(int i=0;i<=spans;i++)
                {
                    var p=Vector3.Lerp(from,to,(float)i/spans);
                    Box(name+" Post",p+Vector3.up*.52f,new Vector3(.07f,1.04f,.07f),metal);
                }
            }
            void Beam(string name,Vector3 from,Vector3 to,float width,Material material)
            {
                var beam=Box(name,(from+to)/2,new Vector3(width,width,Vector3.Distance(from,to)),material);
                beam.transform.rotation=Quaternion.LookRotation(to-from);
            }
            GameObject Box(string name, Vector3 pos, Vector3 size, Material material)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name;
                go.transform.SetParent(structure,false);go.transform.position=pos;go.transform.localScale=size;
                go.GetComponent<Renderer>().sharedMaterial=material;return go;
            }

        }
        private static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>(FacilityBuilder.Content+"/Materials/"+name+".mat");
        private static void SeparateSecurityCorridor()
        {
            // S07's return corridor used the old 10m room width and protruded 2m into S02.
            // Beyond Z=14.8 it must run outside S02's east wall (X>=7.3).
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(t=>t.name.StartsWith("S07 Corridor West Wall")).ToArray())
            {
                var b=t.GetComponent<Collider>().bounds;
                if(b.max.z<=14.8f)continue;
                if(b.min.z>=14.8f){Object.DestroyImmediate(t.gameObject);continue;}
                t.position=new Vector3(b.center.x,b.center.y,(b.min.z+14.8f)/2);
                t.localScale=new Vector3(b.size.x,b.size.y,14.8f-b.min.z);
            }
            foreach(string name in new[]{"S07 Corridor Floor","S07 Corridor Ceiling"})
            {
                var t=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).SingleOrDefault(t=>t.name==name);
                if(t==null)continue;
                var b=t.GetComponent<Collider>().bounds;
                var north=Object.Instantiate(t.gameObject,t.parent);north.name=name+" North";
                north.transform.position=new Vector3(9.65f,b.center.y,17.4f);
                north.transform.localScale=new Vector3(4.7f,b.size.y,5.2f);
                t.name=name+" South";
                t.position=new Vector3(b.center.x,b.center.y,(b.min.z+14.8f)/2);
                t.localScale=new Vector3(b.size.x,b.size.y,14.8f-b.min.z);
            }
            var portal=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).SingleOrDefault(t=>t.name=="S07 Corridor Portal Bulkhead");
            if(portal!=null){portal.position=new Vector3(9.65f,1.7f,20.2f);portal.localScale=new Vector3(4.7f,3.4f,.4f);}
            var parent=portal!=null?portal.parent:null;
            if(parent!=null && parent.Find("S07 Corridor Width Transition")==null)
            {
                var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="S07 Corridor Width Transition";
                wall.transform.SetParent(parent,false);wall.transform.position=new Vector3(5.95f,1.7f,14.8f);
                wall.transform.localScale=new Vector3(2.7f,3.4f,.4f);wall.GetComponent<Renderer>().sharedMaterial=Mat("Concrete");
            }
        }
        private static void FitWorldBounds(Transform t, Vector3 desired)
        {
            var rs=t.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;
            foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
            var ratio=new Vector3(desired.x/b.size.x,desired.y/b.size.y,desired.z/b.size.z);
            bool quarter=Mathf.Abs(Vector3.Dot(t.right,Vector3.forward))>.99f;
            if(quarter)ratio=new Vector3(ratio.z,ratio.y,ratio.x);
            t.localScale=Vector3.Scale(t.localScale,ratio);
        }
        private static Mesh RampMesh()
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(RampPath);
            Directory.CreateDirectory(Path.GetDirectoryName(RampPath));AssetDatabase.Refresh();
            // Thin closed slab: supports feet and blocks heads from underneath.
            // Vertex positions encode the slope without a rotated collider under nonuniform scale.
            bool create=mesh==null;
            if(create)mesh=new Mesh {name="S02 continuous stair walk surface"};
            mesh.Clear();
            mesh.vertices=new[]{
                new Vector3(-.9f,0,-2.6f),new Vector3(.9f,0,-2.6f),new Vector3(-.9f,3.2f,2.6f),new Vector3(.9f,3.2f,2.6f),
                new Vector3(-.9f,-.12f,-2.6f),new Vector3(.9f,-.12f,-2.6f),new Vector3(-.9f,3.08f,2.6f),new Vector3(.9f,3.08f,2.6f)};
            mesh.triangles=new[]{0,2,1,1,2,3, 4,5,6,5,7,6, 0,4,2,4,6,2, 1,3,5,5,3,7, 0,1,4,4,1,5, 2,6,3,6,7,3};
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            if(create)AssetDatabase.CreateAsset(mesh,RampPath);else EditorUtility.SetDirty(mesh);
            return mesh;
        }
    }
}
