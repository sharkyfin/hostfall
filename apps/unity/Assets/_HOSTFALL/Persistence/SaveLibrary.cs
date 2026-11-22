using System;
using System.Collections.Generic;
using System.IO;
using Hostfall.Simulation;

namespace Hostfall.Persistence
{
    public sealed class SaveSlot
    {
        public string Id { get; }
        public string Title { get; }
        public bool Automatic => Id == "auto";
        public bool Available { get; internal set; }
        public bool Exists { get; internal set; }
        public DateTime SavedAt { get; internal set; }
        public string Objective { get; internal set; } = "";
        public string Language { get; internal set; } = "";
        public string Error { get; internal set; } = "";
        internal string File { get; }
        internal SaveSlot(string id,string title,string file) { Id=id; Title=title; File=file; }
    }
    public sealed class SaveLibrary
    {
        private readonly string directory;
        private readonly Func<GameSaveData,bool> validate;
        private readonly int version;
        private readonly SaveSlot[] slots = {
            new("auto","Автосохранение","world-v2.json"), new("quick","Быстрое сохранение","quick-v2.json"),
            new("manual1","Слот 1","manual-1-v2.json"), new("manual2","Слот 2","manual-2-v2.json"), new("manual3","Слот 3","manual-3-v2.json")
        };
        public SaveLibrary(string path,Func<GameSaveData,bool> domainValidation=null,bool modern=false)
        {
            directory=path; validate=domainValidation; version=modern?3:2;
            if(modern) slots=new[] { new SaveSlot("auto","Автосохранение","world-v3.json"),new SaveSlot("quick","Быстрое сохранение","quick-v3.json"),
                new SaveSlot("manual1","Слот 1","manual-1-v3.json"),new SaveSlot("manual2","Слот 2","manual-2-v3.json"),new SaveSlot("manual3","Слот 3","manual-3-v3.json") };
        }
        public IReadOnlyList<SaveSlot> Slots(bool inspectFiles=true)
        {
            if(!inspectFiles) return slots;
            foreach(var slot in slots)
            {
                var store=Store(slot.Id); slot.Exists=store.Exists;
                slot.Available=store.TryLoad(out var data,out string error,validate); slot.Error=error;
                if(slot.Available)
                {
                    slot.SavedAt=File.GetLastWriteTimeUtc(store.FilePath+(error.Length>0?".bak":"")).ToLocalTime();
                    slot.Objective=FacilitySimulation.Restore(data.simulation).Objective;
                    slot.Language=FieldSession.NormalizeLanguage(data.field.programmingLanguage)=="cpp"?"C++":"Python";
                }
            }
            return slots;
        }
        public WorldSaveStore Store(string id)
        {
            foreach(var slot in slots) if(slot.Id==id) return new WorldSaveStore(directory,slot.File,version);
            throw new ArgumentException("Unknown save slot");
        }
        public bool Read(string id,out GameSaveData data,out string error) => Store(id).TryLoad(out data,out error,validate);
        public void Write(string id,GameSaveData data)
        { if(validate!=null && !validate(data)) throw new ArgumentException("Invalid saved world"); Store(id).Write(data); }
    }
}
