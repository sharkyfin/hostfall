using System;
using Hostfall.Interaction;
using Hostfall.Simulation;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.Devices
{
    public static class UpsPowerState
    {
        private static PowerGraph graph;

        public static PowerGraph Graph => graph ??= PowerGraph.CreateSector03UpsPreset();

        public static void Reset() => graph = PowerGraph.CreateSector03UpsPreset();

        public static bool IsLiftEnergized => Graph.TryGetCircuit("lift", out var c) && c.IsEnergized;
        public static bool IsAlarmEnergized => Graph.TryGetCircuit("alarm", out var c) && c.IsEnergized;
        public static bool IsVentEnergized => Graph.TryGetCircuit("vent", out var c) && c.IsEnergized;
        public static bool IsDoorEnergized => Graph.TryGetCircuit("door", out var c) && c.IsEnergized;
    }

    public sealed class UpsPowerRouter : MonoBehaviour, IInteractable
    {
        [SerializeField] private TextMesh screenDisplay;
        [SerializeField] private Renderer[] circuitLeds;
        [SerializeField] private Material matOff;
        [SerializeField] private Material matGreen;
        [SerializeField] private Material matRed;

        private FacilityUI ui;
        private AudioSource audioSource;
        private static AudioClip clipSwitchClunk;
        private static AudioClip clipArcBuzz;
        private static bool audioGenerated;

        public PowerGraph Graph => UpsPowerState.Graph;

        public string Prompt
        {
            get
            {
                var g = Graph;
                if (g.IsOverloaded)
                {
                    return "[!] ПЕРЕГРУЗКА СЕТИ (" + g.TotalActiveLoad + "/8 БЛОКОВ) · [E] Сбросить контур сигнализации";
                }

                if (!UpsPowerState.IsLiftEnergized)
                {
                    return "[E] Перенаправить резерв UPS на грузовой лифт (Отключить сигнализацию: 3+5=8 блоков)";
                }

                return "[E] Вернуть резервное питание сигнализации шахты";
            }
        }

        public void Configure(TextMesh display, Renderer[] leds, Material offMat, Material greenMat, Material redMat)
        {
            screenDisplay = display;
            circuitLeds = leds;
            matOff = offMat;
            matGreen = greenMat;
            matRed = redMat;
            UpdateDisplay();
        }

        public void Initialize(FacilityUI facilityUI)
        {
            ui = facilityUI;
            EnsureAudio();
            UpdateDisplay();
        }

        private void Awake()
        {
            EnsureAudio();
            UpdateDisplay();
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
                    audioSource.maxDistance = 16.0f;
                }
            }

            if (!audioGenerated)
            {
                clipSwitchClunk = CreateSwitchClunkSound();
                clipArcBuzz = CreateArcBuzzSound();
                audioGenerated = true;
            }
        }

        public void Interact()
        {
            var g = Graph;

            if (!UpsPowerState.IsLiftEnergized)
            {
                // Routine: disable alarm (2 units) and enable lift (5 units). Total: 3 (vent) + 5 (lift) = 8 units!
                g.SetCircuit("alarm", false);
                g.SetCircuit("lift", true);

                if (audioSource != null)
                {
                    audioSource.PlayOneShot(clipSwitchClunk, 0.9f);
                    audioSource.PlayOneShot(clipArcBuzz, 0.6f);
                }

                if (ui != null)
                {
                    ui.ShowActionNotice("Бюджет UPS перераспределен (3+5=8 блоков) · Питание грузового лифта ПОДАНО");
                }
            }
            else
            {
                // Revert: enable alarm (2 units) and disable lift (5 units). Total: 3 + 2 = 5 units
                g.SetCircuit("lift", false);
                g.SetCircuit("alarm", true);

                if (audioSource != null)
                {
                    audioSource.PlayOneShot(clipSwitchClunk, 0.8f);
                }

                if (ui != null)
                {
                    ui.ShowActionNotice("Резервное питание возвращено сигнализации · Лифт обесточен");
                }
            }

            UpdateDisplay();
        }

        public void ToggleSpecificCircuit(string circuitId)
        {
            Graph.ToggleCircuit(circuitId);
            if (audioSource != null) audioSource.PlayOneShot(clipSwitchClunk, 0.85f);
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            var g = Graph;
            if (screenDisplay != null)
            {
                string statusStr = g.IsOverloaded ? "<color=#FF3333>OVERLOAD - TRIP</color>" : "<color=#33FF66>STABLE</color>";
                screenDisplay.text =
                    $"[UPS BATT SUB-03] LOAD: {g.TotalActiveLoad}/8 UNITS\n" +
                    $"C1: ВЕНТИЛЯЦИЯ   [3U] - {(g.TryGetCircuit("vent", out var c1) && c1.IsEnergized ? "ON" : "OFF")}\n" +
                    $"C2: СИГНАЛИЗАЦИЯ [2U] - {(g.TryGetCircuit("alarm", out var c2) && c2.IsEnergized ? "ON" : "OFF")}\n" +
                    $"C3: ГРУЗОВОЙ ЛИФТ [5U] - {(g.TryGetCircuit("lift", out var c3) && c3.IsEnergized ? "ACTIVE" : "STANDBY")}\n" +
                    $"C4: КРИО-ШЛЮЗ    [4U] - {(g.TryGetCircuit("door", out var c4) && c4.IsEnergized ? "ON" : "OFF")}\n" +
                    $"STATUS: {statusStr}";
            }

            if (circuitLeds != null && circuitLeds.Length >= 4)
            {
                string[] ids = { "vent", "alarm", "lift", "door" };
                for (int i = 0; i < 4; i++)
                {
                    if (circuitLeds[i] == null) continue;
                    if (g.TryGetCircuit(ids[i], out var circ))
                    {
                        if (g.IsOverloaded) circuitLeds[i].sharedMaterial = matRed;
                        else if (circ.IsEnergized) circuitLeds[i].sharedMaterial = matGreen;
                        else circuitLeds[i].sharedMaterial = matOff;
                    }
                }
            }
        }

        private static AudioClip CreateSwitchClunkSound()
        {
            const int sampleRate = 44100;
            const float duration = 0.35f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float body = Mathf.Sin(2f * Mathf.PI * 120f * t) * Mathf.Exp(-t * 22f);
                float click = (Mathf.PerlinNoise(t * 800f, 0f) - 0.5f) * Mathf.Exp(-t * 70f);
                samples[i] = (body * 0.7f + click * 0.5f);
            }

            var clip = AudioClip.Create("SFX_Knife_Switch_Clunk", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateArcBuzzSound()
        {
            const int sampleRate = 44100;
            const float duration = 0.45f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float hum = Mathf.Sin(2f * Mathf.PI * 100f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 200f * t);
                float crackle = (Mathf.PerlinNoise(t * 2000f, 0f) - 0.5f) * 0.4f;
                float env = Mathf.Exp(-t * 8f);
                samples[i] = (hum + crackle) * env * 0.4f;
            }

            var clip = AudioClip.Create("SFX_Electrical_Arc", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
