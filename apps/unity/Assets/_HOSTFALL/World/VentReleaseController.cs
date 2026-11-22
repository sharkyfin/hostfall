using System;
using Hostfall.Core;
using Hostfall.Player;
using Hostfall.Simulation.Computers;
using UnityEngine;

namespace Hostfall.World
{
    // Authored open service duct with a visible grate and a swept emergence lane.
    public sealed class VentReleaseController : MonoBehaviour
    {
        [SerializeField] private Transform grate;
        [SerializeField] private WatcherController actor;
        [SerializeField] private Vector3 hiddenPosition, exitPosition;
        [SerializeField] private Transform isolationDamper;
        [SerializeField] private Light ductLight;
        private Renderer[] renderers;
        private Collider[] actorColliders, grateColliders;
        private Animator animator;
        private RobotCombatController combat;
        private ComputerCampaign campaign;
        private ControlState controls;
        private PlayerMotor player;
        private PlayerHealth health;
        private PlayerWeapon weapon;
        private Transform arm, shaft, gripper, upperJaw, lowerJaw;
        private Vector3 mouth, outward;
        private Vector3 grateClosed;
        private Vector3 damperClosed;
        private AudioSource sound;
        private static AudioClip scrape;
        internal static AudioClip MetalScrape=>scrape;
        private VentThreatPhase previous;
        private bool released;
        private float nextCue;
        public WatcherController Actor => actor;
        public Vector3 ExitPosition => exitPosition;
        public void Configure(Transform grille, WatcherController robot, Vector3 hidden, Vector3 exit)
        { grate = grille; actor = robot; hiddenPosition = hidden; exitPosition = exit; }

        public void ConfigureDuct(Vector3 start, Transform damper, Light warningLamp)
        {
            hiddenPosition = start;
            isolationDamper = damper;
            ductLight = warningLamp;
        }

        public void Initialize(ComputerCampaign model, ControlState state, PlayerMotor motor)
        {
            campaign = model; grateClosed = grate.localPosition;
            controls = state;
            player = motor;
            health = motor.GetComponent<PlayerHealth>();
            weapon = motor.GetComponent<PlayerWeapon>();
            outward = (exitPosition - hiddenPosition).normalized;
            mouth = grate.position + Vector3.up * 1.39f + outward * .13f;
            BuildArm();
            renderers = actor.GetComponentsInChildren<Renderer>(true);
            actorColliders = actor.GetComponentsInChildren<Collider>(true);
            grateColliders = grate.GetComponentsInChildren<Collider>();
            animator = actor.GetComponentInChildren<Animator>(); combat = actor.GetComponent<RobotCombatController>();
            var soundOrigin = this.gameObject;
            if (isolationDamper != null)
            {
                damperClosed = isolationDamper.localPosition;
                soundOrigin = new GameObject("Duct pressure and crawler audio");
                soundOrigin.transform.SetParent(transform);
                soundOrigin.transform.position = hiddenPosition + Vector3.up * .8f;
            }
            sound = soundOrigin.AddComponent<AudioSource>(); sound.spatialBlend = 1; sound.minDistance = 2; sound.maxDistance = 18;
            sound.playOnAwake = false;
            if (scrape == null)
            {
                const int rate = 22050; var data = new float[rate]; var random = new System.Random(198);
                for (int i = 0; i < data.Length; i++)
                {
                    float t = i / (float)rate, decay = Mathf.Exp(-t * 4);
                    data[i] = ((float)random.NextDouble() * 2 - 1) * .3f * decay + Mathf.Sin(t * 1230) * Mathf.Sin(t * 83) * decay * .25f;
                }
                scrape = AudioClip.Create("Vent / metal strain", rate, 1, rate, false); scrape.SetData(data, 0);
            }
            campaign.ThreatChanged += OnThreat;
            previous = campaign.ThreatPhase;
            released = previous == VentThreatPhase.Released;
            SetActorActive(released);
            if (!released) actor.transform.position = hiddenPosition;
            RefreshState();
        }
        private void OnThreat(VentThreatPhase phase)
        {
            if (phase == previous) return;
            previous = phase;
            if (phase == VentThreatPhase.Warning || phase == VentThreatPhase.Opening)
            {
                sound.PlayOneShot(scrape, ScareVolume(.8f));
                nextCue = Time.time + 1.8f;
            }
            if (phase == VentThreatPhase.Released) Release();
            RefreshState();
            if(phase==VentThreatPhase.Opening)
            {
                Physics.SyncTransforms();var direction=exitPosition-hiddenPosition;
                if(Physics.CapsuleCast(hiddenPosition+Vector3.up*.35f,hiddenPosition+Vector3.up, .28f,direction.normalized,out var hit,direction.magnitude,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore))
                    throw new InvalidOperationException("Vent emergence path intersects "+hit.collider.name);
                if(animator!=null)animator.CrossFadeInFixedTime("Run",.1f);
            }
        }
        public void RefreshState()
        {
            if (campaign == null) return;
            bool open = campaign.ThreatPhase == VentThreatPhase.Opening || campaign.ThreatPhase == VentThreatPhase.Released && !campaign.VentSealed;
            if (isolationDamper != null)
            {
                bool flowing = !campaign.VentSealed && campaign.ThreatPhase is not VentThreatPhase.Sealed;
                isolationDamper.localPosition = damperClosed + (flowing ? Vector3.up * 1.70f : Vector3.zero);
                if (ductLight != null)
                {
                    ductLight.color = flowing ? new Color(.94f, .31f, .13f) : new Color(.27f, .65f, .54f);
                    ductLight.intensity = .48f;
                }
            }
            foreach (var collider in grateColliders) collider.enabled = !open;
            if (campaign.ThreatPhase != VentThreatPhase.Opening) grate.localPosition = grateClosed + (open ? Vector3.up * 1.7f : Vector3.zero);
            if (campaign.ThreatPhase != VentThreatPhase.Opening)
            {
                arm.gameObject.SetActive(false);
                if (!released)
                {
                    SetActorActive(false);
                    actor.transform.position = hiddenPosition;
                }
            }
        }

