using System;
using Hostfall.Interaction;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.Devices
{
    public sealed class RetractableBridgeController : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform bridgeLeaf;
        [SerializeField] private Vector3 retractedPosition;
        [SerializeField] private Vector3 extendedPosition;
        [SerializeField] private float extendDuration = 1.4f;

        [SerializeField] private Light[] bridgeLamps;
        [SerializeField] private Renderer switchRenderer;
        [SerializeField] private Material matAmber;
        [SerializeField] private Material matGreen;

        private AudioSource audioSource;
        private FacilityUI ui;
        private bool isExtended;
        private float transitionProgress;
        private bool isAnimating;

        private static AudioClip clipBreakerLatch, clipServoExtend;
        private static bool audioGenerated;

        public bool IsExtended => isExtended;
        public string Prompt => isExtended
            ? "[ПИТАНИЕ МОСТА ПОДАНО · ВЕРХНИЙ МАРШРУТ РАЗВЕРНУТ]"
            : "[E] Замкнуть шину питания моста [PDU-SEC02]";

        public void Configure(Transform leaf, Vector3 retracted, Vector3 extended, Light[] lamps = null, Renderer breakerRenderer = null, Material amber = null, Material green = null)
        {
            bridgeLeaf = leaf;
            retractedPosition = retracted;
            extendedPosition = extended;
            bridgeLamps = lamps;
            switchRenderer = breakerRenderer;
            matAmber = amber;
            matGreen = green;

            if (bridgeLeaf != null) bridgeLeaf.localPosition = retractedPosition;
            UpdateVisuals();
        }

        public void Initialize(FacilityUI facilityUI)
        {
            ui = facilityUI;
            EnsureAudio();
        }

        private void Awake()
        {
            EnsureAudio();
            if (bridgeLeaf != null && !isExtended) bridgeLeaf.localPosition = retractedPosition;
        }

        private void EnsureAudio()
        {
            if (audioSource == null)
            {
                if (!TryGetComponent<AudioSource>(out audioSource))
                    audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1.0f;
                audioSource.minDistance = 2.0f;
                audioSource.maxDistance = 22.0f;
            }

            if (!audioGenerated)
            {
                GenerateAudioClips();
                audioGenerated = true;
            }
        }

        private static void GenerateAudioClips()
        {
            const int sampleRate = 44100;

            // 1. Heavy knife-switch mechanical clunk
            int latchSamples = sampleRate / 3;
            float[] latchData = new float[latchSamples];
            for (int i = 0; i < latchSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 22f);
                float clunk = Mathf.Sin(2f * Mathf.PI * 65f * t) * 0.8f;
                float click = (UnityEngine.Random.value * 2f - 1f) * 0.3f;
                latchData[i] = (clunk + click) * env;
            }
            clipBreakerLatch = AudioClip.Create("Breaker_Latch", latchSamples, 1, sampleRate, false);
            clipBreakerLatch.SetData(latchData, 0);

            // 2. Servo / hydraulic bridge extension hum
            int servoSamples = sampleRate * 3 / 2; // 1.5s
            float[] servoData = new float[servoSamples];
            for (int i = 0; i < servoSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin(Mathf.Clamp01(t / 1.5f) * Mathf.PI);
                float hum = Mathf.Sin(2f * Mathf.PI * 95f * t) * 0.5f;
                float grind = (UnityEngine.Random.value * 2f - 1f) * 0.15f;
                servoData[i] = (hum + grind) * env * 0.6f;
            }
            clipServoExtend = AudioClip.Create("Bridge_Servo_Extend", servoSamples, 1, sampleRate, false);
            clipServoExtend.SetData(servoData, 0);
        }

        public void Interact()
        {
            if (isExtended || isAnimating) return;

            isExtended = true;
            isAnimating = true;
            transitionProgress = 0f;

            if (audioSource != null)
            {
                if (clipBreakerLatch != null) audioSource.PlayOneShot(clipBreakerLatch, 0.9f);
                if (clipServoExtend != null) audioSource.PlayOneShot(clipServoExtend, 0.7f);
            }

            UpdateVisuals();

            if (ui != null)
            {
                ui.ShowActionNotice("[OK] ВЫДВИЖНОЙ МОСТ МЕЗОНИНА АКТИВИРОВАН // ВЕРХНИЙ МАРШРУТ ОТКРЫТ");
            }
        }

        private void Update()
        {
            if (!isAnimating) return;

            transitionProgress += Time.deltaTime / Mathf.Max(0.1f, extendDuration);
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(transitionProgress));

            if (bridgeLeaf != null)
            {
                bridgeLeaf.localPosition = Vector3.Lerp(retractedPosition, extendedPosition, t);
            }

            if (transitionProgress >= 1f)
            {
                isAnimating = false;
                if (bridgeLeaf != null) bridgeLeaf.localPosition = extendedPosition;
                UpdateVisuals();
            }
        }

        private void UpdateVisuals()
        {
            if (switchRenderer != null)
            {
                if (isExtended && matGreen != null) switchRenderer.sharedMaterial = matGreen;
                else if (!isExtended && matAmber != null) switchRenderer.sharedMaterial = matAmber;
            }

            if (bridgeLamps != null)
            {
                Color targetCol = isExtended ? new Color(0.2f, 0.95f, 0.35f) : new Color(1.0f, 0.65f, 0.15f);
                foreach (var lamp in bridgeLamps)
                {
                    if (lamp != null)
                    {
                        lamp.color = targetCol;
                        lamp.intensity = isExtended ? 3.0f : 1.5f;
                    }
                }
            }
        }
    }
}

