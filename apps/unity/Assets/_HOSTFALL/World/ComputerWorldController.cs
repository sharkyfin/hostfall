using System;
using System.Collections.Generic;
using Hostfall.Core;
using Hostfall.Player;
using Hostfall.Simulation.Computers;
using UnityEngine;

namespace Hostfall.World
{
    public sealed class ComputerWorldController : MonoBehaviour
    {
        [SerializeField] private VentReleaseController vent;
        [SerializeField] private Light supplyLamp;
        private ComputerCampaign campaign;
        private LogisticsMechanisms logistics;
        private ServerNetworkController network;
        private CircuitPowerController power;
        private SignalBarrierController signal;
        private MinefieldFloorController minefield;
        private NeuroInvestigationController investigation;
        private ContainmentWorldController containment;
        private IntegrityTelemetryController integrity;
        private RecoveryTransitController recovery;
        private readonly Dictionary<string, FacilityDoor> doors = new(StringComparer.Ordinal);
        public void Configure(VentReleaseController source, Light lamp) { vent = source; supplyLamp = lamp; }
        public void Initialize(ComputerCampaign model, ControlState controls, PlayerMotor player)
        {
            campaign = model;
            foreach (var door in FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None))
            {
                if (!doors.TryAdd(door.DoorId, door)) throw new InvalidOperationException("Duplicate world door: " + door.DoorId);
            }
            foreach (string id in new[] { "door.soc.corridor", "door.corridor.server" })
                if (!doors.ContainsKey(id)) throw new InvalidOperationException("Missing computer door: " + id);
            if (vent == null || supplyLamp == null) throw new InvalidOperationException("Missing physical ventilation/power binding");
            vent.Initialize(model, controls, player);
            logistics=FindAnyObjectByType<LogisticsMechanisms>();
            if(logistics!=null)logistics.Initialize(model);
            network=FindAnyObjectByType<ServerNetworkController>();if(network!=null)network.Initialize(model);
            power=FindAnyObjectByType<CircuitPowerController>();if(power!=null)power.Initialize(model,FindAnyObjectByType<Hostfall.Core.ControlState>());
            signal=FindAnyObjectByType<SignalBarrierController>();if(signal!=null)signal.Initialize(model,FindAnyObjectByType<Hostfall.Core.ControlState>());
            minefield = FindAnyObjectByType<MinefieldFloorController>();
            if (minefield != null)
                minefield.Initialize(model, FindAnyObjectByType<Hostfall.Core.ControlState>());
            investigation = FindAnyObjectByType<NeuroInvestigationController>();
            if (investigation != null)
                investigation.Initialize(model, FindAnyObjectByType<Hostfall.Core.ControlState>());
            containment = FindAnyObjectByType<ContainmentWorldController>();
            if (containment != null)
                containment.Initialize(model, FindAnyObjectByType<Hostfall.Core.ControlState>(), FindAnyObjectByType<Hostfall.Player.PlayerMotor>());
            var cache = FindAnyObjectByType<BufferCachePickup>();
            if (cache != null)
                cache.Initialize(model.Buffer, FindAnyObjectByType<Hostfall.Player.PlayerMotor>(), FindAnyObjectByType<Hostfall.Core.ControlState>());
            integrity = FindAnyObjectByType<IntegrityTelemetryController>();
            if (integrity != null)
                integrity.Initialize(model.Integrity);
            recovery = FindAnyObjectByType<RecoveryTransitController>();
            if (recovery != null)
                recovery.Initialize(model, FindAnyObjectByType<Hostfall.Core.ControlState>(), FindAnyObjectByType<Hostfall.Player.PlayerMotor>());
            supplyLamp.enabled = false;
            campaign.WorldAction += Apply;
            foreach (var action in campaign.Actions) Apply(action);
        }
        private void Apply(WorldActionData action)
        {
            switch (action.kind)
            {
                case WorldActionKind.SealVent:
                    if (action.target != ComputerCampaign.VentTarget) throw new InvalidOperationException("Unbound vent: " + action.target);
                    vent.RefreshState(); break;
                case WorldActionKind.EnergizeDoor:
                    if (action.target != "door.soc.corridor") throw new InvalidOperationException("Unbound door supply: " + action.target);
                    supplyLamp.enabled = true; break;
                case WorldActionKind.UnlockDoor:
                    if (!doors.TryGetValue(action.target, out var door)) throw new InvalidOperationException("Unbound door: " + action.target);
                    door.UnlockForComputer(); break;
                case WorldActionKind.RecoveryControl:
                    if (recovery == null || action.target != ComputerCampaign.RecoveryTransitTarget)
                        throw new InvalidOperationException("Missing recovery transit");
                    recovery.RefreshPower();
                    break;
                case WorldActionKind.IntegrityControl:
                    if (integrity == null || action.target != ComputerCampaign.IntegrityTarget)
                        throw new InvalidOperationException("Missing core telemetry bindings");
                    integrity.Refresh();
                    break;
                case WorldActionKind.ContainmentControl:
                    if (containment == null || action.target != ComputerCampaign.ContainmentTarget)
                        throw new InvalidOperationException("Missing RBT-01 quarantine bindings");
                    containment.RefreshState();
                    break;
                case WorldActionKind.DetectiveControl:
                    if (investigation == null || action.target != ComputerCampaign.DetectiveSafe)
                        throw new InvalidOperationException("Missing neuroarchive case bindings");
                    investigation.RefreshState();
                    break;
                case WorldActionKind.MinefieldControl:
                    if (minefield == null || action.target != ComputerCampaign.MinefieldFloor)
                        throw new InvalidOperationException("Missing SEC-07 sensor floor bindings");
                    minefield.RefreshState();
                    break;
                case WorldActionKind.SignalControl:
                    if(signal==null||action.target!="quantum.loop.1"&&action.target!="quantum.loop.2"&&action.target!="quantum.loop.3")throw new InvalidOperationException("Missing quantum plant bindings");signal.RefreshState();break;
                case WorldActionKind.PowerDistribution:
                    if(power==null||action.target!="energy.bus")throw new InvalidOperationException("Missing energy power bindings");power.RefreshState();break;
                case WorldActionKind.NetworkControl:
                    if(network==null)throw new InvalidOperationException("Missing server network for "+action.target);
                    network.RefreshState();break;
                case WorldActionKind.StartMechanism:
                case WorldActionKind.RaiseAlarm:
                    if(logistics==null)throw new InvalidOperationException("Missing logistics mechanisms for " + action.target);
                    logistics.RefreshState();
                    if (action.target == "logistics.bridge") minefield?.GrantEntry();
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(action.kind));
            }
        }
        private void OnDestroy() { if (campaign != null) campaign.WorldAction -= Apply; }
    }
}
