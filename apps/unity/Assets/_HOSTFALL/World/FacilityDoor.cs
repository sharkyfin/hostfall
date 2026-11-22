using Hostfall.Core;
using Hostfall.Persistence;
using Hostfall.Interaction;
using Hostfall.Simulation;
using UnityEngine;

namespace Hostfall.World
{
    public enum DoorAccessCondition { ServerAccess, CoreTerminalComplete, AlwaysUnlocked, ManualUnlockOnly, WorkstationTerminalComplete }

    public sealed class FacilityDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private string doorId = "";
        [SerializeField] private Transform leaf;
        [SerializeField, Min(0.1f)] private float travel = 2.5f;
        [SerializeField, Min(0.1f)] private float speed = 1.6f;
        [SerializeField] private Vector3 slideDirection = Vector3.right;
        [SerializeField] private DoorAccessCondition condition = DoorAccessCondition.ServerAccess;
        [SerializeField] private string lockedPromptText = "Шлюз заблокирован · доберитесь до терминала в проходе";
        [SerializeField] private string openPromptText = "[E] Открыть шлюз серверной";
        [SerializeField] private string closePromptText = "[E] Закрыть шлюз серверной";
        [SerializeField] private bool autoOpenOnUnlock = false;
        [SerializeField] private Vector3 closed;

        private FacilitySimulation simulation;
        private bool opened;
        private bool manuallyUnlocked;
        private bool wasOpened;
        private bool? computerPower;
        public void SetComputerPower(bool powered)
        {
            if(computerPower==powered)return;computerPower=powered;
            if(powered&&!opened){opened=true;PlayDoorSound();}
            else if(!powered&&opened&&!Occupied){opened=false;PlayDoorSound();}
        }

        public string DoorId { get => !string.IsNullOrEmpty(doorId) ? doorId : name; set => doorId = value; }
        public bool IsManuallyUnlocked => manuallyUnlocked;
        public bool FullyOpen => opened && leaf != null && Vector3.Distance(leaf.localPosition, closed) >= travel - .01f;
        public void UnlockForComputer() { manuallyUnlocked = true; OnSimulationChanged(); }

        private AudioSource audioSource;
        private static AudioClip clipDoorSlide;
        private static bool audioGenerated;

        public bool IsUnlocked => computerPower ?? (condition == DoorAccessCondition.AlwaysUnlocked || manuallyUnlocked || (simulation != null && (
            (condition == DoorAccessCondition.ServerAccess && simulation.ServerAccess) ||
            (condition == DoorAccessCondition.WorkstationTerminalComplete && simulation.Terminals.IsComplete(DeviceId.Ws07)) ||
            (condition == DoorAccessCondition.CoreTerminalComplete && simulation.Terminals.IsComplete(DeviceId.Core01))
        )));

        public string Prompt => !IsUnlocked ? lockedPromptText : opened ? closePromptText : openPromptText;

        public void UnlockAndOpen()
        {
            manuallyUnlocked = true;
            if (!opened)
            {
                opened = true;
                PlayDoorSound();
            }
        }

