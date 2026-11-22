using System.Collections;
using System.IO;
using System.Linq;
using Hostfall.Core;
using Hostfall.Devices;
using Hostfall.Persistence;
using Hostfall.Player;
using Hostfall.Simulation;
using Hostfall.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Hostfall.Tests
{
    public sealed class EnemyBehaviorTests
    {
        private static WatcherController Enemy(string id)=>Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None).Single(r=>r.Id==id);
        private static void Pose(WatcherController robot,Vector3 position,float yaw=0)
        { var data=robot.Capture(); data.position=position; data.bodyYaw=yaw; data.sensorEuler=Vector3.zero; data.scan=0; data.direction=1; data.contactCooldown=0; robot.Restore(data); }
        private static void PlayerAt(Vector3 position)
        { var p=Object.FindAnyObjectByType<PlayerMotor>(); var data=p.Capture(false); data.position=position; data.planarVelocity=Vector3.zero; p.Restore(data); }

        [UnityTest] public IEnumerator DetectionAndMovementRespectWallsAndContactIsRateLimited()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var controls=Object.FindAnyObjectByType<ControlState>(); var root=Object.FindAnyObjectByType<FacilityRoot>(); var robot=Enemy("watcher-r17");
            Pose(robot,new Vector3(0,0,2.2f)); PlayerAt(new Vector3(0,0,3.5f)); Physics.SyncTransforms();
            Assert.That(robot.Sees(Object.FindAnyObjectByType<PlayerMotor>().Eye),Is.True);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name="Enemy QA wall"; wall.transform.position=new Vector3(0,1,3); wall.transform.localScale=new Vector3(2,2,.15f); Physics.SyncTransforms();
            robot.Tick(0); Assert.That(robot.State.Mode,Is.EqualTo(WatcherMode.Patrol),"A wall must block detection");
            controls.SetMode(ControlMode.Exploring); robot.Tick(1); Physics.SyncTransforms();
            Assert.That(robot.transform.position.z,Is.LessThan(2.6f),"Body sweep must stop before the wall");
            int contacts=root.Simulation.WatcherContacts;
            Object.Destroy(wall); yield return null;
            Pose(robot,new Vector3(0,0,2.2f)); PlayerAt(new Vector3(0,0,2.85f)); Physics.SyncTransforms(); robot.Tick(.02f);
            Assert.That(root.Simulation.WatcherContacts,Is.EqualTo(contacts+1));
            Assert.That(Object.FindAnyObjectByType<PlayerMotor>().transform.position.z,Is.GreaterThan(2),"A hit must not teleport the player out of combat");
            PlayerAt(new Vector3(0,0,2.85f)); Physics.SyncTransforms(); robot.Tick(.02f);
            Assert.That(root.Simulation.WatcherContacts,Is.EqualTo(contacts+1),"Repeated contact must respect cooldown");
            Assert.That(robot.AnimationState,Is.EqualTo("Attack"));
            controls.SetMode(ControlMode.Paused); LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator TwoRobotsAnimateAndObeyTheirOwnControlProcesses()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var controls=Object.FindAnyObjectByType<ControlState>(); var world=Object.FindAnyObjectByType<FacilityRoot>().Simulation;
            var relay=Enemy("watcher-r17"); var core=Enemy("sentry-r18");
            Assert.That(Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None).Length,Is.GreaterThanOrEqualTo(2));
            foreach(var robot in new[] { relay,core })
            {
                var animator=robot.GetComponentInChildren<Animator>(); Assert.That(animator.runtimeAnimatorController,Is.Not.Null); Assert.That(animator.applyRootMotion,Is.False);
                Assert.That(robot.GetComponentsInChildren<SkinnedMeshRenderer>().Length,Is.GreaterThan(0));
                animator.speed=1; animator.Play("Walk",0,0); animator.Update(0);
                var bones=animator.GetComponentsInChildren<Transform>(); var rotations=bones.Select(b=>b.localRotation).ToArray(); var position=robot.transform.position;
                animator.Update(.2f);
                Assert.That(bones.Where((b,i)=>Quaternion.Angle(b.localRotation,rotations[i])>1).Any(),Is.True,"Imported walk must deform the rig");
                Assert.That(robot.transform.position,Is.EqualTo(position),"Animation must not displace the physical root");
            }
            Pose(relay,new Vector3(0,0,2.2f)); PlayerAt(new Vector3(-2.65f,.05f,-1.7f));
            controls.SetMode(ControlMode.Exploring); world.SetPower(DeviceId.Edge01,false); var before=relay.transform.position; relay.Tick(.5f); core.Tick(0);
            Assert.That(relay.State.Mode,Is.Not.EqualTo(WatcherMode.Offline),"Autonomous robot survives loss of network power");
            Assert.That(core.State.Mode,Is.Not.EqualTo(WatcherMode.Offline),"CORE has its own power/control");
            world.SetPower(DeviceId.Edge01,true); var shell=new TerminalSession(world,DeviceId.Ws07); shell.Execute("ps"); shell.Execute("systemctl disable cache-sync"); shell.Execute("systemctl stop cache-sync");
            relay.Tick(0); core.Tick(0); Assert.That(relay.State.Mode,Is.Not.EqualTo(WatcherMode.Maintenance),"Stopping the relay does not remove the autonomous enemy"); Assert.That(core.State.Mode,Is.Not.EqualTo(WatcherMode.Maintenance));
            new TerminalSession(world,DeviceId.Core01).Execute("kill 666"); core.Tick(0); Assert.That(core.State.Mode,Is.EqualTo(WatcherMode.Maintenance));
            Assert.That(core.AnimationState,Is.EqualTo("TurnOff")); controls.SetMode(ControlMode.Paused); before=core.transform.position; core.Tick(1);
            Assert.That(core.transform.position,Is.EqualTo(before)); Assert.That(core.GetComponentInChildren<Animator>().speed,Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator BothEnemyPosesRoundTripAndOldSavesWithoutSecondaryRemainValid()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var root=Object.FindAnyObjectByType<FacilityRoot>(); var data=root.Persistence.Capture();
            var sentryTarget=data.enemies.Single(e=>e.id=="sentry-r18");
            sentryTarget.position=new Vector3(.8f,0,10.8f); sentryTarget.bodyYaw=225; sentryTarget.direction=-1; sentryTarget.scan=3; sentryTarget.contactCooldown=1.5f;
            string path=Path.Combine(Path.GetTempPath(),"hostfall-enemy-"+System.Guid.NewGuid().ToString("N"));
            try
            {
                var store=new WorldSaveStore(path); store.Write(data); Assert.That(store.TryLoad(out var saved,out var error),Is.True,error);
                yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
                Enemy("watcher-r17").Restore(saved.watcher); Enemy("sentry-r18").Restore(saved.enemies.Single(e=>e.id=="sentry-r18"));
                var restored=Enemy("sentry-r18").Capture(); Assert.That(restored.position,Is.EqualTo(sentryTarget.position)); Assert.That(restored.bodyYaw,Is.EqualTo(225).Within(.001f));
                Assert.That(restored.direction,Is.EqualTo(-1)); Assert.That(restored.scan,Is.EqualTo(3)); Assert.That(restored.contactCooldown,Is.EqualTo(1.5f));
                data.enemies=null; store.Write(data); Assert.That(store.TryLoad(out saved,out error),Is.True,error);
                Assert.That(saved.enemies==null||saved.enemies.Length==0,Is.True);
                data.enemies=new[] { new RobotStateData { id="unknown" } }; Assert.Throws<System.ArgumentException>(()=>GameSaveData.Validate(data));
            }
            finally { if(Directory.Exists(path)) Directory.Delete(path,true); }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator AuthoredPatrolsCompleteBothDirectionsWithoutFurnitureBlockingThem()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var controls=Object.FindAnyObjectByType<ControlState>();
            PlayerAt(new Vector3(-2.65f,.05f,-1.7f));
            var relay=Enemy("watcher-r17"); var core=Enemy("sentry-r18");
            // This old saved pose sat just in front of EDGE's tabletop corner.
            Pose(relay,new Vector3(-.25f,0,4.92f)); Pose(core,new Vector3(.7f,0,10.2f));
            controls.SetMode(ControlMode.Exploring);
            Physics.SyncTransforms(); relay.Tick(.5f);
            Assert.That(relay.transform.position.x,Is.LessThan(-.38f),"Rejoin the clear lane before passing the desk corner");
            float relayMin=100,relayMax=-100,coreMin=100,coreMax=-100;
            for(int i=0;i<240;i++)
            {
                Physics.SyncTransforms(); relay.Tick(.1f); core.Tick(.1f);
                relayMin=Mathf.Min(relayMin,relay.transform.position.z); relayMax=Mathf.Max(relayMax,relay.transform.position.z);
                coreMin=Mathf.Min(coreMin,core.transform.position.z); coreMax=Mathf.Max(coreMax,core.transform.position.z);
            }
            Assert.That(relayMin,Is.LessThan(2.35f)); Assert.That(relayMax,Is.GreaterThan(5.45f));
            Assert.That(coreMin,Is.LessThan(9.25f)); Assert.That(coreMax,Is.GreaterThan(11.65f));
            controls.SetMode(ControlMode.Paused); LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator TargetLossTransitionsIntoSearchAtLastKnownPosition()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var controls=Object.FindAnyObjectByType<ControlState>(); var robot=Enemy("watcher-r17");
            Pose(robot,new Vector3(0,0,2.2f)); PlayerAt(new Vector3(0,0,4.3f)); Physics.SyncTransforms();
            controls.SetMode(ControlMode.Exploring);

            // Initial glance starts suspicion buildup
            robot.Tick(.1f);
            Assert.That(robot.State.Mode,Is.EqualTo(WatcherMode.Suspicious),"Brief glance at distance triggers suspicion");

            // Sustained gaze enters full alert
            for(int i=0;i<4;i++) { Physics.SyncTransforms(); robot.Tick(.1f); }
            Assert.That(robot.State.Mode,Is.EqualTo(WatcherMode.Alert).Or.EqualTo(WatcherMode.AttackWindup),"Sustained sight leads to alert or the closer attack windup");
            Assert.That(robot.LastKnownPosition.z,Is.EqualTo(4.3f).Within(.1f));

            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name="Search test wall";
            wall.transform.position=new Vector3(0,1,3.2f); wall.transform.localScale=new Vector3(2,2,.15f); Physics.SyncTransforms();
            robot.Tick(.1f);
            Assert.That(robot.State.Mode,Is.EqualTo(WatcherMode.Search),"Breaking sight must enter Search instead of instant Patrol");
            Assert.That(robot.SearchTimer,Is.GreaterThan(0));

            for(int i=0;i<45;i++) { Physics.SyncTransforms(); robot.Tick(.1f); }
            Assert.That(robot.State.Mode,Is.EqualTo(WatcherMode.Patrol),"Expired search returns to patrol");
            Object.Destroy(wall); controls.SetMode(ControlMode.Paused); LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator AttackWindupTelegraphsAndAllowsDodge()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var controls=Object.FindAnyObjectByType<ControlState>(); var root=Object.FindAnyObjectByType<FacilityRoot>();
            var robot=Enemy("watcher-r17");
            Pose(robot,new Vector3(0,0,2.2f)); PlayerAt(new Vector3(0,0,3.5f)); Physics.SyncTransforms();
            controls.SetMode(ControlMode.Exploring);
            int contactsBefore=root.Simulation.WatcherContacts;

            // Distance is 1.3m: within windup telegraph range (0.7m < dist <= 1.65m)
            robot.Tick(.05f);
            Assert.That(robot.State.Mode,Is.EqualTo(WatcherMode.AttackWindup),"Approaching target initiates telegraph windup");
            Assert.That(root.Simulation.WatcherContacts,Is.EqualTo(contactsBefore),"Windup phase does not cause premature contact");
            Assert.That(robot.WindupTimer,Is.GreaterThan(0));

            // Player dashes away to safe distance
            PlayerAt(new Vector3(0,0,5.2f)); Physics.SyncTransforms();
            robot.Tick(.45f);
            Assert.That(root.Simulation.WatcherContacts,Is.EqualTo(contactsBefore),"Successful dodge avoids contact");
            Assert.That(robot.State.Mode,Is.Not.EqualTo(WatcherMode.AttackWindup));

            controls.SetMode(ControlMode.Paused); LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator RobotArmourNeedsSustainedFireAndDeathIsFinal()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var robot = Enemy("watcher-r17");
            Assert.That(robot.Health, Is.EqualTo(260f));
            Assert.That(robot.IsDestroyed, Is.False);

            // First shot deals 55 damage
            robot.TakeDamage(55f);
            Assert.That(robot.Health, Is.EqualTo(205f));
            Assert.That(robot.IsDestroyed, Is.False);
            Assert.That(robot.State.Mode, Is.EqualTo(WatcherMode.Alert), "First shot alerts the damaged robot");

            // Five body hits, rather than the former two-shot kill.
            for(int i=0;i<4;i++)robot.TakeDamage(55f);
            Assert.That(robot.Health, Is.EqualTo(0f));
            Assert.That(robot.IsDestroyed, Is.True, "Second shot destroys the robot completely");
            Assert.That(robot.State.Mode, Is.EqualTo(WatcherMode.Offline));

            var collider = robot.GetComponent<CapsuleCollider>();
            Assert.That(collider.enabled, Is.False, "Destroyed robot collider is disabled");

            // Tick does not revive or move destroyed robot
            robot.Tick(0.5f);
            Assert.That(robot.IsDestroyed, Is.True);
            Assert.That(robot.State.Mode, Is.EqualTo(WatcherMode.Offline));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator EmpGrenadeStunsRobotsAndCrawlerSentry()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var robot = Enemy("watcher-r17");
            Assert.That(robot.IsStunned, Is.False);
            Assert.That(robot.Health, Is.EqualTo(260f));

            // Stun robot with EMP
            robot.ApplyEmpStun(7f);
            Assert.That(robot.IsStunned, Is.True);
            Assert.That(robot.State.Mode, Is.EqualTo(WatcherMode.Jammed));

            // Crawler sentry test
            var sentry = Object.FindAnyObjectByType<TrackedSentryController>();
            if (sentry != null)
            {
                Assert.That(sentry.IsStunned, Is.False);
                Assert.That(sentry.Health, Is.EqualTo(780f));
                Assert.That(sentry.IsDestroyed, Is.False);

                // Apply EMP Stun to Sentry
                sentry.ApplyEmpStun(7f);
                Assert.That(sentry.IsStunned, Is.True);

                // Apply damage
                sentry.TakeDamage(60f);
                Assert.That(sentry.Health, Is.EqualTo(772.8f).Within(.01f));
                Assert.That(sentry.IsDestroyed, Is.False);

                // Fatal damage
                var combat=sentry.GetComponent<RobotCombatController>();
                combat.Hit(140,RobotHitRegion.LeftCoolant,sentry.transform.position);
                combat.Hit(140,RobotHitRegion.RightCoolant,sentry.transform.position);
                combat.Hit(1000,RobotHitRegion.Core,sentry.transform.position);
                Assert.That(sentry.Health, Is.EqualTo(0f));
                Assert.That(sentry.IsDestroyed, Is.True);
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator AmmoPickupsReplenishPlayerWeapon()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var controls = Object.FindAnyObjectByType<ControlState>();
            controls.SetMode(ControlMode.Exploring);

            var player = Object.FindAnyObjectByType<PlayerMotor>();
            var weapon = player.GetComponent<PlayerWeapon>();
            Assert.That(weapon, Is.Not.Null);

            weapon.Equip();
            Assert.That(weapon.HasWeapon, Is.True);

            var ammoPickups = Object.FindObjectsByType<AmmoBoxPickup>(FindObjectsSortMode.None);
            Assert.That(ammoPickups.Length, Is.GreaterThan(0), "Facility must contain staged ammo pickups");

            var box = ammoPickups[0];
            Assert.That(box.IsPickedUp, Is.False);

            // Initial ammo: magazine 6, reserve 18 (full)
            // Fire 1 shot to create room in magazine
            Assert.That(weapon.TryFire(), Is.True);
            Assert.That(weapon.CurrentAmmo, Is.EqualTo(5));

            // Reload to pull from reserve
            Assert.That(weapon.StartReload(), Is.True);
            yield return new WaitForSeconds(1.5f);
            Assert.That(weapon.CurrentAmmo, Is.EqualTo(6));
            Assert.That(weapon.ReserveAmmo, Is.EqualTo(17)); // 18 - 1 = 17

            // Now pickup ammo box (+12 ammo, capped at max 18 reserve)
            box.Interact();
            Assert.That(box.IsPickedUp, Is.True);
            Assert.That(weapon.ReserveAmmo, Is.EqualTo(29));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator BulwarkSentryShieldsFrontalAttacksAndStuns()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var bulwark = Object.FindAnyObjectByType<BulwarkSentryController>();
            Assert.That(bulwark, Is.Not.Null, "Expected E08 Bulwark Sentry in Sector 07");

            Assert.That(bulwark.Health, Is.EqualTo(180f));
            Assert.That(bulwark.IsStunned, Is.False);
            Assert.That(bulwark.IsShieldActive, Is.True);
            Assert.That(bulwark.IsDestroyed, Is.False);

            // Position player directly in front of Bulwark facing it
            var player = Object.FindAnyObjectByType<PlayerMotor>();
            player.transform.position = bulwark.transform.position + bulwark.transform.forward * 3.5f;
            Physics.SyncTransforms();

            // Frontal attack with active shield: 100 damage is absorbed by 85% -> takes 15 damage
            bulwark.TakeDamage(100f);
            Assert.That(bulwark.Health, Is.EqualTo(165f));

            // Apply EMP stun: drops shield
            bulwark.ApplyEmpStun(6f);
            Assert.That(bulwark.IsStunned, Is.True);
            Assert.That(bulwark.IsShieldActive, Is.False);

            // While stunned, takes full damage
            bulwark.TakeDamage(165f);
            Assert.That(bulwark.Health, Is.EqualTo(0f));
            Assert.That(bulwark.IsDestroyed, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator JammerRobotEmitsRfAndStunsOrDestroys()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var jammer = Object.FindAnyObjectByType<JammerRobotController>();
            Assert.That(jammer, Is.Not.Null, "Expected E09 Jammer Robot in Sector 07");

            Assert.That(jammer.Health, Is.EqualTo(100f));
            Assert.That(jammer.IsStunned, Is.False);
            Assert.That(jammer.IsJammingActive, Is.True);

            jammer.ApplyEmpStun(6f);
            Assert.That(jammer.IsStunned, Is.True);
            Assert.That(jammer.IsJammingActive, Is.False);

            jammer.TakeDamage(100f);
            Assert.That(jammer.Health, Is.EqualTo(0f));
            Assert.That(jammer.IsDestroyed, Is.True);
            Assert.That(jammer.IsJammingActive, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator PulseSidearmFiringAndWeaponSwitching()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var controls = Object.FindAnyObjectByType<ControlState>();
            controls.SetMode(ControlMode.Exploring);

            var player = Object.FindAnyObjectByType<PlayerMotor>();
            var weapon = player.GetComponent<PlayerWeapon>();
            Assert.That(weapon, Is.Not.Null);

            weapon.Equip();
            Assert.That(weapon.ActiveWeapon, Is.EqualTo(ActiveWeaponType.Arc12Shotgun));
            Assert.That(weapon.CurrentAmmo, Is.EqualTo(6));

            // Unlock and equip CIRRUS Pulse Sidearm
            weapon.UnlockPulseSidearm();
            Assert.That(weapon.HasPulseSidearm, Is.True);
            Assert.That(weapon.ActiveWeapon, Is.EqualTo(ActiveWeaponType.CirrusPulseSidearm));
            Assert.That(weapon.CurrentAmmo, Is.EqualTo(15));
            Assert.That(weapon.ReserveAmmo, Is.EqualTo(45));

            // Switching has a short readiness interval.
            yield return new WaitForSeconds(.35f);
            // Fire 1 pulse shot
            Assert.That(weapon.TryFire(), Is.True);
            Assert.That(weapon.CurrentAmmo, Is.EqualTo(14));

            // Numeric slots cannot summon discarded guns; swapping means picking up the floor item.
            weapon.SwitchWeapon(ActiveWeaponType.Arc12Shotgun);
            Assert.That(weapon.ActiveWeapon,Is.EqualTo(ActiveWeaponType.CirrusPulseSidearm));
            Object.FindObjectsByType<DroppedWeapon>(FindObjectsSortMode.None).Single(d=>d.Kind==ActiveWeaponType.Arc12Shotgun).Interact();
            Assert.That(weapon.ActiveWeapon, Is.EqualTo(ActiveWeaponType.Arc12Shotgun));
            Assert.That(weapon.CurrentAmmo, Is.EqualTo(6));

            // Switch back to CIRRUS: verifies magazine preservation
            Object.FindObjectsByType<DroppedWeapon>(FindObjectsSortMode.None).Single(d=>d.Kind==ActiveWeaponType.CirrusPulseSidearm).Interact();
            Assert.That(weapon.ActiveWeapon, Is.EqualTo(ActiveWeaponType.CirrusPulseSidearm));
            Assert.That(weapon.CurrentAmmo, Is.EqualTo(14));

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator RailEyeTurretTracksTargetAndStuns()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var railEye = Object.FindAnyObjectByType<RailEyeTurretController>();
            Assert.That(railEye, Is.Not.Null, "Expected E12 Rail-Eye Turret in Sector 09");

            Assert.That(railEye.Health, Is.EqualTo(120f));
            Assert.That(railEye.IsStunned, Is.False);
            Assert.That(railEye.IsDestroyed, Is.False);

            railEye.ApplyEmpStun(6f);
            Assert.That(railEye.IsStunned, Is.True);

            railEye.TakeDamage(120f);
            Assert.That(railEye.Health, Is.EqualTo(0f));
            Assert.That(railEye.IsDestroyed, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator BastionSentrySuppressionBurstAndStomp()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var bastion = Object.FindAnyObjectByType<BastionSentryController>();
            Assert.That(bastion, Is.Not.Null, "Expected E15 Bastion-4 Assault Sentry in Sector 09");

            Assert.That(bastion.Health, Is.EqualTo(240f));
            Assert.That(bastion.IsStunned, Is.False);
            Assert.That(bastion.IsDestroyed, Is.False);

            var player = Object.FindAnyObjectByType<PlayerMotor>();
            player.transform.position = bastion.transform.position + bastion.transform.forward * 4f;
            Physics.SyncTransforms();

            // Normal damage from front
            bastion.TakeDamage(40f);
            Assert.That(bastion.Health, Is.EqualTo(200f));

            // Flank to rear: rear reactor takes 2.5x damage (40 * 2.5 = 100)
            player.transform.position = bastion.transform.position - bastion.transform.forward * 4f;
            Physics.SyncTransforms();
            bastion.TakeDamage(40f);
            Assert.That(bastion.Health, Is.EqualTo(100f));

            bastion.ApplyEmpStun(6f);
            Assert.That(bastion.IsStunned, Is.True);

            bastion.TakeDamage(100f);
            Assert.That(bastion.Health, Is.EqualTo(0f));
            Assert.That(bastion.IsDestroyed, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator AtlasBossPhaseTransitionsAndWeakpoints()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var atlas = Object.FindAnyObjectByType<AtlasBossController>();
            Assert.That(atlas, Is.Not.Null, "Expected BOSS ATLAS-0 in Sector 10");

            Assert.That(atlas.Health, Is.EqualTo(600f));
            Assert.That(atlas.Phase, Is.EqualTo(AtlasBossPhase.Phase1_OrbitalSweep));
            Assert.That(atlas.IsStaggered, Is.False);
            Assert.That(atlas.IsDestroyed, Is.False);

            // Deal 160 damage -> health 440 -> transition to Phase 2
            atlas.TakeDamage(160f);
            Assert.That(atlas.Health, Is.EqualTo(440f));
            Assert.That(atlas.Phase, Is.EqualTo(AtlasBossPhase.Phase2_CryoShockwave));

            // Deal 150 damage -> health 290 -> transition to Phase 3
            atlas.TakeDamage(150f);
            Assert.That(atlas.Health, Is.EqualTo(290f));
            Assert.That(atlas.Phase, Is.EqualTo(AtlasBossPhase.Phase3_CoreExposed));

            // EMP stun causes 40 damage
            atlas.ApplyEmpStun(6f);
            Assert.That(atlas.Health, Is.EqualTo(250f));

            // Final blow -> Phase 4 Staggered shutdown
            atlas.TakeDamage(250f);
            Assert.That(atlas.Health, Is.EqualTo(0f));
            Assert.That(atlas.Phase, Is.EqualTo(AtlasBossPhase.Phase4_StaggeredShutdown));
            Assert.That(atlas.IsStaggered, Is.True);
            Assert.That(atlas.IsDestroyed, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator TitanbreakerFiringAndRecoil()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var controls = Object.FindAnyObjectByType<ControlState>();
            controls.SetMode(ControlMode.Exploring);

            var player = Object.FindAnyObjectByType<PlayerMotor>();
            var weapon = player.GetComponent<PlayerWeapon>();
            Assert.That(weapon, Is.Not.Null);

            weapon.Equip();weapon.UnlockTitanbreaker();
            Assert.That(weapon.HasTitanbreaker, Is.True);
            Assert.That(weapon.ActiveWeapon, Is.EqualTo(ActiveWeaponType.TitanbreakerHeavyInductor));
            Assert.That(weapon.CurrentAmmo, Is.EqualTo(3));
            Assert.That(weapon.ReserveAmmo, Is.EqualTo(9));

            yield return new WaitForSeconds(.35f);
            // Fire 1 heavy inductor shot
            Assert.That(weapon.BeginCharge(), Is.True);
            yield return new WaitForSeconds(.85f);
            Assert.That(weapon.ReleaseCharge(), Is.True);
            Assert.That(weapon.CurrentAmmo, Is.EqualTo(2));

            // Switch to ARC-12 and verify
            weapon.SwitchWeapon(ActiveWeaponType.Arc12Shotgun);
            Assert.That(weapon.ActiveWeapon,Is.EqualTo(ActiveWeaponType.TitanbreakerHeavyInductor));
            Object.FindObjectsByType<DroppedWeapon>(FindObjectsSortMode.None).Single(d=>d.Kind==ActiveWeaponType.Arc12Shotgun).Interact();
            Assert.That(weapon.ActiveWeapon, Is.EqualTo(ActiveWeaponType.Arc12Shotgun));

            // Switch back to TITANBREAKER and verify state preserved
            Object.FindObjectsByType<DroppedWeapon>(FindObjectsSortMode.None).Single(d=>d.Kind==ActiveWeaponType.TitanbreakerHeavyInductor).Interact();
            Assert.That(weapon.ActiveWeapon, Is.EqualTo(ActiveWeaponType.TitanbreakerHeavyInductor));
            Assert.That(weapon.CurrentAmmo, Is.EqualTo(2));

            LogAssert.NoUnexpectedReceived();
        }
    }
}
