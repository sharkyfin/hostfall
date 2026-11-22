using System;
using System.Collections.Generic;
using System.Linq;
namespace Hostfall.Simulation
{
    [Serializable] public sealed class WeaponItemData
    {
        public string id;
        public int kind,ammo,reserve;public bool overheated;
        public float x,y,z,pitch,yaw,roll,vx,vy,vz,ax,ay,az,heat,cooldown;
        public WeaponItemData Copy()=>(WeaponItemData)MemberwiseClone();
        public static void Validate(WeaponItemData d)
        {
            SaveValidation.Require(d!=null&&!string.IsNullOrEmpty(d.id)&&d.id.Length<512&&d.kind>=0&&d.kind<3,"Invalid dropped weapon identity");
            int capacity=d.kind==0?6:d.kind==1?15:3,max=d.kind==0?72:d.kind==1?120:24;
            SaveValidation.Require(d.ammo>=0&&d.ammo<=capacity&&d.reserve>=0&&d.reserve<=max,"Invalid dropped weapon ammo");
            foreach(float f in new[]{d.x,d.y,d.z,d.pitch,d.yaw,d.roll,d.vx,d.vy,d.vz,d.ax,d.ay,d.az,d.heat,d.cooldown})SaveValidation.Require(!float.IsNaN(f)&&!float.IsInfinity(f),"Invalid dropped weapon pose");
            SaveValidation.Require(d.x> -35&&d.x<95&&d.z> -25&&d.z<210&&d.y> -3&&d.y<18&&d.heat>=0&&d.heat<=1&&d.cooldown>=0&&d.cooldown<=2&&Math.Abs(d.vx)<=50&&Math.Abs(d.vy)<=50&&Math.Abs(d.vz)<=50,"Dropped weapon outside world");
        }
    }
    [Serializable] public sealed class WeaponWorldData {public int nextId=1;public WeaponItemData[] drops;public string[] collected;}
    public sealed class WeaponWorldState
    {
        private readonly Dictionary<string,WeaponItemData> drops=new(StringComparer.Ordinal);
        private readonly HashSet<string> collected=new(StringComparer.Ordinal);
        private int nextId=1;
        public event Action Changed;
        public IEnumerable<WeaponItemData> Drops=>drops.Values;
        public bool WasCollected(string id)=>collected.Contains(id);
        public bool CollectAuthored(string id){if(!collected.Add(id))return false;Changed?.Invoke();return true;}
        public WeaponItemData Add(WeaponItemData value)
        {
            SaveValidation.Require(drops.Count<128,"Too many dropped weapons");
            var item=value.Copy();item.id="dropped.weapon."+nextId++;WeaponItemData.Validate(item);drops.Add(item.id,item);Changed?.Invoke();return item;
        }
        public bool Take(string id,out WeaponItemData value)
        {if(!drops.TryGetValue(id,out value))return false;drops.Remove(id);Changed?.Invoke();return true;}
        public WeaponWorldData Capture()=>new(){nextId=nextId,drops=drops.Values.Select(d=>d.Copy()).ToArray(),collected=collected.ToArray()};
        public void Restore(WeaponWorldData data)
        {
            drops.Clear();collected.Clear();nextId=1;
            // Unity can materialize an absent optional JSON object with zeroed fields.
            // This is the pre-drop-save format, not a malformed current weapon roster.
            if(data==null||((data.nextId==0||data.nextId==1)&&data.drops==null&&data.collected==null))return;
            SaveValidation.Require(data.nextId>0&&data.nextId<int.MaxValue&&data.drops?.Length<=128&&data.collected?.Length<=256,"Invalid weapon world");
            foreach(var d in data.drops){WeaponItemData.Validate(d);SaveValidation.Require(d.id.StartsWith("dropped.weapon.",StringComparison.Ordinal)&&int.TryParse(d.id.Substring(15),out int n)&&n>0&&n<data.nextId,"Invalid dropped weapon counter");SaveValidation.Require(drops.TryAdd(d.id,d.Copy()),"Duplicate dropped weapon");}
            foreach(string id in data.collected)SaveValidation.Require(!string.IsNullOrEmpty(id)&&id.Length<512&&collected.Add(id),"Invalid collected weapon");nextId=data.nextId;
        }
    }
}
