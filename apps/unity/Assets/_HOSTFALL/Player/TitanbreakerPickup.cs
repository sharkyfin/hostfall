using Hostfall.Interaction;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.Player
{
    public sealed class TitanbreakerPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private string pickupPrompt = "[E] ВЗЯТЬ ТЯЖЁЛЫЙ ИНДУКТОР W10 TITANBREAKER";
        private bool collected;

        public string Prompt => collected ? "" : pickupPrompt;
        public bool IsCollected => collected;

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
            var model=transform.Find("W10 TITANBREAKER Inductor Model");if(model!=null)model.gameObject.SetActive(false);
        }
        public void Interact()
        {
            if(collected)return;
            var owner=FindAnyObjectByType<PlayerWeapon>();
            if(owner==null||!owner.PickupWeapon((ActiveWeaponType)2,3,9,StableId))return;
            collected=true;Hide();
        }
    }
}
