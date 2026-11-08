using Hostfall.Core;
using Hostfall.Player;
using Unity.Cinemachine;
using UnityEngine;

namespace Hostfall.CameraSystem
{
    [DefaultExecutionOrder(100)]
    public sealed class PlayerCameraRig : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float thirdPersonDistance = 2.7f;
        [SerializeField, Min(0.05f)] private float collisionRadius = 0.22f;
        [SerializeField] private Vector2 shoulderOffset = new(.45f, .3f);
        private PlayerMotor player;
        private FacilityInput input;
        private ControlState controls;
        private CinemachineCamera first;
        private CinemachineCamera third;
        private CinemachineBrain brain;
        private OperatorVisual body;
        private float distance;
        public bool ThirdPerson { get; private set; }
        public void Initialize(PlayerMotor motor, FacilityInput bindings, ControlState state, GameObject visual,
            CinemachineCamera fps, CinemachineCamera tps, CinemachineBrain cameraBrain)
        {
            player = motor; input = bindings; controls = state; body = visual.GetComponent<OperatorVisual>(); first = fps; third = tps; brain = cameraBrain;
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
            // A cut avoids interpolating the camera through a wall during mode switches.
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0);
            ApplyMode();
        }
        private void LateUpdate()
        {
            if (player == null) return;
            if (controls.CanExplore && input.SwitchCamera.WasPressedThisFrame()) { ThirdPerson = !ThirdPerson; ApplyMode(); }
            var rotation = player.ViewRotation; var eye = player.Eye;
            first.transform.SetPositionAndRotation(eye, rotation);
            Vector3 offset = rotation * new Vector3(shoulderOffset.x, shoulderOffset.y, -thirdPersonDistance);
            Vector3 back = offset.normalized;
            float allowed = offset.magnitude;
            if (Physics.SphereCast(eye, collisionRadius, back, out var hit, allowed, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore))
                allowed = Mathf.Max(0, hit.distance - 0.04f);
            distance = allowed < distance ? allowed : Mathf.MoveTowards(distance, allowed, 5f * Time.unscaledDeltaTime);
            third.transform.SetPositionAndRotation(eye + back * distance, rotation);
            body.SetView(!ThirdPerson, !ThirdPerson || distance > .65f);
            brain.ManualUpdate();
        }
        public void RestoreMode(bool thirdPerson) { ThirdPerson = thirdPerson; distance = 0; ApplyMode(); }
        private void ApplyMode() { first.Priority = ThirdPerson ? 0 : 20; third.Priority = ThirdPerson ? 20 : 0; }
    }
}
