using System;
using System.Linq;
using System.Collections.Generic;

namespace Hostfall.Simulation
{
    // Both the world monitor and the interactive window are views of this same session.
    public sealed class TerminalDisplay
    {
        public string Header { get; }
        public string Prompt { get; }
        public TerminalMemory Memory { get; }
        public string Transcript => string.Join("\n\n",Memory.Lines.Select(l=>l.text));
        private TerminalDisplay(FacilitySimulation world,DeviceId origin)
        {
            var computer=world[origin]; Memory=computer.Terminal; Ensure(computer, world.AutomatedRoute);
            var host=world[Memory.Host]; Header=host.Id.Hostname().ToUpperInvariant()+" / "+host.Address;
            Prompt="operator@"+host.Id.Hostname()+":"+host.WorkingDirectory+"$";
        }
        public static TerminalDisplay Read(FacilitySimulation world,DeviceId origin)=>new(world,origin);
        internal static void Ensure(VirtualComputer computer, bool automatedRoute = true)
        {
            if(computer.Terminal.Initialized) return;
            var memory=computer.Terminal; memory.Initialized=true;
            if(!automatedRoute)
            {
                memory.AddLine("Сеанс "+computer.Id.Hostname().ToUpperInvariant(),"muted");
                memory.AddLine("PID    ПРОЦЕСС\n"+string.Join("\n",computer.Processes.Select(p=>p.Pid.ToString().PadRight(7)+p.Command)),"result");
            }
        }
        public string Tail(int width,int rows)
        {
            var lines=new List<string>();
            foreach(string source in Transcript.Replace("\r","").Split('\n'))
            {
                string line=source;
                while(line.Length>width)
                {
                    int space=line.LastIndexOf(' ',width-1,width); int cut=space>width/2?space:width;
                    lines.Add(line.Substring(0,cut)); line=line.Substring(cut).TrimStart();
                }
                lines.Add(line);
            }
            return string.Join("\n",lines.Skip(Math.Max(0,lines.Count-rows)));
        }
    }
}
