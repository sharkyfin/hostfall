using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hostfall.Core;
using Hostfall.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Hostfall.Editor
{
    public static class WorldBoundaryAudit
    {
        [Serializable] public class Report {public int walkableSamples;public Vector3 minimum,maximum;public Vector3[] exposedEdges;}
        [MenuItem("HOSTFALL/Tools/Scan Ground Boundaries")]
        public static void Scan()
        {
            if(EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save or discard active scene changes before scanning.");
            EditorSceneManager.OpenScene(FacilityBuilder.ScenePath);
            foreach(var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
                if(c.GetComponentInParent<FacilityDoor>()!=null||c.GetComponentInParent<IDamageable>()!=null||c.gameObject.layer==CollisionLayers.Player)c.enabled=false;
            Physics.SyncTransforms();var edges=new List<Vector3>();int free=0;
            var solids=Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>c.enabled&&!c.isTrigger).ToArray();
            var bounds=solids[0].bounds;foreach(var c in solids.Skip(1))bounds.Encapsulate(c.bounds);
            var minimum=new Vector3(Mathf.Floor(bounds.min.x)-1,0,Mathf.Floor(bounds.min.z)-1);
            var maximum=new Vector3(Mathf.Ceil(bounds.max.x)+1,0,Mathf.Ceil(bounds.max.z)+1);
            // Ground layer only. Sloped/upstairs circulation is exercised separately by a real controller.
            for(float z=minimum.z;z<=maximum.z;z+=.5f)for(float x=minimum.x;x<=maximum.x;x+=.5f)
            {
                var p=new Vector3(x,.45f,z);
                if(!Physics.Raycast(p,Vector3.down,out var floor,2,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore))continue;
                p.y=floor.point.y;
                if(Physics.CheckCapsule(p+Vector3.up*.37f,p+Vector3.up*1.5f,.3f,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore))continue;free++;
                foreach(var d in new[]{Vector3.left,Vector3.right,Vector3.forward,Vector3.back})
                {
                    var next=p+d*.75f;
                    if(Physics.Raycast(next+Vector3.up*.45f,Vector3.down,2,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore))continue;
                    if(Physics.CapsuleCast(p+Vector3.up*.37f,p+Vector3.up*1.5f,.3f,d,.75f,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore))continue;
                    edges.Add(next);
                }
            }
            File.WriteAllText(WorldGeometryWorkbench.Output+"/boundary.json",JsonUtility.ToJson(new Report{walkableSamples=free,minimum=minimum,maximum=maximum,exposedEdges=edges.ToArray()},true));
            EditorSceneManager.OpenScene(FacilityBuilder.ScenePath);Debug.Log($"HOSTFALL boundary scan: {free} body-clear samples, {edges.Count} exposed edge samples (review required).");
        }
    }
}
