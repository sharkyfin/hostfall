using System;
using Hostfall.Interaction;
using Hostfall.Simulation;
using UnityEngine;

namespace Hostfall.Devices
{
    public sealed class ComputerTerminal : MonoBehaviour, IInteractable
    {
        [SerializeField] private DeviceId deviceId;
        [SerializeField] private Renderer screen;
        [SerializeField] private Material online;
        [SerializeField] private Material offline;
        [SerializeField] private GameObject readouts;
        private FacilitySimulation simulation;
        private ComputerScreen physicalDisplay;
        private Action<DeviceId> open;
        public DeviceId Id => deviceId;
        public bool ShouldConnect=>simulation!=null&&simulation.Terminals.CanRun(deviceId)&&!simulation.Terminals.IsComplete(deviceId);
        public string Prompt => !simulation[deviceId].Powered?"НЕТ ПИТАНИЯ":!simulation.Terminals.CanRun(deviceId)?"Узел закрыт · завершите предыдущее подключение":ShouldConnect?"Терминал · подключение при приближении":"[E] Просмотреть сеанс";
        public void Configure(DeviceId id, Renderer display, Material on, Material off, GameObject lines) { deviceId = id; screen = display; online = on; offline = off; readouts = lines; }
        public void Initialize(FacilitySimulation model, Action<DeviceId> openTerminal)
        {
            physicalDisplay = GetComponent<ComputerScreen>(); simulation = model; open = openTerminal; model.StateChanged += Refresh; Refresh();
        }
        public void Interact() { if (simulation[deviceId].Powered) open(deviceId); }
        private void Refresh()
        {
            bool powered = simulation[deviceId].Powered;
            physicalDisplay?.Refresh(simulation,deviceId);
            if (screen != null) screen.sharedMaterial = powered ? online : offline;
            if (readouts != null) readouts.SetActive(powered);
        }
        private void OnDestroy() { if (simulation != null) simulation.StateChanged -= Refresh; }
    }
}
