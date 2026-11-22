using System.Collections;
using System.Linq;
using Hostfall.Persistence;
using Hostfall.Player;
using Hostfall.Simulation;
using Hostfall.Simulation.Computers;
using Hostfall.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hostfall.Tests
{
    public sealed partial class ComputerWorldTests
    {
        [UnityTest]
        public IEnumerator NearbyWatchersUseDifferentRolesAndLoseCoordinationWhenCommandIsIsolated()
        {
            yield return EnterGame();
            var campaign = Root.Simulation.ComputerCampaign;
            var player = Object.FindAnyObjectByType<PlayerMotor>();
            var room = GameObject.CreatePrimitive(PrimitiveType.Cube);
            room.name = "Watcher test floor";
            room.transform.position = new Vector3(80, -.1f, 150);
            room.transform.localScale = new Vector3(12, .2f, 18);
            var group = new GameObject("Watcher squad fixture");
            var actors = new WatcherController[4];
            var playerPosition = new Vector3(80, .04f, 152);
            player.Restore(new PlayerStateData { position = playerPosition });
            player.enabled = false;
            var starts = new[]
            {
                new Vector3(79, 0, 149), new Vector3(80, 0, 147.9f),
                new Vector3(81.3f, 0, 148.1f), new Vector3(81.7f, 0, 146)
            };
            try
            {
                for (int i = 0; i < actors.Length; i++)
                {
                    var actor = new GameObject("Watcher squad test actor");
                    actor.transform.SetParent(group.transform);
                    actor.transform.position = starts[i];
                    var sensor = new GameObject("Sensor").transform;
                    sensor.SetParent(actor.transform, false);
                    sensor.localPosition = Vector3.up * 1.2f;
                    var capsule = actor.AddComponent<CapsuleCollider>();
                    capsule.center = Vector3.up * .75f;
                    capsule.height = 1.5f;
                    capsule.radius = .28f;
                    actors[i] = actor.AddComponent<WatcherController>();
                    actors[i].Configure(sensor, null, id: "aux-watch-squad-test-" + i, first: starts[i],
                        last: starts[i] + Vector3.forward, xb: new Vector2(74.5f, 85.5f), zb: new Vector2(141.6f, 158.4f));
                    actors[i].Initialize(Root.Simulation, player, Controls);
                }
                group.AddComponent<WatcherSquads>().Initialize(Controls, campaign, actors);
                Physics.SyncTransforms();
                yield return ObserveContainment(() => actors.All(a => a.TacticalRole != WatcherRole.Local), 4,
                    () => "The visible target did not produce four local squad roles");
                Assert.That(actors.Select(actor => actor.TacticalRole),
                    Is.EquivalentTo(new[] { WatcherRole.Pursuit, WatcherRole.Flank, WatcherRole.Overwatch, WatcherRole.Ambush }));

                for (int i = 0; i < actors.Length; i++)
                    actors[i].transform.position = playerPosition + new Vector3((i - 1.5f) * .39f, -.04f, -1.1f);
                Physics.SyncTransforms();
                foreach (var actor in actors) actor.Tick(.016f);
                Assert.That(actors.Count(actor => actor.State.Mode == WatcherMode.AttackWindup), Is.InRange(1, 2));

                Assert.That(campaign.Network.SetIsolation(NetworkDefenseSession.CommandNode, true), Is.True);
                yield return ObserveContainment(() => actors.All(actor => actor.TacticalRole == WatcherRole.Local), 1,
                    () => "Roles survived the loss of command");
                Assert.That(actors.Any(actor => actor.Sees(player.Eye)), Is.True);
            }
            finally
            {
                Object.Destroy(group);
                Object.Destroy(room);
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
