using Hostfall.Interaction;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.Player
{
    public sealed class PulseSidearmPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private string pickupPrompt = "[E] ВЗЯТЬ ИМПУЛЬСНЫЙ ПИСТОЛЕТ CIRRUS";
        [SerializeField] private string acquiredNotice = "Получен импульсный пистолет NORTHLINE CIRRUS · [1]/[2] Смена оружия";
        private bool collected;

        public string Prompt => collected ? "" : pickupPrompt;

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
                playerWeapon.UnlockPulseSidearm();
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

