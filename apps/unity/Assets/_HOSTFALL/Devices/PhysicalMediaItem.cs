using System;
using System.Collections.Generic;
using Hostfall.Interaction;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.Devices
{
    public static class PhysicalMediaInventory
    {
        private static readonly HashSet<string> collectedItems = new(StringComparer.OrdinalIgnoreCase);

        public static bool HasItem(string itemId) => !string.IsNullOrEmpty(itemId) && collectedItems.Contains(itemId);

        public static bool Collect(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            return collectedItems.Add(itemId);
        }

        public static string[] GetAllCollected()
        {
            var arr = new string[collectedItems.Count];
            collectedItems.CopyTo(arr);
            return arr;
        }

        public static void Restore(string[] items)
        {
            collectedItems.Clear();
            if (items != null)
            {
                foreach (var it in items)
                {
                    if (!string.IsNullOrEmpty(it)) collectedItems.Add(it);
                }
            }
        }

        public static void Reset()
        {
            collectedItems.Clear();
        }
    }

    public sealed class PhysicalMediaItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private string mediaId = "item.usb.hf_tools";
        [SerializeField] private string displayName = "НАКОПИТЕЛЬ HF-TOOLS";
        [SerializeField] private string pickupNotice = "[+] ПОЛУЧЕН СЛУЖЕБНЫЙ НАКОПИТЕЛЬ: HF-TOOLS";

        private FacilityUI ui;
        private AudioSource audioSource;
        private static AudioClip clipPickup;
        private static bool audioGenerated;

        public string MediaId => mediaId;
        public string Prompt => "[E] Подобрать " + displayName;

        public void Configure(string id, string name, string notice)
        {
            mediaId = id;
            displayName = name;
            pickupNotice = notice;
        }

        public void Initialize(FacilityUI facilityUI)
        {
            ui = facilityUI;
            EnsureAudio();
        }

        private void Start()
        {
            EnsureAudio();
            if (PhysicalMediaInventory.HasItem(mediaId))
            {
                gameObject.SetActive(false);
            }
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
                const int rate = 44100;
                int samples = rate / 4;
                float[] data = new float[samples];
                for (int i = 0; i < samples; i++)
                {
                    float t = (float)i / rate;
                    float env = Mathf.Exp(-t * 22f);
                    float click = Mathf.Sin(2f * Mathf.PI * 1800f * t);
                    data[i] = click * env * 0.7f;
                }
                clipPickup = AudioClip.Create("Media_Pickup", samples, 1, rate, false);
                clipPickup.SetData(data, 0);
                audioGenerated = true;
            }
        }

        public void Interact()
        {
            if (PhysicalMediaInventory.HasItem(mediaId)) return;

            PhysicalMediaInventory.Collect(mediaId);

            if (audioSource != null && clipPickup != null)
                audioSource.PlayOneShot(clipPickup, 0.85f);

            if (ui == null) ui = FindAnyObjectByType<FacilityUI>();
            if (ui != null)
            {
                ui.ShowActionNotice(pickupNotice);
            }

            gameObject.SetActive(false);
        }
    }
}
