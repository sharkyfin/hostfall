using System;
using Hostfall.Core;
using Hostfall.Player;
using Hostfall.Simulation.Computers;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Hostfall.World
{
    [DefaultExecutionOrder(140)]
    public sealed class ContainmentWorldController : MonoBehaviour
    {
        [SerializeField] private WatcherController sentry;
        [SerializeField] private FacilityDoor ringGate;
        [SerializeField] private Transform southLeaf, northLeaf;
        [SerializeField] private BoxCollider bay, southBeam, northBeam;
        [SerializeField] private Transform calibrationStart, calibrationEnd, captureStart, captureEnd;
        [SerializeField] private Transform[] cameraViews;
        [SerializeField] private Camera cctv;
        [SerializeField] private Light relayLamp;
        [SerializeField, Min(1)] private float leafTravel = 2.95f;

        private ContainmentSession session;
        private ControlState controls;
        private PlayerMotor player;
        private CharacterController capsule;
        private CapsuleCollider sentryBody;
        private Vector3 southClosed, northClosed;
        private RenderTexture feed;
        private UniversalRenderPipeline.SingleCameraRequest renderRequest;
        private ContainmentCamera? lastCamera;
        private float nextFrame, returnRemaining;
        private bool lockState;
        public int FrameCount { get; private set; }
        public (Vector3 first, Vector3 last) Route => sentry.PatrolRoute;
        public Vector3 SentryPosition => sentry.transform.position;
        public Bounds BayBounds => bay.bounds;
        public Bounds SouthPortal => southBeam.bounds;
        public Bounds NorthPortal => northBeam.bounds;

        public void Configure(WatcherController actor, FacilityDoor gate, Transform south, Transform north,
            BoxCollider baySensor, BoxCollider southSensor, BoxCollider northSensor, Transform calibrationA, Transform calibrationB,
            Transform captureA, Transform captureB, Camera camera, Transform[] views, Light lamp)
        {
            sentry = actor;
            ringGate = gate;
            southLeaf = south;
            northLeaf = north;
            bay = baySensor;
            southBeam = southSensor;
            northBeam = northSensor;
            calibrationStart = calibrationA;
            calibrationEnd = calibrationB;
            captureStart = captureA;
            captureEnd = captureB;
            cctv = camera;
            cameraViews = views;
            relayLamp = lamp;
        }

        public void Initialize(ComputerCampaign model, ControlState control, PlayerMotor motor)
        {
            session = model.Containment;
            controls = control;
            player = motor;
            if (sentry == null || sentry.Id != "assembly-sentry-r26" || ringGate == null ||
                ringGate.DoorId != ComputerCampaign.ContainmentAccess || cameraViews == null || cameraViews.Length != 3)
                throw new InvalidOperationException("Missing RBT-01 safety bindings");

            capsule = player.GetComponent<CharacterController>();
            sentryBody = sentry.GetComponent<CapsuleCollider>();
            southClosed = southLeaf.localPosition;
            northClosed = northLeaf.localPosition;
            sentry.BindContainment(session, calibrationStart.position, calibrationEnd.position, captureStart.position, captureEnd.position);
            lockState = session.Complete;
            ringGate.SetComputerPower(lockState);
            RefreshState();
        }

        private void Update()
        {
            if (session == null) return;
            bool running = controls.WorldRuns;
            if (running && returnRemaining > 0)
            {
                returnRemaining = Mathf.Max(0, returnRemaining - Time.deltaTime);
                RefreshState();
            }

            Bounds playerBounds = capsule.bounds;
            Bounds robotBounds = sentryBody.bounds;
            bool alive = !sentry.IsDestroyed;
            Bounds room = bay.bounds;
            Bounds south = southBeam.bounds;
            Bounds north = northBeam.bounds;
            bool robotAtSouth = alive && south.Intersects(robotBounds);
            bool robotAtNorth = alive && north.Intersects(robotBounds);
            bool southBlocked = south.Intersects(playerBounds) || robotAtSouth;
            bool northBlocked = north.Intersects(playerBounds) || robotAtNorth;
            var zone = !alive ? SentryZone.Offline : robotAtSouth || robotAtNorth ? SentryZone.Transit :
                room.Contains(robotBounds.center) ? SentryZone.Bay :
                robotBounds.center.z < south.center.z ? SentryZone.South : SentryZone.North;
            session.Observe(new ContainmentSensors(zone, southBlocked, northBlocked, room.Intersects(playerBounds)),
                running ? Time.deltaTime : 0);

            AlignLeaves();
        }

        public void RefreshState()
        {
            if (session == null) return;
            bool permit = session.Complete || returnRemaining > 0;
            if (lockState != permit)
            {
                lockState = permit;
                ringGate.SetComputerPower(permit);
            }
            relayLamp.color = session.Complete ? new Color(.2f, 1, .57f) : new Color(1, .46f, .12f);
            AlignLeaves();
        }

        private void AlignLeaves()
        {
            southLeaf.localPosition = southClosed + Vector3.up * (leafTravel * (float)session.Opening(ContainmentShutter.South));
            northLeaf.localPosition = northClosed + Vector3.up * (leafTravel * (float)session.Opening(ContainmentShutter.North));
        }

        public void RequestEscape(ContainmentShutter shutter, Transform panel, bool centralGate)
        {
            if (session == null || !controls.CanExplore || Vector3.Distance(player.Eye, panel.position) > 2.7f) return;
            if (!centralGate)
            {
                session.Release(shutter);
                return;
            }

            returnRemaining = 7f;
            RefreshState();
        }

        // The focused RBT-01 window pulls one shared low-resolution feed on demand.
        public RenderTexture ViewCamera(ContainmentCamera camera)
        {
            if (session == null || cctv == null || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return null;
            if (feed == null)
            {
                feed = new RenderTexture(480, 270, 24, RenderTextureFormat.ARGB32)
                {
                    name = "RBT-01 / CCTV live feed",
                    filterMode = FilterMode.Bilinear
                };
                feed.Create();
                renderRequest = new UniversalRenderPipeline.SingleCameraRequest { destination = feed };
            }

            if (lastCamera == camera && Time.unscaledTime < nextFrame) return feed;
            var anchor = cameraViews[(int)camera];
            cctv.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            if (!RenderPipeline.SupportsRenderRequest(cctv, renderRequest)) return null;
            RenderPipeline.SubmitRenderRequest(cctv, renderRequest);
            FrameCount++;
            lastCamera = camera;
            nextFrame = Time.unscaledTime + .16f;
            return feed;
        }

        private void OnDestroy()
        {
            if (feed == null) return;
            feed.Release();
            Destroy(feed);
        }
    }
}
