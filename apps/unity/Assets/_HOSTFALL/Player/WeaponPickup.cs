using Hostfall.Interaction;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.Player
{
    public sealed class WeaponPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private string pickupPrompt = "[E] ВЗЯТЬ ДРОБОВИК ARC-12";
        [SerializeField] private string acquiredNotice = "Получен импульсный дробовик NORTHLINE ARC-12 · ЛКМ: Выстрел";
        private bool collected;

        public string Prompt => collected ? "" : pickupPrompt;

        public void Interact()
        {
            if (collected) return;
            collected = true;

            var playerWeapon = FindAnyObjectByType<PlayerWeapon>();
            if (playerWeapon != null)
            {
                playerWeapon.Equip();
            }

            var ui = FindAnyObjectByType<FacilityUI>();
            if (ui != null && !string.IsNullOrEmpty(acquiredNotice))
            {
                ui.ShowActionNotice(acquiredNotice);
            }

            // Disable pickup visuals / interaction
            gameObject.SetActive(false);
        }
    }
}

