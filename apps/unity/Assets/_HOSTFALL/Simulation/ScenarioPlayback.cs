using System;
using System.Collections.Generic;
using System.Linq;

namespace Hostfall.Simulation
{
    [Serializable] public sealed class ScenarioPlaybackData
    {
        public string id;
        public int step, typed;
        public float wait;
        public bool rewarded, presented;
        public TerminalMemoryData memory;
    }
    // Playback belongs to the world, so leaving a screen does not erase its session.
    public sealed class ScenarioPlayback
    {
        public TerminalScenario Scenario { get; }
        public TerminalMemory Memory { get; private set; }
        public int Step { get; private set; }
        public int Typed { get; private set; }
        private float wait;
        public bool Complete => Step == Scenario.Steps.Count;
        public bool Rewarded {get;private set;}
        public ScenarioPlayback(TerminalScenario scenario)
        { Scenario=scenario; Memory=new TerminalMemory((uint)scenario.DeviceId==0?DeviceId.Ws07:scenario.DeviceId){Initialized=true}; }
        public void Tick(float dt)
        {
            if(Complete||dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;
            var command=Scenario.Steps[Step]; wait+=Math.Min(dt,.25f);
            if(Typed<command.Command.Length)
            {
                int count=(int)(wait*110);wait-=count/110f;
                Typed=Math.Min(command.Command.Length,Typed+count);Memory.Draft=command.Command.Substring(0,Typed);return;
            }
            if(wait<Math.Max(.06f,command.DelayAfterOutput))return;
            Memory.AddCommand(command.Command);Memory.AddLine(Scenario.FormatPrompt()+command.Command,"command-line");
            if(command.Output.Length>0)Memory.AddLine(command.Output,"result");
            Step++;Typed=0;wait=0;Memory.Draft="";
        }
        public bool ClaimCompletion()
        {if(!Complete||Rewarded)return false;Rewarded=true;return true;}
        public ScenarioPlaybackData Capture()=>new(){id=Scenario.ScenarioId,step=Step,typed=Typed,wait=wait,rewarded=Rewarded,memory=Memory.Capture()};
        public void Restore(ScenarioPlaybackData data)
        {
            SaveValidation.Require(data.step>=0&&data.step<=Scenario.Steps.Count,"Invalid scenario step");
            SaveValidation.Require(data.typed>=0&&data.typed<=(data.step<Scenario.Steps.Count?Scenario.Steps[data.step].Command.Length:0),"Invalid scenario cursor");
            SaveValidation.Require(!float.IsNaN(data.wait)&&!float.IsInfinity(data.wait)&&data.wait>=0&&data.wait<=10,"Invalid scenario wait");
            SaveValidation.Require(!data.rewarded||data.step==Scenario.Steps.Count,"Incomplete scenario reward");
            Memory=TerminalMemory.Restore(data.memory,(uint)Scenario.DeviceId==0?DeviceId.Ws07:Scenario.DeviceId);
            Step=data.step;Typed=data.typed;wait=data.wait;Rewarded=data.rewarded;
            Memory.Draft=Complete?"":Scenario.Steps[Step].Command.Substring(0,Typed);
        }
    }
    public sealed class ScenarioSessions
    {
        private readonly Dictionary<string,ScenarioPlayback> sessions=new(StringComparer.Ordinal);
        private readonly Dictionary<DeviceId,string> active=new();
        public ScenarioPlayback FindById(string id)=>sessions.TryGetValue(id,out var value)?value:null;
        public ScenarioPlayback Present(TerminalScenario scenario){active[scenario.DeviceId]=scenario.ScenarioId;return Get(scenario);}
        public ScenarioPlayback Get(TerminalScenario scenario)
        {
            if(!sessions.TryGetValue(scenario.ScenarioId,out var value))sessions.Add(scenario.ScenarioId,value=new ScenarioPlayback(scenario));return value;
        }
        public ScenarioPlayback Find(DeviceId id)=>active.TryGetValue(id,out var key)?FindById(key):sessions.Values.FirstOrDefault(s=>s.Scenario.DeviceId==id);
        public ScenarioPlaybackData[] Capture()=>sessions.Values.OrderBy(s=>s.Scenario.ScenarioId).Select(s=>{var data=s.Capture();data.presented=active.TryGetValue(s.Scenario.DeviceId,out var key)&&key==data.id;return data;}).ToArray();
        public void Restore(ScenarioPlaybackData[] data)
        {
            if(data==null)return;SaveValidation.Require(data.Length<=32,"Too many screen sessions");
            foreach(var saved in data)
            {
                SaveValidation.Require(saved!=null&&saved.id!=null&&!sessions.ContainsKey(saved.id),"Invalid screen session");
                SaveValidation.Require(TerminalScenarioCatalog.TryGetScenario(saved.id,out var scenario),"Unknown screen scenario");SaveValidation.Require(saved.id==scenario.ScenarioId,"Noncanonical screen scenario");Get(scenario).Restore(saved);if(saved.presented){SaveValidation.Require(!active.ContainsKey(scenario.DeviceId),"Duplicate active screen");active.Add(scenario.DeviceId,scenario.ScenarioId);}
            }
        }
    }
}
