using Hostfall.Simulation;
using NUnit.Framework;

namespace Hostfall.Tests
{
    public sealed class AttackPermitTests
    {
        [Test]
        public void ARepeatedRequestConsumesOneSlotAndAThirdActorWaits()
        {
            var permits = new AttackPermitPool(2);
            Assert.That(permits.Acquire("pursuit", 1.45), Is.True);
            Assert.That(permits.Acquire("pursuit", 1.45), Is.True);
            Assert.That(permits.Acquire("overwatch", 1.45), Is.True);
            Assert.That(permits.Acquire("flanker", 1.45), Is.False);
            permits.Release("pursuit");
            Assert.That(permits.Acquire("flanker", 1.45), Is.True);
        }

        [Test]
        public void AbandonedPermitsExpireWithoutRenewingAtEveryRequest()
        {
            var permits = new AttackPermitPool(1);
            Assert.That(permits.Acquire("old", 1.45), Is.True);
            permits.Advance(1.4);
            Assert.That(permits.Acquire("old", 1.45), Is.True);
            Assert.That(permits.Acquire("new", 1.45), Is.False);
            permits.Advance(.1);
            Assert.That(permits.Acquire("new", 1.45), Is.True);
        }
    }
}
