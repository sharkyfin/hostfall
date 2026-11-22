using System;
using System.Linq;

namespace Hostfall.Simulation.Computers
{
    public enum SignalPhase { Ready,Running,Failed,Complete }
    [Serializable]public sealed class SignalData
    {
        public int version=1,stage,selected;
        public SignalPhase phase;
        public bool started;
        public int[] frequency,trim;
        public bool[] enabled,inverted;
        public double[] drift;
        public double elapsed,runningTime,stable;
    }
    // Relative oscillator phase integrates frequency error. Knobs do not teleport the oscillator.
    public sealed class SignalSession
    {
        public const double StepSeconds=1.0/60;
        private static readonly int[] references={30,44,54}; // Frequency units are 0.025 Hz.
        private static readonly double[][] offsets={new double[]{0,30,-45},new double[]{140,-70,55},new double[]{-130,95,-25}};
        private readonly int[] frequency=new int[3],trim=new int[3];
        private readonly bool[] enabled=new bool[3],inverted=new bool[3];
        private readonly double[] drift=new double[3];
        private double elapsed;
        public bool Started{get;private set;}
        public int Stage{get;private set;}
        public int Selected{get;private set;}
        public SignalPhase Phase{get;private set;}
        public double RunningTime{get;private set;}
        public double StableSeconds{get;private set;}
        public int Difficulty=>Math.Min(Stage,2);
        public int ReferenceUnits=>references[Difficulty];
        public double ReferenceFrequency=>ReferenceUnits*.025;
        public double RequiredHold=>2.5+Difficulty;
        public double PhaseTolerance=>Difficulty==0?25:Difficulty==1?18:10;
        public double FrequencyTolerance=>Difficulty==0?.08:Difficulty==1?.04:.02;
        public double WindowSeconds=>Difficulty==0?40:Difficulty==1?32:25;
        public double WindowRemaining=>Math.Max(0,WindowSeconds-RunningTime);
        public bool AllInLock=>Enumerable.Range(0,3).All(InLock);
        public double Coherence=>Enumerable.Range(0,3).Average(i=>enabled[i]?Math.Max(0,1-Math.Abs(PhaseError(i))/180-Math.Abs(Frequency(i)-ReferenceFrequency)*2):0);
        public event Action Changed,Failed,FinalLock;
        public event Action<int> StageCompleted;
        public SignalSession(){ConfigureStage();}
        private void ConfigureStage()
        {
            for(int i=0;i<3;i++)
            {frequency[i]=ReferenceUnits+(i==0?-4:i==1?6:2);trim[i]=0;drift[i]=0;enabled[i]=i<2;inverted[i]=false;}
            elapsed=RunningTime=StableSeconds=0;
        }
        private static bool Channel(int channel)=>channel>=0&&channel<3;
        public int FrequencyUnits(int channel)=>Channel(channel)?frequency[channel]:0;
        public double Frequency(int channel)=>FrequencyUnits(channel)*.025;
        public int Trim(int channel)=>Channel(channel)?trim[channel]:0;
        public bool Enabled(int channel)=>Channel(channel)&&enabled[channel];
        public bool Inverted(int channel)=>Channel(channel)&&inverted[channel];
        public double PhaseError(int channel)=>Channel(channel)?Signed(drift[channel]+offsets[Difficulty][channel]+trim[channel]+(inverted[channel]?180:0)):180;
        public bool InLock(int channel)=>Enabled(channel)&&Math.Abs(Frequency(channel)-ReferenceFrequency)<=FrequencyTolerance+1e-10&&Math.Abs(PhaseError(channel))<=PhaseTolerance+1e-10;
        public static double Signed(double degrees)=>((degrees+180)%360+360)%360-180;
        public bool Select(int channel)
        {if(!Channel(channel))return false;Selected=channel;Changed?.Invoke();return true;}
        public bool SetFrequency(int channel,int units)
        {if(Phase==SignalPhase.Complete||!Channel(channel)||units<20||units>64)return false;Started=true;frequency[channel]=units;Changed?.Invoke();return true;}
        public bool SetTrim(int channel,int degrees)
        {if(Phase==SignalPhase.Complete||!Channel(channel)||degrees< -90||degrees>90)return false;Started=true;trim[channel]=degrees;Changed?.Invoke();return true;}
        public bool SetEnabled(int channel,bool value)
        {if(Phase==SignalPhase.Complete||!Channel(channel))return false;Started=true;enabled[channel]=value;Changed?.Invoke();return true;}
        public bool SetInverted(int channel,bool value)
        {if(Phase==SignalPhase.Complete||!Channel(channel))return false;Started=true;inverted[channel]=value;Changed?.Invoke();return true;}
        public bool Start()
        {
            if(Phase==SignalPhase.Running)return false;
            Started=true;Phase=SignalPhase.Running;elapsed=RunningTime=StableSeconds=0;Changed?.Invoke();return true;
        }
        public void Fail()
        {
            if(Phase!=SignalPhase.Running)return;
            Phase=SignalPhase.Failed;StableSeconds=elapsed=0;Failed?.Invoke();Changed?.Invoke();
        }
        public void Advance(double seconds)
        {
            if(Phase!=SignalPhase.Running||!Finite(seconds)||seconds<=0)return;
            elapsed+=Math.Min(.25,seconds);
            while(elapsed+1e-12>=StepSeconds&&Phase==SignalPhase.Running)
            {
                elapsed=Math.Max(0,elapsed-StepSeconds);RunningTime+=StepSeconds;
                for(int i=0;i<3;i++)if(enabled[i])drift[i]=(drift[i]+(Frequency(i)-ReferenceFrequency)*360*StepSeconds+360)%360;
                StableSeconds=AllInLock?StableSeconds+StepSeconds:0;
                if(StableSeconds+1e-12>=RequiredHold)
                {
                    bool newStage=Stage<3;if(newStage)Stage++;
                    if(Stage<3){Phase=SignalPhase.Ready;ConfigureStage();}
                    else {Phase=SignalPhase.Complete;elapsed=0;StableSeconds=RequiredHold;}
                    if(newStage)StageCompleted?.Invoke(Stage);
                    if(Stage==3)FinalLock?.Invoke();Changed?.Invoke();
                }
                else if(RunningTime+1e-12>=WindowSeconds){RunningTime=WindowSeconds;Fail();}
            }
            Changed?.Invoke();
        }
        private static bool Finite(double n)=>!double.IsNaN(n)&&!double.IsInfinity(n);
        public SignalData Capture()=>new(){started=Started,stage=Stage,selected=Selected,phase=Phase,frequency=(int[])frequency.Clone(),trim=(int[])trim.Clone(),enabled=(bool[])enabled.Clone(),
            inverted=(bool[])inverted.Clone(),drift=(double[])drift.Clone(),elapsed=elapsed,runningTime=RunningTime,stable=StableSeconds};
        public static SignalSession Restore(SignalData data)
        {
            SaveValidation.Require(data!=null&&data.version==1&&data.stage>=0&&data.stage<=3&&data.selected>=0&&data.selected<3&&Enum.IsDefined(typeof(SignalPhase),data.phase)&&
                data.frequency?.Length==3&&data.trim?.Length==3&&data.enabled?.Length==3&&data.inverted?.Length==3&&data.drift?.Length==3,"Invalid signal channels");
            var game=new SignalSession{Started=data.started,Stage=data.stage,Selected=data.selected,Phase=data.phase};
            SaveValidation.Require(Finite(data.elapsed)&&data.elapsed>=0&&data.elapsed<StepSeconds&&Finite(data.runningTime)&&data.runningTime>=0&&data.runningTime<=game.WindowSeconds+1e-9&&
                Finite(data.stable)&&data.stable>=0&&data.stable<=game.RequiredHold+1e-9,"Invalid signal clocks");
            SaveValidation.Require((data.stage==3||data.phase!=SignalPhase.Complete)&&(data.stage!=3||data.phase!=SignalPhase.Ready)&&
                (data.phase==SignalPhase.Running||data.elapsed==0)&&(data.phase!=SignalPhase.Failed||data.stable==0)&&
                (data.phase!=SignalPhase.Ready||data.stable==0&&data.runningTime==0)&&(data.started||data.stage==0&&data.phase==SignalPhase.Ready),"Inconsistent signal phase");
            for(int i=0;i<3;i++)SaveValidation.Require(data.frequency[i]>=20&&data.frequency[i]<=64&&data.trim[i]>= -90&&data.trim[i]<=90&&Finite(data.drift[i])&&data.drift[i]>=0&&data.drift[i]<360,"Invalid oscillator state");
            Array.Copy(data.frequency,game.frequency,3);Array.Copy(data.trim,game.trim,3);Array.Copy(data.enabled,game.enabled,3);Array.Copy(data.inverted,game.inverted,3);Array.Copy(data.drift,game.drift,3);
            game.elapsed=data.elapsed;game.RunningTime=data.runningTime;game.StableSeconds=data.stable;
            SaveValidation.Require(game.Phase!=SignalPhase.Complete||game.AllInLock&&Math.Abs(game.StableSeconds-game.RequiredHold)<1e-9,"Missing final lock");return game;
        }
    }
}
