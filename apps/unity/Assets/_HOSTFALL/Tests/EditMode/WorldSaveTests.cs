using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using Hostfall.Persistence;
using Hostfall.Simulation;
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
        private static GameSaveData Snapshot()
        {
            var world = new FacilitySimulation(MissionDefinition.NightShift); var terminal = new TerminalSession(world, DeviceId.Ws07);
            var catalog = new FieldCatalog(JsonUtility.FromJson<FieldContentData>(Resources.Load<TextAsset>("FieldContent").text));
            using var field = new FieldSession(catalog, world);
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
    }
}
