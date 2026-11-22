using System;
using System.Linq;

namespace Hostfall.Simulation
{
    [Serializable] public sealed class RoutineData
    {
        public DeviceId device;
        public int step, typed;
        public float wait;
        public bool completed;
    }
    // Fixed authored sequences. Only these commands cross into the internal virtual shell.
    // A step commits once, after typing; the save contains the next step and partial draft.
    public sealed class TerminalDirector
    {
        private readonly FacilitySimulation world;
        private readonly RoutineData[] states;
        private static readonly DeviceId[] devices={DeviceId.Ws07,DeviceId.Edge01,DeviceId.Core01};
        private static readonly string[][] scripts={
            new[] { "hostname", "ps", "cat /var/log/scheduler.log", "cat /etc/systemd/system/cache-sync.service", "systemctl status cache-sync", "systemctl disable cache-sync", "systemctl stop cache-sync", "ps" },
            new[] { "hostname", "cat /etc/network/interfaces", "ps" },
            new[] { "hostname", "ps", "cat /var/log/super.log" }
        };
        public TerminalDirector(FacilitySimulation simulation)
        { world=simulation; states=devices.Select(id=>new RoutineData { device=id }).ToArray(); }
        public bool KnowsDevice(DeviceId id) => Array.IndexOf(devices, id) >= 0;
        private static int Index(DeviceId id)
        { int index=Array.IndexOf(devices,id); if(index<0) throw new ArgumentException("Unknown terminal"); return index; }
        public bool IsComplete(DeviceId id)
        {
            int index = Array.IndexOf(devices, id);
            return index >= 0 && states[index].completed;
        }
        public bool CanRun(DeviceId id)
        {
            if(world.InteractiveComputers)return false;
            int index = Array.IndexOf(devices, id);
            if (index < 0) return false;
            return world[id].Powered && (index == 0 || states[index - 1].completed);
        }
        public string Status(DeviceId id)
        {
            int index = Array.IndexOf(devices, id);
            if (index < 0) return "Неизвестный узел";
            return states[index].completed ? "Данные получены" : !CanRun(id) ? "Сначала доберитесь до предыдущего узла" : "Анализ канала";
        }
        public bool Begin(DeviceId id)
        {
            int index = Array.IndexOf(devices, id);
            if (index < 0 || !CanRun(id) || states[index].completed) return false;
            var memory = world[id].Terminal; TerminalDisplay.Ensure(world[id]);
            memory.Host = id; memory.Draft = scripts[index][states[index].step].Substring(0, states[index].typed);
            world.NotifyTerminalChanged(); return true;
        }
        public void Tick(DeviceId id,float dt)
        {
            int index = Array.IndexOf(devices, id);
            if (index < 0 || !CanRun(id) || states[index].completed || float.IsNaN(dt) || float.IsInfinity(dt) || dt <= 0) return;
            var state = states[index]; var command = scripts[index][state.step];
            // Cap a frame's work: a stalled renderer must not skip a whole sequence.
            state.wait+=Math.Min(dt,.1f);
            if(state.typed<command.Length)
            {
                int count=(int)(state.wait*65); if(count==0) return;
                state.wait-=count/65f; state.typed=Math.Min(command.Length,state.typed+count);
                world[id].Terminal.Draft=command.Substring(0,state.typed); world.NotifyTerminalChanged(); return;
            }
            if(state.wait<.45f) return;
            var shell=new TerminalSession(world,id); shell.Resume(id);
            var result=shell.Execute(command);
            if(result.ExitCode!=0) { state.wait=0; return; }
            state.step++; state.typed=0; state.wait=0;
            if(state.step==scripts[index].Length)
            {
                state.completed=true;
                world.CompleteRoutine(id);
            }
            world.NotifyTerminalChanged();
        }
        public RoutineData[] Capture()=>states.Select(s=>new RoutineData { device=s.device,step=s.step,typed=s.typed,wait=s.wait,completed=s.completed }).ToArray();
        public void Restore(RoutineData[] data)
        {
            SaveValidation.Require(data!=null&&data.Length==3&&data.All(d=>d!=null)&&data.Select(d=>d.device).Distinct().Count()==3,"Incomplete terminal routines");
            foreach(var source in data)
            {
                SaveValidation.Require(source!=null&&Enum.IsDefined(typeof(DeviceId),source.device),"Invalid routine device");
                int index=Index(source.device); int length=scripts[index].Length;
                SaveValidation.Require(source.step>=0&&source.step<=length&&source.completed==(source.step==length),"Invalid routine step");
                SaveValidation.Require(source.typed>=0&&source.typed<=(source.completed?0:scripts[index][source.step].Length),"Invalid routine draft");
                SaveValidation.Require(!float.IsNaN(source.wait)&&!float.IsInfinity(source.wait)&&source.wait>=0&&source.wait<.6f,"Invalid routine delay");
                var state=states[index]; state.step=source.step; state.typed=source.typed; state.wait=source.wait; state.completed=source.completed;
            }
            for(int i=1;i<3;i++) SaveValidation.Require((states[i].step==0&&states[i].typed==0&&!states[i].completed)||states[i-1].completed,"Routine order conflict");
        }
        public void MigrateLegacy(int completed,bool isolated)
        {
            int count=completed==3?3:isolated?2:0;
            for(int i=0;i<count;i++) { states[i].step=scripts[i].Length; states[i].completed=true; }
        }
    }
}
