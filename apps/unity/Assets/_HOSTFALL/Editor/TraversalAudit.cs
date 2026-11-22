using System;
using System.IO;
using System.Linq;
using Hostfall.Core;
using Hostfall.World;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hostfall.Editor
{
    public static class TraversalAudit
    {
        [Serializable] public class Entry { public string path; public Vector3 center, size; }
        [Serializable] public class Report { public Entry[] colliders; public string[] rows; }
        public static void Export()
        {
            EditorSceneManager.OpenScene("Assets/_HOSTFALL/Content/Scenes/NorthlineFacility.unity");
            var colliders = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
            var entries = colliders.Where(c => c.enabled && !c.isTrigger).Select(c => new Entry { path = PathOf(c.transform), center = c.bounds.center, size = c.bounds.size }).ToArray();
            // Exclude moving threats and progression locks, not static structure.
            foreach(var c in colliders)
                if(c.GetComponentInParent<FacilityDoor>() != null || c.GetComponentInParent<WatcherController>() != null || c.gameObject.layer == CollisionLayers.Player) c.enabled = false;
            Physics.SyncTransforms();
            var rows = new string[461];
            for(int z=0; z<rows.Length; z++)
            {
                var row=new char[241];
                for(int x=0;x<row.Length;x++)
                {
                    var p=new Vector3(-30+x*.5f,0,-22+z*.5f);
                    bool floor=Physics.Raycast(p+Vector3.up*.5f,Vector3.down,out var ground,2.5f,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore);
                    if(floor)p.y=ground.point.y;
                    bool blocked=Physics.CheckCapsule(p+Vector3.up*.37f,p+Vector3.up*1.5f,.3f,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore);
                    row[x]=blocked?'#':floor?'.':' ';
                }
                rows[z]=new string(row);
            }
            Directory.CreateDirectory("../../.artifacts/map-repair-20261113");
            File.WriteAllText("../../.artifacts/map-repair-20261113/geometry.json",JsonUtility.ToJson(new Report {colliders=entries,rows=rows},true));
        }
        private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent)+"/"+t.name;
        public static void RepairAndExport() { FacilityGeometryRepair.ApplyAndSave(); Export(); }
    }
}
