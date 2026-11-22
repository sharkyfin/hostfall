using Hostfall.Core;
using Hostfall.Persistence;
using Hostfall.Player;
using Hostfall.Simulation;
using Hostfall.Simulation.Computers;
using UnityEngine;

namespace Hostfall.World
{
    public sealed class WatcherController : MonoBehaviour, IDamageable
    {
        public const string CoreProcess="super-node --leader --replicate";
        [SerializeField] private string enemyId="watcher-r17";
        [SerializeField] private Light indicator;
        [SerializeField] private Transform sensor;
        [SerializeField] private Animator animator;
        [SerializeField] private Vector3 patrolA=new(-.4f,0,2.2f),patrolB=new(-.4f,0,5.6f);
        [SerializeField] private Vector2 xBounds=new(-.58f,.58f),zBounds=new(1.65f,5.8f);

        private RobotNavigation navigation;
        private RobotCombatController combat;
        private FacilitySimulation simulation;
        private ServerNetworkController network;
        private WatcherSquads squad;
        private TerminalAmbush terminalAmbush;
        private bool terminalLeap;
        private Vector3 leapRest;
        private Vector3 fallStart;
        private float leapLift;
        private bool squadMemory, rangedWindup;
        private bool networkTarget;
        private ContainmentSession containment;
        private Vector3 calibrationA, calibrationB, captureA, captureB;
        private bool Transferring => containment != null && containment.Order != SentryOrder.Patrol;
        private bool CanLeavePatrolArea => simulation.InteractiveComputers && containment == null &&
            !Id.StartsWith("aux-watch-logistics-", System.StringComparison.Ordinal);

        public (Vector3 first, Vector3 last) PatrolRoute => containment?.Order switch
        {
            SentryOrder.Calibration => (calibrationA, calibrationB),
            SentryOrder.Capture => (captureA, captureB),
            _ => (patrolA, patrolB)
        };

        public void BindContainment(ContainmentSession session, Vector3 inspectionStart, Vector3 inspectionEnd,
            Vector3 bayStart, Vector3 bayEnd)
        {
            containment = session;
            calibrationA = inspectionStart;
            calibrationB = inspectionEnd;
            captureA = bayStart;
            captureB = bayEnd;
        }
        public bool HasNetworkTarget=>networkTarget;
        public bool NetworkDisconnected=>network!=null&&network.Disconnected;
        public WatcherRole TacticalRole { get; private set; }
        internal bool CanCoordinate => isActiveAndEnabled && !IsDestroyed && !Transferring &&
            !NetworkDisconnected && State.Mode is not (WatcherMode.Maintenance or WatcherMode.Offline or WatcherMode.Jammed);
        internal void BindSquad(WatcherSquads coordinator) => squad = coordinator;
        public void BindNetwork(ServerNetworkController channel){network=channel;}
        internal void BindTerminalAmbush(TerminalAmbush ambush) => terminalAmbush = ambush;

        internal void BeginTerminalLeap()
        {
            terminalLeap = true;
            if (animator != null)
            {
                leapRest = animator.transform.localPosition;
                leapLift = Mathf.Clamp(player.Eye.y - TerminalLeapTarget.y - .10f, .36f, 1.0f);
                animator.CrossFadeInFixedTime("Attack", .045f);
                animationState = "Attack";
            }
        }

        internal Vector3 TerminalLeapTarget => sensor != null ? sensor.position : transform.position + Vector3.up;

        internal bool TerminalLeapHasContact(Vector3 eye) =>
            navigation.Visible(TerminalLeapTarget, eye);

        internal void PresentTerminalLeap(Vector3 direction, float travel, float dt)
        {
            if (direction.sqrMagnitude > .012f)
            {
                navigation.Face(direction, dt, 800);
                if (travel > 0 && direction.magnitude > .65f)
                    navigation.DriveStraight(direction.normalized, Mathf.Min(dt * 4.0f, direction.magnitude - .65f), xBounds, zBounds);
                if (sensor != null && (player.Eye - sensor.position).sqrMagnitude > .001f)
                    sensor.rotation = Quaternion.LookRotation(player.Eye - sensor.position);
            }
            if (animator != null && animator.transform != transform)
                animator.transform.localPosition = leapRest + Vector3.up * Mathf.Sin(travel * Mathf.PI * .5f) * leapLift;
        }

