using System;
using Hostfall.Core;
using Hostfall.Player;
using Hostfall.Simulation.Computers;
using UnityEngine;

namespace Hostfall.World
{
    [DefaultExecutionOrder(210)]
    public sealed class RecoveryTransitController : MonoBehaviour
    {
        [SerializeField] private Transform cabin, cabinDoor, shelterDoor, ringDoor;
        [SerializeField] private Light[] reactionLamps;
        [SerializeField] private AudioSource relay;
        private ComputerCampaign campaign;
        private ControlState controls;
        private PlayerMotor player;
        private CharacterController passengerBody;
        private Vector3 cabinClosed, shelterClosed, ringClosed;
        private Vector3 passengerOffset;
        private float passengerHeading, pulse;
        private bool powered;
        private AudioClip activation;

        public bool Powered => powered;
        public Transform Cabin => cabin;
        public bool Riding => campaign?.Transit.Passenger == true;

        public void Configure(Transform car, Transform innerDoor, Transform lowerStop, Transform upperStop,
            Light[] lamps, AudioSource powerRelay)
        {
            cabin = car;
            cabinDoor = innerDoor;
            shelterDoor = lowerStop;
            ringDoor = upperStop;
            reactionLamps = lamps;
            relay = powerRelay;
        }

        public void Initialize(ComputerCampaign model, ControlState state, PlayerMotor motor)
        {
            if (cabin == null || cabinDoor == null || shelterDoor == null || ringDoor == null ||
                reactionLamps == null || reactionLamps.Length < 3 || relay == null)
                throw new InvalidOperationException("Service shuttle is missing a landing");
            campaign = model;
            controls = state;
            player = motor;
            passengerBody = motor.GetComponent<CharacterController>();
            cabinClosed = cabinDoor.localPosition;
            shelterClosed = shelterDoor.localPosition;
            ringClosed = ringDoor.localPosition;
            powered = campaign.RecoveryAuthorized;
            ApplyPose();
            if (Riding) BindPassenger();
            PaintLamps();
        }

        public void RefreshPower()
        {
            if (powered || campaign?.RecoveryAuthorized != true) return;
            powered = true;
            pulse = 3.0f;
            activation ??= MakeRelayTone();
            relay.PlayOneShot(activation, .82f);
            ApplyPose();
            Physics.SyncTransforms();
            PaintLamps();
        }

        public string Prompt(TransitStop station, bool inCab)
        {
            if (!powered) return "Транспорт ждёт команды восстановления";
            var state = campaign.Transit;
            if (state.Moving) return "Кабина в пути";
            if (inCab) return state.Destination == TransitStop.Shelter ? "[E] К Центральному кольцу" : "[E] Вернуться в убежище";
            return state.Destination == station ? "Кабина на станции" : "[E] Вызвать кабину";
        }

        public void Use(TransitStop station, bool inCab, Transform panel)
        {
            if (!powered || !controls.CanExplore || Vector3.Distance(player.Eye, panel.position) > 2.7f) return;
            var state = campaign.Transit;
            if (inCab)
            {
                var feet = cabin.InverseTransformPoint(player.transform.position);
                if (Mathf.Abs(feet.y) > .2f || Mathf.Abs(feet.x) > 1.03f || Mathf.Abs(feet.z) > 1.06f) return;
                var next = state.Destination == TransitStop.Ring ? TransitStop.Shelter : TransitStop.Ring;
                if (campaign.DispatchTransit(next, true)) BindPassenger();
            }
            else campaign.DispatchTransit(station, false);

            ApplyPose();
        }

        private void BindPassenger()
        {
            passengerOffset = cabin.InverseTransformPoint(player.transform.position);
            passengerHeading = Mathf.DeltaAngle(cabin.eulerAngles.y, player.transform.eulerAngles.y);
            controls.SetTransitPassenger(true);
        }

        private void LateUpdate()
        {
            if (campaign == null || !controls.WorldRuns) return;
            var state = campaign.Transit;
            if (state.Moving)
            {
                bool carry = state.Passenger;
                state.Advance(Time.deltaTime);
                ApplyPose();
                if (!state.Moving)
                    Physics.SyncTransforms();
                if (carry)
                {
                    // The rider stays rigidly in the enclosed car while both travel.
                    // Normal character collisions resume at the flush landing.
                    passengerBody.enabled = false;
                    player.transform.SetPositionAndRotation(cabin.TransformPoint(passengerOffset),
                        Quaternion.Euler(0, cabin.eulerAngles.y + passengerHeading, 0));
                    passengerBody.enabled = true;
                    if (!state.Passenger) controls.SetTransitPassenger(false);
                }
            }
            if (pulse > 0)
            {
                pulse = Mathf.Max(0, pulse - Time.deltaTime);
                PaintLamps();
            }
        }

        private void ApplyPose()
        {
            var state = campaign.Transit;
            var sample = state.Pose;
            cabin.SetPositionAndRotation(new Vector3(sample.X, sample.Y, sample.Z), Quaternion.Euler(0, sample.Yaw, 0));
            float shelter = powered ? (float)state.GateOpening(TransitStop.Shelter) : 0;
            float ring = powered ? (float)state.GateOpening(TransitStop.Ring) : 0;
            shelterDoor.localPosition = shelterClosed + Vector3.up * (2.55f * shelter);
            ringDoor.localPosition = ringClosed + Vector3.up * (2.55f * ring);
            cabinDoor.localPosition = cabinClosed + Vector3.up * (2.55f * Mathf.Max(shelter, ring));
        }

        private void PaintLamps()
        {
            for (int index = 0; index < reactionLamps.Length; index++)
            {
                var lamp = reactionLamps[index];
                lamp.enabled = powered;
                lamp.intensity = pulse <= 0 ? 2.8f : 3.6f + Mathf.Sin(17f * pulse - index * 1.8f) * 2.8f;
                lamp.color = pulse <= 0 ? new Color(.39f, .83f, .74f) :
                    Color.Lerp(new Color(.39f, .83f, .74f), new Color(1, .41f, .12f), pulse / 3f);
            }
        }

        private static AudioClip MakeRelayTone()
        {
            const int frequency = 24000;
            var samples = new float[frequency * 2];
            for (int index = 0; index < samples.Length; index++)
            {
                float time = index / (float)frequency;
                float hum = Mathf.Sin(2 * Mathf.PI * (51f * time + 17f * time * time));
                float contact = Mathf.Sin(2 * Mathf.PI * 183f * time) * Mathf.Exp(-6 * Mathf.Abs(time - .34f));
                samples[index] = (.38f * hum + .2f * contact) * Mathf.Sin(Mathf.PI * time * .5f);
            }
            var clip = AudioClip.Create("Recovery bus and relay", samples.Length, 1, frequency, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (controls != null && Riding) controls.SetTransitPassenger(false);
            if (activation != null) Destroy(activation);
        }
    }
}
