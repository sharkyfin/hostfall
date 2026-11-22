using Hostfall.Core;
using Hostfall.Player;
using Hostfall.Simulation.Computers;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.World
{
    public sealed class CoreAvatarEncounter : MonoBehaviour, IDamageable
    {
        [SerializeField] private Transform reactor, leftEmitter, rightEmitter, interceptor, ram;
        [SerializeField] private Transform[] risers;
        [SerializeField] private Material beamMaterial;
        [SerializeField] private Light statusLight;
        [SerializeField] private FacilityDoor trustGate;

        private ComputerCampaign campaign;
        private CoreAvatarBattle battle;
        private ControlState controls;
        private PlayerMotor player;
        private PlayerHealth health;
        private FacilityUI ui;
        private AudioSource effects;
        private AudioClip warning, discharge;
        private readonly LineRenderer[] guides = new LineRenderer[2];
        private readonly Vector3[] ring = new Vector3[41];
        private Vector3[] rest;
        private Vector3 ramRest;
        private float flash;
        private bool warningPlayed;
        private int shownPhase;

        public string DamageableId => "core-avatar";
        public bool IsDestroyed => battle?.Defeated == true;
        public bool IsStunned => battle?.GuardOpen == true;
        public float Health => battle?.Health ?? CoreAvatarBattle.Maximum;
        public CoreAvatarBattle Battle => battle;
        public bool Visible => battle?.Engaged == true && !IsDestroyed && ArenaContains(player.transform.position);
        private Vector3 LockPoint => new(battle.TargetX, 0, battle.TargetZ);

        public void Configure(Transform lens, Transform left, Transform right, Transform reserve, Transform piston,
            Transform[] barriers, Material signal, Light lamp, FacilityDoor gate)
        {
            reactor = lens;
            leftEmitter = left;
            rightEmitter = right;
            interceptor = reserve;
            ram = piston;
            risers = barriers;
            beamMaterial = signal;
            statusLight = lamp;
            trustGate = gate;
        }

        public void Initialize(ComputerCampaign model, ControlState state, PlayerMotor motor, FacilityUI view)
        {
            campaign = model;
            battle = model.CoreAvatar;
            controls = state;
            player = motor;
            health = motor.GetComponent<PlayerHealth>();
            ui = view;
            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            effects.spatialBlend = .65f;
            effects.minDistance = 7;
            effects.maxDistance = 35;
            warning = Synthesize("Core / aiming relay", 298, .48f);
            discharge = Synthesize("Core / impulse", 91, .34f);
            for (int i = 0; i < guides.Length; i++)
            {
                var trace = new GameObject("Core attack guide " + i);
                trace.transform.SetParent(transform, false);
                var line = guides[i] = trace.AddComponent<LineRenderer>();
                line.sharedMaterial = beamMaterial;
                line.useWorldSpace = true;
                line.generateLightingData = false;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.enabled = false;
            }
            ramRest = ram.localPosition;
            rest = new Vector3[risers.Length];
            for (int i = 0; i < risers.Length; i++) rest[i] = risers[i].position;
            warningPlayed = battle.Clock >= CoreAvatarBattle.WarningAt;
            UpdatePhase(true);
            if (battle.Defeated) trustGate.UnlockForComputer();
        }

        private static bool ArenaContains(Vector3 p) => p.z is > 131 and < 177.8f && p.x is > -14 and < 34 && p.y is > -.25f and < 8;

        private void Update()
        {
            if (battle == null || IsDestroyed || !controls.WorldRuns) return;
            var feet = player.transform.position;
            if (!campaign.RecoveryAuthorized || !ArenaContains(feet))
            {
                foreach (var guide in guides) guide.enabled = false;
                return;
            }
            if (battle.Begin(feet.x, feet.z)) campaign.RefreshCoreProgress();

            float dt = Time.deltaTime;
            UpdatePhase(false);
            battle.Step(dt, feet.x, feet.z, Emit);
            if (battle.Clock < CoreAvatarBattle.WarningAt) warningPlayed = false;
            else if (!warningPlayed)
            {
                warningPlayed = true;
                if (warning != null) effects.PlayOneShot(warning, .43f);
            }
            flash = Mathf.MoveTowards(flash, 0, dt);
            float aim = Mathf.InverseLerp(CoreAvatarBattle.WarningAt, battle.FireAt, battle.Clock);
            float opening = battle.GuardOpen ? 1 : 0;
            reactor.localRotation = Quaternion.RotateTowards(reactor.localRotation, Quaternion.Euler(opening * -28, 0, 0), dt * 140);
            statusLight.intensity = battle.GuardOpen ? 4.8f : 1.4f + aim;
            TurnEmitter(leftEmitter, dt);
            TurnEmitter(rightEmitter, dt);
            PresentAttack(aim);
            MoveBarriers(dt, feet);
        }

        private void TurnEmitter(Transform emitter, float dt)
        {
            var direction = LockPoint + Vector3.up * 1.15f - emitter.position;
            if (direction.sqrMagnitude > .02f)
                emitter.rotation = Quaternion.RotateTowards(emitter.rotation, Quaternion.LookRotation(direction), 75 * dt);
        }

        private void UpdatePhase(bool immediate)
        {
            if (shownPhase == battle.Phase && !immediate) return;
            shownPhase = battle.Phase;
            statusLight.color = IsDestroyed ? new Color(.29f, .85f, .74f) : shownPhase switch
            {
                1 => new Color(.9f, .67f, .29f),
                2 => new Color(.3f, .66f, 1),
                _ => new Color(1, .26f, .19f)
            };
            interceptor.gameObject.SetActive(shownPhase == 2 && !IsDestroyed);
            if (!immediate)
            {
                warningPlayed = false;
                ui.ShowActionNotice(shownPhase == 2 ? "SUPER · Укрытия поднимаются" : "SUPER · Укрытия меняют сторону");
            }
            if (immediate)
            {
                for (int i = 0; i < risers.Length; i++)
                    risers[i].position = BarrierDestination(i);
            }
        }

        private Vector3 BarrierDestination(int index)
        {
            Vector3 basePosition = rest[index];
            if (IsDestroyed || shownPhase == 1) return basePosition;
            if (shownPhase == 2) return basePosition + Vector3.up * 1.55f;

            // The ram locks one half of the room while the other half has cover.
            // Cascade reverses the cover after sending the player across.
            bool targetLeft = LockPoint.x < transform.position.x;
            bool barrierLeft = basePosition.x < transform.position.x;
            bool raised = battle.Attack == CoreAvatarAttack.Rush ? barrierLeft != targetLeft : barrierLeft == targetLeft;
            return raised ? basePosition + Vector3.up * 1.55f : basePosition;
        }

        private void MoveBarriers(float dt, Vector3 feet)
        {
            for (int i = 0; i < risers.Length; i++)
            {
                var fixture = risers[i];
                var flat = Vector3.ProjectOnPlane(feet - fixture.position, Vector3.up);
                // Wait for the swept area to clear before moving the real collider.
                if (Mathf.Abs(flat.x) < 1.8f && Mathf.Abs(flat.z) < .9f) continue;
                // Hold the pattern while emitters are acquiring a target.
                if (shownPhase == 3 && battle.Clock < CoreAvatarBattle.WarningAt) continue;
                fixture.position = Vector3.MoveTowards(fixture.position, BarrierDestination(i), dt * 1.15f);
            }
        }

        private void PresentAttack(float aim)
        {
            var kind = battle.Attack;
            bool aiming = battle.Clock >= CoreAvatarBattle.WarningAt && battle.Clock < battle.FireAt;
            bool visible = aiming || flash > 0;
            guides[0].enabled = visible;
            guides[1].enabled = visible && kind is CoreAvatarAttack.TwinBeam or CoreAvatarAttack.Cascade;
            ram.localPosition = ramRest + Vector3.forward * (kind == CoreAvatarAttack.Rush ? Mathf.Max(aiming ? aim * .5f : 0, flash * 4) : 0);
            if (!visible) return;
            var tint = flash > 0 ? new Color(1, .75f, .38f) : statusLight.color;
            float width = flash > 0 ? .105f : .034f;
            if (kind is CoreAvatarAttack.FieldPulse or CoreAvatarAttack.Cascade)
            {
                DrawRing(guides[0], LockPoint, kind == CoreAvatarAttack.Cascade ? 3.6f : 3.0f, tint, width);
                if (kind == CoreAvatarAttack.Cascade)
                    DrawRing(guides[1], Vector3.Lerp(LockPoint, transform.position, .42f), 2.8f, tint, width);
                return;
            }
            DrawLane(guides[0], Origin(kind), LockPoint + Vector3.up * 1.15f, tint, width);
            if (kind == CoreAvatarAttack.TwinBeam)
                DrawLane(guides[1], rightEmitter.position, LockPoint + Vector3.up * 1.15f, tint, width);
        }

        private Vector3 Origin(CoreAvatarAttack attack) => attack switch
        {
            CoreAvatarAttack.Interceptor => interceptor.position,
            CoreAvatarAttack.Rush => ram.position,
            _ => leftEmitter.position
        };

        private static Vector3 LaneEnd(Vector3 start, Vector3 target)
        {
            return Physics.Linecast(start, target, out var wall, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore)
                ? wall.point : target;
        }

        private static void DrawLane(LineRenderer line, Vector3 from, Vector3 target, Color tint, float width)
        {
            line.startColor = line.endColor = tint;
            line.widthMultiplier = width;
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, LaneEnd(from, target));
        }

        private void DrawRing(LineRenderer line, Vector3 centre, float radius, Color tint, float width)
        {
            line.startColor = line.endColor = tint;
            line.widthMultiplier = width;
            line.positionCount = ring.Length;
            for (int i = 0; i < ring.Length; i++)
            {
                float angle = i * (2 * Mathf.PI / (ring.Length - 1));
                ring[i] = new Vector3(centre.x + Mathf.Cos(angle) * radius, .07f, centre.z + Mathf.Sin(angle) * radius);
            }
            line.SetPositions(ring);
        }

        private void Emit(CoreAvatarAttack kind)
        {
            flash = .24f;
            if (discharge != null) effects.PlayOneShot(discharge, .6f);
            var witness = player.transform.position + Vector3.up * 1.15f;
            var target = LockPoint + Vector3.up * 1.15f;
            bool hit = kind switch
            {
                CoreAvatarAttack.FieldPulse => InPulse(witness, LockPoint, 3.0f),
                CoreAvatarAttack.Cascade => InPulse(witness, LockPoint, 3.6f) ||
                    InPulse(witness, Vector3.Lerp(LockPoint, transform.position, .42f), 2.8f),
                CoreAvatarAttack.TwinBeam => InLane(witness, leftEmitter.position, target, .58f) ||
                    InLane(witness, rightEmitter.position, target, .58f),
                _ => InLane(witness, Origin(kind), target, kind == CoreAvatarAttack.Rush ? .83f : .55f)
            };
            if (!hit || !controls.CanExplore) return;
            if (kind == CoreAvatarAttack.Rush && player.GetComponent<PlayerWeapon>().TryCounter(Origin(kind))) return;
            health.TakeDamage(campaign.Integrity.Stage >= 2 ? 19 : 25, "Импульс аватара SUPER");
        }

        private static bool InPulse(Vector3 player, Vector3 centre, float radius)
        {
            var flat = Vector3.ProjectOnPlane(player - centre, Vector3.up);
            return player.y < 2.12f && flat.sqrMagnitude <= radius * radius &&
                !Physics.Linecast(new Vector3(centre.x, .32f, centre.z), player, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore);
        }

        private static bool InLane(Vector3 player, Vector3 start, Vector3 target, float width)
        {
            var span = LaneEnd(start, target) - start;
            float t = Mathf.Clamp01(Vector3.Dot(player - start, span) / Mathf.Max(.001f, span.sqrMagnitude));
            return Vector3.Distance(player, start + t * span) < width &&
                !Physics.Linecast(start, player, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore);
        }

        public void TakeDamage(float amount) => Hit(amount, CoreAvatarPart.Shell);
        public void ApplyEmpStun(float duration = 2)
        {
            if (duration > 0 && battle?.Interrupt() == true)
                ui.ShowActionNotice("ЭМИ · Защита ядра открыта");
        }

        public void Hit(float damage, CoreAvatarPart part)
        {
            if (battle == null || !battle.Hit(damage, part, campaign.Integrity.Stage)) return;
            if (!IsDestroyed) return;
            foreach (var guide in guides) guide.enabled = false;
            for (int i = 0; i < risers.Length; i++) risers[i].position = rest[i];
            ram.localPosition = ramRest;
            reactor.localRotation = Quaternion.Euler(-35, 0, 0);
            UpdatePhase(true);
            trustGate.UnlockForComputer();
            campaign.RefreshCoreProgress();
            ui.ShowActionNotice("Защитный аватар отключён");
        }

        private static AudioClip Synthesize(string name, float pitch, float duration)
        {
            const int frequency = 22050;
            var samples = new float[Mathf.RoundToInt(frequency * duration)];
            for (int i = 0; i < samples.Length; i++)
            {
                float time = i / (float)frequency;
                float envelope = Mathf.Sin(Mathf.PI * time / duration);
                samples[i] = .35f * envelope * Mathf.Sin(2 * Mathf.PI * (pitch + 110 * time) * time);
            }
            var clip = AudioClip.Create(name, samples.Length, 1, frequency, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (warning != null) Destroy(warning);
            if (discharge != null) Destroy(discharge);
        }
    }
}
