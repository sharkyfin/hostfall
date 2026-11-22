using System;
using System.Collections.Generic;
using System.Linq;

namespace Hostfall.Simulation
{
    [Serializable] public sealed class ExplorationData
    {
        public int version=1;
        public string[] rooms;
    }
    public sealed class ExplorationState
    {
        private readonly HashSet<string> rooms=new(StringComparer.Ordinal);
        public event Action Changed;
        public int Revision { get; private set; }
        public bool HasVisited(string id)=>id!=null && rooms.Contains(id);
        public bool Visit(string id)
        {
            ValidateId(id);
            if(!rooms.Add(id))return false;
            Revision++;Changed?.Invoke();return true;
        }
        public ExplorationData Capture()=>new(){rooms=rooms.OrderBy(id=>id,StringComparer.Ordinal).ToArray()};
        public void Restore(ExplorationData data)
        {
            if(data!=null && data.version==0 && data.rooms==null)data=null;
            if(data!=null)
            {
                SaveValidation.Require(data.version==1 && (data.rooms==null||data.rooms.Length<=512),"Invalid exploration data");
                foreach(var id in data.rooms??Array.Empty<string>())ValidateId(id);
            }
            rooms.Clear();
            foreach(var id in data?.rooms??Array.Empty<string>())rooms.Add(id);
            Revision++;
        }
        private static void ValidateId(string id)=>SaveValidation.Require(!string.IsNullOrWhiteSpace(id)&&id.Length<=512,"Invalid explored room ID");
    }
}
