using System;
using System.IO;
using Hostfall.Simulation;
using UnityEngine;

namespace Hostfall.Field
{
    // Archive memory only. This is deliberately not a world/quest save slot.
    public sealed class ArchiveProgressStore : IDisposable
    {
        private readonly FieldSession session;
        private readonly string path;
        public ArchiveProgressStore(FieldSession field)
        {
            session = field; path = Path.Combine(Application.persistentDataPath, "archive-v1.json");
            if (File.Exists(path))
            {
                try
                {
                    if (new FileInfo(path).Length > 65536) throw new InvalidDataException("Archive file too large");
                    session.Restore(JsonUtility.FromJson<ArchiveSnapshot>(File.ReadAllText(path)));
                }
                catch (Exception error) when (error is IOException || error is ArgumentException || error is UnauthorizedAccessException)
                { Debug.LogWarning("HOSTFALL: archive memory was not loaded: " + error.Message); }
            }
            session.Changed += Save;
        }
        private void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temporary = path + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(session.Capture()));
                if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException) { Debug.LogWarning("HOSTFALL: archive memory was not saved: " + error.Message); }
        }
        public void Dispose() { session.Changed -= Save; }
    }
}
