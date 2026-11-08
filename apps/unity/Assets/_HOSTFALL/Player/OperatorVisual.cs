using UnityEngine;
using UnityEngine.Rendering;

namespace Hostfall.Player
{
    public sealed class OperatorVisual : MonoBehaviour
    {
        [SerializeField] private CharacterController motor;
        [SerializeField] private Transform[] arms, legs;
        [SerializeField] private Renderer[] head;
        private float phase, stride;
        public void Configure(CharacterController controller, Transform[] armJoints, Transform[] legJoints, Renderer[] helmet)
        { motor = controller; arms = armJoints; legs = legJoints; head = helmet; }
        public void SetView(bool first, bool visible)
        {
            gameObject.SetActive(visible);
            foreach (var renderer in head) renderer.shadowCastingMode = first ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            foreach (var arm in arms) arm.gameObject.SetActive(true);
        }
        private void LateUpdate()
        {
            float speed = new Vector2(motor.velocity.x, motor.velocity.z).magnitude;
            stride = Mathf.MoveTowards(stride, motor.isGrounded ? Mathf.Clamp01(speed / 4) : 0, 7 * Time.deltaTime);
            phase += speed * Time.deltaTime * 2.1f;
            for (int i = 0; i < legs.Length; i++)
            {
                float angle = Mathf.Sin(phase + i * Mathf.PI) * 24 * stride;
                legs[i].localRotation = Quaternion.Euler(angle, 0, 0);
                arms[i].localRotation = Quaternion.Euler(-angle * .7f, 0, -5 + i * 10);
            }
        }
    }
}