        private void EnsureAudio()
        {
            if (audioSource == null)
            {
                if (!TryGetComponent<AudioSource>(out audioSource))
                    audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.minDistance = 2.5f;
                audioSource.maxDistance = 28f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
            }

            if (!audioGenerated)
            {
                const int rate = 22050;
                float duration = 1.1f;
                int samples = (int)(rate * duration);
                var data = new float[samples];
                for (int i = 0; i < samples; i++)
                {
                    float t = i / (float)rate;
                    // Low heavy mechanical clunk
                    float thud = Mathf.Sin(2f * Mathf.PI * 65f * t) * Mathf.Exp(-t * 8f);
                    // Pressurized pneumatic air exhaust release
                    float hiss = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 2.8f) * 0.45f;
                    // Hydraulic servo actuator hum
                    float servo = Mathf.Sin(2f * Mathf.PI * 185f * t) * Mathf.Clamp01(1f - t / duration) * 0.22f;
                    data[i] = Mathf.Clamp(thud * 0.8f + hiss + servo, -1f, 1f);
                }
                clipDoorSlide = AudioClip.Create("PneumaticDoor_Actuate", samples, 1, rate, false);
                clipDoorSlide.SetData(data, 0);
                audioGenerated = true;
            }
        }

        private void PlayDoorSound()
        {
            EnsureAudio();
            if (audioSource != null && clipDoorSlide != null)
                audioSource.PlayOneShot(clipDoorSlide, 0.85f);
        }

        public void Configure(Transform panel, DoorAccessCondition access = DoorAccessCondition.ServerAccess, string lockedMsg = null, string openMsg = null, string closeMsg = null, bool autoOpen = false, Vector3? direction = null, float? customTravel = null, float? customSpeed = null, string id = null)
        {
            leaf = panel;
            closed = panel != null ? panel.localPosition : Vector3.zero;
            condition = access;
            if (lockedMsg != null) lockedPromptText = lockedMsg;
            if (openMsg != null) openPromptText = openMsg;
            if (closeMsg != null) closePromptText = closeMsg;
            autoOpenOnUnlock = autoOpen;
            if (direction.HasValue) slideDirection = direction.Value;
            if (customTravel.HasValue) travel = customTravel.Value;
            if (customSpeed.HasValue) speed = customSpeed.Value;
            if (!string.IsNullOrEmpty(id)) doorId = id;
            EnsureAudio();
        }

        private void Awake()
        {
            if (leaf != null && closed == Vector3.zero) closed = leaf.localPosition;
            EnsureAudio();
        }

        public void Initialize(FacilitySimulation model)
        {
            simulation = model;
            if (leaf != null && closed == Vector3.zero) closed = leaf.localPosition;
            if (simulation != null) simulation.StateChanged += OnSimulationChanged;
            EnsureAudio();
            OnSimulationChanged();
        }

        private void OnDestroy()
        {
            if (simulation != null) simulation.StateChanged -= OnSimulationChanged;
        }

        private void OnSimulationChanged()
        {
            if (autoOpenOnUnlock && !opened && IsUnlocked)
            {
                opened = true;
                PlayDoorSound();
            }
        }

        public DoorStateData Capture() => new() { opened = opened, offset = Mathf.Clamp(leaf != null ? (leaf.localPosition - closed).magnitude : 0, 0, travel) };

        public DoorStateEntryData CaptureEntry() => new()
        {
            id = DoorId,
            opened = opened,
            offset = Mathf.Clamp(leaf != null ? (leaf.localPosition - closed).magnitude : 0, 0, travel),
            unlocked = IsUnlocked
        };

        public void Restore(DoorStateData state)
        {
            opened = state.opened;
            wasOpened = opened;
            if (opened) manuallyUnlocked = true;
            if (leaf != null) leaf.localPosition = closed + slideDirection.normalized * state.offset;
        }

        public void RestoreEntry(DoorStateEntryData state)
        {
            if (state == null) return;
            opened = state.opened;
            wasOpened = opened;
            if (state.unlocked || opened) manuallyUnlocked = true;
            if (leaf != null) leaf.localPosition = closed + slideDirection.normalized * state.offset;
        }

        private bool Occupied => Physics.CheckBox(transform.TransformPoint(new Vector3(0, 1.3f, 0)),
            new Vector3(1.3f, 1.3f, .5f), transform.rotation, CollisionLayers.PlayerMask, QueryTriggerInteraction.Ignore);

        public void Interact()
        {
            if (IsUnlocked && (!opened || !Occupied))
            {
                opened = !opened;
                PlayDoorSound();
            }
            else if (!IsUnlocked)
            {
                var ui = UnityEngine.Object.FindAnyObjectByType<Hostfall.UI.FacilityUI>();
                if (ui != null) ui.ShowActionNotice(lockedPromptText);
            }
        }

        private void Update()
        {
            if(computerPower==false&&opened&&!Occupied){opened=false;PlayDoorSound();}
            if (autoOpenOnUnlock && !opened && IsUnlocked)
            {
                opened = true;
                PlayDoorSound();
            }
            if (!opened && leaf != null && leaf.localPosition != closed && Occupied)
            {
                opened = true;
                PlayDoorSound();
            }
            if (leaf != null) leaf.localPosition = Vector3.MoveTowards(leaf.localPosition, closed + (opened ? slideDirection.normalized * travel : Vector3.zero), speed * Time.deltaTime);
        }
    }
}
