using Hostfall.CameraSystem;
using Hostfall.Devices;
using Hostfall.Field;
using Hostfall.Interaction;
using Hostfall.Missions;
using Hostfall.Player;
using Hostfall.Persistence;
using System;
using System.IO;
using System.Linq;
using Hostfall.Simulation;
using Hostfall.UI;
using Hostfall.World;
using Unity.Cinemachine;
using UnityEngine;

namespace Hostfall.Core
{
    public sealed class FacilityRoot : MonoBehaviour
    {
        [SerializeField] private IncidentMissionAsset mission;
        [SerializeField] private ControlState controls;
        [SerializeField] private FacilityInput input;
        [SerializeField] private PlayerMotor player;
        [SerializeField] private PlayerCameraRig rig;
        [SerializeField] private CinemachineCamera fps;
        [SerializeField] private CinemachineCamera tps;
        [SerializeField] private CinemachineBrain brain;
        [SerializeField] private GameObject playerVisual;
        [SerializeField] private InteractionScanner scanner;
        [SerializeField] private FacilityUI ui;
        [SerializeField] private ComputerTerminal[] computers;
        [SerializeField] private FacilityDoor door;
        [SerializeField] private PowerSwitch power;
        [SerializeField] private FieldSystemUI fieldUI;
        [SerializeField] private WatcherController watcher;
        [SerializeField] private WatcherController[] enemies;
        [SerializeField] private FieldDocument[] documents;
        [SerializeField] private WorldSaveController persistence;
        public WorldSaveController Persistence => persistence;
        public FieldSession Field { get; private set; }
        public FacilitySimulation Simulation { get; private set; }
        public void Configure(ControlState state, FacilityInput bindings, PlayerMotor motor, PlayerCameraRig cameras,
            CinemachineCamera first, CinemachineCamera third, CinemachineBrain cameraBrain, GameObject visual,
            InteractionScanner interaction, FacilityUI interfaceView, ComputerTerminal[] terminals, FacilityDoor serverDoor, PowerSwitch powerSwitch, IncidentMissionAsset missionDefinition, FieldSystemUI fieldView, WatcherController observer, FieldDocument[] notes, WorldSaveController saveController,WatcherController[] additional=null)
        {
            mission = missionDefinition; controls = state; input = bindings; player = motor; rig = cameras; fps = first; tps = third; brain = cameraBrain;
            playerVisual = visual; scanner = interaction; ui = interfaceView; computers = terminals; door = serverDoor; power = powerSwitch; fieldUI = fieldView; watcher = observer; documents = notes; persistence = saveController;
            enemies=additional??Array.Empty<WatcherController>();
        }
        private void Awake()
        {
            var data = JsonUtility.FromJson<FieldContentData>(Resources.Load<TextAsset>("FieldContent").text);
            var catalog = new FieldCatalog(data);
            string storagePath=Application.isBatchMode&&WorldSaveController.TestStorageDirectory!=null?WorldSaveController.TestStorageDirectory:Application.persistentDataPath;
            var store = new WorldSaveStore(storagePath);
            GameSaveData saved = null; string notice = "";
            bool fresh = WorldSaveController.FreshStartRequested; WorldSaveController.FreshStartRequested = false;
            bool Valid(GameSaveData data)
                {
                    var candidate = FacilitySimulation.Restore(data.simulation, mission.CreateDefinition());
                    using var progress = new FieldSession(catalog, candidate); progress.RestoreProgress(data.field);
                    SaveValidation.Require(!data.door.opened || candidate.ServerAccess, "Door conflicts with saved access");
                    return true;
                }
            if ((!Application.isBatchMode||WorldSaveController.TestStorageDirectory!=null) && Application.platform != RuntimePlatform.WebGLPlayer && !fresh)
            {
                if (WorldSaveController.PendingLoad!=null)
                { saved=WorldSaveController.PendingLoad; WorldSaveController.PendingLoad=null; Valid(saved); }
                else if (!store.TryLoad(out saved, out notice, Valid) && notice.Length > 0)
                    try { store.PreserveUnreadableFiles(); } catch (Exception error) when (error is IOException || error is UnauthorizedAccessException) { }
            }
            Simulation = saved == null ? new FacilitySimulation(mission.CreateDefinition()) : FacilitySimulation.Restore(saved.simulation, mission.CreateDefinition());
            Field = new FieldSession(catalog, Simulation, fresh?WorldSaveController.NewGameLanguage:"python");
            if (saved != null) Field.RestoreProgress(saved.field);
            foreach (var note in catalog.Documents.Values)
            {
                if (saved != null) continue;
                DeviceId host = note.id == "shift-note" ? DeviceId.Ws07 : note.id == "maintenance-note" ? DeviceId.Edge01 : DeviceId.Core01;
                Simulation[host].WriteFile("/home/operator/" + note.id + ".txt", note.body);
            }
            if (saved == null && !Application.isBatchMode && !fresh)
            {
                string legacy = Path.Combine(Application.persistentDataPath, "archive-v1.json");
                try { if (File.Exists(legacy)) Field.Restore(JsonUtility.FromJson<ArchiveSnapshot>(File.ReadAllText(legacy))); }
                catch (Exception error) when (error is IOException || error is ArgumentException || error is UnauthorizedAccessException) { }
            }
            input.Initialize(controls); player.Initialize(input, controls,Simulation.Actions);
            rig.Initialize(player, input, controls, playerVisual, fps, tps, brain);
            fieldUI.Initialize(Field, controls, input, ui.GetComponent<UnityEngine.UIElements.UIDocument>().rootVisualElement);
            scanner.Initialize(player, input, controls, brain.GetComponent<UnityEngine.Camera>());
            scanner.PromptChanged += ui.SetInteractionPrompt;
            foreach (var computer in computers) computer.Initialize(Simulation, ui.OpenTerminal);
            door.Initialize(Simulation);
            foreach (var d in FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None))
                if (d != door) d.Initialize(Simulation);
            power.Initialize(Simulation);
            watcher.Initialize(Simulation, player, controls);
            var allWatchers = FindObjectsByType<WatcherController>(FindObjectsSortMode.None);
            foreach (var w in allWatchers)
            {
                if (w != watcher && !enemies.Contains(w))
                    w.Initialize(Simulation, player, controls);
            }
            foreach(var enemy in enemies) enemy.Initialize(Simulation,player,controls);
            foreach (var note in documents) note.Initialize(fieldUI);
            var nonPlayerWatchers = allWatchers.Where(w => w != watcher).ToArray();
            persistence.Initialize(Simulation, Field, player, rig, door, watcher, controls, input, store, saved, notice, Valid, nonPlayerWatchers.Length > 0 ? nonPlayerWatchers : enemies);
            ui.Initialize(Simulation, controls, input, Field, fieldUI, persistence);
            foreach (var dtc in FindObjectsByType<DoorTerminalConsole>(FindObjectsSortMode.None))
                dtc.Initialize(ui, Simulation);
            foreach (var drone in FindObjectsByType<LensDroneController>(FindObjectsSortMode.None))
                drone.Initialize(player, ui, allWatchers);
            foreach (var bridge in FindObjectsByType<RetractableBridgeController>(FindObjectsSortMode.None))
                bridge.Initialize(ui);
            foreach (var cartridge in FindObjectsByType<DataCartridgeItem>(FindObjectsSortMode.None))
                cartridge.Initialize(ui);
            foreach (var slot in FindObjectsByType<DataCartridgeSlot>(FindObjectsSortMode.None))
                slot.Initialize(ui);
            foreach (var valve in FindObjectsByType<CoolingValve>(FindObjectsSortMode.None))
                valve.Initialize(ui);
            foreach (var router in FindObjectsByType<UpsPowerRouter>(FindObjectsSortMode.None))
                router.Initialize(ui);
            foreach (var yard in FindObjectsByType<CargoSwitchyard>(FindObjectsSortMode.None))
                yard.Initialize(ui);
            foreach (var ammo in FindObjectsByType<AmmoBoxPickup>(FindObjectsSortMode.None))
                ammo.Initialize(ui);
            foreach (var kit in FindObjectsByType<HealthKitPickup>(FindObjectsSortMode.None))
                kit.Initialize(ui);
            foreach (var crawler in FindObjectsByType<TrackedSentryController>(FindObjectsSortMode.None))
                crawler.Initialize(player, ui, controls);
            var weapon = player.GetComponent<PlayerWeapon>() ?? player.gameObject.AddComponent<PlayerWeapon>();
            weapon.Initialize(player, input, controls, ui);
            var death = player.GetComponent<DeathController>() ?? player.gameObject.AddComponent<DeathController>();
            death.Initialize(controls, ui, Simulation, persistence, player);
            var health = player.GetComponent<PlayerHealth>() ?? player.gameObject.AddComponent<PlayerHealth>();
            health.Initialize(controls, ui, death);
            ui.BindPlayerHealth(health, death);
            persistence.BindHealth(health, saved?.health);
            var actionController=gameObject.AddComponent<FacilityActionController>();
            actionController.Initialize(Simulation,player,controls,input,ui,allWatchers);
        }
        private void OnDestroy() { Field?.Dispose(); if (scanner != null && ui != null) scanner.PromptChanged -= ui.SetInteractionPrompt; }
    }
}
