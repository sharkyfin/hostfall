using Hostfall.Simulation;
using System.Linq;
using UnityEngine;

namespace Hostfall.Devices
{
    // The physical display reads the same model as the interactive terminal.
    public sealed class ComputerScreen : MonoBehaviour
    {
        [SerializeField] private TextMesh header, content, footer;
        private FacilitySimulation world;private DeviceId device;private TerminalMemory shownMemory;private int revision=-1;private string draft;private bool poweredState;
        public void Bind(FacilitySimulation model,DeviceId id){world=model;device=id;model.StateChanged+=UpdateDisplay;UpdateDisplay();}
        private void UpdateDisplay()=>Refresh(world,device);
        private void OnDestroy(){if(world!=null)world.StateChanged-=UpdateDisplay;}
        public string DisplayedText => content != null ? content.text : "";
        public string HeaderText=>header!=null?header.text:"";
        public string PromptText=>footer!=null?footer.text:"";
        public void Configure(TextMesh title, TextMesh body, TextMesh status) { header = title; content = body; footer = status; }
        public void Refresh(FacilitySimulation simulation,DeviceId origin)
        {
            if(header==null||content==null||footer==null)return;
            bool legacy=origin==DeviceId.Ws07||origin==DeviceId.Edge01||origin==DeviceId.Core01;
            var playback=legacy?null:simulation.Screens.Find(origin);
            var display=legacy?TerminalDisplay.Read(simulation,origin):null;
            var current=legacy?display.Memory:playback?.Memory;
            bool powered=!legacy||simulation[origin].Powered;
            if(shownMemory==current&&revision==(current?.Revision??-1)&&draft==current?.Draft&&poweredState==powered)return;
            shownMemory=current;revision=current?.Revision??-1;draft=current?.Draft;poweredState=powered;
            foreach(var text in new[]{header,content,footer})text.gameObject.SetActive(powered);
            if(!powered)return;
            header.text=legacy?display.Header:playback?.Scenario.GetOsTitle()??origin.Hostname().ToUpperInvariant();
            content.text=legacy?display.Tail(62,15):current!=null?TerminalDisplay.TailText(string.Join("\n",current.Lines.Select(l=>l.text)),62,15):"Канал готов";
            string prompt=(legacy?display.Prompt:playback?.Scenario.FormatPrompt()??"")+" "+(current?.Draft??"");
            footer.text=prompt.Length>65?"…"+prompt.Substring(prompt.Length-64):prompt;
        }
    }
}
