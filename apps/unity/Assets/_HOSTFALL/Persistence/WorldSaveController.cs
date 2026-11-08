using System;
using System.IO;
using System.Linq;
using Hostfall.CameraSystem;
using Hostfall.Core;
using Hostfall.Player;
using Hostfall.Simulation;
using Hostfall.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hostfall.Persistence
{
    public sealed class WorldSaveController : MonoBehaviour
    {
        public static bool FreshStartRequested;
        public static bool ResumeRequested;
        public static GameSaveData PendingLoad;
        public static string NewGameLanguage="python";
        // A test can opt into file-backed menu flows in an isolated directory.
        public static string TestStorageDirectory;
        private FacilitySimulation model;
        private FieldSession field;
        private PlayerMotor player;
        private PlayerCameraRig cameraRig;
        private FacilityDoor door;
        private WatcherController watcher;
        private WatcherController[] enemies=Array.Empty<WatcherController>();
        private ControlState controls;
        private FacilityInput input;
        private WorldSaveStore store;
        private SaveLibrary library;
        public System.Collections.Generic.IReadOnlyList<SaveSlot> Slots => library.Slots(enabledStore);
        private float dirtyAt = -1, nextPositionSave;
        private bool started, quitting, enabledStore;
        public bool Loaded { get; private set; }
        public bool HasSave => enabledStore && library.Slots().Any(s=>s.Available);
        public string Status { get; private set; } = "";
        public event Action StatusChanged;
        public void Initialize(FacilitySimulation simulation, FieldSession progress, PlayerMotor motor, PlayerCameraRig rig,
            FacilityDoor airlock, WatcherController robot, ControlState state, FacilityInput bindings, WorldSaveStore storage, GameSaveData loaded, string notice, Func<GameSaveData,bool> validate=null,WatcherController[] additional=null)
        {
            model = simulation; field = progress; player = motor; cameraRig = rig; door = airlock; watcher = robot; controls = state; input = bindings; store = storage; library = new SaveLibrary(Path.GetDirectoryName(store.FilePath),validate);
            enemies=additional??Array.Empty<WatcherController>();
            enabledStore = (!Application.isBatchMode||TestStorageDirectory!=null) && Application.platform != RuntimePlatform.WebGLPlayer;
            Loaded = loaded != null; Status = notice;
            if (loaded != null)
            {
                player.Restore(loaded.player); cameraRig.RestoreMode(loaded.player.thirdPerson);
                door.Restore(loaded.door); watcher.Restore(loaded.watcher);
                if(loaded.enemies!=null) foreach(var saved in loaded.enemies) enemies.Single(e=>e.Id==saved.id).Restore(saved);
            }
            model.StateChanged += Dirty; field.Changed += Dirty; controls.Changed += ModeChanged;
            nextPositionSave = Time.unscaledTime + 15;
        }
        private void Start()
        {
            if (ResumeRequested) { ResumeRequested = false; UnityEngine.Object.FindAnyObjectByType<Hostfall.UI.FacilityUI>().BeginSession(); }
        }
        private void ModeChanged(ControlMode mode)
        {
            if (mode == ControlMode.Exploring) started = true;
            if ((mode == ControlMode.Paused||mode==ControlMode.MainMenu) && started && enabledStore) Save();
        }
        private void Dirty() { if (dirtyAt < 0) dirtyAt = Time.unscaledTime; }
        private void Update()
        {
            if (!enabledStore) return;
            if (started && controls.Mode!=ControlMode.MainMenu && controls.Mode!=ControlMode.Introduction && input.Save.WasPressedThisFrame()) Save("quick");
            if (started && ((dirtyAt >= 0 && Time.unscaledTime - dirtyAt > .6f) || (controls.Mode==ControlMode.Exploring && Time.unscaledTime >= nextPositionSave))) Save();
        }
        public GameSaveData Capture() => new()
        {
            simulation = model.Capture(), field = field.CaptureProgress(), player = player.Capture(cameraRig.ThirdPerson),
            door = door.Capture(), watcher = watcher.Capture(), enemies=enemies.Select(e=>e.Capture()).ToArray()
        };
        public bool Save(string slotId="auto")
        {
            if (!enabledStore) return false;
            try
            {
                library.Write(slotId,Capture()); dirtyAt = -1; nextPositionSave = Time.unscaledTime + 15;
                Status = ""; StatusChanged?.Invoke(); return true;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            { Status = "Не удалось сохранить. Предыдущая копия сохранена."; StatusChanged?.Invoke(); return false; }
        }
        public void Load(string slotId="auto")
        {
            if (!enabledStore) return;
            if (!library.Read(slotId,out var data,out string error)) { Status=error.Length>0?error:"В этом слоте пока нет игры"; StatusChanged?.Invoke(); return; }
            PendingLoad=data;
            started = false;
            ResumeRequested = true; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        public void NewGame(string language="python")
        {
            NewGameLanguage=FieldSession.NormalizeLanguage(language);
            PendingLoad=null; started = false; FreshStartRequested = true; ResumeRequested = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        private void OnApplicationFocus(bool focus) { if (!focus && started && enabledStore) Save(); }
        private void OnApplicationQuit() { quitting = true; if (started && enabledStore) Save(); }
        private void OnDisable() { if (!quitting && started && enabledStore && model != null) Save(); }
        private void OnDestroy()
        {
            if (model != null) model.StateChanged -= Dirty;
            if (field != null) field.Changed -= Dirty;
            if (controls != null) controls.Changed -= ModeChanged;
        }
    }
}
