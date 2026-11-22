using Hostfall.Player;
using Hostfall.Simulation;
using System.Linq;
using UnityEngine;

namespace Hostfall.World
{
    public sealed partial class CrawlerMinibossController
    {
        private const float SlamRadius = 4.35f;
        private CrawlerAttack encounter;
        private TrackedSentryController guardian;
        private PlayerHealth health;
        private Transform emitter;
        private Transform turretPivot, recoilPivot;
        private Transform[] wheels, treads;
        private Vector3 previousPosition, recoilRest;
        private float previousYaw, recoilAmount, leftTrack, rightTrack;
        private LineRenderer aimLine;
        private Material aimMaterial;
        private Vector2 xBounds, zBounds;
        private Vector3 traceStart, traceEnd;
        private float traceRemaining;
        private System.Action<int> emit;
        private System.Action<float> drive;
        private static readonly float[] suppressionAngles = { -10, 1, 10, -5, 5 };

        public CrawlerAttackKind CurrentAttack => encounter?.Kind ?? CrawlerAttackKind.None;
        public CrawlerAttackPhase AttackPhase => encounter?.Phase ?? CrawlerAttackPhase.Idle;
        internal RobotCombatState EncounterHealth => combat?.Model;
        internal bool IsEngaged => encounter != null && guardian.enabled && !combat.Model.IsDead &&
            (combat.Model.Data.memoryTime > 0 || encounter.Phase != CrawlerAttackPhase.Idle);

        private void InitializeEncounter()
        {
            guardian = GetComponent<TrackedSentryController>();
            health = player.GetComponent<PlayerHealth>();
            var parts = GetComponentsInChildren<Transform>();
            emitter = parts.FirstOrDefault(part => part.name == "Emitter");
            turretPivot = parts.FirstOrDefault(part => part.name == "MANTIS turret");
            recoilPivot = parts.FirstOrDefault(part => part.name == "MANTIS recoil");
            if (recoilPivot != null) recoilRest = recoilPivot.localPosition;
            wheels = parts.Where(part => part.name == "MANTIS wheel").ToArray();
            treads = parts.Where(part => part.name.StartsWith("MANTIS tread ")).ToArray();
            previousPosition = transform.position;
            previousYaw = transform.eulerAngles.y;
            xBounds = home.x < 0 ? new Vector2(-23, -9) : new Vector2(-3, 3);
            zBounds = home.x < 0 ? new Vector2(21, 35) : new Vector2(67, 88);
            var data = combat.Model.Data;
            encounter = new CrawlerAttack(data.crawlerAttack ??= new CrawlerAttackData());
            emit = EmitAttack;
            drive = DriveCharge;

            var guide = new GameObject("MANTIS attack guide");
            guide.transform.SetParent(transform, false);
            aimLine = guide.AddComponent<LineRenderer>();
            aimLine.enabled = false;
            aimLine.useWorldSpace = true;
            aimLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            aimLine.receiveShadows = false;
            aimLine.widthMultiplier = .045f;
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                aimMaterial = new Material(shader);
                aimLine.sharedMaterial = aimMaterial;
            }
        }

        private Vector3 Muzzle => emitter != null ? emitter.position : transform.position + Vector3.up * 1.45f + transform.forward * 1.8f;

