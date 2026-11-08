using Hostfall.Core;
using Hostfall.Persistence;
using Hostfall.Interaction;
using Hostfall.Simulation;
using UnityEngine;

namespace Hostfall.World
{
    public sealed class FacilityDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform leaf;
        [SerializeField, Min(0.1f)] private float travel = 2.5f;
        [SerializeField, Min(0.1f)] private float speed = 1.6f;
        private FacilitySimulation simulation;
        private bool opened;
        private Vector3 closed;
        public string Prompt => !simulation.ServerAccess ? "Шлюз заблокирован · доберитесь до терминала в проходе" : opened ? "[E] Закрыть шлюз серверной" : "[E] Открыть шлюз серверной";
        public void Configure(Transform panel) { leaf = panel; closed = panel.localPosition; }
        public void Initialize(FacilitySimulation model) { simulation = model; closed = leaf.localPosition; }
        public DoorStateData Capture() => new() { opened = opened, offset = Mathf.Clamp(leaf.localPosition.x - closed.x, 0, travel) };
        public void Restore(DoorStateData state) { opened = state.opened; leaf.localPosition = closed + Vector3.right * state.offset; }
        private bool Occupied => Physics.CheckBox(transform.TransformPoint(new Vector3(0, 1.3f, 0)),
            new Vector3(1.3f, 1.3f, .5f), transform.rotation, CollisionLayers.PlayerMask, QueryTriggerInteraction.Ignore);
        public void Interact() { if (simulation.ServerAccess && (!opened || !Occupied)) opened = !opened; }
        private void Update()
        {
            if (!opened && leaf != null && leaf.localPosition != closed && Occupied) opened = true;
            if (leaf != null) leaf.localPosition = Vector3.MoveTowards(leaf.localPosition, closed + (opened ? Vector3.right * travel : Vector3.zero), speed * Time.deltaTime);
        }
    }
}