        private float ScareVolume(float normal) => normal * Mathf.Lerp(.25f, 1, player.ScareIntensity);

        private void BuildArm()
        {
            var grateMesh = grate.GetComponentInChildren<MeshFilter>();
            var grateMaterial = grateMesh.GetComponent<Renderer>().sharedMaterial;
            arm = new GameObject("Telescoping vent grabber").transform;
            arm.SetParent(transform, false);
            arm.SetPositionAndRotation(mouth, Quaternion.LookRotation(outward));

            Transform Part(string label, Transform parent, Vector3 position, Vector3 size)
            {
                var part = new GameObject(label);
                part.transform.SetParent(parent, false);
                part.transform.localPosition = position;
                part.transform.localScale = size;
                part.AddComponent<MeshFilter>().sharedMesh = grateMesh.sharedMesh;
                part.AddComponent<MeshRenderer>().sharedMaterial = grateMaterial;
                return part.transform;
            }
            Part("Linear actuator housing", arm, new Vector3(0, 0, .045f), new Vector3(.22f, .19f, .22f));
            shaft = Part("Drive rod", arm, Vector3.zero, new Vector3(.105f, .09f, .1f));
            gripper = new GameObject("Gripper joint").transform;
            gripper.SetParent(arm, false);
            Part("Wrist", gripper, Vector3.zero, new Vector3(.26f, .14f, .16f));
            upperJaw = Part("Upper jaw", gripper, new Vector3(0, .12f, .09f), new Vector3(.18f, .07f, .26f));
            lowerJaw = Part("Lower jaw", gripper, new Vector3(0, -.12f, .09f), new Vector3(.18f, .07f, .26f));
            arm.gameObject.SetActive(false);
        }

