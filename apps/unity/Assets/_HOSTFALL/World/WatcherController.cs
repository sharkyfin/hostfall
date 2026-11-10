using Hostfall.Core;
using Hostfall.Persistence;
using Hostfall.Player;
using Hostfall.Simulation;
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

        private FacilitySimulation simulation;
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

        public bool IsDestroyed => isDestroyed;
        public float Health => currentHealth;
        public bool IsStunned => stunTimer > 0f;

        public void TakeDamage(float amount)
        {
            if (isDestroyed) return;
            currentHealth -= amount;
            staggerTimer = 0.45f;
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
            simulation=model; player=target; controls=state;
            EnsureAudio();
        }

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
            if (audioSource != null && clip != null && controls != null && controls.CanExplore)
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

        public bool Sees(Vector3 target)
        {
            if (isDestroyed) return false;
            if(simulation!=null&&simulation.Actions.Stun(Id)>0) return false;
            Vector3 delta=target-sensor.position;
            float maxRange = Id == "sentry-r18" ? 5.5f : 5.0f;
            float maxAngle = Id == "sentry-r18" ? 55f : 50f;
            return delta.magnitude<maxRange && Vector3.Angle(Vector3.ProjectOnPlane(sensor.forward,Vector3.up),Vector3.ProjectOnPlane(delta,Vector3.up))<maxAngle &&
                !Physics.Linecast(sensor.position,target,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore);
        }

        private void Update()=>Tick(Time.deltaTime);

        public void Tick(float dt)
        {
            if(simulation==null) return;
            if (isDestroyed)
            {
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

            bool relay=Id=="watcher-r17";
            bool disabled = relay ? (!simulation.AutomatedRoute && simulation.SourceIsolated) : !simulation[DeviceId.Core01].HasProcess(CoreProcess);
            bool powered = relay && simulation.AutomatedRoute ? true : simulation[relay ? DeviceId.Edge01 : DeviceId.Core01].Powered;

            if (stunTimer > 0f) stunTimer -= dt;
            if (staggerTimer > 0f) staggerTimer -= dt;

            bool jammed = simulation.Actions.Stun(Id) > 0 || stunTimer > 0f || staggerTimer > 0f;

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

            if (controls.CanExplore && dt > 0 && !disabled && !powered == false && !jammed)
            {
                contactCooldown = Mathf.Max(0, contactCooldown - dt);
                bool seesPlayer = player != null && Sees(player.Eye);
                float distToPlayer = player != null ? Vector3.Distance(player.transform.position, transform.position) : 999f;

                // --- 1. PERCEPTION & SENSORY STATE MACHINE ---
                if (seesPlayer)
                {
                    lastKnownPosition = player.transform.position;
                    searchTimer = 0;

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

                // --- 2. ATTACK TELEGRAPHING & EXECUTION ---
                if (contactCooldown <= 0 && seesPlayer && (State.Mode == WatcherMode.Alert || State.Mode == WatcherMode.AttackWindup))
                {
                    if (distToPlayer <= 0.7f)
                    {
                        // Immediate point-blank strike
                        var health = player.GetComponent<PlayerHealth>();
                        if (health != null) health.TakeDamage(35f, Id == "sentry-r18" ? "SENTRY R18 (Серверная)" : "WATCHER R17 (Служебный проход)");
                        player.ReturnToPost();
                        contactCooldown = 3;
                        simulation.RecordWatcherContact();
                        contact = true;
                        windupTimer = 0;
                        PlayCue(clipAttack, 0.9f);
                        State.SetMode(WatcherMode.Alert);
                    }
                    else if (distToPlayer <= 1.65f && State.Mode != WatcherMode.AttackWindup)
                    {
                        // Begin attack telegraph / charge-up
                        State.SetMode(WatcherMode.AttackWindup);
                        windupTimer = Id == "sentry-r18" ? 0.48f : 0.38f;
                        PlayCue(clipWindup, 0.7f);
                    }
                }

                if (State.Mode == WatcherMode.AttackWindup)
                {
                    windupTimer -= dt;
                    if (windupTimer <= 0)
                    {
                        float reach = Id == "sentry-r18" ? 1.6f : 1.35f;
                        if (seesPlayer && distToPlayer <= reach && contactCooldown <= 0)
                        {
                            // Attack hit!
                            var health = player.GetComponent<PlayerHealth>();
                            if (health != null) health.TakeDamage(45f, Id == "sentry-r18" ? "SENTRY R18 (Серверная)" : "WATCHER R17 (Служебный проход)");
                            player.ReturnToPost();
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
                        destination = new Vector3(Mathf.Clamp(player.transform.position.x, xBounds.x, xBounds.y), position.y, Mathf.Clamp(player.transform.position.z, zBounds.x, zBounds.y));
                    }
                    else if (State.Mode == WatcherMode.AttackWindup)
                    {
                        // Slight lunge forward in direction of target
                        Vector3 lungeDir = (player.transform.position - position).normalized;
                        destination = position + lungeDir * (0.8f * dt);
                    }
                    else if (State.Mode == WatcherMode.Search)
                    {
                        destination = new Vector3(Mathf.Clamp(lastKnownPosition.x, xBounds.x, xBounds.y), position.y, Mathf.Clamp(lastKnownPosition.z, zBounds.x, zBounds.y));
                    }
                    else if (State.Mode == WatcherMode.Suspicious)
                    {
                        // Stop and inspect intruder
                        destination = position;
                    }
                    else
                    {
                        // Patrol loop
                        var end = direction > 0 ? patrolB : patrolA;
                        if (Vector3.Distance(position, end) < .1f) direction = -direction;
                        destination = direction > 0 ? patrolB : patrolA;
                        if (Mathf.Abs(position.x - destination.x) > .02f)
                            destination = new Vector3(destination.x, position.y, position.z);
                    }

                    float multiplier = simulation.Actions.Slow(Id) > 0 ? .4f : 1f;
                    float baseSpeed = State.Mode == WatcherMode.Alert ? (Id == "sentry-r18" ? 1.35f : 1.75f) :
                                      State.Mode == WatcherMode.AttackWindup ? 1.1f :
                                      State.Mode == WatcherMode.Search ? (Id == "sentry-r18" ? 0.55f : 0.8f) :
                                      State.Mode == WatcherMode.Suspicious ? 0.2f :
                                      (Id == "sentry-r18" ? 0.5f : 0.65f);

                    var desired = Vector3.MoveTowards(position, destination, baseSpeed * multiplier * dt);
                    Vector3 delta = desired - position;
                    float distance = delta.magnitude;

                    if (distance > .0001f)
                    {
                        var capsule = GetComponent<CapsuleCollider>();
                        Vector3 center = position + capsule.center;
                        float half = Mathf.Max(0, capsule.height / 2 - capsule.radius);
                        foreach (var hit in Physics.CapsuleCastAll(center + Vector3.up * half, center - Vector3.up * half, capsule.radius, delta.normalized, distance + .025f, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore))
                            if (!hit.collider.transform.IsChildOf(transform)) distance = Mathf.Min(distance, Mathf.Max(0, hit.distance - .025f));
                        transform.position = position + delta.normalized * distance;
                        moving = distance > .001f;

                        float turnRate = Id == "sentry-r18" ? 95f : 135f;
                        if (moving) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(delta), turnRate * dt);
                    }

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
            string clip = State.Mode == WatcherMode.Maintenance || State.Mode == WatcherMode.Offline || State.Mode == WatcherMode.Jammed ? "TurnOff" :
                          contact || contactCooldown > 2.4f || State.Mode == WatcherMode.AttackWindup ? "Attack" :
                          moving ? (State.Mode == WatcherMode.Alert ? "Run" : "Walk") : "Idle";

            if (animator != null)
            {
                animator.speed = controls.CanExplore ? 1 : 0;
                if (animationState != clip) { animator.CrossFadeInFixedTime(clip, .12f); animationState = clip; }
            }

            previousMode = State.Mode;
        }
    }
}
