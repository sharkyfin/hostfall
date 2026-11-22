using System.Linq;
using Hostfall.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Hostfall.Editor
{
    public static class WorldCirculationRepair
    {
        [MenuItem("HOSTFALL/Tools/Repair World Circulation")]
        public static void ApplyAndSave()
        {
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new System.InvalidOperationException("Active scene has unsaved changes.");
            EditorSceneManager.OpenScene(FacilityBuilder.ScenePath);Apply();
            EnvironmentPass.Apply();AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
        public static void Apply()
        {
            // Shorten the S09 ingress to the actual north edge of S06. They previously
            // occupied the same 10m strip; S09's low roof intersected the S06 catwalk.
            foreach(var t in All().Where(t=>t.name.StartsWith("S09 Ring ")||t.name.StartsWith("S09 Backup ")).ToArray())
            {
                var c=t.GetComponent<BoxCollider>();if(c==null)continue;var b=c.bounds;
                if(b.min.z>=95.5f||b.max.z<=95.5f)continue;
                t.position=new Vector3(b.center.x,b.center.y,(95.5f+b.max.z)/2);
                t.localScale=new Vector3(b.size.x,b.size.y,b.max.z-95.5f);
            }
            var sector09=All().Single(t=>t.name=="Sector 09 - Cloud Control");
            // Racks stay below the 5.45m ring ceiling and outside the approach lane.
            var racks=sector09.Cast<Transform>().Where(t=>t.name=="High-Density Memory Module Column").OrderBy(t=>t.position.x).ThenBy(t=>t.position.z).ToArray();
            for(int i=0;i<racks.Length;i++)
            {racks[i].position=new Vector3(i<3?20.1f+2.9f*i:i<6?33+3*(i-3):44+3*(i-6),0,100.2f);racks[i].localScale=new Vector3(1,.64f,1);}
            var track=sector09.Find("Ceiling Rail Track");if(track!=null)track.position=new Vector3(30,5.2f,98.4f);
            var turret=sector09.GetComponentInChildren<RailEyeTurretController>();
            if(turret!=null)
            {
                turret.transform.position=new Vector3(22,4.8f,98.4f);
                var head=turret.transform.Find("Turret Head");var muzzle=turret.GetComponentsInChildren<Transform>().First(t=>t.name=="Rail Muzzle");
                turret.Configure(head,muzzle,turret.GetComponentInChildren<Light>(),new Vector3(22,4.8f,98.4f),new Vector3(38,4.8f,98.4f));EditorUtility.SetDirty(turret);
            }
            foreach(var stair in All().Where(t=>t.name.StartsWith("Platform_Stairs_")).ToArray())
            {
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(stair.gameObject));
                // Measure original normalized visual bounds, independent of scene scale.
                var rs=source.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs.Skip(1))bounds.Encapsulate(r.bounds);
                string sector=PathOf(stair);
                if(sector.Contains("Sector 04"))
                {stair.SetPositionAndRotation(new Vector3(stair.position.x,-1.2f,stair.position.z<50?39.5f:60.5f),Quaternion.Euler(0,stair.position.z<50?180:0,0));Fit(stair,new Vector3(.9f,1.325f,2.2f));}
                if(sector.Contains("Sector 05"))
                {stair.localRotation=Quaternion.Euler(0,stair.position.z<72?180:0,0);}
                if(sector.Contains("Sector 06"))
                {
                    stair.rotation=Quaternion.Euler(0,stair.position.x<33?270:90,0);
                    Fit(stair,new Vector3(4.5f,4.3f,1.8f));
                }
                if(sector.Contains("Sector 07"))
                {stair.SetPositionAndRotation(new Vector3(38,0,22.6f),Quaternion.Euler(0,180,0));Fit(stair,new Vector3(1.8f,3.3f,4));}
                if(sector.Contains("Sector 09"))
                {stair.SetPositionAndRotation(new Vector3(20,0,115.3f),Quaternion.Euler(0,180,0));Fit(stair,new Vector3(1.8f,3.725f,5.4f));}
                StairWalkSurface.Configure(stair.gameObject,bounds.size);
            }
            var mezz=All().Single(t=>t.name=="Logistics Sorting Mezzanine");
            foreach(float z in new[]{68.2f,75.8f})
            {
                string n="Stair top landing "+z;var landing=mezz.Find(n);
                if(landing==null){landing=GameObject.CreatePrimitive(PrimitiveType.Cube).transform;landing.name=n;landing.SetParent(mezz,true);}
                landing.position=new Vector3(4,.925f,z);landing.localScale=new Vector3(1.4f,.15f,1);
                landing.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(FacilityBuilder.Content+"/Materials/Industrial Metal.mat");
            }
            foreach(var t in mezz.Cast<Transform>().Where(t=>t.name.StartsWith("Platform_Rails")&&Mathf.Abs(t.position.x-4.75f)<.1f))Fit(t,new Vector3(.08f,1.1f,6));
            var cat=All().Single(t=>t.name=="Catwalk T1 Slab");cat.position=new Vector3(34,3.2f,36.3f);cat.localScale=new Vector3(14,.2f,23.4f);
            FixRail("Logistics Sorting Mezzanine",t=>t.position.z<67||t.position.z>77,new Vector3(8.7f,1.1f,.08f));
            FixRail("Atrium Catwalk Tier 1",t=>true,new Vector3(.08f,1.1f,23.5f));
            FixRail("Dispatch Observation Mezzanine",t=>true,new Vector3(28,1.1f,.08f));
            // Repair the same quarter-turn error on S04's overhead supply cable bundles.
            foreach(var t in All().Where(t=>t.name.StartsWith("TopCables_Straight_Hanging")&&PathOf(t).Contains("Sector 04")))Fit(t,new Vector3(16,.45f,.6f));
            SealGroundBoundaries();
            Physics.SyncTransforms();
        }
        private static void SealGroundBoundaries()
        {
            var root=GameObject.Find("NORTHLINE / Boundary Repairs");if(root!=null)Object.DestroyImmediate(root);
            var parent=new GameObject("NORTHLINE / Boundary Repairs").transform;
            Box("SOC shortcut end cap",new Vector3(12.15f,1.7f,-2.5f),new Vector3(.3f,3.4f,1.8f));
            Box("Citadel foyer east infill",new Vector3(36.2f,3,53.75f),new Vector3(.4f,6,7.5f));
            Box("Citadel atrium north infill",new Vector3(39.1f,4.5f,50.2f),new Vector3(6.2f,9,.4f));
            Box("Cloud ingress threshold",new Vector3(30,-.1f,95.35f),new Vector3(4,.2f,.7f),true);
            Box("Cloud backup south seal",new Vector3(48,2.5f,95.3f),new Vector3(12.4f,5,.4f));
            Box("Witness south seal",new Vector3(59,2.25f,101.8f),new Vector3(10.4f,4.5f,.4f));
            Box("Witness north seal",new Vector3(59,2.25f,122.2f),new Vector3(10.4f,4.5f,.4f));
            Box("Dispatch north west seal",new Vector3(22.65f,4.5f,125.2f),new Vector3(9.3f,9,.4f));
            Box("Dispatch north east seal",new Vector3(43.35f,4.5f,125.2f),new Vector3(21.3f,9,.4f));
            void Box(string name,Vector3 pos,Vector3 size,bool floor=false)
            {
                var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.position=pos;g.transform.localScale=size;
                g.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(FacilityBuilder.Content+"/Materials/"+(floor?"Floor Panels":"Concrete")+".mat");
            }
        }
        private static void FixRail(string parent,System.Func<Transform,bool> predicate,Vector3 size)
        {foreach(var t in All().Where(t=>t.parent!=null&&t.parent.name==parent&&t.name.StartsWith("Platform_Rails")&&predicate(t)))Fit(t,size);}
        internal static void Fit(Transform t,Vector3 desired)
        {
            var rs=t.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
            var ratio=new Vector3(desired.x/b.size.x,desired.y/b.size.y,desired.z/b.size.z);if(Mathf.Abs(Vector3.Dot(t.right,Vector3.forward))>.99f)ratio=new Vector3(ratio.z,ratio.y,ratio.x);
            t.localScale=Vector3.Scale(t.localScale,ratio);
        }
        private static Transform[] All()=>Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        private static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
    }
}