        internal void BeginTerminalFall()
        {
            if (animator != null) fallStart = animator.transform.localPosition;
        }

        internal void PresentTerminalFall(Vector3 away, float progress, float pushTime)
        {
            float fraction = Mathf.Clamp01(progress);
            if (animator != null && animator.transform != transform)
                animator.transform.localPosition = Vector3.Lerp(fallStart, leapRest, fraction * fraction);
            if (pushTime > 0 && away.sqrMagnitude > .01f)
                navigation.DriveStraight(away.normalized, 2.9f * pushTime, xBounds, zBounds);
        }

        internal void EndTerminalLeap(bool parried)
        {
            if (animator != null && animator.transform != transform) animator.transform.localPosition = leapRest;
            terminalLeap = false;
            windupTimer = 0;
            contactCooldown = 3;
            squad?.Release(this);
            State.SetMode(WatcherMode.Alert);
            if (parried) ReceiveKnockback(transform.position - player.transform.position, .12f);
        }
        public void ReceiveNetworkTarget(Vector3 point)
        {
            if(!isActiveAndEnabled||IsDestroyed||Transferring||containment?.Complete==true||NetworkDisconnected||network==null||!network.CommandOnline||State.Mode is WatcherMode.Alert or WatcherMode.AttackWindup or WatcherMode.Jammed or WatcherMode.Offline or WatcherMode.Maintenance)return;
            networkTarget=true;lastKnownPosition=point;searchTimer=3;suspicion=.8f;State.SetMode(WatcherMode.Search);
        }
        public void ForgetNetworkTarget()
        { if(!networkTarget)return;networkTarget=false;searchTimer=suspicion=windupTimer=0;if(!IsDestroyed)State.SetMode(WatcherMode.Patrol); }
        private PlayerMotor player;
        private ControlState controls;
        private AudioSource audioSource;
        private float scan,contactCooldown;
        private int direction=1;
        private string animationState="";

        // AI perception & state tracking
        private float suspicion;
        private float searchTimer;
        private float windupTimer;
        private float stunTimer;
        private float staggerTimer;
        private Vector3 lastKnownPosition;
        private WatcherMode previousMode = WatcherMode.Patrol;

        // Procedural Audio Clips
        private static AudioClip clipAlert, clipSuspicious, clipWindup, clipAttack, clipSearch, clipJammed;
        private static bool audioGenerated;

        public string Id=>enemyId;
        public string DamageableId => enemyId;
        public string AnimationState=>animationState;
        public WatcherState State { get; }=new();
        public float Suspicion => suspicion;
        public float SearchTimer => searchTimer;
        public float WindupTimer => windupTimer;
        public Vector3 LastKnownPosition => lastKnownPosition;

        [SerializeField] private float maxHealth = 100f;
        private float currentHealth = 100f;
        private bool isDestroyed;

        public bool IsDestroyed => combat?.Model?.IsDead ?? isDestroyed;
        public float Health => combat?.Model?.Health ?? currentHealth;
        public bool IsStunned => stunTimer > 0f;

        public void TakeDamage(float amount)
        {
            if(combat?.Model!=null&&!combat.CompletingDeath){combat.Hit(amount,RobotHitRegion.Hull,transform.position);return;}
            if (isDestroyed) return;
            currentHealth -= amount;
            staggerTimer = 0.08f;
            PlayCue(clipJammed, 0.85f);
            SpawnHitSparks();
            if (currentHealth <= 0f)
            {
                isDestroyed = true;
                currentHealth = 0f;
                State.SetMode(WatcherMode.Offline);
                if (indicator != null) indicator.enabled = false;
                var capsule = GetComponent<CapsuleCollider>();
                if (capsule != null) capsule.enabled = false;
                if (animator != null) animator.CrossFadeInFixedTime("TurnOff", 0.05f);
                PlayCue(clipAttack, 1.0f);
            }
            else
            {
                State.SetMode(WatcherMode.Alert);
                suspicion = 1f;
                if (player != null) lastKnownPosition = player.transform.position;
            }
        }

