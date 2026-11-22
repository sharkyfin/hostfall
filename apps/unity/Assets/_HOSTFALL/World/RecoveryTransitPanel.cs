using Hostfall.Interaction;
using Hostfall.Simulation.Computers;
using UnityEngine;

namespace Hostfall.World
{
    public sealed class RecoveryTransitPanel : MonoBehaviour, IInteractable
    {
        [SerializeField] private RecoveryTransitController controller;
        [SerializeField] private TransitStop station;
        [SerializeField] private bool inCab;

        public string Prompt => controller.Prompt(station, inCab);

        public void Configure(RecoveryTransitController transit, TransitStop stop, bool inside)
        {
            controller = transit;
            station = stop;
            inCab = inside;
        }

        public void Interact() => controller.Use(station, inCab, transform);
    }
}