        private void UpdateEncounter(RobotCombatData data, float dt)
        {
            if (!guardian.enabled || health.CurrentHealth <= 0)
            {
                aimLine.enabled = false;
                return;
            }
            data.stun = Mathf.Max(0, data.stun - dt);
            data.attackCooldown = Mathf.Max(0, data.attackCooldown - dt);
            traceRemaining = Mathf.Max(0, traceRemaining - dt);
            if (data.stun > 0)
            {
                encounter.Interrupt();
                traceRemaining = 0;
                aimLine.enabled = false;
                return;
            }

            var offset = player.transform.position - transform.position;
            float height = Mathf.Abs(offset.y);
            offset.y = 0;
            float distance = offset.magnitude;
            float range = data.sensor > 0 ? 17 : 9;
            bool sees = distance < range && height < 1.65f &&
                Vector3.Angle(transform.forward, offset) < 77 &&
                navigation.Visible(transform.position + Vector3.up * 1.5f, player.Eye);
            if (sees)
            {
                data.memoryTime = 5.5f;
                data.memoryX = player.transform.position.x;
                data.memoryZ = player.transform.position.z;
            }
            else
                data.memoryTime = Mathf.Max(0, data.memoryTime - dt);

            if (encounter.Phase != CrawlerAttackPhase.Idle)
            {
                encounter.Step(dt, emit, drive);
                UpdateGuide();
                return;
            }
            if (sees && data.attackCooldown == 0)
            {
                bool overheated = combat.Model.Health <= combat.Model.Maximum * .4f || combat.Model.CoreExposed;
                var kind = encounter.Select(distance, combat.Mobility, overheated);
                bool turretAttack = turretPivot != null && kind is CrawlerAttackKind.CannonBurst or CrawlerAttackKind.Suppression;
                bool settled;
                if (turretAttack)
                {
                    turretPivot.rotation = Quaternion.RotateTowards(turretPivot.rotation, Quaternion.LookRotation(offset), 125 * dt);
                    settled = Vector3.Angle(turretPivot.forward, offset) < 6;
                }
                else
                    settled = navigation.Face(offset, dt) && Vector3.Angle(transform.forward, offset) < 9;
                if (settled)
                {
                    Vector3 line = player.Eye - Muzzle;
                    float yaw = Mathf.Repeat(Mathf.Atan2(line.x, line.z) * Mathf.Rad2Deg, 360);
                    float pitch = Mathf.Clamp(-Mathf.Atan2(line.y, new Vector2(line.x, line.z).magnitude) * Mathf.Rad2Deg, -35, 35);
                    if (kind == CrawlerAttackKind.HydraulicCharge)
                        yaw = Mathf.Repeat(transform.eulerAngles.y, 360);
                    if (encounter.Commit(kind, yaw, pitch, overheated))
                        guardian.PlayGuardianCue(impact: false);
                }
            }
            else
            {
                if (data.memoryTime > 0)
                    goal = new Vector3(data.memoryX, home.y, data.memoryZ);
                else
                {
                    phase += dt * .3f;
                    goal = home + new Vector3(Mathf.Sin(phase) * 2, 0, Mathf.Cos(phase) * 3);
                }
                if (distance > 4 || !sees)
                    navigation.Move(goal, 1.65f * combat.Mobility, dt, xBounds, zBounds);
            }
            UpdateGuide();
        }

        private Vector3 Aim(float extraYaw = 0) =>
            Quaternion.Euler(encounter.Pitch, encounter.Heading + extraYaw, 0) * Vector3.forward;

        private void EmitAttack(int pulse)
        {
            guardian.PlayGuardianCue(impact: true);
            if (encounter.Kind == CrawlerAttackKind.GroundSlam)
            {
                var delta = player.transform.position - transform.position;
                if (Mathf.Abs(delta.y) < .95f && new Vector2(delta.x, delta.z).magnitude < SlamRadius &&
                    navigation.Visible(transform.position + Vector3.up, player.Eye))
                    health.TakeDamage(26, "Удар MANTIS-6");
                return;
            }
            bool suppression = encounter.Kind == CrawlerAttackKind.Suppression;
            recoilAmount = suppression ? .13f : .22f;
            var ray = Aim(suppression ? suppressionAngles[pulse] : 0);
            traceStart = Muzzle;
            float length = navigation.RayLimit(traceStart, ray, 18);
            traceEnd = traceStart + ray * length;
            traceRemaining = .11f;
            var fromEmitter = player.Eye - traceStart;
            float along = Vector3.Dot(fromEmitter, ray);
            if (along > 0 && along < length &&
                (fromEmitter - ray * along).sqrMagnitude < .48f * .48f)
                health.TakeDamage(suppression ? 8 : 12, "Орудие MANTIS-6");
        }

        private void DriveCharge(float seconds)
        {
            var forward = Quaternion.Euler(0, encounter.Heading, 0) * Vector3.forward;
            float travel = (encounter.Overheated ? 7.3f : 6.4f) * combat.Mobility * seconds;
            var local = transform.InverseTransformPoint(player.transform.position);
            if (Mathf.Abs(local.y) < 1 && Mathf.Abs(local.x) < 1.88f && local.z > 0 &&
                local.z < 2.1f + travel && navigation.Visible(transform.position + Vector3.up, player.Eye))
            {
                if (encounter.SpendContact())
                    health.TakeDamage(33, "Таран MANTIS-6");
                encounter.StopCharge(hitWall: false);
                return;
            }
            if (!navigation.DriveStraight(forward, travel, xBounds, zBounds))
            {
                guardian.PlayGuardianCue(impact: true);
                encounter.StopCharge(hitWall: true);
            }
        }

