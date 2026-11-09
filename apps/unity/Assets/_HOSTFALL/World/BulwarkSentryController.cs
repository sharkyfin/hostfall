using System;
using Hostfall.Core;
using Hostfall.Player;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.World
{
    public sealed class BulwarkSentryController : MonoBehaviour, IDamageable
    {
        [SerializeField] private string sentryId = "bulwark-sentry-r27";
        [SerializeField] private float maxHealth = 180f;
        [SerializeField] private Vector3 patrolA;
        [SerializeField] private Vector3 patrolB;
        [SerializeField] private float patrolSpeed = 1.1f;
        [SerializeField] private float pursueSpeed = 1.6f;
        [SerializeField] private float detectionRange = 15f;
        [SerializeField] private float detectionAngle = 75f;

        private Light searchlight;
        private Transform turretTransform;
        private Transform shieldTransform;
        private Transform muzzleTransform;
        private AudioSource audioSource;
        private BoxCollider boxCollider;

        private PlayerMotor player;
        private FacilityUI ui;

        private float currentHealth;
        private bool isDestroyed;
        private bool isShieldActive = true;
        private float stunTimer;
        private int patrolTarget = 1;

        private bool hasDetectedPlayer;
        private float attackWindupTimer;
        private float attackCooldownTimer;
        private const float WindupDuration = 0.85f;
        private const float CooldownDuration = 2.4f;

        private static AudioClip clipEngine, clipShieldHit, clipHorn, clipFire, clipEmpStun;
        private static bool audioGenerated;

        public string DamageableId => sentryId;
        public bool IsDestroyed => isDestroyed;
        public float Health => currentHealth;
        public bool IsStunned => stunTimer > 0f;
        public bool IsShieldActive => isShieldActive && !IsStunned;

        public void Configure(Light spot, Transform turret, Transform shield, Transform muzzle, Vector3 pointA, Vector3 pointB, string id = "bulwark-sentry-r27")
        {
            searchlight = spot;
            turretTransform = turret;
            shieldTransform = shield;
            muzzleTransform = muzzle;
            patrolA = pointA;
            patrolB = pointB;
            sentryId = id;
            currentHealth = maxHealth;
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
            boxCollider = GetComponent<BoxCollider>();
            EnsureAudio();
        }

        private void Start()
        {
            if (player == null)
            {
                var p = FindFirstObjectByType<PlayerMotor>();
                var u = FindFirstObjectByType<FacilityUI>();
                if (p != null) Initialize(p, u);
            }
        }

        private void EnsureAudio()
        {
            if (audioSource == null)
            {
                if (!TryGetComponent<AudioSource>(out audioSource))
                    audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1.0f;
                audioSource.minDistance = 3.0f;
                audioSource.maxDistance = 24.0f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
            }

            if (!audioGenerated)
            {
                const int rate = 22050;

                // Engine drone
                int engSamples = rate;
                var engData = new float[engSamples];
                for (int i = 0; i < engSamples; i++)
                {
                    float t = (float)i / rate;
                    float f = Mathf.Sin(2f * Mathf.PI * 65f * t) * 0.4f + Mathf.Sin(2f * Mathf.PI * 130f * t) * 0.2f;
                    engData[i] = f * (0.8f + 0.2f * Mathf.Sin(2f * Mathf.PI * 4f * t));
                }
                clipEngine = AudioClip.Create("Bulwark_Engine", engSamples, 1, rate, false);
                clipEngine.SetData(engData, 0);

                // Shield Hit Clang
                int clangSamples = (int)(rate * 0.35f);
                var clangData = new float[clangSamples];
                for (int i = 0; i < clangSamples; i++)
                {
                    float t = (float)i / rate;
                    float ringing = Mathf.Sin(2f * Mathf.PI * 1450f * t) * Mathf.Exp(-t * 22f);
                    float buzz = Mathf.Sin(2f * Mathf.PI * 850f * t) * Mathf.Exp(-t * 15f);
                    clangData[i] = (ringing * 0.6f + buzz * 0.4f);
                }
                clipShieldHit = AudioClip.Create("Bulwark_ShieldClang", clangSamples, 1, rate, false);
                clipShieldHit.SetData(clangData, 0);

                // Warning Horn
                int hornSamples = (int)(rate * 0.65f);
                var hornData = new float[hornSamples];
                for (int i = 0; i < hornSamples; i++)
                {
                    float t = (float)i / rate;
                    float h1 = Mathf.Sin(2f * Mathf.PI * 220f * t);
                    float h2 = Mathf.Sin(2f * Mathf.PI * 225f * t);
                    hornData[i] = (h1 + h2) * 0.35f * Mathf.Clamp01(t / 0.05f) * Mathf.Exp(-t * 2.5f);
                }
                clipHorn = AudioClip.Create("Bulwark_Horn", hornSamples, 1, rate, false);
                clipHorn.SetData(hornData, 0);

                // Heavy Slug Fire
                int fireSamples = (int)(rate * 0.45f);
                var fireData = new float[fireSamples];
                for (int i = 0; i < fireSamples; i++)
                {
                    float t = (float)i / rate;
                    float boom = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(160f, 40f, t / 0.2f) * t) * Mathf.Exp(-t * 14f);
                    float noise = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 20f);
                    fireData[i] = Mathf.Clamp(boom * 0.7f + noise * 0.4f, -1f, 1f);
                }
                clipFire = AudioClip.Create("Bulwark_Fire", fireSamples, 1, rate, false);
                clipFire.SetData(fireData, 0);

                // EMP Stun Buzz
                int stunSamples = (int)(rate * 0.5f);
                var stunData = new float[stunSamples];
                for (int i = 0; i < stunSamples; i++)
                {
                    float t = (float)i / rate;
                    stunData[i] = Mathf.Sin(2f * Mathf.PI * 180f * t) * (UnityEngine.Random.value - 0.5f) * Mathf.Exp(-t * 4f);
                }
                clipEmpStun = AudioClip.Create("Bulwark_EmpStun", stunSamples, 1, rate, false);
                clipEmpStun.SetData(stunData, 0);

                audioGenerated = true;
            }

            if (audioSource != null && clipEngine != null && !audioSource.isPlaying && !isDestroyed)
            {
                audioSource.clip = clipEngine;
                audioSource.loop = true;
                audioSource.volume = 0.4f;
                audioSource.Play();
            }
        }

        private void Update()
        {
            if (isDestroyed) return;

            if (stunTimer > 0f)
            {
                stunTimer -= Time.deltaTime;
                if (searchlight != null)
                {
                    searchlight.color = new Color(0.1f, 0.4f, 0.9f);
                    searchlight.intensity = 1.0f + 0.8f * Mathf.Sin(Time.time * 25f);
                }
                if (shieldTransform != null)
                {
                    // Shield collapses downward when stunned
                    shieldTransform.localPosition = Vector3.Lerp(shieldTransform.localPosition, new Vector3(0, -0.4f, 0.9f), Time.deltaTime * 4f);
                }
                return;
            }

            if (shieldTransform != null)
            {
                shieldTransform.localPosition = Vector3.Lerp(shieldTransform.localPosition, new Vector3(0, 0.2f, 0.9f), Time.deltaTime * 3f);
            }

            if (attackCooldownTimer > 0f) attackCooldownTimer -= Time.deltaTime;

            CheckSensors();
            HandleBehavior();
        }

        private void CheckSensors()
        {
            if (player == null) return;

            Vector3 eyePos = transform.position + Vector3.up * 1.5f;
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
                Vector3 targetDir = (player.transform.position - transform.position);
                targetDir.y = 0;
                if (targetDir.sqrMagnitude > 0.01f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(targetDir.normalized);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRot, 45f * Time.deltaTime);
                }

                if (turretTransform != null)
                {
                    turretTransform.LookAt(player.transform.position + Vector3.up * 0.9f);
                }

                if (searchlight != null)
                {
                    searchlight.color = new Color(1.0f, 0.15f, 0.15f);
                    searchlight.intensity = 5.5f;
                }

                // Attack sequence
                if (attackCooldownTimer <= 0f)
                {
                    attackWindupTimer += Time.deltaTime;
                    if (attackWindupTimer == Time.deltaTime && audioSource != null && clipHorn != null)
                    {
                        audioSource.PlayOneShot(clipHorn, 0.85f);
                    }

                    if (attackWindupTimer >= WindupDuration)
                    {
                        FireKineticSlug();
                        attackWindupTimer = 0f;
                        attackCooldownTimer = CooldownDuration;
                    }
                }

                // Advance slowly toward player if beyond 4m
                if (Vector3.Distance(transform.position, player.transform.position) > 4.5f)
                {
                    transform.position += transform.forward * (pursueSpeed * Time.deltaTime);
                }
            }
            else
            {
                attackWindupTimer = 0f;
                if (searchlight != null)
                {
                    searchlight.color = new Color(1.0f, 0.75f, 0.25f);
                    searchlight.intensity = 3.5f;
                }

                // Patrol
                Vector3 dest = patrolTarget == 0 ? patrolA : patrolB;
                dest.y = transform.position.y;
                Vector3 step = dest - transform.position;
                if (step.magnitude > 0.35f)
                {
                    Quaternion walkRot = Quaternion.LookRotation(step.normalized);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, walkRot, 40f * Time.deltaTime);
                    transform.position += transform.forward * (patrolSpeed * Time.deltaTime);
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

        private void FireKineticSlug()
        {
            if (audioSource != null && clipFire != null)
            {
                audioSource.PlayOneShot(clipFire, 1.0f);
            }

            Vector3 origin = muzzleTransform != null ? muzzleTransform.position : transform.position + Vector3.up * 1.5f;
            Vector3 dir = (player.transform.position + Vector3.up * 0.9f - origin).normalized;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, 25f, CollisionLayers.WorldMask | CollisionLayers.PlayerMask))
            {
                if (hit.collider.GetComponentInParent<PlayerMotor>() != null)
                {
                    if (ui != null)
                    {
                        ui.ShowActionNotice("Кинетический удар");
                    }
                }
            }
        }

        public void TakeDamage(float amount)
        {
            if (isDestroyed) return;

            // Check if damage is incoming from front and shield is active
            if (IsShieldActive && player != null)
            {
                Vector3 toPlayer = (player.transform.position - transform.position).normalized;
                float dot = Vector3.Dot(transform.forward, toPlayer);
                if (dot > 0.35f) // Incoming from front 70 degree arc
                {
                    amount *= 0.15f; // Shield absorbs 85% damage
                    if (audioSource != null && clipShieldHit != null)
                    {
                        audioSource.PlayOneShot(clipShieldHit, 0.9f);
                    }
                    if (ui != null)
                    {
                        ui.ShowActionNotice("Броня щита");
                    }
                }
            }

            currentHealth -= amount;
            if (currentHealth <= 0f)
            {
                DestroySentry();
            }
            else
            {
                hasDetectedPlayer = true;
            }
        }

        public void ApplyEmpStun(float duration = 6f)
        {
            if (isDestroyed) return;
            stunTimer = duration;
            attackWindupTimer = 0f;
            if (audioSource != null && clipEmpStun != null)
            {
                audioSource.PlayOneShot(clipEmpStun, 0.85f);
            }
            if (ui != null)
            {
                ui.ShowActionNotice("Щит отключён");
            }
        }

        private void DestroySentry()
        {
            isDestroyed = true;
            currentHealth = 0f;

            if (audioSource != null)
            {
                audioSource.Stop();
                if (clipFire != null) audioSource.PlayOneShot(clipFire, 0.8f);
            }

            if (searchlight != null) searchlight.enabled = false;
            if (boxCollider != null) boxCollider.enabled = false;

            var rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.mass = 450f;
            rb.AddTorque(UnityEngine.Random.insideUnitSphere * 80f, ForceMode.Impulse);
        }
    }
}

