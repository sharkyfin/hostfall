using System;
using Hostfall.Interaction;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.Devices
{
    public sealed class DataCartridgeSlot : MonoBehaviour, IInteractable
    {
        [SerializeField] private string requiredId = "NL-CART-044";
        [SerializeField] private GameObject insertedCartridgeModel;
        [SerializeField] private Renderer statusLed;
        [SerializeField] private Material matAmber;
        [SerializeField] private Material matGreen;

        private FacilityUI ui;
        private AudioSource audioSource;
        private static AudioClip clipInsert;
        private static bool audioGenerated;

        public bool IsInserted => DataCartridgeState.IsInserted;

        public string Prompt
        {
            get
            {
                if (DataCartridgeState.IsInserted)
                    return "[КАРТРИДЖ NL-CART-044 УСТАНОВЛЕН В ПРИВОД]";
                if (DataCartridgeState.HasCartridge)
                    return "[E] Вставить картридж " + requiredId + " в считыватель";
                return "[!] ТРЕБУЕТСЯ КАРТРИДЖ " + requiredId + " [ХРАНИЛИЩЕ НА МЕЗОНИНЕ]";
            }
        }

        public void Configure(GameObject cartridgeVisual, Renderer ledRenderer = null, Material amber = null, Material green = null)
        {
            insertedCartridgeModel = cartridgeVisual;
            statusLed = ledRenderer;
            matAmber = amber;
            matGreen = green;

            if (insertedCartridgeModel != null)
            {
                insertedCartridgeModel.SetActive(DataCartridgeState.IsInserted);
            }
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
            if (insertedCartridgeModel != null)
            {
                insertedCartridgeModel.SetActive(DataCartridgeState.IsInserted);
            }
            UpdateVisuals();
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
                int samples = sampleRate / 2;
                float[] data = new float[samples];
                for (int i = 0; i < samples; i++)
                {
                    float t = (float)i / sampleRate;
                    float env = Mathf.Exp(-t * 12f);
                    float tone1 = Mathf.Sin(2f * Mathf.PI * 1046f * t); // C6
                    float tone2 = Mathf.Sin(2f * Mathf.PI * 1318f * t); // E6
                    data[i] = (tone1 + tone2) * 0.4f * env;
                }
                clipInsert = AudioClip.Create("Cartridge_Insert_Chime", samples, 1, sampleRate, false);
                clipInsert.SetData(data, 0);
                audioGenerated = true;
            }
        }

        public void Interact()
        {
            if (DataCartridgeState.IsInserted) return;

            if (!DataCartridgeState.HasCartridge)
            {
                if (ui != null)
                {
                    ui.ShowActionNotice("[!] ОШИБКА: Извлеките картридж NL-CART-044 из терминала мезонина");
                }
                return;
            }

            DataCartridgeState.IsInserted = true;

            if (insertedCartridgeModel != null)
            {
                insertedCartridgeModel.SetActive(true);
            }

            if (audioSource != null && clipInsert != null)
            {
                audioSource.PlayOneShot(clipInsert, 0.9f);
            }

            UpdateVisuals();

            if (ui != null)
            {
                ui.ShowActionNotice("[OK] КАРТРИДЖ NL-CART-044 ВАЛИДИРОВАН // СИСТЕМА ГОТОВА К АВТОРИЗАЦИИ");
            }
        }

        private void UpdateVisuals()
        {
            if (statusLed != null)
            {
                if (DataCartridgeState.IsInserted && matGreen != null) statusLed.sharedMaterial = matGreen;
                else if (!DataCartridgeState.IsInserted && matAmber != null) statusLed.sharedMaterial = matAmber;
            }
        }
    }
}

