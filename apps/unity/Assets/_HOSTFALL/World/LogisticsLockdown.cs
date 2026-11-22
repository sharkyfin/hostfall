using Hostfall.Core;
using Hostfall.Player;
using Hostfall.Simulation.Computers;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.World
{
    public sealed class LogisticsLockdown : MonoBehaviour
    {
        [SerializeField] private Transform[] leaves;
        [SerializeField] private WatcherController[] reserves;
        [SerializeField] private Light beacon;
        [SerializeField] private Transform reserveShutter;
        [SerializeField] private Vector3[] reserveStarts;
        [SerializeField] private Vector3[] reserveExits;
        private Vector3[] open;
        private BoxCollider[] solid;
        private RobotPresentation[] wave;
        private ComputerCampaign campaign;
        private ControlState controls;
        private PlayerMotor player;
        private ComputerRuntimeUI desktop;
        private FacilityUI ui;
        private CharacterController capsule;
        private Vector3 shutterClosed;
        private AudioSource shutterAudio;
        private int previousPhase;

        public void Configure(Transform[] gates, WatcherController[] actors, Light light)
        {
            leaves = gates;
            reserves = actors;
            beacon = light;
        }

        public void ConfigureReserveBay(Transform shutter, Vector3[] starts, Vector3[] exits)
        {
            reserveShutter = shutter;
            reserveStarts = starts;
            reserveExits = exits;
        }

        public void Initialize(ComputerCampaign model, ControlState state, PlayerMotor motor, ComputerRuntimeUI computer, FacilityUI interfaceView)
        {
            campaign = model;
            controls = state;
            player = motor;
            desktop = computer;
            ui = interfaceView;
            capsule = player.GetComponent<CharacterController>();
            open = new Vector3[leaves.Length];
            solid = new BoxCollider[leaves.Length];
            for (int i = 0; i < leaves.Length; i++)
            {
                open[i] = leaves[i].localPosition;
                solid[i] = leaves[i].GetComponentInChildren<BoxCollider>();
            }
            wave = new RobotPresentation[reserves.Length];
            for (int i = 0; i < reserves.Length; i++) wave[i] = new RobotPresentation(reserves[i]);
            RefreshReleases();
            previousPhase = model.LogisticsDefense.Phase;
            if (reserveShutter != null)
            {
                shutterClosed = reserveShutter.localPosition;
                shutterAudio = reserveShutter.gameObject.AddComponent<AudioSource>();
                shutterAudio.spatialBlend = 1;
                shutterAudio.minDistance = 2;
                shutterAudio.maxDistance = 16;
                shutterAudio.playOnAwake = false;
                for (int i = model.LogisticsDefense.Released; i < reserves.Length; i++)
                    reserves[i].transform.position = reserveStarts[i];
                PresentReserveBay();
            }
            PresentGates(6);
            model.WorldAction += OnWorldAction;
        }

        private void OnWorldAction(WorldActionData action)
        {
            if (action.kind != WorldActionKind.RaiseAlarm || action.target != "logistics.alarm" ||
                desktop.ActiveStation?.StationId != ComputerCampaign.PayloadStation ||
                !campaign.LogisticsDefense.Begin()) return;
            desktop.Close();
            ui.ShowActionNotice("Карантин грузовой галереи · держите проход");
            campaign.RefreshCoreProgress();
        }

        private void Update()
        {
            if (campaign == null || !controls.WorldRuns) return;
            var state = campaign.LogisticsDefense;
            if (state.Phase == 0) return;
            if (state.Active)
            {
                bool allDown = true;
                for (int i = 0; i < state.Released; i++) allDown &= reserves[i].IsDestroyed;
                int before = state.Released;
                if (state.Advance(Time.deltaTime, allDown))
                {
                    if (reserveShutter != null)
                        for (int index = before; index < state.Released; index++)
                            reserves[index].transform.position = reserveExits[index];
                    RefreshReleases();
                    campaign.RefreshCoreProgress();
                    if (state.Phase == LogisticsDefense.Cleared) ui.ShowActionNotice("Карантин снят · путь открыт");
                }
            }
            if (reserveShutter != null) PresentReserveBay();
            PresentGates(Time.deltaTime * 2.1f);
            beacon.enabled = state.Active;
        }

        private void PresentReserveBay()
        {
            var state = campaign.LogisticsDefense;
            int phase = state.Phase;
            bool first = phase == LogisticsDefense.Warning;
            bool opening = first || phase == LogisticsDefense.BetweenWaves;
            if (previousPhase != phase && opening && VentReleaseController.MetalScrape != null)
                shutterAudio.PlayOneShot(VentReleaseController.MetalScrape, .72f);
            previousPhase = phase;
            if (!opening)
            {
                // The shutter stays open once used, including on the return trip.
                reserveShutter.localPosition = shutterClosed + (phase > 0 ? Vector3.up * 1.95f : Vector3.zero);
                if (phase == LogisticsDefense.FirstWave)
                    for (int index = state.Released; index < reserves.Length; index++)
                        wave[index].ShowStaged();
                return;
            }

            float fade = first ? Mathf.InverseLerp(0, .47f, state.TimeInPhase) : 1;
            reserveShutter.localPosition = shutterClosed + Vector3.up * (1.95f * Mathf.SmoothStep(0, 1, fade));
            if (fade < 1) return;
            float end = first ? 1.5f : 1.8f;
            float travel = Mathf.InverseLerp(.47f, end, state.TimeInPhase);
            if (state.TimeInPhase < .47f) return;
            int until = first ? 1 : reserves.Length;
            for (int index = until; index < reserves.Length; index++)
                wave[index].ShowStaged();
            for (int index = state.Released; index < until; index++)
            {
                wave[index].ShowEmerging();
                reserves[index].transform.position = Vector3.Lerp(reserveStarts[index], reserveExits[index], travel);
            }
        }

        private void RefreshReleases()
        {
            int released = campaign.LogisticsDefense.Released;
            for (int i = 0; i < wave.Length; i++) wave[i].SetActive(i < released);
        }

        private void PresentGates(float maximumMotion)
        {
            bool locked = campaign.LogisticsDefense.Phase is >= LogisticsDefense.FirstWave and < LogisticsDefense.Cleared;
            for (int i = 0; i < leaves.Length; i++)
            {
                var delta = locked ? Vector3.down * 1.82f : Vector3.zero;
                var position = leaves[i].localPosition;
                if (locked && Occupied(i)) continue;
                leaves[i].localPosition = Vector3.MoveTowards(position, open[i] + delta, maximumMotion);
            }
        }

        private bool Occupied(int index)
        {
            var limit = solid[index].bounds;
            limit.Expand(new Vector3(.18f, 3.64f, .18f));
            return limit.Intersects(capsule.bounds);
        }

        private void OnDestroy()
        {
            if (campaign != null) campaign.WorldAction -= OnWorldAction;
        }
    }
}
