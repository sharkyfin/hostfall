using System;
using Hostfall.Mapping;
using Hostfall.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Hostfall.Tests
{
    public sealed class ExplorationTests
    {
        [Test] public void ExplorationRoundTripsWithoutRevealingTheRestOfTheWorld()
        {
            var world=new FacilitySimulation();int changes=0;world.StateChanged+=()=>changes++;
            Assert.That(world.Exploration.Visit("room.soc"),Is.True);
            Assert.That(world.Exploration.Visit("room.soc"),Is.False);
            Assert.That(changes,Is.EqualTo(1));
            var saved=JsonUtility.FromJson<FacilityStateData>(JsonUtility.ToJson(world.Capture()));
            var loaded=FacilitySimulation.Restore(saved);
            Assert.That(loaded.Exploration.HasVisited("room.soc"),Is.True);
            Assert.That(loaded.Exploration.HasVisited("room.secret"),Is.False);
            Assert.That(new FacilitySimulation().Exploration.HasVisited("room.soc"),Is.False);
        }
        [Test] public void LegacySavesBeginWithAnEmptyExplorationRecord()
        {
            var data=new FacilitySimulation().Capture();data.exploration=null;
            var loaded=FacilitySimulation.Restore(JsonUtility.FromJson<FacilityStateData>(JsonUtility.ToJson(data)));
            Assert.That(loaded.Exploration.Capture().rooms,Is.Empty);
        }
        [Test] public void InvalidExplorationCannotPartiallyReplaceValidProgress()
        {
            var state=new ExplorationState();state.Visit("known");
            Assert.Throws<ArgumentException>(()=>state.Restore(new ExplorationData{rooms=new[]{"another", ""}}));
            Assert.That(state.HasVisited("known"),Is.True);
            Assert.That(state.HasVisited("another"),Is.False);
        }
        [Test] public void RotatedWorldObjectsAndMovingGeometryKeepTheirActualFootprints()
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                go.transform.SetPositionAndRotation(new Vector3(4,1,8),Quaternion.Euler(0,90,0));go.transform.localScale=new Vector3(4,2,.5f);
                Physics.SyncTransforms();var shape=new MapShape(go.GetComponent<Collider>(),false);
                var p=shape.Footprint(0);Assert.That(p.Length,Is.EqualTo(4));
                Assert.That(shape.HorizontalBounds.width,Is.EqualTo(.5f).Within(.001));
                Assert.That(shape.HorizontalBounds.height,Is.EqualTo(4).Within(.001));
                go.transform.position+=Vector3.right*3;Physics.SyncTransforms();shape.Refresh();
                Assert.That(shape.HorizontalBounds.center.x,Is.EqualTo(7).Within(.001));
                foreach(var v in MapGeometry.Clip(shape.Footprint(0),new Rect(6.9f,6,1,4)))
                    Assert.That(v.x,Is.GreaterThanOrEqualTo(6.9f-.001));
                go.GetComponent<BoxCollider>().size=new Vector3(2,1,1);Physics.SyncTransforms();shape.Refresh();
                Assert.That(shape.HorizontalBounds.height,Is.EqualTo(8).Within(.001),"Runtime collider resizing must update the map too");
                Assert.That(shape.Footprint(4),Is.Empty,"Objects below another floor are not drawn there");
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
