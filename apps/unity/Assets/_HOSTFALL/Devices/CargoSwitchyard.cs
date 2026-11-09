using System;
using Hostfall.Interaction;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.Devices
{
    public static class CargoSwitchyardState
    {
        public static bool TrackSwitchSet { get; set; } = false;
        public static bool PowerInterlockArmed { get; set; } = false;
        public static bool IsCleared { get; set; } = false;

        public static void Reset()
        {
            TrackSwitchSet = false;
            PowerInterlockArmed = false;
            IsCleared = false;
        }
    }

    public sealed class CargoSwitchyard : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform cartTransform;
        [SerializeField] private Vector3 cartShiftDelta = new Vector3(-4.8f, 0, 0);
        [SerializeField] private Transform leverATransform;
        [SerializeField] private Transform leverBTransform;
        [SerializeField] private Renderer statusLed;
        [SerializeField] private Material matAmber;
        [SerializeField] private Material matGreen;
        [SerializeField] private Material matRed;

        private FacilityUI ui;
        private AudioSource audioSource;
        private static AudioClip clipKlaxon;
        private static AudioClip clipMotorRumble;
        private static AudioClip clipLeverClick;
        private static bool audioGenerated;

        private bool isMoving;
        private float moveTimer;
        private Vector3 startPos;
        private Vector3 targetPos;

        public bool IsCleared => CargoSwitchyardState.IsCleared;

        public string Prompt
        {
            get
            {
                if (CargoSwitchyardState.IsCleared)
                    return "[ГРУЗОВАЯ ПЛАТФОРМА СМЕЩЕНА НА ТУПИКОВЫЙ ПУТЬ · ПРОХОД СВОБОДЕН]";
                if (!CargoSwitchyardState.TrackSwitchSet)
                    return "[E] Перевести стрелочный перевод №1 на боковой путь";
                if (!CargoSwitchyardState.PowerInterlockArmed)
                    return "[E] Взвести силовое реле тяговой лебёдки";
                return "[E] Запустить перемещение контейнера (Шум привлечет внимание)";
            }
        }

        public void Configure(Transform cart, Vector3 shift, Transform leverA, Transform leverB, Renderer led, Material amber, Material green, Material red)
        {
            cartTransform = cart;
            cartShiftDelta = shift;
            leverATransform = leverA;
            leverBTransform = leverB;
            statusLed = led;
            matAmber = amber;
            matGreen = green;
            matRed = red;

            if (cartTransform != null)
            {
                startPos = cartTransform.localPosition;
                targetPos = startPos + cartShiftDelta;
                if (CargoSwitchyardState.IsCleared) cartTransform.localPosition = targetPos;
            }
            UpdateVisuals();
        }

        public void Initialize(FacilityUI facilityUI)
        {
            ui = facilityUI;
            EnsureAudio();
            UpdateVisuals();
        }

        private void Awake()
        {
            EnsureAudio();
            if (cartTransform != null)
            {
                startPos = cartTransform.localPosition;
                targetPos = startPos + cartShiftDelta;
                if (CargoSwitchyardState.IsCleared) cartTransform.localPosition = targetPos;
            }
            UpdateVisuals();
        }

        private void EnsureAudio()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 1.0f;
                    audioSource.minDistance = 2.5f;
                    audioSource.maxDistance = 24.0f;
                }
            }

            if (!audioGenerated)
            {
                clipKlaxon = CreateKlaxonSound();
                clipMotorRumble = CreateMotorRumbleSound();
                clipLeverClick = CreateLeverClickSound();
                audioGenerated = true;
            }
        }

        public void Interact()
        {
            if (CargoSwitchyardState.IsCleared || isMoving) return;

            if (!CargoSwitchyardState.TrackSwitchSet)
            {
                CargoSwitchyardState.TrackSwitchSet = true;
                if (leverATransform != null) leverATransform.localRotation = Quaternion.Euler(-35f, 0, 0);
                if (audioSource != null && clipLeverClick != null) audioSource.PlayOneShot(clipLeverClick, 0.8f);
                if (ui != null) ui.ShowActionNotice("Стрелка №1 переведена: боковой тупиковый путь ВЫБРАН");
                UpdateVisuals();
                return;
            }

            if (!CargoSwitchyardState.PowerInterlockArmed)
            {
                CargoSwitchyardState.PowerInterlockArmed = true;
                if (leverBTransform != null) leverBTransform.localRotation = Quaternion.Euler(-35f, 0, 0);
                if (audioSource != null && clipLeverClick != null) audioSource.PlayOneShot(clipLeverClick, 0.85f);
                if (ui != null) ui.ShowActionNotice("Силовое реле взведено · Тяговая лебёдка ГОТОВА к пуску");
                UpdateVisuals();
                return;
            }

            // Execute Shunting
            isMoving = true;
            moveTimer = 0f;
            CargoSwitchyardState.IsCleared = true;

            if (audioSource != null)
            {
                if (clipKlaxon != null) audioSource.PlayOneShot(clipKlaxon, 0.9f);
                if (clipMotorRumble != null) audioSource.PlayOneShot(clipMotorRumble, 0.85f);
            }

            if (ui != null)
            {
                ui.ShowActionNotice("Тяговая лебёдка запущена · Контейнер смещается на боковой путь!");
            }

            UpdateVisuals();
        }

        private void Update()
        {
            if (!isMoving) return;

            moveTimer += Time.deltaTime;
            float t = Mathf.Clamp01(moveTimer / 3.2f);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            if (cartTransform != null)
            {
                cartTransform.localPosition = Vector3.Lerp(startPos, targetPos, smooth);
            }

            if (t >= 1.0f)
            {
                isMoving = false;
                UpdateVisuals();
            }
        }

        public void UpdateVisuals()
        {
            if (statusLed == null) return;

            if (CargoSwitchyardState.IsCleared)
            {
                if (matGreen != null) statusLed.sharedMaterial = matGreen;
            }
            else if (CargoSwitchyardState.TrackSwitchSet && CargoSwitchyardState.PowerInterlockArmed)
            {
                if (matAmber != null) statusLed.sharedMaterial = matAmber;
            }
            else
            {
                if (matRed != null) statusLed.sharedMaterial = matRed;
            }
        }

        private static AudioClip CreateKlaxonSound()
        {
            const int sampleRate = 44100;
            const float duration = 1.0f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = t < 0.5f ? 440f : 550f;
                float s = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t));
                float env = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
                samples[i] = s * env * 0.35f;
            }

            var clip = AudioClip.Create("SFX_Cargo_Klaxon", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateMotorRumbleSound()
        {
            const int sampleRate = 44100;
            const float duration = 3.2f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float rumble = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.4f + Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.3f;
                float noise = (Mathf.PerlinNoise(t * 80f, 0f) - 0.5f) * 0.3f;
                float env = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
                samples[i] = (rumble + noise) * env * 0.5f;
            }

            var clip = AudioClip.Create("SFX_Winch_Rumble", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateLeverClickSound()
        {
            const int sampleRate = 44100;
            const float duration = 0.25f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float snap = Mathf.Sin(2f * Mathf.PI * 320f * t) * Mathf.Exp(-t * 35f);
                float crack = (Mathf.PerlinNoise(t * 1200f, 0f) - 0.5f) * Mathf.Exp(-t * 80f);
                samples[i] = (snap * 0.7f + crack * 0.4f);
            }

            var clip = AudioClip.Create("SFX_Lever_Click", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}

