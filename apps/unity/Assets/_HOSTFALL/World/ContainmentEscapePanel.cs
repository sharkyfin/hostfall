using Hostfall.Interaction;
using Hostfall.Simulation.Computers;
using UnityEngine;

namespace Hostfall.World
{
    public sealed class ContainmentEscapePanel : MonoBehaviour, IInteractable
    {
        [SerializeField] private ContainmentWorldController source;
        [SerializeField] private ContainmentShutter shutter;
        [SerializeField] private bool centralGate;

        public string Prompt => "[E] Аварийный выход";

        public void Configure(ContainmentWorldController controller, ContainmentShutter exit, bool fromRing = false)
        {
            source = controller;
            shutter = exit;
            centralGate = fromRing;
        }

        public void Interact() => source.RequestEscape(shutter, transform, centralGate);
    }
}
