using System;
using Hostfall.Core;
using Hostfall.Interaction;
using Hostfall.Player;
using Hostfall.Simulation.Computers;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.World
{
    public sealed class NeuroInvestigationController : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform hatch;
        [SerializeField] private GameObject key;
        [SerializeField] private TextMesh caption;
        [SerializeField] private Renderer[] monitors;
        [SerializeField] private Material playbackMaterial;
        private DetectiveSession investigation;
        private ComputerCampaign campaign;
        private ControlState controls;
        private PlayerMotor player;
        private MaterialPropertyBlock properties;
        private bool archiveActive;
        private int shownFrame = -1;
        private static readonly int FrameRect = Shader.PropertyToID("_FrameRect");

        public bool SafeOpen => campaign?.VoiceVerified == true;
        public bool HasPhysicalKey => key != null && key.activeSelf;
        public int ShownFrame => shownFrame;
        public string Prompt => !SafeOpen ? "Сейф закрыт · восстановите хронологию"
            : HasPhysicalKey ? "[E] Забрать ключ хранилища" : "Сейф пуст";

        public void Configure(Transform hatch, GameObject key, Renderer[] monitors, Material playbackMaterial, TextMesh caption)
        {
            this.hatch = hatch;
            this.key = key;
            this.monitors = monitors;
            this.playbackMaterial = playbackMaterial;
            this.caption = caption;
        }

        public void Initialize(ComputerCampaign campaign, ControlState state)
        {
            if (hatch == null || key == null || monitors.Length == 0 || playbackMaterial == null || caption == null)
                throw new InvalidOperationException("Incomplete physical investigation bindings");

            this.campaign = campaign;
            investigation = campaign.Investigation;
            controls = state;
            player = FindAnyObjectByType<PlayerMotor>();
            properties = new MaterialPropertyBlock();
            investigation.CaseVerified += RefreshState;
            investigation.KeyCollected += RefreshState;
            RefreshState();
        }

        public void Interact()
        {
            if (!controls.CanExplore || !HasPhysicalKey || !SafeOpen ||
                (player.transform.position - transform.position).sqrMagnitude > 2.8f * 2.8f)
                return;
            if (campaign.TakeArchiveKey()) RefreshState();
        }

        public void RefreshState()
        {
            if (investigation == null)
                return;

            hatch.localRotation = Quaternion.Euler(0, SafeOpen ? 112 : 0, 0);
            key.SetActive(SafeOpen && !campaign.ArchiveKeyTaken);
            if (SafeOpen && !archiveActive)
            {
                archiveActive = true;
                foreach (var monitor in monitors)
                    monitor.sharedMaterial = playbackMaterial;
            }
            UpdateMonitorFrame();
        }

        private void Update()
        {
            if (archiveActive)
                UpdateMonitorFrame();
        }

        private void UpdateMonitorFrame()
        {
            if (!archiveActive)
                return;
            int frame = Mathf.Clamp(campaign.UsesVoiceCase ? 9 : (int)(investigation.StationPlaybackTime * 2), 0, DetectiveSession.RecordingFrames - 1);
            if (shownFrame == frame)
                return;
            shownFrame = frame;
            Rect rectangle = NeuroArchiveMedia.Frame(frame);
            properties.SetVector(FrameRect, new Vector4(rectangle.x, rectangle.y, rectangle.width, rectangle.height));
            foreach (var monitor in monitors)
                monitor.SetPropertyBlock(properties);

            caption.text = frame < 4 ? "17 НОЯБРЯ / 03:04 · ПЛОМБА"
                : frame < 8 ? "17 НОЯБРЯ / 03:07 · КАРАНТИН" : "17 НОЯБРЯ / 03:17 · УДАЛЁННАЯ ПОДПИСЬ";
        }

        private void OnDestroy()
        {
            if (investigation == null)
                return;
            investigation.CaseVerified -= RefreshState;
            investigation.KeyCollected -= RefreshState;
        }
    }
}
