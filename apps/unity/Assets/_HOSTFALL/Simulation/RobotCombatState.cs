using System;
using System.Collections.Generic;
using System.Linq;

namespace Hostfall.Simulation
{
    public enum RobotHitRegion { Hull, Sensor, LeftDrive, RightDrive, LeftCoolant, RightCoolant, Core }
    public enum RobotImpact { Standard, Pulse, Scatter, Coil, Melee }
    [Serializable] public sealed class RobotCombatData
    {
        public string id;
        public bool boss,dead,hasPose,networkMemory,squadMemory,rangedWindup;
        public float maximum,health,sensor=100,leftDrive=100,rightDrive=100,leftCoolant=100,rightCoolant=100;
        public int brokenMask,mode;
        public float x,y,z,yaw,memoryX,memoryZ,memoryTime,stun,attackCooldown,windup;
        public CrawlerAttackData crawlerAttack;
    }
    public sealed class RobotCombatState
    {
        private RobotCombatData data;
        public event Action Changed;
        public string Id=>data.id;
        public bool IsBoss=>data.boss;
        public bool IsDead=>data.dead;
        public float Health=>data.health;
        public float Maximum=>data.maximum;
        public int BrokenMask=>data.brokenMask;
        public bool CoreExposed => !IsBoss || data.leftCoolant <= 0 && data.rightCoolant <= 0 ||
            data.crawlerAttack?.version == CrawlerAttack.SaveVersion && data.health <= data.maximum * .4f &&
            (data.leftCoolant <= 0 || data.rightCoolant <= 0);
        public float Mobility=>((data.leftDrive<=0?1:0)+(data.rightDrive<=0?1:0)) switch {2=>.38f,1=>.68f,_=>1};
        public RobotCombatData Data=>data;
        public RobotCombatState(string id,bool boss,float maximum)
        { data=new RobotCombatData{id=id,boss=boss,maximum=maximum,health=maximum}; }
        public float ApplyHit(float raw,RobotHitRegion region,RobotImpact impact=RobotImpact.Standard,float charge=0)
        {
            if(IsDead||!Finite(raw)||raw<=0)return 0;
            float multiplier=region==RobotHitRegion.Sensor?2.2f:region==RobotHitRegion.Core?(CoreExposed?2.5f:.10f):region==RobotHitRegion.Hull?(IsBoss?.12f:1):region==RobotHitRegion.LeftDrive||region==RobotHitRegion.RightDrive?.65f:1;
            if (impact == RobotImpact.Pulse && (region is RobotHitRegion.Sensor or RobotHitRegion.LeftCoolant or RobotHitRegion.RightCoolant ||
                    region == RobotHitRegion.Core && CoreExposed))
                raw *= 1.35f;
            if (impact == RobotImpact.Coil && IsBoss && region == RobotHitRegion.Hull && Finite(charge))
                multiplier += .39f * Math.Clamp(charge, 0, 1);
            float partDamage=raw*(IsBoss?.75f:1);
            switch(region)
            {
                case RobotHitRegion.Sensor:data.sensor=Math.Max(0,data.sensor-partDamage);break;
                case RobotHitRegion.LeftDrive:data.leftDrive=Math.Max(0,data.leftDrive-partDamage);break;
                case RobotHitRegion.RightDrive:data.rightDrive=Math.Max(0,data.rightDrive-partDamage);break;
                case RobotHitRegion.LeftCoolant:data.leftCoolant=Math.Max(0,data.leftCoolant-partDamage);break;
                case RobotHitRegion.RightCoolant:data.rightCoolant=Math.Max(0,data.rightCoolant-partDamage);break;
            }
            if(data.sensor<=0)data.brokenMask|=1;
            if(data.leftDrive<=0)data.brokenMask|=2;if(data.rightDrive<=0)data.brokenMask|=4;
            if(data.leftCoolant<=0)data.brokenMask|=8;if(data.rightCoolant<=0)data.brokenMask|=16;
            float before=data.health;
            // Thick chassis cannot be brute-forced to death while the reactor remains sealed.
            data.health=Math.Max(IsBoss&&!CoreExposed?1:0,data.health-raw*multiplier);
            if(data.health<Maximum*.70f)data.brokenMask|=32;if(data.health<Maximum*.40f)data.brokenMask|=64;
            data.dead=data.health<=0;Changed?.Invoke();return before-data.health;
        }
        public void Defeat(){if(IsDead)return;data.health=0;data.dead=true;Changed?.Invoke();}
        public RobotCombatData Capture()=>Clone(data);
        public void Restore(RobotCombatData saved){Validate(saved);data=Clone(saved);}
        private static RobotCombatData Clone(RobotCombatData d) => new()
        {
            id = d.id, boss = d.boss, dead = d.dead, hasPose = d.hasPose, networkMemory = d.networkMemory,
            squadMemory = d.squadMemory, rangedWindup = d.rangedWindup,
            maximum = d.maximum, health = d.health, sensor = d.sensor,
            leftDrive = d.leftDrive, rightDrive = d.rightDrive, leftCoolant = d.leftCoolant, rightCoolant = d.rightCoolant,
            brokenMask = d.brokenMask, mode = d.mode, windup = d.windup, x = d.x, y = d.y, z = d.z, yaw = d.yaw,
            memoryX = d.memoryX, memoryZ = d.memoryZ, memoryTime = d.memoryTime, stun = d.stun,
            attackCooldown = d.attackCooldown,
            crawlerAttack = d.crawlerAttack?.version == CrawlerAttack.SaveVersion ? d.crawlerAttack.Clone() : null
        };
        public static void Validate(RobotCombatData d)
        {
            SaveValidation.Require(d!=null&&!string.IsNullOrEmpty(d.id)&&d.id.Length<=80,"Invalid combat identity");
            SaveValidation.Require(new[]{d.maximum,d.health,d.sensor,d.leftDrive,d.rightDrive,d.leftCoolant,d.rightCoolant,d.x,d.y,d.z,d.yaw,d.memoryX,d.memoryZ,d.memoryTime,d.stun,d.attackCooldown,d.windup}.All(Finite),"Invalid combat number");
            SaveValidation.Require(d.maximum>=1&&d.maximum<=4000&&d.health>=0&&d.health<=d.maximum&&d.dead==(d.health<=0),"Invalid robot health");
            SaveValidation.Require(new[]{d.sensor,d.leftDrive,d.rightDrive,d.leftCoolant,d.rightCoolant}.All(v=>v>=0&&v<=100)&&d.brokenMask>=0&&d.brokenMask<=127,"Invalid robot modules");
            SaveValidation.Require(d.x>-35&&d.x<95&&d.z>-25&&d.z<210&&d.y>-3&&d.y<18&&d.memoryTime>=0&&d.memoryTime<=20&&d.stun>=0&&d.stun<=20&&d.attackCooldown>=0&&d.attackCooldown<=10&&d.windup>=0&&d.windup<=3&&d.mode>=0&&d.mode<=7,"Invalid robot pose/timers");
            CrawlerAttack.Validate(d.crawlerAttack);
            SaveValidation.Require(d.crawlerAttack?.version != CrawlerAttack.SaveVersion || d.boss, "Crawler state belongs to a boss");
        }
        private static bool Finite(float f)=>!float.IsNaN(f)&&!float.IsInfinity(f);
    }
    public sealed class CombatRoster
    {
        private readonly Dictionary<string,RobotCombatState> states=new(StringComparer.Ordinal);
        public event Action Changed;
        public RobotCombatState Get(string id,bool boss=false,float maximum=260)
        {
            if(states.TryGetValue(id,out var state))return state;
            state=new RobotCombatState(id,boss,maximum);state.Changed+=()=>Changed?.Invoke();states.Add(id,state);return state;
        }
        public RobotCombatData[] Capture()=>states.Values.OrderBy(s=>s.Id,StringComparer.Ordinal).Select(s=>s.Capture()).ToArray();
        public void Restore(RobotCombatData[] saved)
        {
            if(saved==null)return;SaveValidation.Require(saved.Length<=64,"Too many combat actors");
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var d in saved){RobotCombatState.Validate(d);SaveValidation.Require(ids.Add(d.id),"Duplicate combat identity");Get(d.id,d.boss,d.maximum).Restore(d);}
        }
    }
    [Serializable] public sealed class WeaponLoadoutData
    {
        public int active,arc=6,arcReserve=18,cirrus=15,cirrusReserve=45,titan=3,titanReserve=9,grenades=2;
        public bool equipped,pulse,heavy;
        public int carryVersion;public float heat,cooldown,reloadRemaining,charge;public bool overheated,reloading,charging;
        public static void Validate(WeaponLoadoutData d)
        {
            if(d==null)return;
            SaveValidation.Require(d.carryVersion>=0&&d.carryVersion<=1&&!float.IsNaN(d.heat)&&d.heat>=0&&d.heat<=1&&d.cooldown>=0&&d.cooldown<=2&&d.reloadRemaining>=0&&d.reloadRemaining<=3&&d.charge>=0&&d.charge<=1,"Invalid weapon mechanism state");
            SaveValidation.Require(d.active>=0&&d.active<=2&&(d.active!=1||d.pulse)&&(d.active!=2||d.heavy),"Invalid active weapon");
            SaveValidation.Require(d.arc>=0&&d.arc<=6&&d.arcReserve>=0&&d.arcReserve<=72&&d.cirrus>=0&&d.cirrus<=15&&d.cirrusReserve>=0&&d.cirrusReserve<=120&&d.titan>=0&&d.titan<=3&&d.titanReserve>=0&&d.titanReserve<=24&&d.grenades>=0&&d.grenades<=4,"Invalid weapon ammunition");
        }
    }
}
