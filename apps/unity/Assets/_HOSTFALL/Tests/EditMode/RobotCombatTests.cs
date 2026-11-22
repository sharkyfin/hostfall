using Hostfall.Simulation;
using NUnit.Framework;
namespace Hostfall.Tests
{
    public sealed class RobotCombatTests
    {
        [Test] public void PulseDamagesElectronicsAndChargedCoilPenetratesHull()
        {
            var standard = new RobotCombatState("standard", true, 780);
            var specialized = new RobotCombatState("specialized", true, 780);
            var unchargedHull = standard.ApplyHit(80, RobotHitRegion.Hull, RobotImpact.Coil);
            var chargedHull = specialized.ApplyHit(80, RobotHitRegion.Hull, RobotImpact.Coil, 1);
            Assert.That(chargedHull, Is.GreaterThan(unchargedHull * 3));
            Assert.That(standard.ApplyHit(25, RobotHitRegion.Sensor),
                Is.LessThan(specialized.ApplyHit(25, RobotHitRegion.Sensor, RobotImpact.Pulse)));
            Assert.That(specialized.CoreExposed, Is.False);
        }
        [Test] public void PrecisionPaysAndDriveDamageChangesMobility()
        {
            var body=new RobotCombatState("a",false,260);var head=new RobotCombatState("b",false,260);
            body.ApplyHit(40,RobotHitRegion.Hull);head.ApplyHit(40,RobotHitRegion.Sensor);
            Assert.That(head.Health,Is.LessThan(body.Health));
            body.ApplyHit(100,RobotHitRegion.LeftDrive);Assert.That(body.Mobility,Is.EqualTo(.68f));
            body.ApplyHit(100,RobotHitRegion.RightDrive);Assert.That(body.Mobility,Is.EqualTo(.38f));
        }
        [Test] public void BossRequiresCoolingThenReactorAndDeathIsIdempotent()
        {
            var b=new RobotCombatState("boss",true,780);b.ApplyHit(10000,RobotHitRegion.Hull);
            Assert.That(b.IsDead,Is.False);Assert.That(b.CoreExposed,Is.False);
            b.ApplyHit(140,RobotHitRegion.LeftCoolant);Assert.That(b.CoreExposed,Is.False);
            b.ApplyHit(140,RobotHitRegion.RightCoolant);Assert.That(b.CoreExposed,Is.True);
            b.ApplyHit(95,RobotHitRegion.Core);Assert.That(b.IsDead,Is.True);Assert.That(b.ApplyHit(20,RobotHitRegion.Core),Is.Zero);
        }
        [Test] public void CombatSaveRestoresModulesPoseAndDoesNotAlias()
        {
            var roster=new CombatRoster();var r=roster.Get("sentry-crawler-e06",true,780);
            r.ApplyHit(140,RobotHitRegion.LeftCoolant);r.Data.hasPose=true;r.Data.x=-15;r.Data.z=25;r.Data.yaw=170;r.Data.stun=3;
            var data=roster.Capture();var restored=new CombatRoster();restored.Restore(data);var copy=restored.Get(r.Id);
            Assert.That(copy.Health,Is.EqualTo(r.Health));Assert.That(copy.BrokenMask,Is.EqualTo(r.BrokenMask));Assert.That(copy.Data.stun,Is.EqualTo(3));
            data[0].health=1;Assert.That(copy.Health,Is.Not.EqualTo(1));
            Assert.Throws<System.ArgumentException>(()=>restored.Restore(new[]{r.Capture(),r.Capture()}));
        }
        [Test] public void LegacySavesHaveOptionalCombatAndWeaponData()
        {
            var roster=new CombatRoster();roster.Restore(null);Assert.That(roster.Capture(),Is.Empty);
            WeaponLoadoutData.Validate(null);
            var d=new WeaponLoadoutData{active=2};Assert.Throws<System.ArgumentException>(()=>WeaponLoadoutData.Validate(d));
        }
    }
}
