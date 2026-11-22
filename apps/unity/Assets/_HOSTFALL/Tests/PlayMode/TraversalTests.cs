using System.Collections;
using System.Linq;
using Hostfall.Core;
using Hostfall.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Hostfall.Tests
{
    public sealed class TraversalTests
    {
        [UnityTest] public IEnumerator SideDoorsSealTheirActualApertures()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            foreach(string id in new[]{"door.sec07.soc","door.sec08.neuro","door.loop5.shortcut"})
            {
                var door=Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None).Single(d=>d.DoorId==id);
                var start=door.transform.position+new Vector3(-1,.8f,.8f);
                Assert.That(Physics.Raycast(start,Vector3.right,out var hit,2,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore),Is.True,id);
                Assert.That(hit.collider.GetComponentInParent<FacilityDoor>(),Is.EqualTo(door),id+" must block the side of its doorway");
            }
        }
        [UnityTest] public IEnumerator RepairedPassagesHaveFloorAndBodyClearance()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            foreach(var door in Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None))
                foreach(var c in door.GetComponentsInChildren<Collider>())c.enabled=false;
            Physics.SyncTransforms();
            var routes=new[]{
                (new Vector3(62,0,63),new Vector3(62,0,67)),
                (new Vector3(79,0,63),new Vector3(79,0,67)),
                (new Vector3(68,0,56),new Vector3(72,0,56)),
                (new Vector3(71,0,38),new Vector3(71,0,42)),
                (new Vector3(52,0,112),new Vector3(56,0,112)),
                (new Vector3(8.5f,0,18.5f),new Vector3(27,0,18.5f)),
                (new Vector3(27,0,18.5f),new Vector3(27,0,22)),
                (new Vector3(13,0,94),new Vector3(13,0,108)),
                (new Vector3(13,0,108),new Vector3(20,0,108)),
                (new Vector3(30,0,93),new Vector3(30,0,97)),
                // Use the walking lane beside QNT-01, not the console pedestal.
                (new Vector3(1.3f,.125f,38.5f),new Vector3(1.3f,.125f,61.5f)),
                (new Vector3(30,0,124),new Vector3(30,0,130))};
            foreach(var route in routes)
            {
                var delta=route.Item2-route.Item1;
                var hits=Physics.CapsuleCastAll(route.Item1+Vector3.up*.37f,route.Item1+Vector3.up*1.5f,.3f,delta.normalized,delta.magnitude,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore);
                Assert.That(hits,Is.Empty,"Blocked passage at "+route.Item1+": "+string.Join(", ",hits.Select(h=>h.collider.name)));
                for(float d=0;d<=delta.magnitude;d+=.25f)
                    Assert.That(Physics.Raycast(route.Item1+delta.normalized*d+Vector3.up*.25f,Vector3.down,.5f,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore),Is.True,"Missing floor at "+(route.Item1+delta.normalized*d));
            }
        }
    }
}
