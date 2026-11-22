using System.Collections;
using System.Linq;
using Hostfall.Persistence;
using Hostfall.Player;
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
        [UnityTest] public IEnumerator MatriarchLeapsFromItsRealMountAndCanBeDodged()
        {
            yield return EnterGame();
            var spider = Object.FindObjectsByType<FacilityMiniboss>(FindObjectsSortMode.None)
                .Single(boss => boss.Battle.Kind == FacilityMinibossKind.SpiderMatriarch);
            foreach (var component in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (component is IDamageable && component is not PlayerHealth && component != spider) component.enabled = false;
            var player = Object.FindAnyObjectByType<PlayerMotor>();
            var hp = player.GetComponent<PlayerHealth>();
            var basePoint = spider.transform.position;
            player.Restore(new PlayerStateData { position = basePoint + new Vector3(0, .04f, 2.65f), yaw = 180 });
            player.enabled = false;
            Physics.SyncTransforms();
            var shell = spider.GetComponentInChildren<MinibossComponent>().transform.parent;
            yield return ObserveContainment(() => spider.Battle.Clock > .38f, 3, () => "Matriarch did not telegraph its leap");
            Assert.That(spider.Battle.Move, Is.EqualTo(MinibossMove.Leap));
            Assert.That(shell.position.y, Is.GreaterThan(basePoint.y + .4f));
            player.Restore(new PlayerStateData { position = basePoint + new Vector3(-3.8f, .04f, 4.4f), yaw = 180 });
            yield return ObserveContainment(() => spider.Battle.Exposed, 3, () => "The leap did not expose its coupler");
            Assert.That(hp.CurrentHealth, Is.EqualTo(100));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator MinibossesHavePhysicalComponentsAndWardenPowerPersists()
        {
            yield return EnterGame();
            var roster = Object.FindObjectsByType<FacilityMiniboss>(FindObjectsSortMode.None);
            Assert.That(roster.Select(boss => boss.Battle.Kind), Is.EquivalentTo(new[] {
                FacilityMinibossKind.SpiderMatriarch, FacilityMinibossKind.SecurityWarden, FacilityMinibossKind.Harvester }));
            foreach (var boss in roster)
            {
                var collider = boss.GetComponentInChildren<MinibossComponent>().GetComponent<Collider>();
                Assert.That(collider.enabled && collider.bounds.size.sqrMagnitude > .05f, Is.True);
                Assert.That(boss.Battle.Health, Is.EqualTo(boss.Battle.Maximum));
            }
            var warden = roster.Single(boss => boss.Battle.Kind == FacilityMinibossKind.SecurityWarden);
            var player = Object.FindAnyObjectByType<PlayerMotor>();
            player.Restore(new PlayerStateData { position = warden.transform.position + warden.transform.forward * 4 + Vector3.up * .04f });
            var breaker = Object.FindAnyObjectByType<WardenBreaker>();
            breaker.Interact();
            Assert.That(warden.Battle.PowerCut, Is.True);
            Assert.That(breaker.Prompt, Does.Contain("отключён"));
            warden.TakeDamage(80);
            Assert.That(warden.Health, Is.LessThan(warden.Maximum - 40));
            var campaign = Root.Simulation.ComputerCampaign;
            Assert.That(ComputerCampaign.Restore(campaign.Capture()).Miniboss(FacilityMinibossKind.SecurityWarden).PowerCut, Is.True);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
