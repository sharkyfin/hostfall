using System;
using Hostfall.Interaction;
using Hostfall.Player;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.Devices
{
    public sealed class AmmoBoxPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private int ammoAmount = 12;
        [SerializeField] private string ammoLabel = "12-GA EMP AMMO";

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
                var weapon = FindAnyObjectByType<PlayerWeapon>();
                if (weapon != null && weapon.IsAmmoFull)
                    return "[БОЕКОМПЛЕКТ ARC-12 ПОЛОН (МАКСИМУМ 18 ШТ.)]";
                return $"[E] Взять патроны ARC-12 (+{ammoAmount} шт.)";
            }
        }

        public void Configure(int amount, string label = "12-GA EMP AMMO")
        {
            ammoAmount = Mathf.Max(1, amount);
            ammoLabel = label;
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
                float duration = 0.45f;
                int samples = (int)(rate * duration);
                var data = new float[samples];
                for (int i = 0; i < samples; i++)
                {
                    float t = i / (float)rate;
                    float latch = Mathf.Sin(2f * Mathf.PI * 480f * t) * Mathf.Exp(-t * 28f);
                    float clink = Mathf.Sin(2f * Mathf.PI * 1420f * t) * Mathf.Exp(-t * 18f);
                    float metal = (Mathf.PerlinNoise(t * 1200f, 0f) - 0.5f) * Mathf.Exp(-t * 35f);
                    data[i] = Mathf.Clamp(latch * 0.6f + clink * 0.5f + metal * 0.3f, -1f, 1f);
                }
                clipPickup = AudioClip.Create("SFX_Ammo_Pickup", samples, 1, rate, false);
                clipPickup.SetData(data, 0);
                audioGenerated = true;
            }
        }

        public void Interact()
        {
            if (isPickedUp) return;

            var weapon = FindAnyObjectByType<PlayerWeapon>();
            if (weapon == null) return;

            if (weapon.IsAmmoFull)
            {
                if (ui != null) ui.ShowActionNotice("Боезапас дробовика ARC-12 полон (18 патронов в резерве)");
                return;
            }

            int added = weapon.AddAmmo(ammoAmount);
            isPickedUp = true;

            if (audioSource != null && clipPickup != null)
            {
                audioSource.PlayOneShot(clipPickup, 0.9f);
            }

            if (ui != null)
            {
                ui.ShowActionNotice($"Получены патроны для дробовика ARC-12 (+{added} шт.)");
            }

            // Hide mesh and collider
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;

            Destroy(gameObject, 0.6f);
        }
    }
}

