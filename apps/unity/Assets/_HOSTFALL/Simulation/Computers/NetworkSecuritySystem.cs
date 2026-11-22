using System;

namespace Hostfall.Simulation.Computers
{
    [Serializable] public sealed class NetworkSecurityData
    {
        public int version=1, released;
        public bool started;
        public double releaseClock, outage, relayClock;
    }
    // World time, independent of the window/game clock. Already released actors are never erased.
    public sealed class NetworkSecuritySystem
    {
        private NetworkSecurityData state=new();
        private int stage;
        public int Released=>state.released;
        public double ReleaseClock=>state.releaseClock;
        public double OutageRemaining=>state.outage;
        public bool Started=>state.started;
        public int ReinforcementLimit=>stage>=3?0:stage>=1?1:2;
        public bool HasPending=>state.started&&state.released<ReinforcementLimit;
        public event Action<int> ReleaseChanged;
        public event Action RelayRequested;
        public void Start(){state.started=true;}
        public void EarnStage(int value)
        {
            if(value<=stage)return;
            stage=value;state.relayClock=0;
            if(value==2)state.outage=15;
            if(!HasPending)state.releaseClock=0;
        }
        public void Advance(double seconds)
        {
            if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0)return;
            seconds=Math.Min(seconds,.25);
            if(stage==0){state.relayClock+=seconds;if(state.relayClock>=.5){state.relayClock-=.5;RelayRequested?.Invoke();}}
            state.outage=Math.Max(0,state.outage-seconds);
            if(!HasPending)return;
            state.releaseClock+=seconds;
            if(state.releaseClock>=12)
            {
                state.releaseClock=0;state.released++;
                ReleaseChanged?.Invoke(state.released);
            }
        }
        public NetworkSecurityData Capture()=>new(){started=state.started,released=state.released,releaseClock=state.releaseClock,outage=state.outage,relayClock=state.relayClock};
        public static NetworkSecuritySystem Restore(NetworkSecurityData data,int earned)
        {
            if(data==null){var blank=new NetworkSecuritySystem();blank.stage=earned;return blank;}
            SaveValidation.Require(!double.IsNaN(data.relayClock)&&!double.IsInfinity(data.relayClock)&&data.relayClock>=0&&data.relayClock<.5&&(earned==0||data.relayClock==0),"Invalid network relay clock");
            SaveValidation.Require(data.version==1&&data.released>=0&&data.released<=2&&
                !double.IsNaN(data.releaseClock)&&!double.IsInfinity(data.releaseClock)&&data.releaseClock>=0&&data.releaseClock<12&&
                !double.IsNaN(data.outage)&&!double.IsInfinity(data.outage)&&data.outage>=0&&data.outage<=15&&
                (earned>=2||data.outage==0)&&(data.started||data.released==0&&data.releaseClock==0&&earned==0),"Invalid network security state");
            var result=new NetworkSecuritySystem{stage=earned,state=new NetworkSecurityData{started=data.started,released=data.released,releaseClock=data.releaseClock,outage=data.outage,relayClock=data.relayClock}};
            SaveValidation.Require(result.HasPending||data.releaseClock==0,"Invalid inactive reinforcement clock");
            return result;
        }
    }
}
