using System;
using Hostfall.Interaction;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.Devices
{
    public static class DataCartridgeState
    {
        public static bool HasCartridge { get; set; }
        public static bool IsInserted { get; set; }

        public static void Reset()
        {
            HasCartridge = false;
            IsInserted = false;
        }
    }

    public sealed class DataCartridgeItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private string cartridgeId = "NL-CART-044";
        private FacilityUI ui;
        private AudioSource audioSource;
        private static AudioClip clipPickup;
        private static bool audioGenerated;

        public string Prompt => "[E] Извлечь картридж эвакуации [" + cartridgeId + "]";

        public void Initialize(FacilityUI facilityUI)
        {
            ui = facilityUI;
            EnsureAudio();
        }

        private void Awake()
        {
            EnsureAudio();
        }

        private void EnsureAudio()
        {
            if (audioSource == null)
            {
                if (!TryGetComponent<AudioSource>(out audioSource))
                    audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1.0f;
                audioSource.minDistance = 1.0f;
                audioSource.maxDistance = 15.0f;
            }

            if (!audioGenerated)
            {
                const int sampleRate = 44100;
                int samples = sampleRate / 4;
                float[] data = new float[samples];
                for (int i = 0; i < samples; i++)
                {
                    float t = (float)i / sampleRate;
                    float env = Mathf.Exp(-t * 20f);
                    float click = Mathf.Sin(2f * Mathf.PI * 1400f * t);
                    data[i] = click * env * 0.7f;
                }
                clipPickup = AudioClip.Create("Cartridge_Pickup", samples, 1, sampleRate, false);
                clipPickup.SetData(data, 0);
                audioGenerated = true;
            }
        }

        public void Interact()
        {
            if (DataCartridgeState.HasCartridge) return;

            DataCartridgeState.HasCartridge = true;

            if (audioSource != null && clipPickup != null)
            {
                audioSource.PlayOneShot(clipPickup, 0.85f);
            }

            if (ui != null)
            {
                ui.ShowActionNotice("[+] ПОЛУЧЕН КАРТРИДЖ: " + cartridgeId + " [EMERGENCY MANIFEST]");
            }

            // Hide pickup geometry from world
            gameObject.SetActive(false);
        }
    }
}

