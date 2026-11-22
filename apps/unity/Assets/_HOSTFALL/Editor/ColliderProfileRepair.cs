using System;
using System.Linq;
using Hostfall.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Hostfall.Editor
{
    // A world-space AABB cannot be copied into a rotated object's local BoxCollider.
    // Measure the visible meshes directly in the collider's coordinate system instead.
    internal static class ColliderProfileRepair
    {
        internal static void Fit(BoxCollider collider)
        {
            if(collider==null)return;
            Bounds bounds=default;bool found=false;
            foreach(var filter in collider.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<MeshRenderer>();
                if(filter.sharedMesh==null||renderer==null||!renderer.enabled)continue;
                var b=filter.sharedMesh.bounds;
                var matrix=collider.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                for(int i=0;i<8;i++)
                {
                    var p=matrix.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
                    if(!found){bounds=new Bounds(p,Vector3.zero);found=true;}else bounds.Encapsulate(p);
                }
            }
            if(!found)throw new InvalidOperationException("No visible mesh for collider "+collider.name);
            collider.center=bounds.center;collider.size=bounds.size;
            EditorUtility.SetDirty(collider);
            PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
        }
        [MenuItem("HOSTFALL/Tools/Build Collision Review")]
        public static void Build()
        {
            UnityEditor.OSXStandalone.UserBuildSettings.architecture=UnityEditor.Build.OSArchitecture.x64;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{FacilityBuilder.ScenePath},locationPathName="Builds/macOS/HOSTFALL-MapFix.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.None});
            System.IO.File.WriteAllText(CollisionAudit.Output+"/desktop-result.txt",report.summary.result+"\n"+report.summary.totalErrors+" errors\n"+report.summary.totalSize+" bytes");
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new InvalidOperationException("Collision review build failed");
        }
        [MenuItem("HOSTFALL/Tools/Repair Visible Collider Profiles")]
        public static void Apply()
        {
            if(!Application.dataPath.Contains("/.artifacts/"))throw new InvalidOperationException("Repair and verify in the isolated copy first.");
            if(EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Unsaved scene changes");
            EditorSceneManager.OpenScene(FacilityBuilder.ScenePath);
            int count=0;
            foreach(var c in Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
            {
                bool pickup=c.GetComponent<WeaponPickup>()!=null||c.GetComponent<PulseSidearmPickup>()!=null;
                bool fixture=c.name=="Prop_Light_Small"&&c.transform.parent!=null&&c.transform.parent.parent!=null&&c.transform.parent.parent.name=="NORTHLINE / Service Bays";
                if(!pickup&&!fixture)continue;
                Fit(c);count++;
            }
            var window=GameObject.Find("Dawn Window Glass");
            if(window==null)throw new InvalidOperationException("Missing evacuation window");
            if(window.GetComponent<Collider>()==null)window.AddComponent<BoxCollider>();
            foreach(float x in new[]{3f,17f})
            {
                string name=x<10?"S10 Evac South Shoulder West":"S10 Evac South Shoulder East";
                var go=GameObject.Find(name);
                if(go==null){go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(window.transform.parent,true);}
                go.transform.position=new Vector3(x,2.4f,191.8f);go.transform.localScale=new Vector3(2.4f,4.8f,.4f);
                go.GetComponent<Renderer>().sharedMaterial=GameObject.Find("S10 Evac West Wall").GetComponent<Renderer>().sharedMaterial;
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("HOSTFALL collider profiles repaired: "+count);
        }
    }
}
