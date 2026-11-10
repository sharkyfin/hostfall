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
        public bool IsSprinting => input != null && input.Sprint.IsPressed() && planarVelocity.sqrMagnitude > 1f;
        public Vector3 PlanarVelocity => planarVelocity;
        public Vector3 Eye => transform.position + Vector3.up * 1.64f + transform.forward * .2f;
        public Quaternion ViewRotation => Quaternion.Euler(Pitch, transform.eulerAngles.y, 0);
        public void Initialize(FacilityInput bindings, ControlState state,ActionState actionState=null)
        {
            input = bindings; controls = state; controller = GetComponent<CharacterController>(); actions=actionState??new ActionState();
        }
        public PlayerStateData Capture(bool thirdPerson)
        {
            var weapon = GetComponent<PlayerWeapon>();
            return new PlayerStateData
            {
                position = transform.position, yaw = transform.eulerAngles.y, pitch = Pitch, targetPitch = targetPitch,
                planarVelocity = planarVelocity, verticalSpeed = verticalSpeed, thirdPerson = thirdPerson,
                dashRemaining=dashRemaining,dashDirection=dashDirection,airDashUsed=airDashUsed,
                hasWeapon = weapon != null && weapon.HasWeapon
            };
        }
        public void Restore(PlayerStateData state)
        {
            controller.enabled = false; transform.SetPositionAndRotation(state.position, Quaternion.Euler(0, state.yaw, 0)); controller.enabled = true;
            Pitch = state.pitch; targetPitch = state.targetPitch; planarVelocity = state.planarVelocity; verticalSpeed = state.verticalSpeed;
            pitchVelocity = 0; jumpBuffer = 0; coyote = 0;
            dashRemaining=state.dashRemaining; dashDirection=state.dashDirection; airDashUsed=state.airDashUsed;
            var weapon = GetComponent<PlayerWeapon>();
            if (weapon != null)
            {
                if (state.hasWeapon) weapon.Equip();
                else weapon.Unequip();
            }
        }
        public void ReturnToPost()
        {
            controller.enabled = false; transform.position = new Vector3(-2.65f, .08f, -1.7f);
            controller.enabled = true; planarVelocity = Vector3.zero; verticalSpeed = 0; dashRemaining=0; airDashUsed=false;
        }
        public void RecoverToSafety()
        {
            controller.enabled = false;
            Vector3 pos = transform.position;
            Vector3 target;
            if (pos.z >= 37f && pos.x >= -10f && pos.x <= 10f) target = new Vector3(0, 0.1f, 40f);
            else if (pos.x >= 7f && pos.z >= 19f) target = new Vector3(10.75f, 0.1f, 25f);
            else if (pos.x <= -7f && pos.z >= 17f) target = new Vector3(-16.5f, 0.1f, 20f);
            else if (pos.z >= 14f) target = new Vector3(0, 0.1f, 17f);
            else target = new Vector3(-2.65f, .08f, -1.7f);

            transform.position = target;
            controller.enabled = true;
            planarVelocity = Vector3.zero;
            verticalSpeed = 0;
            dashRemaining = 0;
            airDashUsed = false;
        }
        public bool TryDash(Vector3 direction)
        {
            if(!controls.CanExplore||airDashUsed||!actions.TryDash()) return false;
            dashDirection=Vector3.ProjectOnPlane(direction,Vector3.up).normalized;
            if(dashDirection.sqrMagnitude<.01f) dashDirection=transform.forward;
            dashRemaining=.18f; airDashUsed=!controller.isGrounded; return true;
        }
        public void AddPitchKick(float kick) => targetPitch = Mathf.Clamp(targetPitch + kick, -82, 82);
        private void Update()
        {
            if (input == null || !controls.CanExplore) { planarVelocity = Vector3.zero; jumpBuffer = 0; return; }
            if (Cursor.lockState != CursorLockMode.Locked || Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            float dt = Time.deltaTime;
            Vector2 look = input.Look.ReadValue<Vector2>() * mouseSensitivity;
            look += input.KeyboardLook.ReadValue<Vector2>() * (90f * dt);
            transform.Rotate(0, look.x * LookSensitivity, 0, Space.World);
            targetPitch = Mathf.Clamp(targetPitch - look.y * LookSensitivity, -82, 82);
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

            // Out-of-bounds safety failsafe: recover if player ever drops into the void or escapes boundaries
            Vector3 p = transform.position;
            if (p.y <= -3.0f || p.y >= 18.0f || p.x <= -35.0f || p.x >= 95.0f || p.z <= -25.0f || p.z >= 210.0f)
            {
                RecoverToSafety();
            }
        }
    }
}
