using Hostfall.Core;
using Hostfall.Interaction;
using Hostfall.Player;
using Hostfall.Simulation.Computers;
using UnityEngine;

namespace Hostfall.World
{
    public sealed class BufferCachePickup : MonoBehaviour, IInteractable
    {
        private BufferRecallSession session;
        private PlayerMotor player;
        private PlayerHealth health;
        private ControlState controls;

        public void Initialize(BufferRecallSession model, PlayerMotor motor, ControlState control)
        {
            session = model;
            player = motor;
            controls = control;
            health = motor.GetComponent<PlayerHealth>();
            if (session.SupplyClaimed) gameObject.SetActive(false);
        }

        public string Prompt => session?.Complete != true || session.SupplyClaimed ? "" :
            health.CurrentHealth >= health.MaxHealth ? "Корпус полностью исправен" : "[E] Взять аварийный ремкомплект";

        public void Interact()
        {
            if (session?.Complete != true || controls?.CanExplore != true || health.IsDestroyed ||
                health.CurrentHealth >= health.MaxHealth || Vector3.Distance(player.Eye, transform.position) > 2.4f)
                return;

            if (!session.ClaimSupply()) return;
            health.Heal(35);
            gameObject.SetActive(false);
        }
    }
}
