using Hostfall.Core;
using Hostfall.Player;
using Hostfall.Simulation.Computers;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.World
{
    public sealed class FacilityMiniboss : MonoBehaviour, IDamageable
    {
        [SerializeField] private FacilityMinibossKind kind;
        [SerializeField] private Transform assembly, shield, support, debris;
        [SerializeField] private Transform[] movingParts;
        [SerializeField] private Material signal;
        [SerializeField] private Light pilot;
        private FacilityMinibossState battle;
        private ComputerCampaign campaign;
        private ControlState controls;
        private PlayerMotor player;
        private PlayerWeapon weapon;
        private PlayerHealth health;
        private CharacterController capsule;
        private FacilityUI ui;
        private AudioSource motorSound;
        private RobotNavigation navigation;
        private LineRenderer marker;
        private Vector3 floor, assemblyRest, debrisRest;
        private Vector3[] joints;
        private Quaternion shieldRest;
        private bool near;
        private readonly Vector3[] outline = new Vector3[33];
        private MinibossMove previousMove;
        private readonly RaycastHit[] sweep = new RaycastHit[24];
        private const float MatriarchLandingRadius = 1.4f;

        public string DamageableId => kind.ToString();
        public string Caption => kind == FacilityMinibossKind.SpiderMatriarch ? "Матриарх" : kind == FacilityMinibossKind.SecurityWarden ? "Страж" : "Сборщик";
        public float Health => battle?.Health ?? 1;
        public float Maximum => battle?.Maximum ?? 1;
        public bool IsDestroyed => battle?.Defeated == true;
        public bool IsStunned => battle?.Exposed == true;
        public bool Visible => near && !IsDestroyed && battle?.Engaged == true;
        public FacilityMinibossState Battle => battle;

        public void Configure(FacilityMinibossKind type, Transform movingBody, Transform barrier, Transform reserve,
            Transform thrownPart, Transform[] mechanisms, Material warning, Light indicator)
        {
            kind = type;
            assembly = movingBody;
            shield = barrier;
            support = reserve;
            debris = thrownPart;
            movingParts = mechanisms;
            signal = warning;
            pilot = indicator;
        }

        public void Initialize(ComputerCampaign model, ControlState state, PlayerMotor motor, FacilityUI view)
        {
            campaign = model;
            battle = model.Miniboss(kind);
            controls = state;
            player = motor;
            health = motor.GetComponent<PlayerHealth>();
            weapon = motor.GetComponent<PlayerWeapon>();
            capsule = motor.GetComponent<CharacterController>();
            ui = view;
            floor = transform.position;
            assemblyRest = assembly.localPosition;
            debrisRest = debris != null ? debris.localPosition : Vector3.zero;
            if (shield != null) shieldRest = shield.localRotation;
            joints = new Vector3[movingParts.Length];
            for (int i = 0; i < joints.Length; i++) joints[i] = movingParts[i].localEulerAngles;
            var guide = new GameObject("Miniboss attack outline");
            guide.transform.SetParent(transform, false);
            marker = guide.AddComponent<LineRenderer>();
            marker.sharedMaterial = signal;
            marker.widthMultiplier = .055f;
            marker.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            marker.enabled = false;
            navigation = new RobotNavigation(transform);
            motorSound = gameObject.AddComponent<AudioSource>();
            motorSound.playOnAwake = false;
            motorSound.spatialBlend = 1;
            motorSound.minDistance = 2.1f;
            motorSound.maxDistance = 18;
            motorSound.pitch = kind == FacilityMinibossKind.SpiderMatriarch ? 1.3f : kind == FacilityMinibossKind.SecurityWarden ? .89f : .62f;
            previousMove = battle.Move;
            if (IsDestroyed) DisableBody();
            else if (support != null) support.gameObject.SetActive(battle.Support);
        }

        private void Update()
        {
            if (battle == null || IsDestroyed || !controls.WorldRuns) return;
            var feet = player.transform.position;
            near = Mathf.Abs(feet.y - floor.y) < 3.2f && Vector3.SqrMagnitude(feet - floor) < 110;
            if (!near || !controls.CanExplore)
            {
                marker.enabled = false;
                return;
            }
            var from = transform.position + Vector3.up * 1.15f;
            if (!battle.Engaged && !navigation.Visible(from, player.Eye)) return;
            float dt = Time.deltaTime;
            battle.Step(battle.Health <= Maximum * .42f ? dt * 1.17f : dt, feet.x, feet.z, Resolve);
            if (battle.Move != previousMove)
            {
                previousMove = battle.Move;
                ui.ShowActionNotice(Caption + " · " + Warning(previousMove));
                if (VentReleaseController.MetalScrape != null) motorSound.PlayOneShot(VentReleaseController.MetalScrape, .56f);
            }
            Vector3 target = new(battle.TargetX, floor.y, battle.TargetZ);
            Vector3 direction = Vector3.ProjectOnPlane(target - floor, Vector3.up);
            if (battle.Clock < .3f) navigation.Face(direction, dt, 180);
            Present(target, direction);
            if (battle.Move == MinibossMove.Magnet && battle.Clock is > .63f and < 1.28f &&
                DirectionInReach(feet, floor, 6.0f) && navigation.Visible(from, player.Eye))
                capsule.Move((floor - feet).normalized * (dt * 1.65f));
        }

        private void Present(Vector3 target, Vector3 direction)
        {
            var move = battle.Move;
            float charge = Mathf.InverseLerp(.3f, battle.ImpactAt, battle.Clock);
            float recovery = Mathf.InverseLerp(battle.ImpactAt + .1f, battle.ImpactAt + 1.23f, battle.Clock);
            float live = charge * (1 - recovery);
            var forward = direction.normalized;
            bool pounce = IsMatriarchPounce(move);
            var landing = pounce ? MatriarchLanding(direction) : target;
            float advance = move is MinibossMove.ShieldRam or MinibossMove.Crush
                ? Mathf.Min(3.6f, Mathf.Max(0, direction.magnitude - .9f)) * Mathf.SmoothStep(0, 1, live) : 0;
            var travel = pounce ? (landing - floor) * Mathf.SmoothStep(0, 1, live) : forward * ClearAdvance(forward, advance);
            float lift = pounce && battle.Clock < battle.ImpactAt
                ? Mathf.Sin(Mathf.PI * Mathf.Sqrt(charge)) * (move == MinibossMove.Skitter ? .44f : campaign.VentSealed ? .88f : 1.18f)
                : 0;
            assembly.localPosition = assemblyRest + transform.InverseTransformVector(travel + Vector3.up * lift);
            for (int i = 0; i < movingParts.Length; i++)
            {
                float rotation = move == MinibossMove.Blades ? live * 540 * (i % 2 == 0 ? 1 : -1) :
                    (move is MinibossMove.Sweep or MinibossMove.Grab or MinibossMove.Leap ? 55 : 19) * live * (i % 2 == 0 ? 1 : -1);
                Vector3 motion = move == MinibossMove.Blades ? Vector3.forward * rotation : new Vector3(rotation, 0, rotation * .25f);
                movingParts[i].localRotation = Quaternion.Euler(joints[i] + motion);
            }
            if (shield != null)
                shield.localRotation = shieldRest * Quaternion.Euler(0, battle.PowerCut || battle.Exposed ? 58 : 0, 0);
            if (debris != null)
            {
                bool thrown = move == MinibossMove.Debris && battle.Clock > .39f && battle.Clock < battle.ImpactAt + .28f;
                debris.gameObject.SetActive(thrown);
                if (thrown)
                {
                    var start = floor + Vector3.up * 1.72f;
                    var end = target + Vector3.up * .71f;
                    float path = navigation.RayLimit(start, (end - start).normalized, Vector3.Distance(start, end));
                    debris.position = start + (end - start).normalized * (path * charge) + Vector3.up * (1.35f * Mathf.Sin(charge * Mathf.PI));
                    debris.rotation = Quaternion.Euler(charge * 380, charge * 230, charge * 151);
                }
                else debris.localPosition = debrisRest;
            }
            pilot.intensity = battle.Exposed ? 5f : 1.4f + charge;
            marker.enabled = battle.Clock is > .28f && battle.Clock < battle.ImpactAt &&
                move is not (MinibossMove.Repair or MinibossMove.Brood or MinibossMove.Reinforce);
            if (marker.enabled) Paint(landing);
        }

        private bool IsMatriarchPounce(MinibossMove move) =>
            kind == FacilityMinibossKind.SpiderMatriarch && move is MinibossMove.Leap or MinibossMove.Skitter;

        private Vector3 MatriarchLanding(Vector3 direction)
        {
            var heading = direction.normalized;
            float distance = ClearAdvance(heading, Mathf.Clamp(direction.magnitude - .55f, 0, 3.6f));
            return floor + heading * distance;
        }

        private float ClearAdvance(Vector3 heading, float distance)
        {
            if (distance <= .001f) return 0;
            int count = Physics.SphereCastNonAlloc(floor + Vector3.up * .8f, .66f, heading, sweep, distance,
                CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore);
            if (count == sweep.Length) return 0;
            for (int i = 0; i < count; i++)
                if (!sweep[i].transform.IsChildOf(transform)) distance = Mathf.Min(distance, Mathf.Max(0, sweep[i].distance - .07f));
            return distance;
        }

        private void Paint(Vector3 target)
        {
            var move = battle.Move;
            if (move is MinibossMove.ShieldRam or MinibossMove.Crush or MinibossMove.Grab or MinibossMove.Debris)
            {
                marker.positionCount = 2;
                var origin = floor + Vector3.up * .32f;
                var delta = target + Vector3.up * .32f - origin;
                marker.SetPosition(0, origin);
                marker.SetPosition(1, origin + delta.normalized * navigation.RayLimit(origin, delta.normalized, delta.magnitude));
                return;
            }
            marker.positionCount = outline.Length;
            var center = move is MinibossMove.Web or MinibossMove.Leap or MinibossMove.Skitter ? target : floor;
            float radius = IsMatriarchPounce(move) ? MatriarchLandingRadius : move == MinibossMove.Magnet ? 6f : move == MinibossMove.Web ? 2.75f : 2.25f;
            for (int i = 0; i < outline.Length; i++)
            {
                float angle = i * (2 * Mathf.PI / (outline.Length - 1));
                outline[i] = center + new Vector3(Mathf.Cos(angle) * radius, .07f, Mathf.Sin(angle) * radius);
            }
            marker.SetPositions(outline);
        }

        private void Resolve(MinibossMove move)
        {
            var target = new Vector3(battle.TargetX, floor.y, battle.TargetZ);
            if (move is MinibossMove.Brood or MinibossMove.Reinforce)
            {
                if (support != null) support.gameObject.SetActive(true);
                return;
            }
            var feet = player.transform.position;
            bool pounce = IsMatriarchPounce(move);
            Vector3 landing = pounce ? MatriarchLanding(target - floor) : target;
            Vector3 sight = (pounce ? landing : assembly.position) + Vector3.up * .8f;
            if (!navigation.Visible(sight, player.Eye)) return;
            float radius = move switch
            {
                MinibossMove.Grab => 2.05f, MinibossMove.Magnet => 5.8f, MinibossMove.Web => 2.75f,
                MinibossMove.Debris => 1.1f, MinibossMove.Crush or MinibossMove.ShieldRam => 1.45f, _ => 2.25f
            };
            var source = pounce ? landing : move is MinibossMove.Web or MinibossMove.Debris
                ? target : move is MinibossMove.Crush or MinibossMove.ShieldRam ? assembly.position : floor;
            if (pounce) radius = MatriarchLandingRadius;
            if (!DirectionInReach(feet, source, radius))
            {
                if (battle.Support && support != null && Vector3.Distance(support.position, player.Eye) < 7 &&
                    navigation.Visible(support.position, player.Eye))
                    health.TakeDamage(12, Caption + " · импульс поддержки");
                return;
            }
            if (move == MinibossMove.Grab && weapon.TryCounter(sight)) return;
            if (move == MinibossMove.Web) player.ApplyMobilityLoss(2.4f);
            health.TakeDamage(move is MinibossMove.Web or MinibossMove.Magnet ? 13 : 28, Caption);
            if (move is MinibossMove.Grab or MinibossMove.Leap) player.StartleFromImpact();
        }

        private static bool DirectionInReach(Vector3 feet, Vector3 center, float radius) =>
            Mathf.Abs(feet.y - center.y) < 1.4f && Vector3.ProjectOnPlane(feet - center, Vector3.up).sqrMagnitude <= radius * radius;

        private static string Warning(MinibossMove move) => move switch
        {
            MinibossMove.Web => "электрическая сеть", MinibossMove.Magnet => "магнитное поле",
            MinibossMove.Grab => "захват", MinibossMove.Repair => "восстановление брони",
            MinibossMove.Brood or MinibossMove.Reinforce => "подкрепление",
            MinibossMove.Debris => "бросок", MinibossMove.Sweep or MinibossMove.Blades => "круговой удар", _ => "рывок"
        };

        public void TakeDamage(float amount) => Hit(amount, false);
        public void Hit(float amount, bool component)
        {
            if (battle == null) return;
            bool front = Vector3.Dot(transform.forward, player.transform.position - transform.position) > 0;
            if (battle.Hit(amount, component, front) && IsDestroyed) DisableBody();
        }

        public void ApplyEmpStun(float duration = 1.5f)
        {
            if (duration > 0) battle?.Interrupt();
        }

        public bool CutShield()
        {
            if (battle?.CutPower() != true) return false;
            ui.ShowActionNotice("Генератор щита отключён");
            return true;
        }

        private void DisableBody()
        {
            marker.enabled = false;
            pilot.enabled = false;
            if (support != null) support.gameObject.SetActive(false);
            foreach (var shape in GetComponentsInChildren<Collider>()) shape.enabled = false;
            assembly.localPosition = assemblyRest - Vector3.up * .24f;
            assembly.localRotation = Quaternion.Euler(0, 0, -9);
        }
    }
}