        public void CaptureCombatTimers(RobotCombatData d)
        { d.networkMemory=networkTarget;d.squadMemory=squadMemory;d.mode=(int)State.Mode;d.memoryX=lastKnownPosition.x;d.memoryZ=lastKnownPosition.z;d.memoryTime=Mathf.Clamp(searchTimer,0,20);d.stun=Mathf.Clamp(stunTimer,0,20);d.attackCooldown=contactCooldown;d.windup=Mathf.Clamp(windupTimer,0,3);d.rangedWindup=rangedWindup&&State.Mode==WatcherMode.AttackWindup; }
        public void RestoreCombatTimers(RobotCombatData d)
        { networkTarget=d.networkMemory;squadMemory=d.squadMemory;lastKnownPosition=new Vector3(d.memoryX,transform.position.y,d.memoryZ);searchTimer=d.memoryTime;stunTimer=d.stun;contactCooldown=d.attackCooldown;windupTimer=d.windup;rangedWindup=d.rangedWindup;State.SetMode((WatcherMode)d.mode); }
        public void ReactToHit(Vector3 point)
        {
            if (player == null || squad == null || Sees(player.Eye))
            {
                HearShot(player != null ? player.transform.position : transform.position);
                return;
            }
            if (isDestroyed || NetworkDisconnected || State.Mode is WatcherMode.Alert or WatcherMode.Search or WatcherMode.AttackWindup)
                return;
            var bearing = Vector3.ProjectOnPlane(point - transform.position, Vector3.up).normalized;
            HearShot(transform.position + bearing * 2);
        }
        public void HearShot(Vector3 point)
        {
            if(isDestroyed||NetworkDisconnected)return;
            networkTarget=false;squadMemory=false;lastKnownPosition=point;
            suspicion=1;searchTimer=5;State.SetMode(WatcherMode.Alert);
        }

        public void ApplyEmpStun(float duration = 7.0f)
        {
            if (isDestroyed) return;
            stunTimer = duration;
            staggerTimer = 0.5f;
            State.SetMode(WatcherMode.Jammed);
            if (indicator != null)
            {
                indicator.enabled = true;
                indicator.color = new Color(0.2f, 0.7f, 1.0f);
            }
            PlayCue(clipJammed, 0.95f);
            SpawnHitSparks();
        }

        internal void ReceiveKnockback(Vector3 direction, float distance)
        {
            if (IsDestroyed || Transferring || containment?.Complete == true) return;
            stunTimer = Mathf.Max(stunTimer, .38f);
            windupTimer = 0;
            squad?.Release(this);
            var planar = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            if (planar.sqrMagnitude > .01f)
                navigation?.DriveStraight(planar, distance, xBounds, zBounds);
        }

        private void SpawnHitSparks()
        {
            var sparkObj = new GameObject("Robot Spark Burst");
            sparkObj.transform.position = sensor != null ? sensor.position : transform.position + Vector3.up * 1.2f;
            var l = sparkObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.4f, 0.85f, 1.0f);
            l.range = 3.5f;
            l.intensity = 4.0f;
            Destroy(sparkObj, 0.12f);
        }

        public void Configure(Transform eye,Light lamp,Animator model=null,string id="watcher-r17",Vector3? first=null,Vector3? last=null,Vector2? xb=null,Vector2? zb=null)
        {
            sensor=eye; indicator=lamp; animator=model; enemyId=id;
            if(first.HasValue) patrolA=first.Value; if(last.HasValue) patrolB=last.Value;
            if(xb.HasValue) xBounds=xb.Value;
            else if(id=="sentry-r18") xBounds=new Vector2(-1.5f,1.5f);
            if(zb.HasValue) zBounds=zb.Value;
            else if(id=="sentry-r18") zBounds=new Vector2(8.2f,14.4f);
        }

        public void Initialize(FacilitySimulation model,PlayerMotor target,ControlState state)
        {
            simulation=model; player=target; controls=state; navigation=new RobotNavigation(transform,GetComponent<CapsuleCollider>().radius);
            EnsureAudio();
        }
        internal void BindCombat(RobotCombatController controller) => combat = controller;

