using System;
using Hostfall.Interaction;
using Hostfall.Simulation;
using Hostfall.UI;
using Hostfall.World;
using UnityEngine;

namespace Hostfall.Devices
{
    public sealed class DoorTerminalConsole : MonoBehaviour, IInteractable
    {
        [SerializeField] private FacilityDoor targetDoor;
        [SerializeField] private FacilityDoor[] additionalDoors = Array.Empty<FacilityDoor>();
        [SerializeField] private DeviceId deviceId;
        [SerializeField] private string hostname = "data01";
        [SerializeField] private string ipAddress = "10.17.0.101";
        [SerializeField] private string[] commands = Array.Empty<string>();
        [SerializeField] private string[] outputs = Array.Empty<string>();
        [SerializeField] private string promptText = "[E] Авторизовать доступ через терминал";
        [SerializeField] private string completedPromptText = "[E] Просмотреть сеанс терминала";
        [SerializeField] private string hudNotice = "Шлюз разблокирован";
        [SerializeField] private Renderer screenRenderer;
        [SerializeField] private Renderer statusLedRenderer;
        [SerializeField] private Material activeMaterial;
        [SerializeField] private Material completeMaterial;
        [SerializeField] private TextMesh screenStatusText;
        [SerializeField] private TextMesh screenBodyText;

        private FacilityUI ui;
        private FacilitySimulation simulation;
        private bool completed;

        public DeviceId DeviceId => deviceId;
        public bool Completed => completed;
        public bool ShouldConnect => !completed;
        public string Prompt => completed ? completedPromptText : promptText;

        public void Configure(FacilityDoor door, string host, string ip, string[] cmds, string[] outs, string notice,
            string prompt = null, Renderer screen = null, Material activeMat = null, Material completeMat = null, DeviceId devId = DeviceId.Ws07,
            TextMesh statusTxt = null, TextMesh bodyTxt = null, Renderer led = null)
        {
            targetDoor = door;
            hostname = host;
            ipAddress = ip;
            commands = cmds;
            outputs = outs;
            hudNotice = notice;
            if (!string.IsNullOrEmpty(prompt)) promptText = prompt;
            screenRenderer = screen;
            activeMaterial = activeMat;
            completeMaterial = completeMat;
            deviceId = devId;
            screenStatusText = statusTxt;
            screenBodyText = bodyTxt;
            statusLedRenderer = led;
        }

        public void Initialize(FacilityUI interfaceUI, FacilitySimulation sim = null)
        {
            ui = interfaceUI;
            simulation = sim;
            if (simulation != null)
            {
                bool wasCompleted = false;
                if (deviceId == DeviceId.Data01 && simulation.Mission.CompletedObjectives > 3) wasCompleted = true;
                else if (deviceId == DeviceId.Pwr01 && simulation.Mission.CompletedObjectives > 4) wasCompleted = true;
                else if (deviceId == DeviceId.Qnt01 && simulation.Mission.CompletedObjectives > 5) wasCompleted = true;

                if (wasCompleted)
                {
                    completed = true;
                    if (targetDoor != null) targetDoor.UnlockAndOpen();
                    if (additionalDoors != null)
                    {
                        foreach (var door in additionalDoors)
                            if (door != null) door.UnlockAndOpen();
                    }
                    if (screenStatusText != null)
                    {
                        screenStatusText.text = "[AUTHORIZED · AIRLOCK UNLOCKED]";
                        screenStatusText.color = new Color(0.25f, 0.95f, 0.45f);
                    }
                    if (screenBodyText != null) screenBodyText.color = new Color(0.7f, 0.95f, 0.75f);
                    if (statusLedRenderer != null && completeMaterial != null) statusLedRenderer.sharedMaterial = completeMaterial;
                }
            }
        }

        public void Interact()
        {
            if (completed)
            {
                if (targetDoor != null) targetDoor.UnlockAndOpen();
                if (additionalDoors != null)
                {
                    foreach (var door in additionalDoors)
                        if (door != null) door.UnlockAndOpen();
                }
                if (ui == null) ui = FindAnyObjectByType<FacilityUI>();
                if (ui != null) ui.OpenTerminalSession(hostname, ipAddress, commands, outputs, null);
                return;
            }
            if (ui == null) ui = FindAnyObjectByType<FacilityUI>();
            if (ui != null)
            {
                ui.OpenTerminalSession(hostname, ipAddress, commands, outputs, OnCompleted);
            }
            else
            {
                OnCompleted();
            }
        }

        public void AddTargetDoor(FacilityDoor extraDoor)
        {
            if (extraDoor == null) return;
            if (additionalDoors == null || additionalDoors.Length == 0)
            {
                additionalDoors = new[] { extraDoor };
            }
            else
            {
                var list = new System.Collections.Generic.List<FacilityDoor>(additionalDoors);
                if (!list.Contains(extraDoor)) list.Add(extraDoor);
                additionalDoors = list.ToArray();
            }
        }

        private void OnCompleted()
        {
            completed = true;
            if (targetDoor != null) targetDoor.UnlockAndOpen();
            if (additionalDoors != null)
            {
                foreach (var door in additionalDoors)
                    if (door != null) door.UnlockAndOpen();
            }
            if (screenStatusText != null)
            {
                screenStatusText.text = "[AUTHORIZED · AIRLOCK UNLOCKED]";
                screenStatusText.color = new Color(0.25f, 0.95f, 0.45f);
            }
            if (screenBodyText != null)
            {
                screenBodyText.color = new Color(0.7f, 0.95f, 0.75f);
            }
            if (ui != null && !string.IsNullOrEmpty(hudNotice)) ui.ShowActionNotice(hudNotice);
            if (statusLedRenderer != null && completeMaterial != null) statusLedRenderer.sharedMaterial = completeMaterial;
            if (simulation != null && (uint)deviceId != 0) simulation.RecordTerminalObjective(deviceId);
        }
    }
}

