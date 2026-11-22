using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using Hostfall.Persistence;
using Hostfall.Simulation;
using Hostfall.Simulation.Computers;
using NUnit.Framework;
using UnityEngine;

namespace Hostfall.Tests
{
    public sealed class WorldSaveTests
    {
        private string directory;
        private WorldSaveStore store;
        [SetUp] public void SetUp() { directory = Path.Combine(Path.GetTempPath(), "hostfall-save-test-" + Guid.NewGuid().ToString("N")); store = new WorldSaveStore(directory); }
        [TearDown] public void TearDown() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        [Test] public void NetworkJsonResumeKeepsTheSameProcessesAndDefensiveOutcome()
        {
            var a=new ComputerCampaign();a.BeginSnake();ComputerCampaignTests.Complete(a.Snake);a.BeginNetwork();NetworkDefenseTests.Configure(a.Network);
            for(int i=0;i<180;i++)a.Network.Advance(NetworkDefenseSession.StepSeconds);
            a.Network.Advance(.011234567890123);
            var b=ComputerCampaign.Restore(JsonUtility.FromJson<ComputerCampaignData>(JsonUtility.ToJson(a.Capture())));
            for(int i=0;i<5000&&a.Network.Phase!=NetworkPhase.Complete;i++)
            {
                if(a.Network.Phase is NetworkPhase.Ready or NetworkPhase.Failed){a.BeginNetwork();b.BeginNetwork();}
                int target=a.Network.Infection(5)>=a.Network.Infection(6)?5:6;
                if(a.Network.Infection(target)>30||a.Network.Processes.Count(p=>p.node==target)>=2)
                    Assert.That(b.Network.Purge(target),Is.EqualTo(a.Network.Purge(target)));
                a.Network.Advance(.017);b.Network.Advance(.017);a.AdvanceWorld(.017);b.AdvanceWorld(.017);
                Assert.That(b.Network.Stage,Is.EqualTo(a.Network.Stage));Assert.That(b.Network.Phase,Is.EqualTo(a.Network.Phase));
                Assert.That(b.Network.Integrity,Is.EqualTo(a.Network.Integrity).Within(1e-10));Assert.That(b.Network.Credits,Is.EqualTo(a.Network.Credits).Within(1e-10));
                Assert.That(b.Network.Processes.Select(p=>p.id),Is.EqualTo(a.Network.Processes.Select(p=>p.id)));
                Assert.That(b.Network.Processes.Select(p=>p.node),Is.EqualTo(a.Network.Processes.Select(p=>p.node)));
                for(int j=0;j<a.Network.Processes.Count;j++)Assert.That(b.Network.Processes[j].health,Is.EqualTo(a.Network.Processes[j].health).Within(1e-10));
                for(int j=0;j<8;j++)Assert.That(b.Network.Infection(j),Is.EqualTo(a.Network.Infection(j)).Within(1e-10));
            }
            Assert.That(a.Network.Phase,Is.EqualTo(NetworkPhase.Complete));Assert.That(b.NetworkSecurity.Released,Is.EqualTo(a.NetworkSecurity.Released));
        }
        private static GameSaveData Snapshot()
        {
            var world = new FacilitySimulation(MissionDefinition.NightShift); var terminal = new TerminalSession(world, DeviceId.Ws07);
            var catalog = new FieldCatalog(JsonUtility.FromJson<FieldContentData>(Resources.Load<TextAsset>("FieldContent").text));
            using var field = new FieldSession(catalog, world);
            world.Exploration.Visit("room.soc");
            terminal.Execute("ps"); terminal.Execute("systemctl disable cache-sync"); terminal.Execute("systemctl stop cache-sync");
            field.ReadDocument("shift-note"); field.Study("ip"); field.Study("server"); field.Study("http"); field.RequestHint("q.witness"); field.SelectedArticle = "http";
            world.SetPower(DeviceId.Edge01, false); world[DeviceId.Core01].WriteFile("/tmp/personal.txt", "проверка");
            return new GameSaveData
            {
                simulation = world.Capture(), field = field.CaptureProgress(),
                player = new PlayerStateData { position = new Vector3(.3f,.05f,5), yaw = 210, pitch = 25, targetPitch = 25, thirdPerson = true },
                door = new DoorStateData { opened = true, offset = 1.25f },
                watcher = new RobotStateData { position = new Vector3(-.25f,0,4), direction = -1, scan = 5, contactCooldown = 1 }
            };
        }
        private static bool Validate(GameSaveData data)
        {
            var model = FacilitySimulation.Restore(data.simulation);
            using var field = new FieldSession(new FieldCatalog(JsonUtility.FromJson<FieldContentData>(Resources.Load<TextAsset>("FieldContent").text)),model);
            field.RestoreProgress(data.field); return true;
        }
        [Test] public void ComputerWorldV3UsesSeparateSlotsAndRestoresExactPartialApplication()
        {
            var legacy=Snapshot();store.Write(legacy);byte[] legacyBytes=File.ReadAllBytes(store.FilePath);
            var data=Snapshot();var world=new FacilitySimulation(MissionDefinition.ComputerWorld);
            world.ComputerCampaign.BeginSnake();world.ComputerCampaign.Snake.Turn(Hostfall.Simulation.Computers.SnakeDirection.Up);
            world.ComputerCampaign.Snake.Advance(.18320874646306035);world.ComputerCampaign.AdvanceWorld(.1998754031956196);world.ComputerCampaign.Shell.SetDraft("cat ~/archive/");
            data.version=3;data.worldId="northline-2";data.simulation=world.Capture();
            var modern=new SaveLibrary(directory,null,true);modern.Write("manual1",data);modern.Write("auto",data);
            Assert.That(File.ReadAllBytes(store.FilePath),Is.EqualTo(legacyBytes));
            Assert.That(File.Exists(Path.Combine(directory,"manual-1-v3.json")),Is.True);
            Assert.That(File.Exists(Path.Combine(directory,"manual-1-v2.json")),Is.False);
            Assert.That(modern.Read("auto",out var restored,out string error),Is.True,error);
            var restoredApp=restored.simulation.computerCampaign;var originalApp=data.simulation.computerCampaign;
            Assert.That(restoredApp.snake.elapsed,Is.EqualTo(originalApp.snake.elapsed).Within(1e-12));
            Assert.That(restoredApp.threat.elapsed,Is.EqualTo(originalApp.threat.elapsed).Within(1e-12));
            // Unity's JSON parser can move a binary64 value by one ULP. All non-clock state is exact.
            restoredApp.snake.elapsed=originalApp.snake.elapsed;restoredApp.threat.elapsed=originalApp.threat.elapsed;
            Assert.That(JsonUtility.ToJson(restoredApp),Is.EqualTo(JsonUtility.ToJson(originalApp)));
            var actual=FacilitySimulation.Restore(restored.simulation);
            Assert.That(actual.InteractiveComputers,Is.True);Assert.That(actual.AutomatedRoute,Is.False);
            Assert.That(actual.ComputerCampaign.Snake.PartialStep,Is.EqualTo(.18320874646306035).Within(1e-12));
            Assert.That(actual.ComputerCampaign.Home.Terminal.Draft,Is.EqualTo("cat ~/archive/"));
            Assert.That(actual.ComputerCampaign.Home.Terminal.Host,Is.EqualTo(DeviceId.Home01));
            Assert.Throws<ArgumentException>(()=>store.Write(data));
            Assert.Throws<ArgumentException>(()=>modern.Write("quick",legacy));
            Assert.That(modern.Slots().Single(s=>s.Id=="auto").Objective,Is.EqualTo(actual.Objective));
        }
        [Test] public void ExpandedCampaignSlotsCanBeListedAndLoaded()
        {
            var data=Snapshot();var campaign=new FacilitySimulation(MissionDefinition.FacilityCampaign);
            foreach(var id in new[]{DeviceId.Ws07,DeviceId.Edge01,DeviceId.Core01}){campaign.Terminals.Begin(id);for(int i=0;i<5000&&!campaign.Terminals.IsComplete(id);i++)campaign.Terminals.Tick(id,.1f);}
            campaign.RecordTerminalObjective(DeviceId.Data01);data.simulation=campaign.Capture();
            var library=new SaveLibrary(directory);library.Write("auto",data);
            Assert.DoesNotThrow(()=>library.Slots());Assert.That(library.Read("auto",out var saved,out var error),Is.True,error);Assert.That(saved.simulation.completedObjectives,Is.EqualTo(4));
        }
        [Test] public void RoundTripRestoresTheEntireWorldAndFieldProgress()
        {
            var original = Snapshot(); store.Write(original);
            Assert.That(store.TryLoad(out var saved,out string error,Validate), Is.True, error);
            Assert.That(JsonUtility.ToJson(saved), Is.EqualTo(JsonUtility.ToJson(original)));
            var world = FacilitySimulation.Restore(saved.simulation);
            using var field = new FieldSession(new FieldCatalog(JsonUtility.FromJson<FieldContentData>(Resources.Load<TextAsset>("FieldContent").text)),world);
            field.RestoreProgress(saved.field);
            Assert.That(field.FoundDocuments, Does.Contain("shift-note")); Assert.That(field.HintLevel("q.witness"), Is.EqualTo(1));
            Assert.That(field.State("http"), Is.EqualTo(KnowledgeState.Studied)); Assert.That(field.SelectedArticle, Is.EqualTo("http"));
            Assert.That(world.SourceIsolated, Is.True); Assert.That(world[DeviceId.Edge01].Powered, Is.False);
            Assert.That(saved.player.thirdPerson, Is.True); Assert.That(saved.door.offset, Is.EqualTo(1.25f));
        }
        [Test] public void WatcherCanBeSavedAfterChasingPlayerIntoOperatorRoom()
        {
            var data = Snapshot();
            data.watcher.position = new Vector3(-2.5f, 0, -1.5f);
            store.Write(data);
            Assert.That(store.TryLoad(out var loaded, out var error, Validate), Is.True, error);
            Assert.That(loaded.watcher.position, Is.EqualTo(data.watcher.position));
        }
        [Test] public void PlayerCanBeSavedAtEvacuationOverlook()
        {
            var data = Snapshot();
            data.player.position = new Vector3(10f, 0.1f, 199f);
            store.Write(data);
            Assert.That(store.TryLoad(out var loaded, out var error, Validate), Is.True, error);
            Assert.That(loaded.player.position, Is.EqualTo(data.player.position));
        }
        [Test] public void OlderSaveWithoutHealthKeepsOptionalFieldMissing()
        {
            var legacy = JsonUtility.FromJson<GameSaveData>("{\"version\":2}");
            Assert.That(legacy.health == null || !legacy.health.present, Is.True);
        }
        [Test] public void DamagedPrimaryRecoversTheLastGoodBackupWithoutOverwritingIt()
        {
            var original = Snapshot(); store.Write(original); original.player.yaw = 80; store.Write(original);
            File.WriteAllText(store.FilePath,"damaged");
            Assert.That(store.TryLoad(out var recovered,out string status,Validate), Is.True);
            Assert.That(recovered.player.yaw, Is.EqualTo(210)); Assert.That(status, Does.Contain("резервная"));
            recovered.player.yaw = 45; store.Write(recovered);
            File.WriteAllText(store.FilePath,"damaged again");
            Assert.That(store.TryLoad(out recovered,out _,Validate), Is.True); Assert.That(recovered.player.yaw, Is.EqualTo(210));
        }
        [Test] public void SaveSlotsRemainIndependentAndTheOldAutosaveStillLoads()
        {
            var library=new SaveLibrary(directory,Validate); var data=Snapshot();
            library.Write("auto",data); data.player.yaw=45; library.Write("manual1",data);
            data.player.yaw=90; library.Write("manual2",data); data.player.yaw=180; library.Write("quick",data);
            Assert.That(library.Slots().Count,Is.EqualTo(5));
            Assert.That(library.Read("manual1",out var first,out _),Is.True); Assert.That(first.player.yaw,Is.EqualTo(45));
            Assert.That(library.Read("manual2",out var second,out _),Is.True); Assert.That(second.player.yaw,Is.EqualTo(90));
            Assert.That(library.Read("quick",out var quick,out _),Is.True); Assert.That(quick.player.yaw,Is.EqualTo(180));
            Assert.That(store.TryLoad(out var old,out _,Validate),Is.True); Assert.That(old.player.yaw,Is.EqualTo(210));
            Assert.That(library.Read("manual3",out _,out _),Is.False);
            Assert.Throws<ArgumentException>(()=>library.Store("../escape"));
        }
        [Serializable] private sealed class LegacyEnvelope { public int version=2; public string payload,sha256; }
        [Test] public void PreviousWorldJsonWithoutTerminalFieldsStillLoads()
        {
            var original=Snapshot();
            foreach(var computer in original.simulation.computers) computer.terminal=null;
            string payload=Regex.Replace(JsonUtility.ToJson(original),"\"terminal\":\\{[^{}]*\\},","");
            Assert.That(payload,Does.Not.Contain("\"terminal\":"));
            var parsed=JsonUtility.FromJson<GameSaveData>(payload);
            Assert.DoesNotThrow(()=>FacilitySimulation.Restore(parsed.simulation));
            using var sha=SHA256.Create();
            string hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-","");
            Directory.CreateDirectory(directory); File.WriteAllText(store.FilePath,JsonUtility.ToJson(new LegacyEnvelope { payload=payload,sha256=hash }));
            Assert.That(store.TryLoad(out var restored,out string error,Validate),Is.True,error);
            Assert.That(restored.player.yaw,Is.EqualTo(original.player.yaw));
            var world=FacilitySimulation.Restore(restored.simulation);
            Assert.That(world[DeviceId.Ws07].Terminal.Lines,Is.Not.Empty,"The last display from the old format must be migrated");
        }
        [Test] public void InvalidPoseVersionChecksumAndDomainStateAreRejected()
        {
            var data = Snapshot(); data.player.position.x = float.NaN;
            Assert.Throws<ArgumentException>(() => store.Write(data));
            data = Snapshot(); data.version = 999; Assert.Throws<ArgumentException>(() => store.Write(data));
            data = Snapshot(); store.Write(data); string text = File.ReadAllText(store.FilePath); File.WriteAllText(store.FilePath,text.Replace("northline-04-v1","northline-04-v9"));
            Assert.That(store.TryLoad(out _,out _,Validate), Is.False);
            data = Snapshot(); data.field.hints[0].id = "unknown"; store.Write(data);
            Assert.That(store.TryLoad(out _,out _,Validate), Is.False);
        }
        [Test] public void MultiDoorAndDeviceMechanicStateSurvivesRoundTrip()
        {
            var data = Snapshot();
            data.doors = new[]
            {
                new DoorStateEntryData { id = "door.sec02.vault", opened = true, offset = 2.6f, unlocked = true },
                new DoorStateEntryData { id = "door.sec02.armory", opened = false, offset = 0f, unlocked = true },
                new DoorStateEntryData { id = "door.sec07.citadel", opened = true, offset = 3.0f, unlocked = true }
            };
            data.devices = new DeviceMechanicStateData
            {
                cartridgeCollected = true,
                cartridgeInserted = true,
                insertedCartridgeId = "NL-CART-044",
                valveCooled = true,
                cargoSwitched = true,
                cargoSwitchTrackSet = true,
                cargoSwitchInterlockArmed = true,
                upsPowerRouted = true,
                collectedItems = new[] { "item.usb.hf_tools", "item.cartridge.nl044", "item.key.rbt_diag" }
            };
            store.Write(data);
            Assert.That(store.TryLoad(out var loaded, out string error, Validate), Is.True, error);
            Assert.That(loaded.doors.Length, Is.EqualTo(3));
            Assert.That(loaded.doors[0].id, Is.EqualTo("door.sec02.vault"));
            Assert.That(loaded.doors[0].unlocked, Is.True);
            Assert.That(loaded.devices.cartridgeCollected, Is.True);
            Assert.That(loaded.devices.collectedItems, Does.Contain("item.usb.hf_tools"));
        }
    }
}