        private void EnsureAudio()
        {
            if (audioSource == null)
            {
                if (!TryGetComponent<AudioSource>(out audioSource))
                    audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.minDistance = 1.5f;
                audioSource.maxDistance = 14f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
            }
            if (!audioGenerated)
            {
                clipAlert = GenerateTone("WATCHER/Alert", 0.32f, t => 0.25f * (Mathf.Sin(2 * Mathf.PI * ((int)(t * 16) % 2 == 0 ? 880 : 660) * t) + 0.1f * Mathf.Sin(2 * Mathf.PI * 1760 * t)) * (1 - t / 0.35f));
                clipSuspicious = GenerateTone("WATCHER/Suspicious", 0.22f, t => 0.18f * Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(480, 680, t / 0.22f) * t) * (1 - t / 0.22f));
                clipWindup = GenerateTone("WATCHER/Windup", 0.42f, t => 0.22f * Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(300, 1200, Mathf.Pow(t / 0.42f, 2)) * t) * (0.3f + 0.7f * (t / 0.42f)));
                clipAttack = GenerateTone("WATCHER/Attack", 0.25f, t => 0.35f * Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(140, 50, t / 0.25f) * t) * Mathf.Exp(-t * 12));
                clipSearch = GenerateTone("WATCHER/Search", 0.28f, t => 0.16f * Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(660, 380, t / 0.28f) * t) * (1 - t / 0.28f));
                clipJammed = GenerateTone("WATCHER/Jammed", 0.4f, t => 0.2f * (Random.value * 2 - 1) * Mathf.Exp(-t * 6) + 0.15f * Mathf.Sin(2 * Mathf.PI * 120 * t));
                audioGenerated = true;
            }
        }

        private static AudioClip GenerateTone(string name, float duration, System.Func<float, float> synth)
        {
            const int rate = 22050;
            int samples = (int)(rate * duration);
            var data = new float[samples];
            for (int i = 0; i < samples; i++) data[i] = Mathf.Clamp(synth(i / (float)rate), -1f, 1f);
            var clip = AudioClip.Create(name, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void PlayCue(AudioClip clip, float volume = 0.6f)
        {
            if (audioSource != null && clip != null && controls != null && controls.WorldRuns)
                audioSource.PlayOneShot(clip, volume);
        }

        public RobotStateData Capture()=>new()
        { id=Id,position=transform.position,sensorEuler=(Quaternion.Inverse(transform.rotation)*sensor.rotation).eulerAngles,bodyYaw=transform.eulerAngles.y,scan=scan,direction=direction,contactCooldown=Mathf.Clamp(contactCooldown,0,3),destroyed=isDestroyed };

        public void Restore(RobotStateData state)
        {
            transform.SetPositionAndRotation(state.position,Quaternion.Euler(0,state.bodyYaw,0)); sensor.rotation=transform.rotation*Quaternion.Euler(state.sensorEuler);
            scan=state.scan; direction=state.direction; contactCooldown=state.contactCooldown;
            suspicion = 0; searchTimer = 0; windupTimer = 0;
            isDestroyed = state.destroyed;
            if (isDestroyed)
            {
                squad?.Release(this);
                currentHealth = 0;
                State.SetMode(WatcherMode.Offline);
                if (indicator != null) indicator.enabled = false;
                var capsule = GetComponent<CapsuleCollider>();
                if (capsule != null) capsule.enabled = false;
                if (animator != null) animator.Play("TurnOff", 0, 1.0f);
            }
            else
            {
                currentHealth = maxHealth;
                ResolveSavedOverlap();
            }
        }

        private void ResolveSavedOverlap()
        {
            var capsule=GetComponent<CapsuleCollider>();
            for(int pass=0;pass<4;pass++)
            {
                var center=transform.TransformPoint(capsule.center);
                float half=Mathf.Max(0,capsule.height/2-capsule.radius); bool moved=false;
                foreach(var obstacle in Physics.OverlapCapsule(center+Vector3.up*half,center-Vector3.up*half,capsule.radius,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore))
                {
                    if(obstacle.transform.IsChildOf(transform)) continue;
                    if(Physics.ComputePenetration(capsule,transform.position,transform.rotation,obstacle,obstacle.transform.position,obstacle.transform.rotation,out var normal,out var depth) && depth>.0001f)
                    { transform.position+=normal*(depth+.005f); moved=true; }
                }
                if(!moved) break;
            }
        }

        private Vector3 SquadDestination(Vector3 position, Vector3 target)
        {
            var toward = Vector3.ProjectOnPlane(target - position, Vector3.up);
            float range = toward.magnitude;
            switch (TacticalRole)
            {
                case WatcherRole.Flank when range > 1.65f:
                    var flank = Vector3.Cross(Vector3.up, toward.normalized) * 2.7f;
                    var side = target + flank;
                    if (side.x < xBounds.x || side.x > xBounds.y || side.z < zBounds.x || side.z > zBounds.y)
                        side = target - flank;
                    return side;
                case WatcherRole.Overwatch:
                    return range < 2.4f ? position - toward.normalized * 1.4f : position;
                case WatcherRole.Ambush when range > 3.6f:
                    var route = PatrolRoute;
                    return (route.first - target).sqrMagnitude < (route.last - target).sqrMagnitude ? route.first : route.last;
                default:
                    return target;
            }
        }

        private Vector3 Pursue(Vector3 position, Vector3 target)
        {
            target.y = position.y;
            if (CanLeavePatrolArea)
                return position + Vector3.ClampMagnitude(target - position, 12f);

            target.x = Mathf.Clamp(target.x, xBounds.x, xBounds.y);
            target.z = Mathf.Clamp(target.z, zBounds.x, zBounds.y);
            return target;
        }

        private (Vector2 x, Vector2 z) MovementBounds(Vector3 position, Vector3 destination)
        {
            bool Contains(Vector3 point) => point.x >= xBounds.x && point.x <= xBounds.y &&
                point.z >= zBounds.x && point.z <= zBounds.y;
            if (!CanLeavePatrolArea || Contains(position) && Contains(destination))
                return (xBounds, zBounds);

            // A short search spanning the actual doorway is enough to leave or return to the patrol.
            const float margin = 2.4f;
            return (new Vector2(Mathf.Min(position.x, destination.x) - margin, Mathf.Max(position.x, destination.x) + margin),
                new Vector2(Mathf.Min(position.z, destination.z) - margin, Mathf.Max(position.z, destination.z) + margin));
        }

        public bool Sees(Vector3 target)
        {
            if (isDestroyed||NetworkDisconnected||Transferring||containment?.Complete==true) return false;
            if(simulation!=null&&simulation.Actions.Stun(Id)>0) return false;
            Vector3 delta=target-sensor.position;
            bool impaired = combat?.Model?.Data.sensor <= 0;
            float maxRange = impaired ? 2.2f : 7f;
            float maxAngle = Id == "sentry-r18" ? 55f : 50f;
            bool closeRange = simulation?.InteractiveComputers == true && !impaired && delta.sqrMagnitude < 2.25f;
            return delta.sqrMagnitude < maxRange * maxRange &&
                (closeRange || Vector3.Angle(Vector3.ProjectOnPlane(sensor.forward,Vector3.up),Vector3.ProjectOnPlane(delta,Vector3.up)) < maxAngle) &&
                (navigation??=new RobotNavigation(transform)).Visible(sensor.position,target);
        }

        private void Update()=>Tick(Time.deltaTime);

        public void Tick(float dt)
        {
            if(simulation==null || terminalLeap) return;
            if(controls!=null&&!controls.WorldRuns)dt=0;
            if (isDestroyed)
            {
                if(animator!=null)animator.speed=controls==null||controls.WorldRuns?1:0;
                State.SetMode(WatcherMode.Offline);
                if (indicator != null) indicator.enabled = false;
                var capsule = GetComponent<CapsuleCollider>();
                if (capsule != null && capsule.enabled) capsule.enabled = false;
                if (animator != null && animationState != "TurnOff")
                {
                    animator.CrossFadeInFixedTime("TurnOff", 0.05f);
                    animationState = "TurnOff";
                }
                return;
            }
            EnsureAudio();
            if(network!=null&&!network.CommandOnline)ForgetNetworkTarget();

            bool relay=Id=="watcher-r17";
            bool disabled = Id.StartsWith("aux-watch-") ? false : relay ? (!simulation.AutomatedRoute && simulation.SourceIsolated) : !simulation[DeviceId.Core01].HasProcess(CoreProcess);
            bool powered = relay && simulation.AutomatedRoute ? true : simulation[relay ? DeviceId.Edge01 : DeviceId.Core01].Powered;

            if (containment != null)
            {
                disabled = containment.Complete;
                powered = true;
            }

            if (stunTimer > 0f) stunTimer -= dt;
            if (staggerTimer > 0f) staggerTimer -= dt;

            bool jammed = simulation.Actions.Stun(Id) > 0 || stunTimer > 0f || staggerTimer > 0f || NetworkDisconnected;

            if (disabled)
            {
                State.SetMode(WatcherMode.Maintenance);
                suspicion = 0; searchTimer = 0; windupTimer = 0;
            }
            else if (!powered)
            {
                State.SetMode(WatcherMode.Offline);
                suspicion = 0; searchTimer = 0; windupTimer = 0;
            }
            else if (jammed)
            {
                if (State.Mode != WatcherMode.Jammed) PlayCue(clipJammed, 0.7f);
                State.SetMode(WatcherMode.Jammed);
                suspicion = 0; searchTimer = 0; windupTimer = 0;
            }
            else if (Transferring)
            {
                State.SetMode(WatcherMode.Patrol);
                suspicion = searchTimer = windupTimer = contactCooldown = 0;
            }
            else if (State.Mode == WatcherMode.Jammed || State.Mode == WatcherMode.Offline || State.Mode == WatcherMode.Maintenance)
            {
                State.SetMode(player != null && Sees(player.Eye) ? WatcherMode.Alert : WatcherMode.Patrol);
            }
            else if (dt <= 0)
            {
                bool sees = player != null && Sees(player.Eye);
                if (!sees && State.Mode != WatcherMode.Search && State.Mode != WatcherMode.Suspicious)
                    State.SetMode(WatcherMode.Patrol);
                else if (sees && State.Mode == WatcherMode.Patrol)
                    State.SetMode(WatcherMode.Alert);
            }

            bool moving = false, contact = false;

            if (controls.WorldRuns && dt > 0 && !disabled && !powered == false && !jammed)
            {
                contactCooldown = Mathf.Max(0, contactCooldown - dt);
                bool seesPlayer = player != null && Sees(player.Eye);
                float distToPlayer = player != null ? Vector3.Distance(player.transform.position, transform.position) : 999f;

                // --- 1. PERCEPTION & SENSORY STATE MACHINE ---
                if (seesPlayer)
                {
                    networkTarget=false; squadMemory=false; lastKnownPosition = player.transform.position;
                    searchTimer = 0;
                    if (simulation.InteractiveComputers && distToPlayer <= 1.5f)
                        navigation.Face(lastKnownPosition - transform.position, dt, 420);

                    if (State.Mode == WatcherMode.Alert || State.Mode == WatcherMode.AttackWindup)
                    {
                        // Maintain Alert / Windup
                    }
                    else
                    {
                        // Fast detection at close range, or buildup over distance
                        if (distToPlayer <= 1.5f || (player != null && player.IsSprinting))
                        {
                            suspicion = 1f;
                            State.SetMode(WatcherMode.Alert);
                            PlayCue(clipAlert, 0.75f);
                        }
                        else
                        {
                            suspicion += dt * 3.5f;
                            if (suspicion < 1f)
                            {
                                if (State.Mode != WatcherMode.Suspicious) PlayCue(clipSuspicious, 0.5f);
                                State.SetMode(WatcherMode.Suspicious);
                            }
                            else
                            {
                                State.SetMode(WatcherMode.Alert);
                                PlayCue(clipAlert, 0.75f);
                            }
                        }
                    }
                }
                else
                {
                    // Target lost or not in sight
                    suspicion = Mathf.Max(0, suspicion - dt * 2.2f);

                    if (State.Mode == WatcherMode.AttackWindup)
                    {
                        // Target escaped sight during windup
                        windupTimer = 0;
                        State.SetMode(WatcherMode.Search);
                        searchTimer = Id == "sentry-r18" ? 4.0f : 3.2f;
                        PlayCue(clipSearch, 0.55f);
                    }
                    else if (State.Mode == WatcherMode.Alert || State.Mode == WatcherMode.Suspicious)
                    {
                        // Transition into Search around last known position
                        State.SetMode(WatcherMode.Search);
                        searchTimer = Id == "sentry-r18" ? 4.0f : 3.2f;
                        PlayCue(clipSearch, 0.55f);
                    }
                    else if (State.Mode == WatcherMode.Search)
                    {
                        searchTimer -= dt;
                        if (searchTimer <= 0)
                        {
                            State.SetMode(WatcherMode.Patrol);
                            suspicion = 0;
                        }
                    }
                    else
                    {
                        State.SetMode(WatcherMode.Patrol);
                    }
                }

                if (squad != null)
                {
                    if (seesPlayer && State.Mode is WatcherMode.Alert or WatcherMode.AttackWindup)
                        squad.ReportSight(this, lastKnownPosition);
                    TacticalRole = squad.TryAssignment(this, out var role, out var observation) ? role : WatcherRole.Local;
                    if (!seesPlayer)
                    {
                        if (TacticalRole != WatcherRole.Local && (State.Mode == WatcherMode.Patrol || squadMemory))
                        {
                            lastKnownPosition = observation;
                            searchTimer = 1;
                            squadMemory = true;
                            State.SetMode(WatcherMode.Search);
                        }
                        else if (TacticalRole == WatcherRole.Local && squadMemory)
                        {
                            squadMemory = false;
                            searchTimer = 0;
                            State.SetMode(WatcherMode.Patrol);
                        }
                    }
                }

                float readinessRange = TacticalRole == WatcherRole.Overwatch ? 5.4f : 1.65f;
                // --- 2. ATTACK TELEGRAPHING & EXECUTION ---
                if (contactCooldown <= 0 && seesPlayer && (State.Mode == WatcherMode.Alert || State.Mode == WatcherMode.AttackWindup))
                {
                    if (squad == null && distToPlayer <= 0.7f)
                    {
                        // Immediate point-blank strike
                        var health = player.GetComponent<PlayerHealth>();
                        if (health != null) health.TakeDamage(35f, Id == "sentry-r18" ? "SENTRY R18 (Серверная)" : "WATCHER R17 (Служебный проход)");
                        // Hits leave the player in the fight; dodging remains meaningful.
                        contactCooldown = 3;
                        simulation.RecordWatcherContact();
                        contact = true;
                        windupTimer = 0;
                        PlayCue(clipAttack, 0.9f);
                        State.SetMode(WatcherMode.Alert);
                    }
                    else if (distToPlayer <= readinessRange && State.Mode != WatcherMode.AttackWindup &&
                        (squad == null || squad.Acquire(this)))
                    {
                        // Begin attack telegraph / charge-up
                        State.SetMode(WatcherMode.AttackWindup);
                        rangedWindup = TacticalRole == WatcherRole.Overwatch;
                        windupTimer = squad != null ? (rangedWindup ? .92f : .58f) :
                            Id == "sentry-r18" ? 0.48f : 0.38f;
                        PlayCue(clipWindup, 0.7f);
                    }
                }

                if (State.Mode == WatcherMode.AttackWindup)
                {
                    if (squad != null && !squad.Acquire(this))
                    {
                        windupTimer = 0;
                        State.SetMode(WatcherMode.Alert);
                    }
                    else
                        windupTimer -= dt;
                    if (windupTimer <= 0 && State.Mode == WatcherMode.AttackWindup)
                    {
                        bool covering = rangedWindup;
                        float reach = covering ? 5.2f : Id == "sentry-r18" ? 1.6f : 1.35f;
                        if (seesPlayer && distToPlayer <= reach && contactCooldown <= 0)
                        {
                            if (terminalAmbush?.OnContact(this, !covering) == true) return;
                            // Attack hit!
                            var health = player.GetComponent<PlayerHealth>();
                            if (health != null) health.TakeDamage(covering ? 17f : squad != null ? 30f : 45f,
                                squad != null ? "Удар охранного робота" : Id == "sentry-r18" ? "SENTRY R18 (Серверная)" : "WATCHER R17 (Служебный проход)");
                            // Hits leave the player in the fight; dodging remains meaningful.
                            contactCooldown = 3;
                            simulation.RecordWatcherContact();
                            contact = true;
                            PlayCue(clipAttack, 0.9f);
                            State.SetMode(WatcherMode.Alert);
                        }
                        else
                        {
                            // Whiff: player successfully dodged with Q or broke range
                            contactCooldown = 1.0f;
                            State.SetMode(seesPlayer ? WatcherMode.Alert : WatcherMode.Search);
                        }
                    }
                }

                // --- 3. MOVEMENT & NAVIGATION ---
                if (contactCooldown <= 2.4f && State.Mode != WatcherMode.Maintenance && State.Mode != WatcherMode.Offline && State.Mode != WatcherMode.Jammed)
                {
                    scan += dt;
                    Vector3 position = transform.position;
                    Vector3 destination;

                    if (State.Mode == WatcherMode.Alert)
                    {
                        destination = SquadDestination(position, Pursue(position, player.transform.position));
                    }
                    else if (State.Mode == WatcherMode.AttackWindup)
                    {
                        // Slight lunge forward in direction of target
                        Vector3 lungeDir = (player.transform.position - position).normalized;
                        destination = rangedWindup ? position : position + lungeDir * (0.8f * dt);
                    }
                    else if (State.Mode == WatcherMode.Search)
                    {
                        destination = SquadDestination(position, Pursue(position, lastKnownPosition));
                    }
                    else if (State.Mode == WatcherMode.Suspicious)
                    {
                        // Stop and inspect intruder
                        destination = position;
                    }
                    else
                    {
                        var (first, last) = PatrolRoute;
                        var end = direction > 0 ? last : first;
                        if (Vector3.Distance(position, end) < .1f) direction = -direction;
                        destination = direction > 0 ? last : first;
                        if (!simulation.InteractiveComputers && Mathf.Abs(position.x - destination.x) > .02f)
                            destination = new Vector3(destination.x, position.y, position.z);
                    }

                    float multiplier = simulation.Actions.Slow(Id) > 0 ? .4f : 1f;
                    float baseSpeed = State.Mode == WatcherMode.Alert ? (Id == "sentry-r18" ? 2.4f : 2.8f) :
                                      State.Mode == WatcherMode.AttackWindup ? 1.1f :
                                      State.Mode == WatcherMode.Search ? (Id == "sentry-r18" ? 1.4f : 1.6f) :
                                      State.Mode == WatcherMode.Suspicious ? 0.2f :
                                      (Transferring ? 1.7f : Id == "sentry-r18" ? 0.5f : 0.65f);

                    multiplier *= combat?.Mobility ?? 1;
                    var (searchX, searchZ) = MovementBounds(position, destination);
                    moving=(navigation??=new RobotNavigation(transform)).Move(destination,baseSpeed*multiplier,dt,searchX,searchZ);

                    // Sensor orientation
                    if (State.Mode == WatcherMode.Alert || State.Mode == WatcherMode.AttackWindup || State.Mode == WatcherMode.Suspicious)
                    {
                        sensor.rotation = Quaternion.LookRotation(player.Eye - sensor.position);
                    }
                    else if (State.Mode == WatcherMode.Search)
                    {
                        sensor.rotation = transform.rotation * Quaternion.Euler(0, Mathf.Sin(scan * 2.2f) * 65f, 0);
                    }
                    else
                    {
                        sensor.rotation = transform.rotation * Quaternion.Euler(0, Mathf.Sin(scan * .8f) * 60f, 0);
                    }
                }
            }

            if (State.Mode != WatcherMode.AttackWindup)
                squad?.Release(this);

            // --- 4. VISUAL INDICATOR & LIGHTS ---
            if (indicator != null)
            {
                if (State.Mode == WatcherMode.Maintenance) indicator.color = new Color(.35f, 1f, .7f);
                else if (State.Mode == WatcherMode.Offline || State.Mode == WatcherMode.Jammed) indicator.color = new Color(.35f, .55f, 1f);
                else if (State.Mode == WatcherMode.AttackWindup)
                {
                    // Rapid flashing crimson / white warning
                    indicator.color = ((int)(Time.time * 24f) % 2 == 0) ? new Color(1f, 0.05f, 0.05f) : Color.white;
                }
                else if (State.Mode == WatcherMode.Alert) indicator.color = new Color(1f, .15f, .08f);
                else if (State.Mode == WatcherMode.Suspicious) indicator.color = new Color(1f, .75f, .1f);
                else if (State.Mode == WatcherMode.Search)
                {
                    // Pulsing amber search light
                    indicator.color = new Color(1f, .45f, .1f) * (0.75f + 0.25f * Mathf.Sin(Time.time * 7f));
                }
                else indicator.color = new Color(1f, .6f, .2f);
            }

            // --- 5. ANIMATIONS ---
            string clip = State.Mode == WatcherMode.Maintenance || State.Mode == WatcherMode.Offline ? "TurnOff" :
                          contact || contactCooldown > 2.4f || State.Mode == WatcherMode.AttackWindup ? "Attack" :
                          moving ? (State.Mode == WatcherMode.Alert ? "Run" : "Walk") : "Idle";

            if (animator != null)
            {
                animator.speed = controls.WorldRuns && !NetworkDisconnected ? 1 : 0;
                if (animationState != clip) { animator.CrossFadeInFixedTime(clip, .12f); animationState = clip; }
            }

            previousMode = State.Mode;
        }
    }
}
