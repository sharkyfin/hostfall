using System;
using Hostfall.Core;
using Hostfall.Player;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.World
{
    public sealed class JammerRobotController : MonoBehaviour, IDamageable
    {
        [SerializeField] private string jammerId = "security-jammer-r28";
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float jamRadius = 12f;
        [SerializeField] private Vector3 patrolA;
        [SerializeField] private Vector3 patrolB;
        [SerializeField] private float moveSpeed = 1.35f;

        private Transform antennaTransform;
        private Light beaconLight;
        private AudioSource audioSource;
        private BoxCollider boxCollider;

        private PlayerMotor player;
        private FacilityUI ui;

        private float currentHealth;
        private bool isDestroyed;
        private float stunTimer;
        private int patrolTarget = 1;
        private bool wasJammingPlayer;

        private static AudioClip clipRfHum, clipSpark, clipEmpStun;
        private static bool audioGenerated;

        public string DamageableId => jammerId;
        public bool IsDestroyed => isDestroyed;
        public float Health => currentHealth;
        public bool IsStunned => stunTimer > 0f;
        public bool IsJammingActive => !isDestroyed && !IsStunned;

        public void Configure(Transform antenna, Light beacon, Vector3 pointA, Vector3 pointB, string id = "security-jammer-r28")
        {
            antennaTransform = antenna;
            beaconLight = beacon;
            patrolA = pointA;
            patrolB = pointB;
            jammerId = id;
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
                audioSource.minDistance = 2.5f;
                audioSource.maxDistance = 18.0f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
            }

            if (!audioGenerated)
            {
                const int rate = 22050;

                // High-frequency RF Warble Hum
                int rfSamples = rate;
                var rfData = new float[rfSamples];
                for (int i = 0; i < rfSamples; i++)
                {
                    float t = (float)i / rate;
                    float baseTone = Mathf.Sin(2f * Mathf.PI * 440f * t);
                    float warble = Mathf.Sin(2f * Mathf.PI * (880f + 120f * Mathf.Sin(2f * Mathf.PI * 18f * t)) * t) * 0.35f;
                    float noise = (UnityEngine.Random.value * 2f - 1f) * 0.15f;
                    rfData[i] = (baseTone * 0.4f + warble + noise) * 0.5f;
                }
                clipRfHum = AudioClip.Create("Jammer_RfHum", rfSamples, 1, rate, false);
                clipRfHum.SetData(rfData, 0);

                // Antenna Discharge Spark
                int sparkSamples = (int)(rate * 0.25f);
                var sparkData = new float[sparkSamples];
                for (int i = 0; i < sparkSamples; i++)
                {
                    float t = (float)i / rate;
                    sparkData[i] = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 18f);
                }
                clipSpark = AudioClip.Create("Jammer_Spark", sparkSamples, 1, rate, false);
                clipSpark.SetData(sparkData, 0);

                // EMP Stun
                int stunSamples = (int)(rate * 0.4f);
                var stunData = new float[stunSamples];
                for (int i = 0; i < stunSamples; i++)
                {
                    float t = (float)i / rate;
                    stunData[i] = Mathf.Sin(2f * Mathf.PI * 120f * t) * Mathf.Exp(-t * 5f);
                }
                clipEmpStun = AudioClip.Create("Jammer_EmpStun", stunSamples, 1, rate, false);
                clipEmpStun.SetData(stunData, 0);

                audioGenerated = true;
            }

            if (audioSource != null && clipRfHum != null && !audioSource.isPlaying && !isDestroyed)
            {
                audioSource.clip = clipRfHum;
                audioSource.loop = true;
                audioSource.volume = 0.45f;
                audioSource.Play();
            }
        }

        private void Update()
        {
            if (isDestroyed) return;

            if (stunTimer > 0f)
            {
                stunTimer -= Time.deltaTime;
                if (beaconLight != null)
                {
                    beaconLight.color = Color.blue;
                    beaconLight.intensity = 0.8f;
                }
                return;
            }

            // Spin jamming antenna mast
            if (antennaTransform != null)
            {
                antennaTransform.Rotate(Vector3.up, 360f * Time.deltaTime, Space.Self);
            }

            // Beacon strobe
            if (beaconLight != null)
            {
                beaconLight.color = new Color(0.9f, 0.2f, 1.0f);
                beaconLight.intensity = 2.5f + 1.8f * Mathf.Sin(Time.time * 16f);
            }

            // Patrol movement
            Vector3 dest = patrolTarget == 0 ? patrolA : patrolB;
            dest.y = transform.position.y;
            Vector3 step = dest - transform.position;
            if (step.magnitude > 0.35f)
            {
                Quaternion walkRot = Quaternion.LookRotation(step.normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, walkRot, 55f * Time.deltaTime);
                transform.position += transform.forward * (moveSpeed * Time.deltaTime);
            }
            else
            {
                patrolTarget = 1 - patrolTarget;
            }

            // Jamming proximity check
            if (player != null && !isDestroyed)
            {
                float dist = Vector3.Distance(transform.position, player.transform.position);
                bool inRadius = dist <= jamRadius;

                if (inRadius && !wasJammingPlayer)
                {
                    wasJammingPlayer = true;
                    if (ui != null)
                    {
                        ui.ShowActionNotice("Радиопомехи");
                    }
                }
                else if (!inRadius && wasJammingPlayer)
                {
                    wasJammingPlayer = false;
                }
            }
        }

        public void TakeDamage(float amount)
        {
            if (isDestroyed) return;

            currentHealth -= amount;
            if (audioSource != null && clipSpark != null)
            {
                audioSource.PlayOneShot(clipSpark, 0.8f);
            }

            if (currentHealth <= 0f)
            {
                DestroyJammer();
            }
        }

        public void ApplyEmpStun(float duration = 6f)
        {
            if (isDestroyed) return;
            stunTimer = duration;
            if (audioSource != null && clipEmpStun != null)
            {
                audioSource.PlayOneShot(clipEmpStun, 0.85f);
            }
            if (ui != null)
            {
                ui.ShowActionNotice("Помехи сняты");
            }
        }

        private void DestroyJammer()
        {
            isDestroyed = true;
            currentHealth = 0f;

            if (audioSource != null) audioSource.Stop();
            if (beaconLight != null) beaconLight.enabled = false;
            if (boxCollider != null) boxCollider.enabled = false;

            if (wasJammingPlayer && ui != null)
            {
                ui.ShowActionNotice("Связь восстановлена");
            }

            var rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.mass = 120f;
            rb.AddTorque(UnityEngine.Random.insideUnitSphere * 40f, ForceMode.Impulse);
        }
    }
}