        private void UpdateGuide()
        {
            bool warning = encounter.Phase == CrawlerAttackPhase.Telegraph;
            if (lamp != null)
            {
                lamp.color = encounter.Overheated ? new Color(1, .34f, .07f) : new Color(1, .12f, .09f);
                lamp.intensity = warning ? 4 : 1;
            }
            aimLine.enabled = warning || traceRemaining > 0;
            if (!aimLine.enabled) return;
            aimLine.loop = warning && encounter.Kind == CrawlerAttackKind.GroundSlam;
            var color = warning ? new Color(1, .47f, .12f, .85f) : new Color(1, .85f, .49f, .94f);
            aimLine.startColor = aimLine.endColor = color;
            aimLine.widthMultiplier = warning ? .045f : .095f;
            if (aimLine.loop)
            {
                const int sides = 48;
                aimLine.positionCount = sides;
                for (int i = 0; i < sides; i++)
                {
                    float angle = i * Mathf.PI * 2 / sides;
                    aimLine.SetPosition(i, transform.position + new Vector3(Mathf.Sin(angle) * SlamRadius, .075f, Mathf.Cos(angle) * SlamRadius));
                }
                return;
            }
            if (!warning)
            {
                SetGuide(traceStart, traceEnd);
                return;
            }
            var from = encounter.Kind == CrawlerAttackKind.HydraulicCharge ?
                transform.position + Vector3.up * .075f + transform.forward * 1.85f : Muzzle;
            if (encounter.Kind == CrawlerAttackKind.Suppression)
            {
                var left = Aim(-10);
                var right = Aim(10);
                aimLine.positionCount = 3;
                aimLine.SetPosition(0, from + left * navigation.RayLimit(from, left, 18));
                aimLine.SetPosition(1, from);
                aimLine.SetPosition(2, from + right * navigation.RayLimit(from, right, 18));
                return;
            }
            var direction = encounter.Kind == CrawlerAttackKind.HydraulicCharge ? transform.forward : Aim();
            SetGuide(from, from + direction * navigation.RayLimit(from, direction,
                encounter.Kind == CrawlerAttackKind.HydraulicCharge ? 6.3f : 18));
        }

        private void SetGuide(Vector3 a, Vector3 b)
        {
            aimLine.positionCount = 2;
            aimLine.SetPosition(0, a);
            aimLine.SetPosition(1, b);
        }

        private void AnimateEncounter(float dt)
        {
            if (turretPivot == null || !guardian.enabled) return;
            if (encounter.Phase is CrawlerAttackPhase.Telegraph or CrawlerAttackPhase.Active &&
                encounter.Kind is CrawlerAttackKind.CannonBurst or CrawlerAttackKind.Suppression)
                turretPivot.rotation = Quaternion.RotateTowards(turretPivot.rotation,
                    Quaternion.Euler(0, encounter.Heading, 0), 125 * dt);
            recoilAmount = Mathf.MoveTowards(recoilAmount, 0, dt * 1.25f);
            if (recoilPivot != null) recoilPivot.localPosition = recoilRest - Vector3.forward * recoilAmount;

            float distance = Vector3.Dot(transform.position - previousPosition, transform.forward);
            float turn = Mathf.DeltaAngle(previousYaw, transform.eulerAngles.y) * Mathf.Deg2Rad * 1.3f;
            previousPosition = transform.position;
            previousYaw = transform.eulerAngles.y;
            if (Mathf.Abs(distance) + Mathf.Abs(turn) < .0001f) return;
            leftTrack = Mathf.Repeat(leftTrack + distance + turn, .19f);
            rightTrack = Mathf.Repeat(rightTrack + distance - turn, .19f);
            foreach (var wheel in wheels)
                wheel.Rotate(Vector3.right, (distance + (wheel.localPosition.x < 0 ? turn : -turn)) * 185f, Space.Self);
            foreach (var tread in treads)
            {
                string treadName = tread.name;
                float progress = treadName.EndsWith(" L") ? leftTrack : rightTrack;
                tread.localPosition = Vector3.forward * (treadName.Contains(" lower ") ? -progress : progress);
            }
        }

        private void OnDestroy()
        {
            if (aimMaterial != null) Destroy(aimMaterial);
        }
    }
}
