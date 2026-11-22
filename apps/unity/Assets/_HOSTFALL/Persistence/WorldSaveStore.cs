using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Hostfall.Simulation;
using UnityEngine;

namespace Hostfall.Persistence
{
    [Serializable] public sealed class PlayerStateData
    {
        public Vector3 position, planarVelocity;
        public float yaw, pitch, targetPitch, verticalSpeed;
        public bool thirdPerson;
        public Vector3 dashDirection;
        public float dashRemaining;
        public float mobilityLoss;
        public bool airDashUsed;
        public bool hasWeapon;
    }
    [Serializable] public sealed class DoorStateData { public bool opened; public float offset; }
    [Serializable]
    public sealed class DoorStateEntryData
    {
        public string id;
        public bool opened;
        public float offset;
        public bool unlocked;
    }
    [Serializable]
    public sealed class DeviceMechanicStateData
    {
        public bool cartridgeCollected;
        public bool cartridgeInserted;
        public string insertedCartridgeId;
        public bool valveCooled;
        public bool cargoSwitched;
        public bool cargoSwitchTrackSet;
        public bool cargoSwitchInterlockArmed;
        public bool upsPowerRouted;
        public string[] collectedItems;
    }
    [Serializable] public sealed class PlayerHealthStateData { public bool present; public float current, invulnerability; }
    [Serializable] public sealed class RobotStateData
    {
        public string id;
        public Vector3 position, sensorEuler;
        public float scan, contactCooldown, bodyYaw;
        public int direction;
        public bool destroyed;
    }
    [Serializable] public sealed class GameSaveData
    {
        public int version = 2;
        public string worldId = "northline-04-v1";
        public FacilityStateData simulation;
        public FieldProgressData field;
        public PlayerStateData player;
        // Optional for saves written before chassis health was introduced.
        public PlayerHealthStateData health;
        public WeaponLoadoutData weapon;
        public DoorStateData door;
        public DoorStateEntryData[] doors;
        public DeviceMechanicStateData devices;
        public RobotStateData watcher;
        public RobotStateData[] enemies;
        public static void Validate(GameSaveData data)
        {
            SaveValidation.Require(data != null && (data.version == 2 && data.worldId == "northline-04-v1" || data.version == 3 && data.worldId == "northline-2"), "Unsupported save version or world");
            SaveValidation.Require(data.simulation != null && data.field != null && data.player != null && data.door != null && data.watcher != null, "Incomplete save");
            var campaign = FacilitySimulation.Restore(data.simulation).ComputerCampaign;
            SaveValidation.Require((data.version==3)==(data.simulation.routeVersion==3), "World and route version mismatch");
            WeaponLoadoutData.Validate(data.weapon);
            var p = data.player; var r = data.watcher;
            SaveValidation.Require(Finite(p.position) && Finite(p.planarVelocity) && Finite(p.yaw) && Finite(p.pitch) && Finite(p.targetPitch) && Finite(p.verticalSpeed), "Invalid player pose");
            bool inShuttle = campaign?.Transit.Passenger == true;
            if (inShuttle)
            {
                var pose = campaign.Transit.Pose;
                var offset = p.position - new Vector3(pose.X, pose.Y, pose.Z);
                SaveValidation.Require(Mathf.Abs(offset.y) < .33f && new Vector2(offset.x, offset.z).sqrMagnitude < 2.9f,
                    "Saved passenger outside service shuttle");
            }
            SaveValidation.Require(p.position.x > -35 && p.position.x < 95 && p.position.z > -25 && p.position.z < 210 && (inShuttle || p.position.y > -3.0f && p.position.y < 18.0f), "Saved player outside facility");
            SaveValidation.Require(Mathf.Abs(p.pitch) <= 82 && Mathf.Abs(p.targetPitch) <= 82 && p.planarVelocity.magnitude <= 12 && p.verticalSpeed >= -35 && p.verticalSpeed <= 12, "Invalid player motion");
            SaveValidation.Require(Finite(p.dashDirection)&&p.dashDirection.magnitude<=1.01f&&Finite(p.dashRemaining)&&p.dashRemaining>=0&&p.dashRemaining<=.181f,"Invalid saved dash");
            SaveValidation.Require(Finite(p.mobilityLoss) && p.mobilityLoss is >= 0 and <= 2.4f, "Invalid saved mobility penalty");
            SaveValidation.Require(data.health == null || !data.health.present || (Finite(data.health.current) && data.health.current >= 0 && data.health.current <= 100 && Finite(data.health.invulnerability) && data.health.invulnerability >= 0 && data.health.invulnerability <= 0.61f), "Invalid player health");
            SaveValidation.Require(Finite(data.door.offset) && data.door.offset >= 0 && data.door.offset <= 2.5f, "Invalid door state");
            SaveValidation.Require(data.doors == null || data.doors.Length <= 64, "Too many saved doors");
            if (data.doors != null)
            {
                foreach (var d in data.doors)
                {
                    SaveValidation.Require(d != null && !string.IsNullOrEmpty(d.id), "Invalid door entry");
                    SaveValidation.Require(Finite(d.offset) && d.offset >= 0 && d.offset <= 6.0f, "Invalid door entry offset");
                }
            }
            SaveValidation.Require(Finite(r.position) && Finite(r.sensorEuler) && Finite(r.scan) && Finite(r.contactCooldown) && (r.direction == -1 || r.direction == 1), "Invalid robot pose");
            SaveValidation.Require(r.position.x > -35 && r.position.x < 95 && r.position.z > -25 && r.position.z < 210 && r.position.y > -3 && r.position.y < 18 && r.scan >= 0 && r.contactCooldown >= 0 && r.contactCooldown <= 3.1f, "Invalid robot state");
            SaveValidation.Require(Finite(r.bodyYaw),"Invalid robot orientation");
            // Old saves contain only watcher. Extra enemies are optional and keyed by stable ID.
            SaveValidation.Require(data.enemies==null||data.enemies.Length<=32,"Unknown saved enemies");
            if(data.enemies!=null) foreach(var enemy in data.enemies)
            {
                SaveValidation.Require(enemy != null && KnownEnemy(enemy.id, data.version), "Unknown saved enemy");
                SaveValidation.Require(Finite(enemy.position)&&Finite(enemy.sensorEuler)&&Finite(enemy.bodyYaw)&&Finite(enemy.scan)&&Finite(enemy.contactCooldown),"Invalid enemy pose");
                SaveValidation.Require(enemy.position.y>=-3.0f&&enemy.position.y<18f&&enemy.scan>=0&&enemy.contactCooldown>=0&&enemy.contactCooldown<=3.1f&&(enemy.direction==1||enemy.direction==-1),"Invalid enemy state");
            }
        }

        private static bool KnownEnemy(string id, int version) => id switch
        {
            "vent-r19" or "network-response-r31" or "network-response-r32" or "aux-watch-energy-r36" or
                "aux-watch-logistics-0" or "aux-watch-logistics-1" or "aux-watch-logistics-2" => version == 3,
            "sentry-r18" or "sentinel-r21" or "reactor-sentry-r22" or "quantum-watcher-r23" or "patrol-drone-r24" or
                "logistics-sentry-r25" or "assembly-sentry-r26" or "bulwark-sentry-r27" or "security-jammer-r28" or
                "neuro-sentry-r29" or "trace-hound-r30" or "rail-eye-r31" or "bastion-r32" or "atlas-boss-0" or
                "aux-watch-01" or "aux-watch-02" or "aux-watch-03" or "aux-watch-04" => true,
            _ => false
        };
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
    }
    [Serializable] internal sealed class SaveEnvelope { public int version = 2; public string payload, sha256; }
    public sealed class WorldSaveStore
    {
        private const int MaxBytes = 4 * 1024 * 1024;
        public string FilePath { get; }
        public int Version { get; }
        public WorldSaveStore(string directory,string filename = "world-v2.json",int version=2)
        {
            if (Path.GetFileName(filename)!=filename || !filename.EndsWith(".json",StringComparison.Ordinal)) throw new ArgumentException("Invalid save filename");
            FilePath=Path.Combine(directory,filename);
            SaveValidation.Require(version==2||version==3,"Unsupported store version"); Version=version;
        }
        public bool HasValidData { get; private set; }
        public bool Exists => File.Exists(FilePath) || File.Exists(FilePath + ".bak");
        public bool TryLoad(out GameSaveData data, out string error, Func<GameSaveData, bool> validate = null)
        {
            data = null; error = ""; HasValidData = false;
            foreach (string path in new[] { FilePath, FilePath + ".bak" })
            {
                if (!File.Exists(path)) continue;
                try
                {
                    var candidate = Read(path); GameSaveData.Validate(candidate);
                    SaveValidation.Require(candidate.version==Version,"Save belongs to a different campaign");
                    if (validate != null && !validate(candidate)) throw new ArgumentException("Saved world failed validation");
                    HasValidData = true; data = candidate; error = path.EndsWith(".bak", StringComparison.Ordinal) ? "Восстановлена резервная копия" : ""; return true;
                }
                catch (Exception failure) when (failure is IOException || failure is ArgumentException || failure is UnauthorizedAccessException)
                { error = "Не удалось прочитать сохранение. Исходные файлы сохранены."; }
            }
            return false;
        }
        public void Write(GameSaveData data)
        {
            GameSaveData.Validate(data);
            SaveValidation.Require(data.version==Version,"Save belongs to a different campaign");
            string payload = JsonUtility.ToJson(data);
            var envelope = new SaveEnvelope { version=data.version, payload = payload, sha256 = Hash(payload) };
            string text = JsonUtility.ToJson(envelope);
            if (Encoding.UTF8.GetByteCount(text) > MaxBytes) throw new IOException("Save exceeds size limit");
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            string temporary = FilePath + ".tmp";
            WriteFlushed(temporary, text);
            if (File.Exists(FilePath))
            {
                // Never replace a healthy backup with a damaged primary file.
                try
                {
                    GameSaveData.Validate(Read(FilePath));
                    string backup = FilePath + ".bak", staged = backup + ".tmp";
                    File.Copy(FilePath, staged, true);
                    Replace(staged, backup);
                }
                catch (ArgumentException) { }
            }
            Replace(temporary, FilePath); HasValidData = true;
        }
        public void PreserveUnreadableFiles()
        {
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-ffffff");
            foreach (string source in new[] { FilePath, FilePath + ".bak" })
                if (File.Exists(source)) File.Copy(source, source + ".damaged-" + stamp, false);
        }
        private static GameSaveData Read(string path)
        {
            if (new FileInfo(path).Length > MaxBytes) throw new ArgumentException("Save exceeds size limit");
            var envelope = JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(path));
            if (envelope == null || (envelope.version != 2 && envelope.version != 3) || string.IsNullOrEmpty(envelope.payload) || !string.Equals(envelope.sha256, Hash(envelope.payload), StringComparison.Ordinal))
                throw new ArgumentException("Save checksum or schema mismatch");
            var data=JsonUtility.FromJson<GameSaveData>(envelope.payload) ?? throw new ArgumentException("Empty save payload");
            SaveValidation.Require(data.version==envelope.version,"Save envelope version mismatch");return data;
        }
        private static string Hash(string text)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "");
        }
        private static void WriteFlushed(string path, string text)
        {
            byte[] bytes = new UTF8Encoding(false).GetBytes(text);
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
        }
        private static void Replace(string staged, string target)
        { if (File.Exists(target)) File.Replace(staged, target, null); else File.Move(staged, target); }
    }
}
