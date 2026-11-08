using Hostfall.Core;
using Hostfall.Persistence;
using Hostfall.Simulation;
using UnityEngine;

namespace Hostfall.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float walkSpeed = 3.4f;
        [SerializeField, Min(0.1f)] private float sprintSpeed = 5.4f;
        [SerializeField, Min(0.1f)] private float acceleration = 24f;
        [SerializeField, Min(0.1f)] private float jumpHeight = 0.8f;
        [SerializeField, Min(0.01f)] private float mouseSensitivity = 0.09f;
        [SerializeField] private float gravity = -22f;
        private CharacterController controller;
        private FacilityInput input;
        private ControlState controls;
        private Vector3 planarVelocity;
        private float verticalSpeed;
        private float targetPitch;
        private float pitchVelocity;
        private float coyote;
        private float jumpBuffer;
        private ActionState actions;
        private float dashRemaining;
        private Vector3 dashDirection;
        private bool airDashUsed;
        public event System.Action<bool> DashFeedback;
        public float Pitch { get; private set; }
        public float LookSensitivity { get; set; } = 1;
        public Vector3 Eye => transform.position + Vector3.up * 1.64f + transform.forward * .2f;
        public Quaternion ViewRotation => Quaternion.Euler(Pitch, transform.eulerAngles.y, 0);
        public void Initialize(FacilityInput bindings, ControlState state,ActionState actionState=null)
        {
            input = bindings; controls = state; controller = GetComponent<CharacterController>(); actions=actionState??new ActionState();
        }
        public PlayerStateData Capture(bool thirdPerson) => new()
        {
            position = transform.position, yaw = transform.eulerAngles.y, pitch = Pitch, targetPitch = targetPitch,
            planarVelocity = planarVelocity, verticalSpeed = verticalSpeed, thirdPerson = thirdPerson,
            dashRemaining=dashRemaining,dashDirection=dashDirection,airDashUsed=airDashUsed
        };
        public void Restore(PlayerStateData state)
        {
            controller.enabled = false; transform.SetPositionAndRotation(state.position, Quaternion.Euler(0, state.yaw, 0)); controller.enabled = true;
            Pitch = state.pitch; targetPitch = state.targetPitch; planarVelocity = state.planarVelocity; verticalSpeed = state.verticalSpeed;
            pitchVelocity = 0; jumpBuffer = 0; coyote = 0;
            dashRemaining=state.dashRemaining; dashDirection=state.dashDirection; airDashUsed=state.airDashUsed;
        }
        public void ReturnToPost()
        {
            controller.enabled = false; transform.position = new Vector3(-2.65f, .08f, -1.7f);
            controller.enabled = true; planarVelocity = Vector3.zero; verticalSpeed = 0; dashRemaining=0; airDashUsed=false;
        }
        public bool TryDash(Vector3 direction)
        {
            if(!controls.CanExplore||airDashUsed||!actions.TryDash()) return false;
            dashDirection=Vector3.ProjectOnPlane(direction,Vector3.up).normalized;
            if(dashDirection.sqrMagnitude<.01f) dashDirection=transform.forward;
            dashRemaining=.18f; airDashUsed=!controller.isGrounded; return true;
        }
        private void Update()
        {
            if (input == null || !controls.CanExplore) { planarVelocity = Vector3.zero; jumpBuffer = 0; return; }
            float dt = Time.deltaTime;
            Vector2 look = input.Look.ReadValue<Vector2>();
            transform.Rotate(0, look.x * mouseSensitivity * LookSensitivity, 0, Space.World);
            targetPitch = Mathf.Clamp(targetPitch - look.y * mouseSensitivity * LookSensitivity, -82, 82);
            Pitch = Mathf.SmoothDamp(Pitch, targetPitch, ref pitchVelocity, 0.025f);
            Vector2 move = Vector2.ClampMagnitude(input.Move.ReadValue<Vector2>(), 1);
            float speed = input.Sprint.IsPressed() ? sprintSpeed : walkSpeed;
            Vector3 desired = (transform.forward * move.y + transform.right * move.x) * speed;
            if(controller.isGrounded) airDashUsed=false;
            if(input.Dash.WasPressedThisFrame()) DashFeedback?.Invoke(TryDash(desired));
            if(dashRemaining>0&&dt>0)
            {
                float active=Mathf.Min(dashRemaining,dt);
                desired=(dashDirection*10*active+desired*(dt-active))/dt;
                dashRemaining=Mathf.Max(0,dashRemaining-dt); planarVelocity=desired;
            }
            planarVelocity = Vector3.MoveTowards(planarVelocity, desired, acceleration * dt);
            if (controller.isGrounded) { coyote = 0.1f; if (verticalSpeed < 0) verticalSpeed = -2; }
            else coyote -= dt;
            jumpBuffer = input.Jump.WasPressedThisFrame() ? 0.12f : jumpBuffer - dt;
            if (jumpBuffer > 0 && coyote > 0) { verticalSpeed = Mathf.Sqrt(jumpHeight * -2 * gravity); jumpBuffer = 0; coyote = 0; }
            verticalSpeed = Mathf.Max(verticalSpeed + gravity * dt, -35);
            var collision = controller.Move((planarVelocity + Vector3.up * verticalSpeed) * dt);
            if ((collision & CollisionFlags.Above) != 0 && verticalSpeed > 0) verticalSpeed = 0;
        }
    }
}
