using System;
namespace Hostfall.Simulation.Computers
{
    [Serializable]public sealed class SignalEquipmentData
    {
        public int version=1,calibrated;
        public bool started;
        public double heat,recharge,barrierWindow,returnWindow;
    }
    // World time outlives app focus. Repeated final tuning reopens the passage without repeating rewards.
    public sealed class SignalEquipment
    {
        public const double OpenSeconds=20;
        public bool Started{get;private set;}
        public int Calibrated{get;private set;}
        public double Heat{get;private set;}
        public double Recharge{get;private set;}
        public double BarrierWindow{get;private set;}
        public double ReturnWindow{get;private set;}
        public bool BarrierRequestedOpen=>BarrierWindow>0||ReturnWindow>0;
        public bool ReleaseReturn(){if(Calibrated!=3)return false;ReturnWindow=8;Changed?.Invoke();return true;}
        public event Action FirstStarted,Overheated,Changed;
        public bool Start()
        {
            if(Recharge>0)return false;
            if(!Started){Started=true;FirstStarted?.Invoke();}Changed?.Invoke();return true;
        }
        public bool Calibrate(int stage)
        {if(!Started||stage!=Calibrated+1||stage>3)return false;Calibrated=stage;Changed?.Invoke();return true;}
        public void OpenBarrier()
        {if(Calibrated!=3)return;BarrierWindow=OpenSeconds;Changed?.Invoke();}
        public void Fail()
        {if(Recharge>0)return;Recharge=5;Heat=Math.Max(Heat,.85);Changed?.Invoke();}
        public void Advance(double seconds,bool running,double coherence,bool ventilationPowered)
        {
            if(!Finite(seconds)||seconds<=0)return;seconds=Math.Min(.25,seconds);
            double oldHeat=Heat,oldRecharge=Recharge,oldWindow=BarrierWindow,oldReturn=ReturnWindow;
            if(ReturnWindow>0)ReturnWindow=Math.Max(0,ReturnWindow-seconds);
            if(BarrierWindow>0)BarrierWindow=Math.Max(0,BarrierWindow-seconds);
            if(Recharge>0){Recharge=Math.Max(0,Recharge-seconds);if(Recharge<1e-12)Recharge=0;Heat=Math.Max(0,Heat-seconds*.14);}
            else if(running)
            {
                Heat=Math.Max(0,Math.Min(1,Heat+seconds*(.006+.035*(1-Math.Max(0,Math.Min(1,coherence)))-(ventilationPowered?.035:0))));
                if(Heat>=1-1e-12){Heat=1;Fail();Overheated?.Invoke();}
            }
            else Heat=Math.Max(0,Heat-seconds*(ventilationPowered?.06:.025));
            if(Heat!=oldHeat||Recharge!=oldRecharge||BarrierWindow!=oldWindow||ReturnWindow!=oldReturn)Changed?.Invoke();
        }
        private static bool Finite(double n)=>!double.IsNaN(n)&&!double.IsInfinity(n);
        public SignalEquipmentData Capture()=>new(){started=Started,calibrated=Calibrated,heat=Heat,recharge=Recharge,barrierWindow=BarrierWindow,returnWindow=ReturnWindow};
        public static SignalEquipment Restore(SignalEquipmentData data,int stage)
        {
            if(data==null){SaveValidation.Require(stage==0,"Missing signal equipment");return new SignalEquipment();}
            SaveValidation.Require(data.version==1&&data.calibrated==stage&&stage>=0&&stage<=3&&Finite(data.heat)&&data.heat>=0&&data.heat<=1&&
                Finite(data.recharge)&&data.recharge>=0&&data.recharge<=5&&Finite(data.barrierWindow)&&data.barrierWindow>=0&&data.barrierWindow<=OpenSeconds&&
                Finite(data.returnWindow)&&data.returnWindow>=0&&data.returnWindow<=8&&(stage==3||data.barrierWindow==0&&data.returnWindow==0)&&(data.started||stage==0&&data.heat==0&&data.recharge==0&&data.barrierWindow==0),"Invalid signal equipment");
            return new SignalEquipment{Started=data.started,Calibrated=stage,Heat=data.heat,Recharge=data.recharge,BarrierWindow=data.barrierWindow,ReturnWindow=data.returnWindow};
        }
    }
}
