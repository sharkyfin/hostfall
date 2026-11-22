using System;
using System.Collections.Generic;
using System.Linq;
using Hostfall.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace Hostfall.Editor
{
    public static class RobotArmorAuthor
    {
        [MenuItem("HOSTFALL/Tools/Repair Robot Armor And Render Parts")]
        public static void Apply()
        {
            if(!Application.dataPath.Contains("/.artifacts/"))throw new InvalidOperationException("Repair the verification copy first.");
            WithReadableSources(()=>
            {
                CombineGuardian();
                EditorSceneManager.OpenScene(FacilityBuilder.ScenePath);
                foreach(var watcher in Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None))Fit(watcher);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
            });
            EditorSceneManager.OpenScene(FacilityBuilder.ScenePath);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
            Debug.Log("HOSTFALL armor fitted to rigid torso triangles; guardian render parts combined");
        }
        public static void WithReadableSources(Action author)
        {
            var importers=new[]{"QuaterniusRobots/Enemy_QuadShell","QuaterniusRobots/Enemy_Trilobite","HostfallCombat/HF_MANTIS6"}.Select(id=>(ModelImporter)AssetImporter.GetAtPath("Assets/_HOSTFALL/Art/"+id+".fbx")).ToArray();
            var readable=importers.Select(i=>i.isReadable).ToArray();
            try{foreach(var importer in importers)if(!importer.isReadable){importer.isReadable=true;importer.SaveAndReimport();}author();}
            finally{for(int i=0;i<importers.Length;i++)if(importers[i].isReadable!=readable[i]){importers[i].isReadable=readable[i];importers[i].SaveAndReimport();}}
        }
        public static void Fit(WatcherController watcher)
        {
            var animator=watcher.GetComponentInChildren<Animator>();animator.Rebind();animator.Play("Idle",0,0);animator.Update(0);
            var shell=watcher.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name.EndsWith("_Body",StringComparison.Ordinal));
            var body=shell.bones.Single(t=>t.name=="Body");int bone=Array.IndexOf(shell.bones,body);
            var mesh=shell.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;var bind=mesh.bindposes[bone];var points=new Vector3[vertices.Length];var rigid=new bool[vertices.Length];Bounds bounds=default;bool first=true;
            for(int i=0;i<vertices.Length;i++)
            {
                var w=weights[i];float influence=(w.boneIndex0==bone?w.weight0:0)+(w.boneIndex1==bone?w.weight1:0)+(w.boneIndex2==bone?w.weight2:0)+(w.boneIndex3==bone?w.weight3:0);
                rigid[i]=influence>.98f;if(!rigid[i])continue;
                points[i]=watcher.transform.InverseTransformPoint(body.TransformPoint(bind.MultiplyPoint3x4(vertices[i])));
                if(first){bounds=new Bounds(points[i],Vector3.zero);first=false;}else bounds.Encapsulate(points[i]);
            }
            if(first)throw new InvalidOperationException("No rigid torso vertices on "+watcher.Id);
            var triangles=mesh.triangles;var plates=watcher.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("ArmorL")||t.name.StartsWith("ArmorR")).ToArray();
            foreach(var plate in plates)
            {
                float side=plate.name.StartsWith("ArmorL")?-1:1;int index=int.Parse(plate.name.Substring(6));
                var origin=bounds.center+new Vector3(side*(bounds.extents.x+.5f),0,(index<2?-1:1)*bounds.size.z*.22f);var ray=new Ray(origin,Vector3.left*side);
                float nearest=float.PositiveInfinity;Vector3 point=default,normal=default;
                for(int n=0;n<triangles.Length;n+=3)
                {
                    int a=triangles[n],b=triangles[n+1],c=triangles[n+2];if(!rigid[a]||!rigid[b]||!rigid[c])continue;
                    if(!Intersect(ray,points[a],points[b],points[c],out float distance)||distance>=nearest)continue;
                    nearest=distance;point=ray.GetPoint(distance);normal=Vector3.Cross(points[b]-points[a],points[c]-points[a]).normalized;
                }
                if(float.IsPositiveInfinity(nearest))throw new InvalidOperationException("No torso mounting surface for "+watcher.Id+" / "+plate.name);
                if(Vector3.Dot(normal,Vector3.right*side)<0)normal=-normal;
                normal=watcher.transform.TransformDirection(normal);var forward=Vector3.ProjectOnPlane(watcher.transform.forward,normal).normalized;
                const float thickness=.035f,embed=.008f;
                plate.SetParent(null,true);plate.localScale=new Vector3(thickness,Mathf.Min(.10f,bounds.size.y*.42f),Mathf.Min(.14f,bounds.size.z*.24f));
                plate.SetPositionAndRotation(watcher.transform.TransformPoint(point)+normal*(thickness*.5f-embed),Quaternion.LookRotation(forward,Vector3.Cross(forward,normal)));
                plate.SetParent(body,true);
            }
        }
        private static bool Intersect(Ray ray,Vector3 a,Vector3 b,Vector3 c,out float distance)
        {
            var e1=b-a;var e2=c-a;var p=Vector3.Cross(ray.direction,e2);float det=Vector3.Dot(e1,p);distance=0;if(Mathf.Abs(det)<1e-9f)return false;
            float inverse=1/det;var t=ray.origin-a;float u=Vector3.Dot(t,p)*inverse;if(u<0||u>1)return false;
            var q=Vector3.Cross(t,e1);float v=Vector3.Dot(ray.direction,q)*inverse;if(v<0||u+v>1)return false;
            distance=Vector3.Dot(e2,q)*inverse;return distance>=0;
        }
        private static bool Independent(string name)=>name.StartsWith("ArmorL")||name.StartsWith("ArmorR")||name.StartsWith("CoolingLeft")||name.StartsWith("CoolingRight")||name.StartsWith("Coolant glass")||name.StartsWith("CoreHatch")||name=="Core"||name=="Sensor";
        private static void CombineGuardian()
        {
            const string prefab="Assets/_HOSTFALL/Resources/Combat/MANTIS6.prefab",folder="Assets/_HOSTFALL/Content/Meshes/Guardian";
            var root=PrefabUtility.LoadPrefabContents(prefab);
            try
            {
                if(root.transform.Find("Fixed render groups") is Transform existingGroups)
                {
                    var meshes=existingGroups.GetComponentsInChildren<MeshFilter>();
                    if(meshes.Length!=6||meshes.Any(f=>f.sharedMesh==null||f.sharedMesh.vertexCount==0))throw new InvalidOperationException("Invalid existing guardian groups; restore the original prefab before repair.");
                    return;
                }
                foreach(var nested in root.GetComponentsInChildren<Transform>().Where(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)).ToArray())
                    PrefabUtility.UnpackPrefabInstance(nested.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                System.IO.Directory.CreateDirectory(folder);AssetDatabase.Refresh();
                var fixedParts=root.GetComponentsInChildren<MeshFilter>().Where(f=>!Independent(f.name)).ToArray();
                long inputIndices=0;
                var groups=new Dictionary<Material,List<CombineInstance>>();
                foreach(var filter in fixedParts)
                {
                    var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null)continue;
                    for(int i=0;i<filter.sharedMesh.subMeshCount;i++)
                    {
                        if(!filter.sharedMesh.isReadable)throw new InvalidOperationException("Unreadable source mesh: "+filter.name);
                        inputIndices+=filter.sharedMesh.GetIndexCount(i);
                        var material=renderer.sharedMaterials[i];if(!groups.TryGetValue(material,out var group))groups[material]=group=new List<CombineInstance>();
                        group.Add(new CombineInstance{mesh=filter.sharedMesh,subMeshIndex=i,transform=root.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix});
                    }
                }
                var parent=new GameObject("Fixed render groups");parent.transform.SetParent(root.transform,false);
                long outputIndices=0;int index=0;foreach(var group in groups)
                {
                    string path=folder+"/Fixed-"+(index++)+".asset";var mesh=new Mesh{name=group.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(group.Value.ToArray(),true,true);mesh.RecalculateBounds();
                    if(mesh.vertexCount==0)throw new InvalidOperationException("Empty guardian render group");outputIndices+=mesh.GetIndexCount(0);
                    var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing!=null){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
                    var go=new GameObject("Fixed "+group.Key.name);go.transform.SetParent(parent.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=group.Key;
                }
                if(inputIndices!=outputIndices)throw new InvalidOperationException("Guardian geometry lost during combine");
                System.IO.File.WriteAllText("../../.artifacts/robot-performance-20261115/guardian-geometry.txt",$"Fixed indices before={inputIndices} after={outputIndices}; groups={groups.Count}; independent={root.GetComponentsInChildren<MeshFilter>().Count(f=>Independent(f.name))}\n");
                foreach(var filter in fixedParts){Object.DestroyImmediate(filter.GetComponent<MeshRenderer>());Object.DestroyImmediate(filter);}
                PrefabUtility.SaveAsPrefabAsset(root,prefab);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
