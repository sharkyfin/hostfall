using Hostfall.Simulation;
using UnityEngine;

namespace Hostfall.Devices
{
    // The physical display reads the same model as the interactive terminal.
    public sealed class ComputerScreen : MonoBehaviour
    {
        [SerializeField] private TextMesh header, content, footer;
        public string DisplayedText => content != null ? content.text : "";
        public string HeaderText=>header!=null?header.text:"";
        public string PromptText=>footer!=null?footer.text:"";
        public void Configure(TextMesh title, TextMesh body, TextMesh status) { header = title; content = body; footer = status; }
        public void Refresh(FacilitySimulation simulation,DeviceId origin)
        {
            var computer=simulation[origin]; var display=TerminalDisplay.Read(simulation,origin);
            header.gameObject.SetActive(computer.Powered); content.gameObject.SetActive(computer.Powered); footer.gameObject.SetActive(computer.Powered);
            if (!computer.Powered) return;
            header.text=display.Header;
            content.text=display.Tail(52,12);
            string prompt=display.Prompt+" "+display.Memory.Draft;
            footer.text=prompt.Length>62?"…"+prompt.Substring(prompt.Length-61):prompt;
        }
    }
}
