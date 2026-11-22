using System;
using Hostfall.Core;
using Hostfall.Player;
using Hostfall.Simulation;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.World
{
    public sealed class TrackedSentryController : MonoBehaviour, IDamageable
    {
        [SerializeField] private string sentryId = "sentry-crawler-e06";
        [SerializeField] private float maxHealth = 160f;
        [SerializeField] private Light searchlight;
        [SerializeField] private Transform turretTransform;
        [SerializeField] private Transform muzzleTransform;

        [SerializeField] private Vector3 patrolA = new Vector3(-21.0f, 0, 24.0f);
        [SerializeField] private Vector3 patrolB = new Vector3(-12.0f, 0, 32.0f);
        [SerializeField] private float moveSpeed = 2.2f;

        private PlayerMotor player;
        private FacilityUI ui;
        private ControlState controls;
        private AudioSource audioSource;

        private float currentHealth;
        private bool isDestroyed;
        private int patrolDirection = 1;

        // Combat & AI states
        private float stunTimer;
        private float staggerTimer;
        private float attackWindupTimer;
        private float attackCooldown;
        private bool isAimingAtPlayer;

        // Procedural Audio
        private static AudioClip clipEngine;
        private static AudioClip clipWindup;
        private static AudioClip clipShockArc;
        private static AudioClip clipHurt;

        public string DamageableId => sentryId;
        public bool IsDestroyed => GetComponent<RobotCombatController>()?.Model?.IsDead ?? isDestroyed;
        public float Health => GetComponent<RobotCombatController>()?.Model?.Health ?? currentHealth;
        public bool IsStunned => GetComponent<RobotCombatController>()?.Model!=null ? GetComponent<RobotCombatController>().Model.Data.stun>0 : stunTimer>0;
        public bool IsAimingAtPlayer => isAimingAtPlayer;

        internal void PlayGuardianCue(bool impact)
        {
            var clip = impact ? clipShockArc : clipWindup;
            if (clip == null)
            {
                EnsureAudio();
                clip = impact ? clipShockArc : clipWindup;
            }
            if (clip != null) audioSource.PlayOneShot(clip, impact ? .9f : .75f);
        }

        public void Configure(Light headLight, Transform turret, Transform muzzle, Vector3 pA, Vector3 pB, string id = "sentry-crawler-e06")
        {
            searchlight = headLight;
            turretTransform = turret;
            muzzleTransform = muzzle;
            patrolA = pA;
            patrolB = pB;
            sentryId = id;
            currentHealth = maxHealth;
        }

        public void Initialize(PlayerMotor targetPlayer, FacilityUI facilityUI, ControlState controlState = null)
        {
            player = targetPlayer;
            ui = facilityUI;
            controls = controlState;
            EnsureAudio();
        }

        private void Awake()
        {
            currentHealth = maxHealth;
            EnsureAudio();
        }

        private void EnsureAudio()
        {
            if (audioSource == null)
            {
                if (!TryGetComponent<AudioSource>(out audioSource))
                    audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1.0f;
                audioSource.minDistance = 2.5f;
                audioSource.maxDistance = 22.0f;
            }

            if (clipEngine == null) clipEngine = CreateEngineSound();
            if (clipWindup == null) clipWindup = CreateWindupSound();
            if (clipShockArc == null) clipShockArc = CreateShockArcSound();
            if (clipHurt == null) clipHurt = CreateHurtSound();

            if (audioSource != null && clipEngine != null && !audioSource.isPlaying && !isDestroyed)
            {
                audioSource.clip = clipEngine;
                audioSource.loop = true;
                audioSource.volume = 0.45f;
                audioSource.Play();
            }
        }

        private void Update()
        {
            if(GetComponent<CrawlerMinibossController>()!=null)return;
            if (isDestroyed) return;

            // Handle Stun & Stagger Timers
            if (stunTimer > 0f)
            {
                stunTimer -= Time.deltaTime;
                if (searchlight != null)
                {
                    searchlight.color = new Color(0.2f, 0.7f, 1.0f);
                    searchlight.intensity = UnityEngine.Random.Range(1.0f, 3.5f);
                }
                return;
            }

            if (staggerTimer > 0f)
            {
                staggerTimer -= Time.deltaTime;
                return;
            }

            if (attackCooldown > 0f)
            {
                attackCooldown -= Time.deltaTime;
            }

            // AI Decision Loop
            bool seesPlayer = CheckSightToPlayer();

            if (seesPlayer)
            {
                EngagePlayer();
            }
            else
            {
                Patrol();
            }
        }

        private bool CheckSightToPlayer()
        {
            if (player == null) return false;

            Vector3 eyePos = transform.position + Vector3.up * 1.2f;
            Vector3 targetPos = player.Eye;
            Vector3 toPlayer = targetPos - eyePos;
            float dist = toPlayer.magnitude;

            if (dist > 12.0f) return false;

            float angle = Vector3.Angle(transform.forward, toPlayer);
            if (angle > 70f) return false;

            // Line of sight check
            if (Physics.Linecast(eyePos, targetPos, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            return true;
        }

        private void EngagePlayer()
        {
            isAimingAtPlayer = true;

            // Rotate towards player
            Vector3 targetDir = Vector3.ProjectOnPlane(player.transform.position - transform.position, Vector3.up).normalized;
            if (targetDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(targetDir), Time.deltaTime * 85f);
            }

            if (searchlight != null)
            {
                searchlight.color = new Color(1.0f, 0.15f, 0.1f);
                searchlight.intensity = 5.0f;
            }

            // Attack Windup Sequence
            if (attackCooldown <= 0f)
            {
                attackWindupTimer += Time.deltaTime;

                if (attackWindupTimer == Time.deltaTime && audioSource != null && clipWindup != null)
                {
                    audioSource.PlayOneShot(clipWindup, 0.9f);
                }

                // Visual cue: flashing headlight during charge
                if (searchlight != null)
                {
                    searchlight.intensity = Mathf.PingPong(Time.time * 25f, 6.0f) + 2.0f;
                }

                if (attackWindupTimer >= 0.85f)
                {
                    DischargeStunArc();
                    attackWindupTimer = 0f;
                    attackCooldown = 2.4f;
                }
            }
        }

        private void DischargeStunArc()
        {
            if (player == null) return;

            if (audioSource != null && clipShockArc != null)
            {
                audioSource.PlayOneShot(clipShockArc, 1.0f);
            }

            // Raycast check: if player hasn't dodged behind cover
            Vector3 origin = muzzleTransform != null ? muzzleTransform.position : transform.position + Vector3.up * 1.2f;
            Vector3 toPlayer = player.Eye - origin;

            if (!Physics.Raycast(origin, toPlayer.normalized, out var hit, toPlayer.magnitude, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore))
            {
                // Player hit by electrical arc
                if (ui != null)
                {
                    ui.ShowActionNotice("Электроразряд");
                }
            }

            SpawnMuzzleFlash();
        }

        private void Patrol()
        {
            isAimingAtPlayer = false;
            attackWindupTimer = 0f;

            if (searchlight != null)
            {
                searchlight.color = new Color(1.0f, 0.75f, 0.25f);
                searchlight.intensity = 3.5f;
            }

            Vector3 targetPoint = patrolDirection > 0 ? patrolB : patrolA;
            Vector3 toTarget = Vector3.ProjectOnPlane(targetPoint - transform.position, Vector3.up);

            if (toTarget.magnitude < 0.6f)
            {
                patrolDirection = -patrolDirection;
            }
            else
            {
                Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, Time.deltaTime * 65f);
                PerformMovement(transform.forward * (moveSpeed * Time.deltaTime));
            }
        }

        private void PerformMovement(Vector3 delta)
        {
            float d = delta.magnitude;
            if (d < 0.0001f) return;
            Vector3 dir = delta / d;
            const float radius = 0.55f;
            const float height = 1.7f;
            Vector3 bot = transform.position + Vector3.up * radius;
            Vector3 top = transform.position + Vector3.up * Mathf.Max(radius, height - radius);

            if (Physics.CapsuleCast(bot, top, radius, dir, out RaycastHit hit, d + 0.04f, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore))
            {
                float allowed = Mathf.Max(0f, hit.distance - 0.04f);
                transform.position += dir * allowed;
                Vector3 slide = Vector3.ProjectOnPlane(dir, hit.normal);
                slide.y = 0;
                float rem = d - allowed;
                if (rem > 0.001f && slide.sqrMagnitude > 0.001f)
                {
                    slide.Normalize();
                    if (!Physics.CapsuleCast(bot + dir * allowed, top + dir * allowed, radius, slide, out _, rem + 0.04f, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore))
                    {
                        transform.position += slide * rem;
                    }
                }
            }
            else
            {
                transform.position += delta;
            }
        }

        public void TakeDamage(float amount)
        {
            var combat=GetComponent<RobotCombatController>();
            if(combat?.Model!=null&&!combat.CompletingDeath){combat.Hit(amount,RobotHitRegion.Hull,transform.position);return;}
            if (isDestroyed) return;

            currentHealth -= amount;
            staggerTimer = 0.5f;
            attackWindupTimer = 0f; // Cancels current windup on taking heavy hit!

            if (audioSource != null && clipHurt != null)
            {
                audioSource.PlayOneShot(clipHurt, 0.85f);
            }

            SpawnDamageSparks();

            if (currentHealth <= 0f)
            {
                DestroySentry();
            }
        }

        public void ApplyEmpStun(float duration = 7.0f)
        {
            var combat=GetComponent<RobotCombatController>();
            if(combat?.Model!=null)combat.Model.Data.stun=duration;
            if (isDestroyed) return;
            stunTimer = duration;
            staggerTimer = 0.6f;
            attackWindupTimer = 0f;

            if (audioSource != null && clipHurt != null)
            {
                audioSource.PlayOneShot(clipHurt, 1.0f);
            }

            SpawnDamageSparks();
        }

        private void DestroySentry()
        {
            isDestroyed = true;
            currentHealth = 0f;

            if (audioSource != null)
            {
                audioSource.Stop();
                if (clipHurt != null) audioSource.PlayOneShot(clipHurt, 1.0f);
            }

            if (searchlight != null) searchlight.enabled = false;

            // Spawns persistent destroyed smoke & sparks
            var smoke = new GameObject("Sentry Destroyed Smoke");
            smoke.transform.position = transform.position + Vector3.up * 0.8f;
            var l = smoke.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.35f, 0.1f);
            l.range = 3.0f;
            l.intensity = 2.0f;
            Destroy(smoke, 8.0f);

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            if (ui != null)
            {
                ui.ShowActionNotice("Страж нейтрализован");
            }
        }

        private void SpawnDamageSparks()
        {
            var sparks = new GameObject("Sentry Hit Sparks");
            sparks.transform.position = transform.position + Vector3.up * 1.0f;
            var l = sparks.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.35f, 0.85f, 1.0f);
            l.range = 3.5f;
            l.intensity = 5.0f;
            Destroy(sparks, 0.12f);
        }

        private void SpawnMuzzleFlash()
        {
            var flash = new GameObject("Sentry Arc Flash");
            flash.transform.position = muzzleTransform != null ? muzzleTransform.position : transform.position + Vector3.up * 1.2f;
            var l = flash.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.3f, 0.85f, 1.0f);
            l.range = 6.0f;
            l.intensity = 7.0f;
            Destroy(flash, 0.08f);
        }

        private static AudioClip CreateEngineSound()
        {
            const int rate = 22050;
            float duration = 1.0f;
            int samples = (int)(rate * duration);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)rate;
                float diesel = Mathf.Sin(2f * Mathf.PI * 45f * t) * 0.4f + Mathf.Sin(2f * Mathf.PI * 90f * t) * 0.3f;
                float rumble = (Mathf.PerlinNoise(t * 120f, 0f) - 0.5f) * 0.3f;
                data[i] = diesel + rumble;
            }
            var clip = AudioClip.Create("SFX_Tracked_Engine", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateWindupSound()
        {
            const int rate = 22050;
            float duration = 0.85f;
            int samples = (int)(rate * duration);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)rate;
                float freq = Mathf.Lerp(220f, 1850f, Mathf.Pow(t / duration, 2f));
                float s = Mathf.Sin(2f * Mathf.PI * freq * t);
                float env = t / duration;
                data[i] = s * env * 0.55f;
            }
            var clip = AudioClip.Create("SFX_Sentry_Windup", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateShockArcSound()
        {
            const int rate = 22050;
            float duration = 0.35f;
            int samples = (int)(rate * duration);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)rate;
                float arc = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 12f);
                float sub = Mathf.Sin(2f * Mathf.PI * 75f * t) * Mathf.Exp(-t * 8f);
                data[i] = Mathf.Clamp(arc * 0.7f + sub * 0.5f, -1f, 1f);
            }
            var clip = AudioClip.Create("SFX_Sentry_ShockArc", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateHurtSound()
        {
            const int rate = 22050;
            float duration = 0.4f;
            int samples = (int)(rate * duration);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)rate;
                float clatter = Mathf.Sin(2f * Mathf.PI * 180f * t) * Mathf.Exp(-t * 16f);
                float crack = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 26f);
                data[i] = Mathf.Clamp(clatter * 0.6f + crack * 0.5f, -1f, 1f);
            }
            var clip = AudioClip.Create("SFX_Sentry_Hurt", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
