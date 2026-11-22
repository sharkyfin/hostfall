using Hostfall.Interaction;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.Devices
{
    public sealed class ComputerStation : MonoBehaviour, IInteractable
    {
        [SerializeField] private string stationId;
        [SerializeField] private WorkstationSurface surface;
        private ComputerRuntimeUI runtime;
        public string StationId => stationId;
        public WorkstationSurface Surface => surface;
        public string Prompt => runtime == null ? "Узел отключён" :
            stationId == Simulation.Computers.ComputerCampaign.HomeStation
                ? "Нажмите E, чтобы открыть домашний терминал"
                : "Нажмите E, чтобы использовать компьютер";
        public void Configure(string id, WorkstationSurface screen) { stationId = id; surface = screen; }
        public void Initialize(ComputerRuntimeUI view) => runtime = view;
        public void Interact() { if (runtime != null) runtime.Open(this); }
    }
}
