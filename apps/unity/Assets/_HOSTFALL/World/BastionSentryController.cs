using System;
using Hostfall.Core;
using Hostfall.Player;
using Hostfall.Simulation;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.World
{
    public sealed class BastionSentryController : MonoBehaviour, IDamageable
    {
        [SerializeField] private string enemyId = "bastion-r32";
        [SerializeField] private float maxHealth = 240f;
        [SerializeField] private float patrolSpeed = 0.9f;
        [SerializeField] private float pursueSpeed = 1.6f;
        [SerializeField] private float detectionRange = 22f;
        [SerializeField] private float detectionAngle = 75f;

        private float currentHealth = 240f;
        private bool isDestroyed;
        private float stunTimer;
        private float attackWindupTimer;
        private float attackCooldownTimer;
        private const float WindupDuration = 0.95f;
        private const float CooldownDuration = 3.0f;

        private Vector3 patrolA, patrolB;
        private int patrolTarget;
        private Transform turretTransform;
        private Transform muzzleL, muzzleR;
        private Light searchlight;
        private AudioSource audioSource;
        private static AudioClip clipHorn, clipBurst, clipStomp, clipEmpStun;
        private static bool audioGenerated;

        private PlayerMotor player;
        private FacilityUI ui;
        private bool hasDetectedPlayer;

        public string DamageableId => enemyId;
        public float Health => currentHealth;
        public bool IsDestroyed => isDestroyed;
        public bool IsStunned => stunTimer > 0f;
        public bool HasDetectedPlayer => hasDetectedPlayer;

        public void Configure(Transform turret, Transform leftMuzzle, Transform rightMuzzle, Light light, Vector3 pA, Vector3 pB, string id = "bastion-r32")
        {
            turretTransform = turret;
            muzzleL = leftMuzzle;
            muzzleR = rightMuzzle;
            searchlight = light;
            patrolA = pA;
            patrolB = pB;
            enemyId = id;
        }

        public void Initialize(PlayerMotor playerMotor, FacilityUI facilityUI)
        {
            player = playerMotor;
            ui = facilityUI;
            EnsureAudio();
        }

        private void Awake()
        {
            currentHealth = maxHealth;
            EnsureAudio();
        }

        private void Start()
        {
            player = FindAnyObjectByType<PlayerMotor>();
            ui = FindAnyObjectByType<FacilityUI>();
        }

        private void EnsureAudio()
        {
            if (audioSource == null)
            {
                if (!TryGetComponent<AudioSource>(out audioSource))
                    audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1.0f;
                audioSource.minDistance = 3.5f;
                audioSource.maxDistance = 28.0f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
            }

            if (!audioGenerated)
            {
                const int rate = 22050;

                // Heavy Bastion Siren / Horn
                int hornSamples = (int)(rate * 0.85f);
                var hornData = new float[hornSamples];
                for (int i = 0; i < hornSamples; i++)
                {
                    float t = (float)i / rate;
                    float h1 = Mathf.Sin(2f * Mathf.PI * 180f * t);
                    float h2 = Mathf.Sin(2f * Mathf.PI * 185f * t);
                    hornData[i] = (h1 + h2) * 0.4f * Mathf.Clamp01(t / 0.05f) * Mathf.Exp(-t * 1.8f);
                }
                clipHorn = AudioClip.Create("Bastion_Horn", hornSamples, 1, rate, false);
                clipHorn.SetData(hornData, 0);

                // Twin Cannon Burst
                int burstSamples = (int)(rate * 0.45f);
                var burstData = new float[burstSamples];
                for (int i = 0; i < burstSamples; i++)
                {
                    float t = (float)i / rate;
                    float crack1 = Mathf.Sin(2f * Mathf.PI * 80f * t) * (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 16f);
                    burstData[i] = Mathf.Clamp(crack1, -1f, 1f);
                }
                clipBurst = AudioClip.Create("Bastion_Burst", burstSamples, 1, rate, false);
                clipBurst.SetData(burstData, 0);

                // Ground Stomp Shockwave
                int stompSamples = (int)(rate * 0.65f);
                var stompData = new float[stompSamples];
                for (int i = 0; i < stompSamples; i++)
                {
                    float t = (float)i / rate;
                    float rumble = Mathf.Sin(2f * Mathf.PI * 45f * t) * Mathf.Exp(-t * 5f);
                    float slam = Mathf.Sin(2f * Mathf.PI * 120f * t) * Mathf.Exp(-t * 22f);
                    stompData[i] = Mathf.Clamp(rumble * 0.7f + slam * 0.5f, -1f, 1f);
                }
                clipStomp = AudioClip.Create("Bastion_Stomp", stompSamples, 1, rate, false);
                clipStomp.SetData(stompData, 0);

                // EMP Stun
                int stunSamples = (int)(rate * 0.5f);
                var stunData = new float[stunSamples];
                for (int i = 0; i < stunSamples; i++)
                {
                    float t = (float)i / rate;
                    stunData[i] = Mathf.Sin(2f * Mathf.PI * 60f * t) * (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 4f);
                }
                clipEmpStun = AudioClip.Create("Bastion_Stun", stunSamples, 1, rate, false);
                clipEmpStun.SetData(stunData, 0);

                audioGenerated = true;
            }
        }

        private void Update()
        {
            if (isDestroyed) return;

            if (stunTimer > 0f)
            {
                stunTimer -= Time.deltaTime;
                attackWindupTimer = 0f;
                return;
            }

            if (attackCooldownTimer > 0f) attackCooldownTimer -= Time.deltaTime;

            CheckSensors();
            HandleBehavior();
        }

        private void CheckSensors()
        {
            if (player == null) return;

            Vector3 eyePos = transform.position + Vector3.up * 1.8f;
            Vector3 toPlayer = (player.transform.position + Vector3.up * 0.9f) - eyePos;
            float dist = toPlayer.magnitude;

            if (dist > detectionRange)
            {
                hasDetectedPlayer = false;
                return;
            }

            float angle = Vector3.Angle(transform.forward, toPlayer);
            if (angle < detectionAngle)
            {
                if (Physics.Raycast(eyePos, toPlayer.normalized, out RaycastHit hit, dist, CollisionLayers.WorldMask | CollisionLayers.PlayerMask))
                {
                    hasDetectedPlayer = hit.collider.GetComponentInParent<PlayerMotor>() != null;
                }
                else
                {
                    hasDetectedPlayer = true;
                }
            }
            else
            {
                hasDetectedPlayer = false;
            }
        }

        private void HandleBehavior()
        {
            if (hasDetectedPlayer && player != null)
            {
                Vector3 targetDir = player.transform.position - transform.position;
                targetDir.y = 0;
                if (targetDir.sqrMagnitude > 0.01f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(targetDir.normalized);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRot, 50f * Time.deltaTime);
                }

                if (turretTransform != null)
                {
                    turretTransform.LookAt(player.transform.position + Vector3.up * 0.9f);
                }

                if (searchlight != null)
                {
                    searchlight.color = new Color(1.0f, 0.1f, 0.1f);
                    searchlight.intensity = 6.0f;
                }

                float dist = Vector3.Distance(transform.position, player.transform.position);

                // Attack sequence
                if (attackCooldownTimer <= 0f)
                {
                    attackWindupTimer += Time.deltaTime;
                    if (attackWindupTimer == Time.deltaTime && audioSource != null && clipHorn != null)
                    {
                        audioSource.PlayOneShot(clipHorn, 0.9f);
                    }

                    if (attackWindupTimer >= WindupDuration)
                    {
                        if (dist < 4.2f)
                        {
                            ExecuteGroundStomp();
                        }
                        else
                        {
                            FireCannonBurst();
                        }
                        attackWindupTimer = 0f;
                        attackCooldownTimer = CooldownDuration;
                    }
                }

                // Advance if player beyond 5m (with collision sweep)
                if (dist > 5.0f)
                {
                    PerformMovement(transform.forward * (pursueSpeed * Time.deltaTime));
                }
            }
            else
            {
                attackWindupTimer = 0f;
                if (searchlight != null)
                {
                    searchlight.color = new Color(1.0f, 0.65f, 0.2f);
                    searchlight.intensity = 3.5f;
                }

                // Patrol with collision sweep
                Vector3 dest = patrolTarget == 0 ? patrolA : patrolB;
                dest.y = transform.position.y;
                Vector3 step = dest - transform.position;
                if (step.magnitude > 0.35f)
                {
                    Quaternion walkRot = Quaternion.LookRotation(step.normalized);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, walkRot, 45f * Time.deltaTime);
                    PerformMovement(transform.forward * (patrolSpeed * Time.deltaTime));
                }
                else
                {
                    patrolTarget = 1 - patrolTarget;
                }

                if (turretTransform != null)
                {
                    turretTransform.localRotation = Quaternion.RotateTowards(turretTransform.localRotation, Quaternion.identity, 50f * Time.deltaTime);
                }
            }
        }

        private void PerformMovement(Vector3 delta)
        {
            float d = delta.magnitude;
            if (d < 0.0001f) return;
            Vector3 dir = delta / d;
            const float radius = 0.85f;
            const float height = 2.2f;
            Vector3 bot = transform.position + Vector3.up * radius;
            Vector3 top = transform.position + Vector3.up * Mathf.Max(radius, height - radius);

            if (Physics.CapsuleCast(bot, top, radius, dir, out RaycastHit hit, d + 0.05f, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore))
            {
                float allowed = Mathf.Max(0f, hit.distance - 0.05f);
                transform.position += dir * allowed;
                Vector3 slide = Vector3.ProjectOnPlane(dir, hit.normal);
                slide.y = 0;
                float rem = d - allowed;
                if (rem > 0.001f && slide.sqrMagnitude > 0.001f)
                {
                    slide.Normalize();
                    if (!Physics.CapsuleCast(bot + dir * allowed, top + dir * allowed, radius, slide, out _, rem + 0.05f, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore))
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

        private void FireCannonBurst()
        {
            if (audioSource != null && clipBurst != null)
            {
                audioSource.PlayOneShot(clipBurst, 1.0f);
            }

            Vector3 origin = muzzleL != null ? muzzleL.position : transform.position + Vector3.up * 1.8f;
            Vector3 dir = (player.transform.position + Vector3.up * 0.9f - origin).normalized;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, 25f, CollisionLayers.WorldMask | CollisionLayers.PlayerMask))
            {
                if (hit.collider.GetComponentInParent<PlayerMotor>() != null)
                {
                    if (ui != null)
                    {
                        ui.ShowActionNotice("Шквальный огонь");
                    }
                }
            }
        }

        private void ExecuteGroundStomp()
        {
            if (audioSource != null && clipStomp != null)
            {
                audioSource.PlayOneShot(clipStomp, 1.0f);
            }

            if (player != null && Vector3.Distance(transform.position, player.transform.position) < 5.0f)
            {
                if (ui != null)
                {
                    ui.ShowActionNotice("Ударная волна");
                }
            }
        }

        public void TakeDamage(float amount)
        {
            if (isDestroyed) return;

            // Check if damage is incoming from rear (reactor vent takes 2.5x damage!)
            if (player != null)
            {
                Vector3 toPlayer = (player.transform.position - transform.position).normalized;
                float dot = Vector3.Dot(transform.forward, toPlayer);
                if (dot < -0.35f) // Rear 70-degree arc
                {
                    amount *= 2.5f;
                    if (ui != null)
                    {
                        ui.ShowActionNotice("Уязвимость реактора");
                    }
                }
            }

            currentHealth -= amount;
            if (currentHealth <= 0f)
            {
                DestroyBastion();
            }
            else
            {
                hasDetectedPlayer = true;
            }
        }

        public void ApplyEmpStun(float duration = 7.0f)
        {
            if (isDestroyed) return;
            stunTimer = duration;
            attackWindupTimer = 0f;
            if (audioSource != null && clipEmpStun != null)
            {
                audioSource.PlayOneShot(clipEmpStun, 0.9f);
            }
            if (ui != null)
            {
                ui.ShowActionNotice("Страж обесточен");
            }
        }

        private void DestroyBastion()
        {
            isDestroyed = true;
            currentHealth = 0f;
            if (audioSource != null)
            {
                audioSource.Stop();
                if (clipBurst != null) audioSource.PlayOneShot(clipBurst, 0.9f);
            }
            if (searchlight != null) searchlight.enabled = false;
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }
}

