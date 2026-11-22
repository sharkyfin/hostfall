using Hostfall.Core;
using Hostfall.Player;
using Hostfall.Simulation.Computers;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.World
{
    public sealed class TerminalAmbush : MonoBehaviour
    {
        private const float Tell = .22f, Contact = .56f, Escape = 1.26f, Fall = .32f;
        private PlayerMotor player;
        private PlayerWeapon weapon;
        private PlayerHealth health;
        private ControlState controls;
        private ComputerCampaign campaign;
        private ComputerRuntimeUI desktop;
        private FacilityUI ui;
        private WatcherController attacker;
        private float elapsed;
        private bool latched, falling, parried;
        private float fallTime;
        public bool Busy => attacker != null;

        public void Initialize(ComputerCampaign model, ControlState state, ComputerRuntimeUI computer, FacilityUI interfaceView)
        {
            campaign = model;
            controls = state;
            desktop = computer;
            ui = interfaceView;
            player = GetComponent<PlayerMotor>();
            weapon = GetComponent<PlayerWeapon>();
            health = GetComponent<PlayerHealth>();
        }

        public bool OnContact(WatcherController robot, bool melee)
        {
            if (Busy) return melee;
            if (controls.Mode != ControlMode.Computer || desktop.ActiveStation == null) return false;
            string station = desktop.ActiveStation.StationId;
            desktop.Close();
            if (!melee || !campaign.TrySpendTerminalAmbush(station))
            {
                ui.ShowActionNotice("Охрана рядом · отойдите от экрана");
                return false;
            }
            attacker = robot;
            elapsed = 0;
            latched = falling = parried = false;
            fallTime = 0;
            robot.BeginTerminalLeap();
            player.FaceThreat(robot.transform.position);
            ui.ShowActionNotice("Робот бросается · Q отскочить");
            return true;
        }

        private void Update()
        {
            if (!Busy || !controls.WorldRuns) return;
            elapsed += Time.deltaTime;
            if (attacker.IsDestroyed)
            {
                Finish(false);
                return;
            }
            Vector3 toward = Vector3.ProjectOnPlane(player.transform.position - attacker.transform.position, Vector3.up);
            if (falling)
            {
                fallTime += Time.deltaTime;
                attacker.PresentTerminalFall(-toward, fallTime / Fall, parried ? Time.deltaTime : 0);
                if (fallTime >= Fall) Finish(parried);
                return;
            }
            bool clear = toward.magnitude < 1.8f && attacker.TerminalLeapHasContact(player.Eye);
            if (elapsed > Tell && !clear)
            {
                StartFall(false);
                return;
            }
            float travel = Mathf.InverseLerp(Tell, Contact, elapsed);
            attacker.PresentTerminalLeap(toward, travel, Time.deltaTime);
            if (travel > 0 && clear && weapon.TryCounter(attacker.TerminalLeapTarget))
            {
                StartFall(true);
                return;
            }
            if (!latched && elapsed >= Contact)
            {
                if (toward.magnitude >= 1.25f) { StartFall(false); return; }
                latched = true;
                player.StartleFromImpact();
                ui.ShowActionNotice("Робот вцепился · C оттолкнуть");
            }
            if (elapsed >= Escape)
            {
                health.TakeDamage(23, "Захват робота у терминала");
                StartFall(false);
            }
        }

        private void StartFall(bool shoved)
        {
            falling = true;
            parried = shoved;
            attacker.BeginTerminalFall();
        }

        private void Finish(bool parried)
        {
            attacker.EndTerminalLeap(parried);
            attacker = null;
        }

        private void OnDisable()
        {
            if (Busy) Finish(false);
        }
    }
}
