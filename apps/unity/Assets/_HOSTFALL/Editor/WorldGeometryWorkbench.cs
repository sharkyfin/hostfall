using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Hostfall.Editor
{
    // Inspection is separate from repair; reports never delete suspected overlaps.
    public static class WorldGeometryWorkbench
    {
        [Serializable] public class Shape { public string path,prefab;public Vector3 position,rotation,size,center;public Vector3[] colliderSizes; }
        [Serializable] public class Surface { public string name;public int renderers;public string shader,baseMap,normalMap; }
        [Serializable] public class Report {public Shape[] modules;public Surface[] materials;}
        public static string Output
        {
            get {var p=new DirectoryInfo(Application.dataPath);while(p!=null&&!File.Exists(Path.Combine(p.FullName,"AGENTS.md")))p=p.Parent;
                string path=Path.Combine(p?.FullName??Application.dataPath,".artifacts/world-workbench");Directory.CreateDirectory(path);return path;}
        }
        [MenuItem("HOSTFALL/Tools/Audit Saved World")]
        public static void Audit()
        {
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save or discard the active scene explicitly before auditing the saved world.");
            EditorSceneManager.OpenScene(FacilityBuilder.ScenePath);
            Physics.SyncTransforms();
            var modules=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Platform_")||t.name.StartsWith("Column_")||t.name.Contains("Slab")||t.name.Contains("Floor")||t.name.Contains("Ceiling")||t.name.Contains("Wall"))
                .Where(t=>t.GetComponentsInChildren<Renderer>().Length>0).Select(t=>
                {var rs=t.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
                    return new Shape {path=PathOf(t),prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject),position=t.position,rotation=t.eulerAngles,size=b.size,center=b.center,colliderSizes=t.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).Select(c=>c.bounds.size).ToArray()};}).ToArray();
            var materials=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).GroupBy(m=>m).Select(g=>new Surface {name=g.Key.name,renderers=g.Count(),shader=g.Key.shader.name,baseMap=g.Key.HasProperty("_BaseMap")?AssetDatabase.GetAssetPath(g.Key.GetTexture("_BaseMap")):"",normalMap=g.Key.HasProperty("_BumpMap")?AssetDatabase.GetAssetPath(g.Key.GetTexture("_BumpMap")):""}).OrderByDescending(m=>m.renderers).ToArray();
            File.WriteAllText(Output+"/geometry.json",JsonUtility.ToJson(new Report{modules=modules,materials=materials},true));
            FacilityReview.CaptureWorld();
            Debug.Log("HOSTFALL geometry/material audit written to "+Output);
        }
        private static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
    }
}
