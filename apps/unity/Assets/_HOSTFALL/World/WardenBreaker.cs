using Hostfall.Interaction;
using UnityEngine;

namespace Hostfall.World
{
    public sealed class WardenBreaker : MonoBehaviour, IInteractable
    {
        [SerializeField] private FacilityMiniboss warden;
        [SerializeField] private Transform lever;
        public void Configure(FacilityMiniboss guardian, Transform handle) { warden = guardian; lever = handle; }
        public string Prompt => warden.Battle?.PowerCut == true ? "Генератор щита отключён" : "[E] Снять питание щита";
        public void Interact()
        {
            if (warden.CutShield()) RefreshLever();
        }
        private void Start() => RefreshLever();
        private void RefreshLever()
        {
            if (warden.Battle?.PowerCut == true) lever.localRotation = Quaternion.Euler(28, 0, 0);
        }
    }
}
