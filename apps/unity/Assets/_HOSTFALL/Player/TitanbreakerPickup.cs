using Hostfall.Interaction;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.Player
{
    public sealed class TitanbreakerPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private string pickupPrompt = "[E] ВЗЯТЬ ТЯЖЁЛЫЙ ИНДУКТОР W10 TITANBREAKER";
        [SerializeField] private string acquiredNotice = "Получен W10 TITANBREAKER Heavy Inductor · [1]/[2]/[3] Смена оружия · Урон 120";
        private bool collected;

        public string Prompt => collected ? "" : pickupPrompt;
        public bool IsCollected => collected;

        public void Configure(string prompt, string notice)
        {
            pickupPrompt = prompt;
            acquiredNotice = notice;
        }

        public void Interact()
        {
            if (collected) return;
            collected = true;

            var playerWeapon = FindAnyObjectByType<PlayerWeapon>();
            if (playerWeapon != null)
            {
                playerWeapon.UnlockTitanbreaker();
            }

            var ui = FindAnyObjectByType<FacilityUI>();
            if (ui != null && !string.IsNullOrEmpty(acquiredNotice))
            {
                ui.ShowActionNotice(acquiredNotice);
            }

            gameObject.SetActive(false);
        }
    }
}

