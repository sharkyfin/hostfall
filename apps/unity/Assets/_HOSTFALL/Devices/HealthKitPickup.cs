using System;
using Hostfall.Interaction;
using Hostfall.Player;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.Devices
{
    public sealed class HealthKitPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private float healAmount = 50f;
        [SerializeField] private string kitLabel = "TYPE-IV REPAIR KIT";

        private FacilityUI ui;
        private AudioSource audioSource;
        private static AudioClip clipPickup;
        private static bool audioGenerated;
        private bool isPickedUp;

        public bool IsPickedUp => isPickedUp;

        public string Prompt
        {
            get
            {
                if (isPickedUp) return "";
                var health = FindAnyObjectByType<PlayerHealth>();
                if (health != null && health.CurrentHealth >= health.MaxHealth)
                    return "[ЦЕЛОСТНОСТЬ КОРПУСА 100%]";
                return $"[E] Взять ремкомплект Type-IV (+{Mathf.CeilToInt(healAmount)}% HP)";
            }
        }

        public void Configure(float amount, string label = "TYPE-IV REPAIR KIT")
        {
            healAmount = Mathf.Max(1f, amount);
            kitLabel = label;
        }

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
                audioSource.minDistance = 1.5f;
                audioSource.maxDistance = 14.0f;
            }

            if (!audioGenerated)
            {
                const int rate = 22050;
                float duration = 0.5f;
                int samples = (int)(rate * duration);
                var data = new float[samples];
                for (int i = 0; i < samples; i++)
                {
                    float t = i / (float)rate;
                    float hum = Mathf.Sin(2f * Mathf.PI * 520f * t) * Mathf.Exp(-t * 12f);
                    float chime = Mathf.Sin(2f * Mathf.PI * 1040f * t) * Mathf.Exp(-t * 20f);
                    data[i] = Mathf.Clamp(hum * 0.7f + chime * 0.4f, -1f, 1f);
                }
                clipPickup = AudioClip.Create("SFX_Health_Pickup", samples, 1, rate, false);
                clipPickup.SetData(data, 0);
                audioGenerated = true;
            }
        }

        public void Interact()
        {
            if (isPickedUp) return;

            var health = FindAnyObjectByType<PlayerHealth>();
            if (health == null) return;

            if (health.CurrentHealth >= health.MaxHealth)
            {
                if (ui != null) ui.ShowActionNotice("Целостность корпуса уже на максимуме (100%)");
                return;
            }

            health.Heal(healAmount);
            isPickedUp = true;

            if (audioSource != null && clipPickup != null)
            {
                audioSource.PlayOneShot(clipPickup, 0.9f);
            }

            if (ui != null)
            {
                ui.ShowActionNotice($"Целостность корпуса восстановлена (+{Mathf.CeilToInt(healAmount)}%)");
            }

            // Hide mesh and collider
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;

            Destroy(gameObject, 0.6f);
        }
    }
}

