using Hostfall.Simulation;
using NUnit.Framework;
namespace Hostfall.Tests
{
    public sealed class WeaponWorldTests
    {
        [Test]public void DroppedWeaponsAreIndependentAndCollectionIsIdempotent()
        {
            var state=new WeaponWorldState();var first=state.Add(new WeaponItemData{kind=1,ammo=7,reserve=19,heat=.8f,overheated=true,x=1,y=.5f,z=1});
            Assert.That(state.CollectAuthored("weapon.rack.1"),Is.True);Assert.That(state.CollectAuthored("weapon.rack.1"),Is.False);
            var saved=state.Capture();first.ammo=1;Assert.That(saved.drops[0].ammo,Is.EqualTo(7));
            var restored=new WeaponWorldState();restored.Restore(saved);Assert.That(restored.WasCollected("weapon.rack.1"),Is.True);
            Assert.That(restored.Take(first.id,out var taken),Is.True);Assert.That(taken.ammo,Is.EqualTo(7));Assert.That(taken.overheated,Is.True);Assert.That(restored.Take(first.id,out _),Is.False);
            var next=restored.Add(new WeaponItemData{kind=0,ammo=1,y=.5f});Assert.That(next.id,Is.Not.EqualTo(first.id));
        }
        [Test]public void OldSavesNeedNoWeaponWorldAndInvalidCountersAreRejected()
        {
            var state=new WeaponWorldState();state.Restore(null);Assert.That(state.Capture().drops,Is.Empty);
            var item=state.Add(new WeaponItemData{kind=0,ammo=1,y=.5f});var data=state.Capture();data.nextId=1;
            Assert.Throws<System.ArgumentException>(()=>state.Restore(data));
            Assert.Throws<System.ArgumentException>(()=>WeaponItemData.Validate(new WeaponItemData{id="x",kind=1,ammo=99}));
            Assert.Throws<System.ArgumentException>(()=>WeaponItemData.Validate(new WeaponItemData{id="x",kind=1,y=float.NaN}));
        }
    }
}
