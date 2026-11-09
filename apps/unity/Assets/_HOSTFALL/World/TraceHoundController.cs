using System;
using Hostfall.Core;
using Hostfall.Player;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.World
{
    public sealed class TraceHoundController : MonoBehaviour, IDamageable
    {
        [SerializeField] private string houndId = "trace-hound-r30";
        [SerializeField] private float maxHealth = 90f;
        [SerializeField] private Vector3 patrolA;
        [SerializeField] private Vector3 patrolB;
        [SerializeField] private float patrolSpeed = 1.8f;
        [SerializeField] private float sprintSpeed = 4.6f;
        [SerializeField] private float visionRange = 16f;
        [SerializeField] private float hearingSprintRange = 20f;
        [SerializeField] private float hearingWalkRange = 8f;

        private Light eyeLight;
        private Transform headTransform;
        private AudioSource audioSource;
        private BoxCollider boxCollider;

        private PlayerMotor player;
        private FacilityUI ui;

        private float currentHealth;
        private bool isDestroyed;
        private float stunTimer;
        private int patrolTarget = 1;

        private bool isAlerted;
        private Vector3 lastHeardPosition;
        private float pounceCooldown;
        private const float PounceInterval = 2.2f;

        private static AudioClip clipPantHum, clipBarkAlert, clipPounce, clipEmpStun;
        private static bool audioGenerated;

        public string DamageableId => houndId;
        public bool IsDestroyed => isDestroyed;
        public float Health => currentHealth;
        public bool IsStunned => stunTimer > 0f;
        public bool IsAlerted => isAlerted;

        public void Configure(Light eyes, Transform head, Vector3 pointA, Vector3 pointB, string id = "trace-hound-r30")
        {
            eyeLight = eyes;
            headTransform = head;
            patrolA = pointA;
            patrolB = pointB;
            houndId = id;
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
                audioSource.minDistance = 2.0f;
                audioSource.maxDistance = 20.0f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
            }

            if (!audioGenerated)
            {
                const int rate = 22050;

                // Mechanical Panting / Servomotor loop
                int pantSamples = rate;
                var pantData = new float[pantSamples];
                for (int i = 0; i < pantSamples; i++)
                {
                    float t = (float)i / rate;
                    float pant = Mathf.Sin(2f * Mathf.PI * 4f * t);
                    float hiss = (UnityEngine.Random.value * 2f - 1f) * Mathf.Max(0f, pant) * 0.25f;
                    float servo = Mathf.Sin(2f * Mathf.PI * 320f * t) * 0.15f;
                    pantData[i] = (hiss + servo) * 0.6f;
                }
                clipPantHum = AudioClip.Create("Hound_PantHum", pantSamples, 1, rate, false);
                clipPantHum.SetData(pantData, 0);

                // High-pitched Mechanical Bark / Sensor Alarm
                int barkSamples = (int)(rate * 0.35f);
                var barkData = new float[barkSamples];
                for (int i = 0; i < barkSamples; i++)
                {
                    float t = (float)i / rate;
                    float f = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(600f, 1400f, t / 0.15f) * t) * Mathf.Exp(-t * 12f);
                    barkData[i] = f * 0.8f;
                }
                clipBarkAlert = AudioClip.Create("Hound_BarkAlert", barkSamples, 1, rate, false);
                clipBarkAlert.SetData(barkData, 0);

                // Pounce Thruster Surge
                int pounceSamples = (int)(rate * 0.5f);
                var pounceData = new float[pounceSamples];
                for (int i = 0; i < pounceSamples; i++)
                {
                    float t = (float)i / rate;
                    float whoosh = (UnityEngine.Random.value * 2f - 1f) * Mathf.Sin(Mathf.Clamp01(t / 0.5f) * Mathf.PI) * 0.7f;
                    pounceData[i] = whoosh;
                }
                clipPounce = AudioClip.Create("Hound_Pounce", pounceSamples, 1, rate, false);
                clipPounce.SetData(pounceData, 0);

                // EMP Stun
                int stunSamples = (int)(rate * 0.4f);
                var stunData = new float[stunSamples];
                for (int i = 0; i < stunSamples; i++)
                {
                    float t = (float)i / rate;
                    stunData[i] = Mathf.Sin(2f * Mathf.PI * 150f * t) * Mathf.Exp(-t * 6f);
                }
                clipEmpStun = AudioClip.Create("Hound_EmpStun", stunSamples, 1, rate, false);
                clipEmpStun.SetData(stunData, 0);

                audioGenerated = true;
            }

            if (audioSource != null && clipPantHum != null && !audioSource.isPlaying && !isDestroyed)
            {
                audioSource.clip = clipPantHum;
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
                if (eyeLight != null)
                {
                    eyeLight.color = Color.blue;
                    eyeLight.intensity = 0.8f;
                }
                return;
            }

            if (pounceCooldown > 0f) pounceCooldown -= Time.deltaTime;

            DetectPlayerAcousticsAndSight();
            ExecuteMovement();
        }

        private void DetectPlayerAcousticsAndSight()
        {
            if (player == null) return;

            Vector3 toPlayer = player.transform.position - transform.position;
            float dist = toPlayer.magnitude;

            // 1. Acoustic Hearing
            float playerSpeed = player.PlanarVelocity.magnitude;
            bool isSprinting = playerSpeed > 4.0f;
            bool isWalking = playerSpeed > 0.5f;

            if (isSprinting && dist <= hearingSprintRange)
            {
                AlertHound(player.transform.position);
                return;
            }
            if (isWalking && dist <= hearingWalkRange)
            {
                AlertHound(player.transform.position);
                return;
            }

            // 2. Optical Vision
            if (dist <= visionRange)
            {
                float angle = Vector3.Angle(transform.forward, toPlayer);
                if (angle < 75f)
                {
                    Vector3 eyePos = transform.position + Vector3.up * 0.8f;
                    if (Physics.Raycast(eyePos, toPlayer.normalized, out RaycastHit hit, dist, CollisionLayers.WorldMask | CollisionLayers.PlayerMask))
                    {
                        if (hit.collider.GetComponentInParent<PlayerMotor>() != null)
                        {
                            AlertHound(player.transform.position);
                        }
                    }
                }
            }
        }

        private void AlertHound(Vector3 targetPos)
        {
            lastHeardPosition = targetPos;
            if (!isAlerted)
            {
                isAlerted = true;
                if (audioSource != null && clipBarkAlert != null)
                {
                    audioSource.PlayOneShot(clipBarkAlert, 0.9f);
                }
                if (eyeLight != null)
                {
                    eyeLight.color = new Color(1.0f, 0.1f, 0.1f);
                    eyeLight.intensity = 4.5f;
                }
                if (ui != null)
                {
                    ui.ShowActionNotice("[!] E10 TRACE-HOUND ЗАСЕК АКУСТИЧЕСКИЙ СЛЕД!");
                }
            }
        }

        private void ExecuteMovement()
        {
            if (isAlerted)
            {
                Vector3 toTarget = lastHeardPosition - transform.position;
                toTarget.y = 0;
                float dist = toTarget.magnitude;

                if (dist > 0.6f)
                {
                    Quaternion rot = Quaternion.LookRotation(toTarget.normalized);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, 90f * Time.deltaTime);
                    transform.position += transform.forward * (sprintSpeed * Time.deltaTime);
                }
                else
                {
                    // Reached target position, lose alert if player not present
                    isAlerted = false;
                    if (eyeLight != null)
                    {
                        eyeLight.color = new Color(1.0f, 0.75f, 0.25f);
                        eyeLight.intensity = 2.5f;
                    }
                }

                // Pounce strike if very close to player
                if (player != null && Vector3.Distance(transform.position, player.transform.position) < 2.5f && pounceCooldown <= 0f)
                {
                    pounceCooldown = PounceInterval;
                    if (audioSource != null && clipPounce != null)
                    {
                        audioSource.PlayOneShot(clipPounce, 0.85f);
                    }
                    if (ui != null)
                    {
                        ui.ShowActionNotice("[!] УДАР КОГТЯМИ E10 TRACE-HOUND!");
                    }
                }
            }
            else
            {
                // Normal Patrol
                Vector3 dest = patrolTarget == 0 ? patrolA : patrolB;
                dest.y = transform.position.y;
                Vector3 step = dest - transform.position;
                if (step.magnitude > 0.35f)
                {
                    Quaternion walkRot = Quaternion.LookRotation(step.normalized);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, walkRot, 60f * Time.deltaTime);
                    transform.position += transform.forward * (patrolSpeed * Time.deltaTime);
                }
                else
                {
                    patrolTarget = 1 - patrolTarget;
                }
            }
        }

        public void TakeDamage(float amount)
        {
            if (isDestroyed) return;

            currentHealth -= amount;
            if (currentHealth <= 0f)
            {
                DestroyHound();
            }
            else if (player != null)
            {
                AlertHound(player.transform.position);
            }
        }

        public void ApplyEmpStun(float duration = 6f)
        {
            if (isDestroyed) return;
            stunTimer = duration;
            isAlerted = false;
            if (audioSource != null && clipEmpStun != null)
            {
                audioSource.PlayOneShot(clipEmpStun, 0.85f);
            }
            if (ui != null)
            {
                ui.ShowActionNotice("[*] E10 TRACE-HOUND ВРЕМЕННО ПАРАЛИЗОВАН ЭМИ!");
            }
        }

        private void DestroyHound()
        {
            isDestroyed = true;
            currentHealth = 0f;

            if (audioSource != null) audioSource.Stop();
            if (eyeLight != null) eyeLight.enabled = false;
            if (boxCollider != null) boxCollider.enabled = false;

            var rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.mass = 85f;
            rb.AddTorque(UnityEngine.Random.insideUnitSphere * 60f, ForceMode.Impulse);
        }
    }
}

