using System;
using Hostfall.Core;
using Hostfall.Player;
using Hostfall.Simulation;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.World
{
    public sealed class RailEyeTurretController : MonoBehaviour, IDamageable
    {
        [SerializeField] private string enemyId = "rail-eye-r31";
        [SerializeField] private float maxHealth = 120f;
        [SerializeField] private float railSpeed = 2.4f;
        [SerializeField] private float attackRange = 18f;
        [SerializeField] private float detectionAngle = 65f;

        private float currentHealth = 120f;
        private bool isDestroyed;
        private float stunTimer;
        private float attackWindupTimer;
        private float attackCooldownTimer;
        private const float WindupDuration = 0.75f;
        private const float CooldownDuration = 2.2f;

        private Vector3 railA, railB;
        private int railDirection = 1;
        private Transform turretHead;
        private Transform muzzleTransform;
        private Light targetingLaser;
        private AudioSource audioSource;
        private static AudioClip clipCharge, clipFire, clipEmpStun;
        private static bool audioGenerated;

        private PlayerMotor player;
        private FacilityUI ui;
        private bool hasDetectedPlayer;

        public string DamageableId => enemyId;
        public float Health => currentHealth;
        public bool IsDestroyed => isDestroyed;
        public bool IsStunned => stunTimer > 0f;
        public bool HasDetectedPlayer => hasDetectedPlayer;

        public void Configure(Transform head, Transform muzzle, Light laser, Vector3 start, Vector3 end, string id = "rail-eye-r31")
        {
            turretHead = head;
            muzzleTransform = muzzle;
            targetingLaser = laser;
            railA = start;
            railB = end;
            enemyId = id;
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
                audioSource.minDistance = 3.0f;
                audioSource.maxDistance = 26.0f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
            }

            if (!audioGenerated)
            {
                const int rate = 22050;

                // Rail charge whine
                int chargeSamples = (int)(rate * 0.75f);
                var chargeData = new float[chargeSamples];
                for (int i = 0; i < chargeSamples; i++)
                {
                    float t = (float)i / rate;
                    float freq = Mathf.Lerp(350f, 2200f, Mathf.Pow(t / 0.75f, 2f));
                    chargeData[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * (0.2f + 0.6f * (t / 0.75f));
                }
                clipCharge = AudioClip.Create("RailEye_Charge", chargeSamples, 1, rate, false);
                clipCharge.SetData(chargeData, 0);

                // Rail shot crack
                int fireSamples = (int)(rate * 0.35f);
                var fireData = new float[fireSamples];
                for (int i = 0; i < fireSamples; i++)
                {
                    float t = (float)i / rate;
                    float snap = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 30f);
                    float ring = Mathf.Sin(2f * Mathf.PI * 1650f * t) * Mathf.Exp(-t * 12f) * 0.5f;
                    fireData[i] = Mathf.Clamp(snap * 0.8f + ring, -1f, 1f);
                }
                clipFire = AudioClip.Create("RailEye_Fire", fireSamples, 1, rate, false);
                clipFire.SetData(fireData, 0);

                // EMP stun fizzle
                int stunSamples = (int)(rate * 0.45f);
                var stunData = new float[stunSamples];
                for (int i = 0; i < stunSamples; i++)
                {
                    float t = (float)i / rate;
                    stunData[i] = Mathf.Sin(2f * Mathf.PI * 90f * t) * (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 5f);
                }
                clipEmpStun = AudioClip.Create("RailEye_Stun", stunSamples, 1, rate, false);
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
                if (targetingLaser != null) targetingLaser.enabled = false;
                return;
            }

            if (targetingLaser != null) targetingLaser.enabled = true;
            if (attackCooldownTimer > 0f) attackCooldownTimer -= Time.deltaTime;

            CheckSensors();
            HandleBehavior();
        }

        private void CheckSensors()
        {
            if (player == null) return;

            Vector3 eyePos = transform.position;
            Vector3 toPlayer = (player.transform.position + Vector3.up * 0.9f) - eyePos;
            float dist = toPlayer.magnitude;

            if (dist > attackRange)
            {
                hasDetectedPlayer = false;
                return;
            }

            // Downward cone check
            float angle = Vector3.Angle(Vector3.down, toPlayer);
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
                // Head tracks player
                if (turretHead != null)
                {
                    Vector3 lookDir = (player.transform.position + Vector3.up * 0.9f) - turretHead.position;
                    if (lookDir.sqrMagnitude > 0.01f)
                    {
                        turretHead.rotation = Quaternion.RotateTowards(turretHead.rotation, Quaternion.LookRotation(lookDir), 140f * Time.deltaTime);
                    }
                }

                if (targetingLaser != null)
                {
                    targetingLaser.color = Color.red;
                    targetingLaser.intensity = 5.0f;
                }

                // Attack sequence
                if (attackCooldownTimer <= 0f)
                {
                    if (attackWindupTimer == 0f && audioSource != null && clipCharge != null)
                    {
                        audioSource.PlayOneShot(clipCharge, 0.75f);
                    }

                    attackWindupTimer += Time.deltaTime;
                    if (attackWindupTimer >= WindupDuration)
                    {
                        FireRailSlug();
                        attackWindupTimer = 0f;
                        attackCooldownTimer = CooldownDuration;
                    }
                }
            }
            else
            {
                attackWindupTimer = 0f;
                if (targetingLaser != null)
                {
                    targetingLaser.color = new Color(1.0f, 0.45f, 0.1f);
                    targetingLaser.intensity = 2.5f;
                }

                // Rail traversal patrol
                Vector3 dest = railDirection > 0 ? railB : railA;
                Vector3 step = dest - transform.position;
                if (step.magnitude > 0.15f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, dest, railSpeed * Time.deltaTime);
                }
                else
                {
                    railDirection = -railDirection;
                }

                if (turretHead != null)
                {
                    turretHead.localRotation = Quaternion.RotateTowards(turretHead.localRotation, Quaternion.identity, 60f * Time.deltaTime);
                }
            }
        }

        private void FireRailSlug()
        {
            if (audioSource != null && clipFire != null)
            {
                audioSource.PlayOneShot(clipFire, 0.95f);
            }

            Vector3 origin = muzzleTransform != null ? muzzleTransform.position : transform.position;
            Vector3 dir = (player.transform.position + Vector3.up * 0.9f - origin).normalized;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, attackRange, CollisionLayers.WorldMask | CollisionLayers.PlayerMask))
            {
                if (hit.collider.GetComponentInParent<PlayerMotor>() != null)
                {
                    if (ui != null)
                    {
                        ui.ShowActionNotice("Прицельный выстрел");
                    }
                }
            }
        }

        public void TakeDamage(float amount)
        {
            if (isDestroyed) return;
            currentHealth -= amount;
            if (currentHealth <= 0f)
            {
                DestroyTurret();
            }
            else
            {
                hasDetectedPlayer = true;
            }
        }

        public void ApplyEmpStun(float duration = 8.0f)
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
                ui.ShowActionNotice("Турель обесточена");
            }
        }

        private void DestroyTurret()
        {
            isDestroyed = true;
            currentHealth = 0f;
            if (targetingLaser != null) targetingLaser.enabled = false;
            if (audioSource != null)
            {
                audioSource.Stop();
                if (clipFire != null) audioSource.PlayOneShot(clipFire, 0.8f);
            }
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }
}

