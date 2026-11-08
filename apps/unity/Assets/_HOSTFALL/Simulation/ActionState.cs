using System;

namespace Hostfall.Simulation
{
    [Serializable] public sealed class ActionStateData
    {
        public float pulseCooldown,dashCooldown,watcherStun,watcherSlow,sentryStun,sentrySlow;
    }
    public sealed class ActionState
    {
        public const float PulseRecharge=9, DashRecharge=1.4f;
        private ActionStateData data=new();
        public float PulseCooldown=>data.pulseCooldown;
        public float DashCooldown=>data.dashCooldown;
        private static bool Known(string id)=>id=="watcher-r17"||id=="sentry-r18";
        public float Stun(string id)=>id=="watcher-r17"?data.watcherStun:id=="sentry-r18"?data.sentryStun:0;
        public float Slow(string id)=>id=="watcher-r17"?data.watcherSlow:id=="sentry-r18"?data.sentrySlow:0;
        public bool TryPulse(string id,bool unlocked)
        {
            if(!unlocked||!Known(id)||data.pulseCooldown>0) return false;
            data.pulseCooldown=PulseRecharge;
            if(id=="watcher-r17") { data.watcherStun=3; data.watcherSlow=7; }
            else { data.sentryStun=3; data.sentrySlow=7; }
            return true;
        }
        public bool TryDash()
        { if(data.dashCooldown>0) return false; data.dashCooldown=DashRecharge; return true; }
        public void ApplyNetworkSlow()
        { data.watcherSlow=Math.Max(data.watcherSlow,12); data.sentrySlow=Math.Max(data.sentrySlow,12); }
        public void Tick(float dt)
        {
            if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0) return;
            data.pulseCooldown=Decay(data.pulseCooldown,dt); data.dashCooldown=Decay(data.dashCooldown,dt);
            data.watcherStun=Decay(data.watcherStun,dt); data.watcherSlow=Decay(data.watcherSlow,dt);
            data.sentryStun=Decay(data.sentryStun,dt); data.sentrySlow=Decay(data.sentrySlow,dt);
        }
        private static float Decay(float value,float dt)=>Math.Max(0,value-dt);
        public ActionStateData Capture()=>new() { pulseCooldown=data.pulseCooldown,dashCooldown=data.dashCooldown,watcherStun=data.watcherStun,watcherSlow=data.watcherSlow,sentryStun=data.sentryStun,sentrySlow=data.sentrySlow };
        public void Restore(ActionStateData saved)
        {
            if(saved==null) return;
            foreach(float value in new[] { saved.pulseCooldown,saved.dashCooldown,saved.watcherStun,saved.watcherSlow,saved.sentryStun,saved.sentrySlow })
                SaveValidation.Require(!float.IsNaN(value)&&!float.IsInfinity(value)&&value>=0&&value<=12,"Invalid action timer");
            data=new ActionStateData { pulseCooldown=saved.pulseCooldown,dashCooldown=saved.dashCooldown,watcherStun=saved.watcherStun,watcherSlow=saved.watcherSlow,sentryStun=saved.sentryStun,sentrySlow=saved.sentrySlow };
        }
    }
}
