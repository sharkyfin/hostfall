using Hostfall.Interaction;
using Hostfall.Simulation;
using UnityEngine;

namespace Hostfall.Devices
{
    public sealed class PowerSwitch : MonoBehaviour, IInteractable
    {
        [SerializeField] private DeviceId deviceId = DeviceId.Edge01;
        [SerializeField] private Renderer indicator;
        [SerializeField] private Material on;
        [SerializeField] private Material off;
        private FacilitySimulation simulation;
        public string Prompt => "[E] " + (simulation[deviceId].Powered ? "Отключить " : "Включить ") + deviceId.Hostname().ToUpperInvariant();
        public void Configure(Renderer light, Material powered, Material unpowered) { indicator = light; on = powered; off = unpowered; }
        public void Initialize(FacilitySimulation model) { simulation = model; simulation.StateChanged += Refresh; Refresh(); }
        public void Interact() => simulation.SetPower(deviceId, !simulation[deviceId].Powered);
        private void Refresh() { if (indicator != null) indicator.sharedMaterial = simulation[deviceId].Powered ? on : off; }
        private void OnDestroy() { if (simulation != null) simulation.StateChanged -= Refresh; }
    }
}
