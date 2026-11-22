using Hostfall.Interaction;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.Player
{
    public sealed class WeaponPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private string pickupPrompt = "[E] ВЗЯТЬ ДРОБОВИК ARC-12";
        private bool collected;

        public string Prompt => collected ? "" : pickupPrompt;

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
            if(owner==null||!owner.PickupWeapon((ActiveWeaponType)0,6,18,StableId))return;
            collected=true;Hide();
        }
    }
}
