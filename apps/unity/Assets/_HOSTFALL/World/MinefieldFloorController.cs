using System;
using Hostfall.Core;
using Hostfall.Player;
using Hostfall.Simulation.Computers;
using UnityEngine;

namespace Hostfall.World
{
    public sealed class MinefieldFloorController : MonoBehaviour
    {
        [SerializeField] private FacilityDoor entry;
        [SerializeField] private BoxCollider[] sensors;
        [SerializeField] private Renderer[] beams;
        [SerializeField] private Renderer[] statusLights;
        [SerializeField] private Light alarmLamp;
        [SerializeField] private AudioSource siren;
        [SerializeField] private float spacing;
        private MinefieldSession board;
        private ComputerCampaign campaign;
        private ControlState controls;
        private PlayerMotor player;
        private PlayerHealth health;
        private Bounds[] volumes;
        private Vector3 firstCell;
        private static AudioClip alarmClip;

        public void Configure(FacilityDoor entry, BoxCollider[] sensors, Renderer[] beams, Renderer[] statusLights,
            Light alarmLamp, AudioSource siren, float spacing)
        {
            this.entry = entry;
            this.sensors = sensors;
            this.beams = beams;
            this.statusLights = statusLights;
            this.alarmLamp = alarmLamp;
            this.siren = siren;
            this.spacing = spacing;
        }

        public void Initialize(ComputerCampaign campaign, ControlState state)
        {
            if (sensors.Length != MinefieldSession.CellCount || beams.Length != sensors.Length || statusLights.Length != sensors.Length ||
                entry == null || siren == null || alarmLamp == null || spacing <= 0)
                throw new InvalidOperationException("Missing SEC-07 floor bindings");

            this.campaign = campaign;
            board = campaign.Minefield;
            controls = state;
            player = FindAnyObjectByType<PlayerMotor>();
            health = player.GetComponent<PlayerHealth>();
            volumes = new Bounds[sensors.Length];
            for (int cell = 0; cell < sensors.Length; cell++)
            {
                var sensor = sensors[cell];
                Vector3 size = Vector3.Scale(sensor.size, sensor.transform.lossyScale);
                volumes[cell] = new Bounds(sensor.transform.TransformPoint(sensor.center), size);
            }
            firstCell = volumes[0].center;
            board.Changed += RefreshState;
            board.AlarmTriggered += PlayAlarm;
            RefreshState();
        }

        public BoxCollider Sensor(int cell) => sensors[cell];
        public Renderer Beam(int cell) => beams[cell];
        public FacilityDoor Entry => entry;
        public void GrantEntry() => entry.UnlockForComputer();

        public void RefreshState()
        {
            if (board == null)
                return;

            for (int cell = 0; cell < sensors.Length; cell++)
            {
                bool armed = !campaign.FloorServiced && board.IsArmed(cell);
                sensors[cell].enabled = armed;
                beams[cell].enabled = armed;
                statusLights[cell].enabled = !armed && board.IsFlagged(cell);
            }
            alarmLamp.enabled = !campaign.FloorServiced && board.AlarmRemaining > 0;
        }

        private void LateUpdate()
        {
            if (board == null || !controls.WorldRuns || campaign.FloorServiced || board.AlarmRemaining > 0)
                return;

            Vector3 feet = player.transform.position + Vector3.up * .16f;
            int column = Mathf.FloorToInt((feet.x - firstCell.x) / spacing + .5f);
            int row = Mathf.FloorToInt((feet.z - firstCell.z) / spacing + .5f);
            if (column < 0 || column >= MinefieldSession.Side || row < 0 || row >= MinefieldSession.Side)
                return;

            int cell = row * MinefieldSession.Side + column;
            if (sensors[cell].enabled && volumes[cell].Contains(feet) && board.TriggerSensor(cell))
                health.TakeDamage(18, "Сенсорный пол SEC-07");
        }

        private void PlayAlarm(int cell)
        {
            if (alarmClip == null)
            {
                const int rate = 22050;
                var samples = new float[rate];
                for (int index = 0; index < samples.Length; index++)
                {
                    float t = (float)index / rate;
                    float pulse = Mathf.Sin(Mathf.PI * Mathf.Repeat(t * 3, 1));
                    samples[index] = Mathf.Sin(2 * Mathf.PI * (620 * t + 130 * t * t)) * pulse * pulse * .16f;
                }
                alarmClip = AudioClip.Create("SEC-07 floor warning", samples.Length, 1, rate, false);
                alarmClip.SetData(samples, 0);
            }
            siren.PlayOneShot(alarmClip);
        }

        private void OnDestroy()
        {
            if (board == null)
                return;

            board.Changed -= RefreshState;
            board.AlarmTriggered -= PlayAlarm;
        }
    }
}
