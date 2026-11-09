using System;
using Hostfall.Core;
using Hostfall.Player;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.World
{
    public sealed class LensDroneController : MonoBehaviour, IDamageable
    {
        [SerializeField] private string droneId = "drone-lens-01";
        [SerializeField] private float maxHealth = 60f;
        [SerializeField] private Vector3 patrolA = new(0, 3.9f, 19.0f);
        [SerializeField] private Vector3 patrolB = new(0, 3.9f, 34.0f);
        [SerializeField] private float speed = 1.6f;
        [SerializeField] private float coneAngle = 60f;
        [SerializeField] private float scanRange = 9.5f;

        private Light searchlight;
        private Transform lensTransform;
        private AudioSource audioSource;
        private Rigidbody body;
        private Collider col;

        private PlayerMotor player;
        private FacilityUI ui;
        private WatcherController[] groundRobots;

        private float currentHealth;
        private bool isDestroyed;
        private int patrolDirection = 1;
        private float alertTimer;
        private bool isAlerted;
        private float hoverBob;

        private Color searchColor = new(1.0f, 0.72f, 0.22f);
        private Color alertColor = new(1.0f, 0.08f, 0.08f);

        private static AudioClip clipHover, clipAlertBuzzer, clipCrash;
        private static bool audioGenerated;

        public string DamageableId => droneId;
        public bool IsDestroyed => isDestroyed;
        public float Health => currentHealth;

        public void Configure(Vector3 start, Vector3 end, Light spot, Transform lens, string id = "drone-lens-01")
        {
            patrolA = start;
            patrolB = end;
            searchlight = spot;
            lensTransform = lens;
            droneId = id;
            currentHealth = maxHealth;
        }

        public void Initialize(PlayerMotor playerMotor, FacilityUI facilityUI, WatcherController[] robots = null)
        {
            player = playerMotor;
            ui = facilityUI;
            groundRobots = robots;
            EnsureAudio();
        }

        private void Awake()
        {
            currentHealth = maxHealth;
            body = GetComponent<Rigidbody>();
            if (body == null) body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            col = GetComponent<Collider>();
            EnsureAudio();
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
                GenerateAudioClips();
                audioGenerated = true;
            }

            if (audioSource != null && clipHover != null && !audioSource.isPlaying && !isDestroyed)
            {
                audioSource.clip = clipHover;
                audioSource.loop = true;
                audioSource.volume = 0.35f;
                audioSource.Play();
            }
        }

        private static void GenerateAudioClips()
        {
            const int sampleRate = 44100;

            // 1. Drone Rotor / Thruster Hover Hum (180 Hz with harmonic modulation)
            int hoverSamples = sampleRate; // 1 second loop
            float[] hoverData = new float[hoverSamples];
            for (int i = 0; i < hoverSamples; i++)
            {
                float t = (float)i / sampleRate;
                float f1 = Mathf.Sin(2f * Mathf.PI * 180f * t);
                float f2 = Mathf.Sin(2f * Mathf.PI * 360f * t) * 0.4f;
                float mod = 1f + 0.15f * Mathf.Sin(2f * Mathf.PI * 12f * t);
                hoverData[i] = (f1 + f2) * 0.25f * mod;
            }
            clipHover = AudioClip.Create("Drone_Hover_Loop", hoverSamples, 1, sampleRate, false);
            clipHover.SetData(hoverData, 0);

            // 2. High-Pitched Threat Buzzer (880 Hz square-like sharp burst)
            int alertSamples = sampleRate / 2; // 0.5s burst
            float[] alertData = new float[alertSamples];
            for (int i = 0; i < alertSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.PingPong(t * 16f, 1f);
                float wave = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 880f * t));
                alertData[i] = wave * env * 0.45f;
            }
            clipAlertBuzzer = AudioClip.Create("Drone_Alert_Buzzer", alertSamples, 1, sampleRate, false);
            clipAlertBuzzer.SetData(alertData, 0);

            // 3. Crash / Explosion Impact (low transient + noise burst)
            int crashSamples = sampleRate * 3 / 4;
            float[] crashData = new float[crashSamples];
            for (int i = 0; i < crashSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 6f);
                float sub = Mathf.Sin(2f * Mathf.PI * 75f * t * (1f - t * 0.5f));
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.7f;
                crashData[i] = (sub * 0.6f + noise * 0.4f) * env * 0.8f;
            }
            clipCrash = AudioClip.Create("Drone_Crash", crashSamples, 1, sampleRate, false);
            clipCrash.SetData(crashData, 0);
        }

        private void Update()
        {
            if (isDestroyed) return;

            // 1. Hover & Patrol Locomotion
            hoverBob += Time.deltaTime * 2.5f;
            Vector3 targetPatrol = patrolDirection > 0 ? patrolB : patrolA;
            Vector3 flatPos = transform.position;
            flatPos.y = targetPatrol.y + Mathf.Sin(hoverBob) * 0.12f;
            transform.position = flatPos;

            Vector3 stepDir = (targetPatrol - transform.position).normalized;
            transform.position += stepDir * (speed * Time.deltaTime);

            if (stepDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(stepDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 3.0f);
            }

            if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                                 new Vector3(targetPatrol.x, 0, targetPatrol.z)) < 0.6f)
            {
                patrolDirection = -patrolDirection;
            }

            // 2. Player Perception in Spotlight Cone
            CheckPlayerDetection();
        }

        private void CheckPlayerDetection()
        {
            if (player == null || isDestroyed) return;

            Vector3 dronePos = transform.position;
            Vector3 playerTarget = player.transform.position + Vector3.up * 1.0f;
            Vector3 delta = playerTarget - dronePos;
            float dist = delta.magnitude;

            bool insideCone = false;
            if (dist <= scanRange)
            {
                // Spotlight shines downward-forward (pitch ~75 deg)
                Vector3 spotForward = searchlight != null ? searchlight.transform.forward : (transform.forward + Vector3.down * 1.8f).normalized;
                float angle = Vector3.Angle(spotForward, delta.normalized);
                if (angle <= coneAngle * 0.5f)
                {
                    // Check physical line-of-sight
                    if (!Physics.Linecast(dronePos, playerTarget, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore))
                    {
                        insideCone = true;
                    }
                }
            }

            if (insideCone)
            {
                alertTimer += Time.deltaTime * 2.0f;
                if (alertTimer >= 0.4f && !isAlerted)
                {
                    TriggerAlert();
                }
            }
            else
            {
                alertTimer = Mathf.Max(0f, alertTimer - Time.deltaTime * 1.2f);
                if (alertTimer <= 0.05f && isAlerted)
                {
                    ClearAlert();
                }
            }

            // Smooth light color transitions
            if (searchlight != null)
            {
                float t = Mathf.Clamp01(alertTimer);
                searchlight.color = Color.Lerp(searchColor, alertColor, t);
                searchlight.intensity = Mathf.Lerp(3.2f, 5.5f, t);
            }
        }

        private void TriggerAlert()
        {
            isAlerted = true;
            if (audioSource != null && clipAlertBuzzer != null)
            {
                audioSource.PlayOneShot(clipAlertBuzzer, 0.75f);
            }

            if (ui != null)
            {
                ui.ShowActionNotice("Обнаружение");
            }

            // Alert ground robots in area
            if (groundRobots != null && player != null)
            {
                Vector3 playerPos = player.transform.position;
                foreach (var robot in groundRobots)
                {
                    if (robot != null && !robot.IsDestroyed)
                    {
                        robot.TakeDamage(0f); // Triggers Alert & lastKnownPosition update without subtracting health
                    }
                }
            }
        }

        private void ClearAlert()
        {
            isAlerted = false;
        }

        public void TakeDamage(float amount)
        {
            if (isDestroyed) return;

            currentHealth -= amount;
            SpawnSparks();

            if (currentHealth <= 0f)
            {
                DestroyDrone();
            }
            else
            {
                // Immediate alert on being shot
                TriggerAlert();
            }
        }

        public void ApplyEmpStun(float duration = 6f)
        {
            if (isDestroyed) return;
            // EMP pulse shuts down turbine drivers, causing fatal crash
            TakeDamage(maxHealth);
        }

        private void DestroyDrone()
        {
            isDestroyed = true;
            currentHealth = 0f;

            if (audioSource != null)
            {
                audioSource.Stop();
                if (clipCrash != null) audioSource.PlayOneShot(clipCrash, 1.0f);
            }

            if (searchlight != null)
            {
                searchlight.enabled = false;
            }

            if (col != null)
            {
                col.enabled = false;
            }

            // Release physics body to tumble and crash to the floor
            if (body != null)
            {
                body.isKinematic = false;
                body.useGravity = true;
                body.linearVelocity = new Vector3(UnityEngine.Random.Range(-1.5f, 1.5f), 0.5f, UnityEngine.Random.Range(-1.5f, 1.5f));
                body.angularVelocity = UnityEngine.Random.insideUnitSphere * 8f;
            }

            SpawnDestructionFX();

            if (ui != null)
            {
                ui.ShowActionNotice("Дрон нейтрализован");
            }
        }

        private void SpawnSparks()
        {
            var spark = new GameObject("Drone Spark");
            spark.transform.position = transform.position;
            var light = spark.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.4f, 0.85f, 1.0f);
            light.range = 3.0f;
            light.intensity = 4.0f;
            Destroy(spark, 0.12f);
        }

        private void SpawnDestructionFX()
        {
            var blast = new GameObject("Drone Explosion Flash");
            blast.transform.position = transform.position;
            var light = blast.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1.0f, 0.5f, 0.15f);
            light.range = 6.0f;
            light.intensity = 8.0f;
            Destroy(blast, 0.25f);
        }
    }
}

