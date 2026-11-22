using Hostfall.Interaction;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.Player
{
    public sealed class PulseSidearmPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private string pickupPrompt = "[E] ВЗЯТЬ ИМПУЛЬСНЫЙ ПИСТОЛЕТ CIRRUS";
        private bool collected;

        public string Prompt => collected ? "" : pickupPrompt;

        public void Configure(string prompt, string notice)
        {
            pickupPrompt = prompt;
        }

        private string StableId=>DroppedWeapon.AuthoredId(transform);
        private void Start()
        {
            var owner=FindAnyObjectByType<PlayerWeapon>();
            if(owner!=null&&owner.AuthoredWasCollected(StableId)){collected=true;Hide();}
        }
        private void Hide()
        {
            gameObject.SetActive(false);
        }
        public void Interact()
        {
            if(collected)return;
            var owner=FindAnyObjectByType<PlayerWeapon>();
            if(owner==null||!owner.PickupWeapon((ActiveWeaponType)1,15,45,StableId))return;
            collected=true;Hide();
        }
    }
}
