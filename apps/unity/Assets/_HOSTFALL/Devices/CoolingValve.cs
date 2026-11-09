using System;
using Hostfall.Interaction;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.Devices
{
    public static class CoolingValveState
    {
        public static bool IsOpen { get; set; } = false;

        public static void Reset() => IsOpen = false;
    }

    public sealed class CoolingValve : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform wheelTransform;
        [SerializeField] private Transform gaugeNeedleTransform;
        [SerializeField] private Renderer indicatorLed;
        [SerializeField] private Material matAmber;
        [SerializeField] private Material matGreen;
        [SerializeField] private TextMesh gaugeText;

        private FacilityUI ui;
        private AudioSource audioSource;
        private static AudioClip clipValveTurn;
        private static AudioClip clipSteamHiss;
        private static bool audioGenerated;

        private bool isAnimating;
        private float animationTimer;
        private Quaternion startWheelRot;
        private Quaternion targetWheelRot;
        private Quaternion startNeedleRot;
        private Quaternion targetNeedleRot;

        public bool IsOpen => CoolingValveState.IsOpen;

        public string Prompt => CoolingValveState.IsOpen
            ? "[КЛАПАН ОХЛАЖДЕНИЯ V-102 ОТКРЫТ · ПОТОК: 420 Л/С]"
            : "[E] Повернуть вентиль байпаса охлаждения V-102";

        public void Configure(Transform wheel, Transform needle, Renderer led, Material amber, Material green, TextMesh text = null)
        {
            wheelTransform = wheel;
            gaugeNeedleTransform = needle;
            indicatorLed = led;
            matAmber = amber;
            matGreen = green;
            gaugeText = text;
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
                    audioSource.minDistance = 2.0f;
                    audioSource.maxDistance = 18.0f;
                    audioSource.rolloffMode = AudioRolloffMode.Linear;
                }
            }

            if (!audioGenerated)
            {
                clipValveTurn = CreateValveTurnSound();
                clipSteamHiss = CreateSteamHissSound();
                audioGenerated = true;
            }
        }

        public void Interact()
        {
            if (CoolingValveState.IsOpen || isAnimating) return;

            CoolingValveState.IsOpen = true;
            isAnimating = true;
            animationTimer = 0f;

            if (wheelTransform != null)
            {
                startWheelRot = wheelTransform.localRotation;
                targetWheelRot = startWheelRot * Quaternion.Euler(0, 0, 720f);
            }

            if (gaugeNeedleTransform != null)
            {
                startNeedleRot = gaugeNeedleTransform.localRotation;
                targetNeedleRot = Quaternion.Euler(0, 0, -110f); // Rotates from red (overheat) to green (safe)
            }

            if (audioSource != null && clipValveTurn != null)
            {
                audioSource.PlayOneShot(clipValveTurn, 0.9f);
                audioSource.PlayOneShot(clipSteamHiss, 0.75f);
            }

            if (ui != null)
            {
                ui.ShowActionNotice("Контур охлаждения V-102 открыт · Температура реактора падает: 842 K -> 295 K");
            }

            UpdateVisuals();
        }

        private void Update()
        {
            if (!isAnimating) return;

            animationTimer += Time.deltaTime;
            float t = Mathf.Clamp01(animationTimer / 1.4f);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            if (wheelTransform != null)
            {
                wheelTransform.localRotation = Quaternion.Slerp(startWheelRot, targetWheelRot, smooth);
            }

            if (gaugeNeedleTransform != null)
            {
                gaugeNeedleTransform.localRotation = Quaternion.Slerp(startNeedleRot, targetNeedleRot, smooth);
            }

            if (gaugeText != null)
            {
                int currentTemp = (int)Mathf.Lerp(842f, 295f, smooth);
                gaugeText.text = $"TEMP: {currentTemp} K\nFLOW: {(int)(420f * smooth)} L/S\nSTATUS: {(t > 0.8f ? "COOLING NOMINAL" : "STABILIZING")}";
                gaugeText.color = Color.Lerp(new Color(1f, 0.25f, 0.15f), new Color(0.2f, 0.9f, 0.4f), smooth);
            }

            if (t >= 1.0f)
            {
                isAnimating = false;
                UpdateVisuals();
            }
        }

        public void UpdateVisuals()
        {
            bool open = CoolingValveState.IsOpen;
            if (indicatorLed != null)
            {
                if (open && matGreen != null) indicatorLed.sharedMaterial = matGreen;
                else if (!open && matAmber != null) indicatorLed.sharedMaterial = matAmber;
            }

            if (gaugeText != null && !isAnimating)
            {
                if (open)
                {
                    gaugeText.text = "TEMP: 295 K\nFLOW: 420 L/S\nSTATUS: COOLING NOMINAL";
                    gaugeText.color = new Color(0.2f, 0.9f, 0.4f);
                }
                else
                {
                    gaugeText.text = "TEMP: 842 K [ALARM]\nFLOW: 0 L/S\nSTATUS: OVERHEAT";
                    gaugeText.color = new Color(1f, 0.25f, 0.15f);
                }
            }

            if (!isAnimating && gaugeNeedleTransform != null)
            {
                gaugeNeedleTransform.localRotation = open ? Quaternion.Euler(0, 0, -110f) : Quaternion.Euler(0, 0, 45f);
            }
        }

        private static AudioClip CreateValveTurnSound()
        {
            const int sampleRate = 44100;
            const float duration = 1.2f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float groan = Mathf.Sin(2f * Mathf.PI * 95f * t) * Mathf.Sin(2f * Mathf.PI * 185f * t);
                float squeak = Mathf.Sin(2f * Mathf.PI * 740f * t) * (Mathf.PerlinNoise(t * 40f, 0f) - 0.5f);
                float env = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
                samples[i] = (groan * 0.5f + squeak * 0.4f) * env;
            }

            var clip = AudioClip.Create("SFX_Valve_Turn", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateSteamHissSound()
        {
            const int sampleRate = 44100;
            const float duration = 1.6f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];
            var rng = new System.Random(42);

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float noise = ((float)rng.NextDouble() * 2f - 1f);
                float env = t < 0.2f ? t / 0.2f : Mathf.Exp(-(t - 0.2f) * 1.8f);
                samples[i] = noise * env * 0.45f;
            }

            var clip = AudioClip.Create("SFX_Steam_Hiss", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}