        private void PresentGrab(float time)
        {
            bool moving = time is > .34f and < 1.09f;
            if (arm.gameObject.activeSelf != moving) arm.gameObject.SetActive(moving);
            float clearance = moving ? GrabClearance() : 0;
            if (moving)
            {
                float extension = Mathf.Min(Mathf.InverseLerp(.34f, .76f, time), Mathf.InverseLerp(1.09f, .86f, time));
                float range = Mathf.Min(clearance, Mathf.SmoothStep(0, 1.45f, extension));
                float rodLength = Mathf.Max(.1f, range);
                shaft.localPosition = new Vector3(0, 0, rodLength * .5f);
                shaft.localScale = new Vector3(.105f, .09f, rodLength);
                gripper.localPosition = new Vector3(0, 0, range);
                float gap = Mathf.Lerp(.19f, .043f, Mathf.InverseLerp(.73f, .90f, time));
                upperJaw.localPosition = new Vector3(0, gap, .09f);
                lowerJaw.localPosition = new Vector3(0, -gap, .09f);
                upperJaw.localRotation = Quaternion.Euler(gap * 95f, 0, 0);
                lowerJaw.localRotation = Quaternion.Euler(-gap * 95f, 0, 0);
            }

            if (!campaign.ResolveVentGrab() || !controls.CanExplore || health.IsDestroyed) return;
            var torso = player.transform.position + Vector3.up * 1.34f;
            var tip = mouth + outward * (moving ? clearance : GrabClearance());
            if (Vector3.Distance(torso, tip) > .54f ||
                Physics.Linecast(mouth, torso, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore)) return;
            sound.PlayOneShot(scrape, ScareVolume(.95f));
            if (weapon.TryCounter(mouth)) return;
            health.TakeDamage(16, "Захват вентиляционного манипулятора");
            player.StartleFromImpact();
        }

        private float GrabClearance()
        {
            const float reach = 1.45f;
            return Physics.SphereCast(mouth, .19f, outward, out var wall, reach, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore)
                ? Mathf.Max(0, wall.distance - .16f) : reach;
        }
        private void SetActorActive(bool visible)
        {
            actor.enabled = visible; if (combat != null) combat.enabled = visible;
            if (animator != null) animator.enabled = visible;
            foreach (var renderer in renderers) renderer.enabled = visible;
            foreach (var collider in actorColliders) collider.enabled = visible && !actor.IsDestroyed;
            foreach (var light in actor.GetComponentsInChildren<Light>()) light.enabled = visible && !actor.IsDestroyed;
        }
        private void Release()
        {
            if (released) return;
            actor.transform.position = exitPosition;
            Physics.SyncTransforms();
            // Stop a bad authoring binding from putting an actor inside walls.
            if (Physics.CheckCapsule(exitPosition + Vector3.up * .35f, exitPosition + Vector3.up * 1.0f, .28f, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore))
                throw new InvalidOperationException("Vent release lane intersects world geometry");
            released = true; SetActorActive(true);
        }
        private void Update()
        {
            if (campaign == null || !controls.WorldRuns) return;
            if (campaign.ThreatPhase == VentThreatPhase.Warning)
            {
                grate.localPosition = grateClosed + Vector3.right * (Mathf.Sin(Time.time * 51) * .009f);
                float warningTime = (float)campaign.ThreatElapsed;
                if (ductLight != null)
                    ductLight.intensity = .34f + .48f * Mathf.SmoothStep(0, 1, Mathf.Repeat(warningTime * .8f, 1));
                if (Time.time >= nextCue && warningTime < 3.3f)
                {
                    if (isolationDamper != null)
                        sound.transform.position = Vector3.Lerp(hiddenPosition, grate.position, warningTime / 5) + Vector3.up * .8f;
                    sound.PlayOneShot(scrape, ScareVolume(.65f)); nextCue = Time.time + 1.8f;
                }
            }
            if (campaign.ThreatPhase == VentThreatPhase.Opening)
            {
                float time = (float)campaign.ThreatElapsed;
                grate.localPosition = grateClosed + Vector3.up * (Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0, .62f, time)) * 1.7f);
                PresentGrab(time);
                float leap = Mathf.InverseLerp(isolationDamper != null ? .20f : .67f, 1.2f, time);
                if (leap > 0)
                {
                    foreach (var renderer in renderers) renderer.enabled = true;
                    if (animator != null) animator.enabled = true;
                    actor.transform.position = Vector3.Lerp(hiddenPosition, exitPosition, leap) +
                        Vector3.up * ((isolationDamper != null ? .16f : .3f) * Mathf.Sin(leap * Mathf.PI));
                }
            }
        }
        private void OnDestroy() { if (campaign != null) campaign.ThreatChanged -= OnThreat; }
    }
}
