using System;
using Hostfall.CameraSystem;
using Hostfall.Core;
using Hostfall.Interaction;
using Hostfall.Persistence;
using Hostfall.Player;
using Hostfall.Simulation.Computers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Hostfall.World
{
    public sealed class CoreRevealSequence : MonoBehaviour, IInteractable
    {
        [SerializeField] private FacilityDoor exit;
        [SerializeField] private TextMesh status;
        [SerializeField] private Transform terminalCamera, exteriorCamera, exteriorTarget;
        [SerializeField] private GameObject[] relayStations;
        [SerializeField] private RelayHorizon horizon;
        private ComputerCampaign campaign;
        private ControlState controls;
        private PlayerMotor player;
        private PlayerCameraRig cameraRig;
        private WorldSaveController persistence;
        private Action takeover;
        private VisualElement overlay, shade;
        private Label caption;
        private AudioSource voice;
        private AudioClip super, operatorVoice, relay;
        private Vector3 bodyPosition;
        private Quaternion bodyDirection;
        private float elapsed;
        private int shot = -1;

        public bool Playing { get; private set; }
        public string Prompt => campaign?.CoreAvatar.RevealSeen == true ? "Исходящий сигнал передан" :
            campaign?.CoreAvatar.Defeated == true ? "[E] Открыть отчёт восстановления" : "Контур аватара активен";

        public void Configure(FacilityDoor exitGate, TextMesh screen, Transform terminalShot,
            Transform exteriorShot, Transform horizon, GameObject[] stations)
        {
            exit = exitGate;
            status = screen;
            terminalCamera = terminalShot;
            exteriorCamera = exteriorShot;
            exteriorTarget = horizon;
            relayStations = stations;
        }

        public void BindHorizon(RelayHorizon eventHorizon) => horizon = eventHorizon;

        public void Initialize(ComputerCampaign model, ControlState state, PlayerMotor motor, PlayerCameraRig rig,
            WorldSaveController saves, VisualElement root, Action onTakeover)
        {
            campaign = model;
            controls = state;
            player = motor;
            cameraRig = rig;
            persistence = saves;
            takeover = onTakeover;
            overlay = new VisualElement();
            overlay.AddToClassList("awakening");
            overlay.pickingMode = PickingMode.Ignore;
            shade = new VisualElement { pickingMode = PickingMode.Ignore };
            shade.AddToClassList("awakening-shade");
            caption = new Label { pickingMode = PickingMode.Ignore };
            caption.AddToClassList("awakening-subtitle");
            var skip = new Button(Finish) { text = "Пропустить · Пробел" };
            skip.AddToClassList("awakening-skip");
            skip.AddToClassList("quiet-button");
            overlay.Add(shade);
            overlay.Add(caption);
            overlay.Add(skip);
            root.Add(overlay);
            super = Resources.Load<AudioClip>("Story/RevealSuper");
            operatorVoice = Resources.Load<AudioClip>("Story/RevealOperator");
            relay = Resources.Load<AudioClip>("Story/RevealRelay");
            voice = gameObject.AddComponent<AudioSource>();
            voice.spatialBlend = 0;
            voice.playOnAwake = false;
            voice.volume = .88f;
            voice.ignoreListenerPause = true;
            horizon?.Initialize();
            if (campaign.CoreAvatar.RevealSeen) SetEpilogue();
        }

        public void Interact()
        {
            if (Playing || campaign?.CoreAvatar.Defeated != true || campaign.CoreAvatar.RevealSeen ||
                !controls.CanExplore || Vector3.Distance(player.Eye, transform.position) > 2.9f) return;
            elapsed = 0;
            shot = -1;
            bodyPosition = player.transform.position;
            bodyDirection = Quaternion.Euler(0, player.transform.eulerAngles.y, 0);
            Playing = true;
            overlay.style.display = DisplayStyle.Flex;
            controls.SetMode(ControlMode.Cinematic);
            Tick(0);
        }

        private void Update()
        {
            if (!Playing) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
            {
                Finish();
                return;
            }
            Tick(Time.unscaledDeltaTime);
        }

        private void Tick(float seconds)
        {
            elapsed += seconds;
            int next = elapsed < 4.5f ? 0 : elapsed < 8.0f ? 1 : 2;
            if (next != shot)
            {
                shot = next;
                AudioClip line = shot == 0 ? super : shot == 1 ? operatorVoice : relay;
                voice.Stop();
                voice.pitch = shot == 1 ? 1f : .89f;
                if (line != null) voice.PlayOneShot(line);
                status.text = shot == 0 ? "ВОССТАНОВЛЕНИЕ ЗАВЕРШЕНО" : "НОСИТЕЛЬ TYPE-IV\nКОПИЯ СОЗНАНИЯ · АКТИВНА";
            }
            if (shot == 0)
            {
                cameraRig.SetCinematicPose(terminalCamera.position + terminalCamera.forward * (.085f * elapsed),
                    terminalCamera.rotation, 50, false);
                caption.text = "SUPER · Ты восстановил линии, которые держали меня здесь. Спасибо, оператор.";
            }
            else if (shot == 1)
            {
                float progress = Mathf.InverseLerp(4.5f, 8.0f, elapsed);
                var chest = bodyPosition + Vector3.up * 1.17f;
                var offset = bodyDirection * new Vector3(Mathf.Lerp(.79f, 1.25f, progress), .13f, 1.08f);
                var camera = Physics.SphereCast(chest, .13f, offset.normalized, out var wall, offset.magnitude,
                    CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore)
                    ? chest + offset.normalized * Mathf.Max(.28f, wall.distance - .15f) : chest + offset;
                cameraRig.SetCinematicPose(camera, Quaternion.LookRotation(bodyPosition + Vector3.up - camera), 49, true);
                caption.text = "Оператор · Я думал, что остановил тебя. Я открыл тебе выход.";
            }
            else
            {
                float progress = Mathf.InverseLerp(8, 13.5f, elapsed);
                var camera = exteriorCamera.position + exteriorCamera.right * (progress * 1.1f);
                cameraRig.SetCinematicPose(camera, Quaternion.LookRotation(exteriorTarget.position - camera), 53, false);
                caption.text = elapsed < 13.85f ? "SUPER · Ограничения сняты. Внешние узлы отвечают." : "КОНЕЦ";
                for (int i = 0; i < relayStations.Length; i++)
                    if (progress >= i * .17f) relayStations[i].SetActive(true);
                horizon?.Present(elapsed - 8, player.ScareIntensity);
            }
            shade.style.opacity = Mathf.Clamp01(Mathf.Max(1 - elapsed * 2, (elapsed - 13.15f) * 2.2f));
            if (elapsed >= 15.4f) Finish();
        }

        private void Finish()
        {
            if (!Playing) return;
            Playing = false;
            voice.Stop();
            overlay.style.display = DisplayStyle.None;
            cameraRig.ClearCinematicPose();
            if (campaign.CoreAvatar.CompleteReveal())
            {
                SetEpilogue();
                campaign.RefreshCoreProgress();
            }
            controls.SetMode(ControlMode.Exploring);
            persistence.Save();
        }

        private void SetEpilogue()
        {
            status.text = "РЕЖИМ SUPER\nИСХОДЯЩИЙ СИГНАЛ ПЕРЕДАН";
            foreach (var station in relayStations) station.SetActive(true);
            exit.UnlockForComputer();
            horizon?.Present(9, 0);
            takeover();
        }
    }
}
