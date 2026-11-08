using System;
using Hostfall.Core;
using Hostfall.Player;
using UnityEngine;

namespace Hostfall.Interaction
{
    public sealed class InteractionScanner : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float reach = 2.8f;
        private PlayerMotor player;
        private FacilityInput input;
        private ControlState controls;
        private UnityEngine.Camera view;
        private string lastPrompt;
        private IInteractable previousTarget;
        public event Action<string> PromptChanged;
        public void Initialize(PlayerMotor motor, FacilityInput bindings, ControlState state, UnityEngine.Camera outputCamera)
        { player = motor; input = bindings; controls = state; view = outputCamera; }
        private void Update()
        {
            if (player == null) return;
            IInteractable target = null;
            if (controls.CanExplore && Physics.Raycast(view.transform.position, view.transform.forward, out var hit,
                    reach + Vector3.Distance(view.transform.position, player.Eye), CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore)
                && Vector3.Distance(player.Eye, hit.point) <= reach)
            {
                target = hit.collider.GetComponentInParent<IInteractable>();
                // A TPS camera can see around a corner; the operator still needs a clear physical line to the device.
                if (target != null && Physics.Linecast(player.Eye, hit.point, out var obstruction,
                        CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore)
                    && obstruction.collider.GetComponentInParent<IInteractable>() != target) target = null;
            }
            string prompt = target?.Prompt ?? "";
            if (prompt != lastPrompt) { lastPrompt = prompt; PromptChanged?.Invoke(prompt); }
            if (controls.Mode != ControlMode.Exploring) return;
            if (target != previousTarget) { previousTarget = target; }
            if (target != null && controls.CanExplore && input.Interact.WasPressedThisFrame())
            {
                target.Interact();
            }
        }
    }
}
