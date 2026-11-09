using System;
using Hostfall.Core;
using Hostfall.Player;
using Hostfall.Simulation;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.World
{
    public enum AtlasBossPhase
    {
        Phase1_OrbitalSweep = 1,
        Phase2_CryoShockwave = 2,
        Phase3_CoreExposed = 3,
        Phase4_StaggeredShutdown = 4
    }

    public sealed class AtlasBossController : MonoBehaviour, IDamageable
    {
        [SerializeField] private string enemyId = "atlas-boss-0";
        [SerializeField] private float maxHealth = 600f;

        private float currentHealth = 600f;
        private AtlasBossPhase currentPhase = AtlasBossPhase.Phase1_OrbitalSweep;
        private bool isDestroyed;
        private bool isStaggered;

        private float attackTimer;
        private float phaseTransitionTimer;
        private const float AttackInterval = 4.0f;

        private Transform coreEye;
        private Transform leftArm, rightArm;
        private Light coreLight;
        private AudioSource audioSource;
        private static AudioClip clipSiren, clipLaserSweep, clipSlam, clipExplosion, clipPhaseTransition;
        private static bool audioGenerated;

        private PlayerMotor player;
        private FacilityUI ui;

        public string DamageableId => enemyId;
        public float Health => currentHealth;
        public bool IsDestroyed => isDestroyed;
        public bool IsStaggered => isStaggered;
        public AtlasBossPhase Phase => currentPhase;

        public void Configure(Transform eye, Transform armL, Transform armR, Light light, string id = "atlas-boss-0")
        {
            coreEye = eye;
            leftArm = armL;
            rightArm = armR;
            coreLight = light;
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
                audioSource.minDistance = 6.0f;
                audioSource.maxDistance = 50.0f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
            }

            if (!audioGenerated)
            {
                const int rate = 22050;

                // Boss Warning Siren
                int sirenSamples = (int)(rate * 1.2f);
                var sirenData = new float[sirenSamples];
                for (int i = 0; i < sirenSamples; i++)
                {
                    float t = (float)i / rate;
                    float s1 = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(220f, 440f, Mathf.PingPong(t * 3f, 1f)) * t);
                    sirenData[i] = s1 * 0.45f * Mathf.Exp(-t * 0.8f);
                }
                clipSiren = AudioClip.Create("Atlas_Siren", sirenSamples, 1, rate, false);
                clipSiren.SetData(sirenData, 0);

                // Laser Sweep
                int laserSamples = (int)(rate * 0.85f);
                var laserData = new float[laserSamples];
                for (int i = 0; i < laserSamples; i++)
                {
                    float t = (float)i / rate;
                    float freq = Mathf.Lerp(1200f, 180f, t / 0.85f);
                    laserData[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * (0.3f + 0.5f * (1f - t / 0.85f));
                }
                clipLaserSweep = AudioClip.Create("Atlas_LaserSweep", laserSamples, 1, rate, false);
                clipLaserSweep.SetData(laserData, 0);

                // Heavy Hydraulic Slam
                int slamSamples = (int)(rate * 0.95f);
                var slamData = new float[slamSamples];
                for (int i = 0; i < slamSamples; i++)
                {
                    float t = (float)i / rate;
                    float sub = Mathf.Sin(2f * Mathf.PI * 35f * t) * Mathf.Exp(-t * 4f);
                    float impact = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 20f);
                    slamData[i] = Mathf.Clamp(sub * 0.8f + impact * 0.7f, -1f, 1f);
                }
                clipSlam = AudioClip.Create("Atlas_Slam", slamSamples, 1, rate, false);
                clipSlam.SetData(slamData, 0);

                // Phase Transition Groan
                int transSamples = (int)(rate * 1.5f);
                var transData = new float[transSamples];
                for (int i = 0; i < transSamples; i++)
                {
                    float t = (float)i / rate;
                    float rumble = Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Exp(-t * 1.5f);
                    float hiss = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 3f) * 0.4f;
                    transData[i] = Mathf.Clamp(rumble + hiss, -1f, 1f);
                }
                clipPhaseTransition = AudioClip.Create("Atlas_PhaseTransition", transSamples, 1, rate, false);
                clipPhaseTransition.SetData(transData, 0);

                // Collapse Explosion
                int expSamples = (int)(rate * 1.8f);
                var expData = new float[expSamples];
                for (int i = 0; i < expSamples; i++)
                {
                    float t = (float)i / rate;
                    float boom = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 3.5f);
                    float deep = Mathf.Sin(2f * Mathf.PI * 40f * t) * Mathf.Exp(-t * 2f);
                    expData[i] = Mathf.Clamp(boom * 0.8f + deep * 0.6f, -1f, 1f);
                }
                clipExplosion = AudioClip.Create("Atlas_Explosion", expSamples, 1, rate, false);
                clipExplosion.SetData(expData, 0);

                audioGenerated = true;
            }
        }

        private void Update()
        {
            if (isDestroyed || isStaggered) return;

            if (phaseTransitionTimer > 0f)
            {
                phaseTransitionTimer -= Time.deltaTime;
                return;
            }

            attackTimer += Time.deltaTime;
            if (attackTimer >= AttackInterval)
            {
                attackTimer = 0f;
                ExecutePhaseAttack();
            }

            // Track eye toward player
            if (coreEye != null && player != null)
            {
                Vector3 toPlayer = (player.transform.position + Vector3.up * 0.9f) - coreEye.position;
                if (toPlayer.sqrMagnitude > 0.01f)
                {
                    coreEye.rotation = Quaternion.RotateTowards(coreEye.rotation, Quaternion.LookRotation(toPlayer), 45f * Time.deltaTime);
                }
            }
        }

        private void ExecutePhaseAttack()
        {
            switch (currentPhase)
            {
                case AtlasBossPhase.Phase1_OrbitalSweep:
                    if (audioSource != null && clipLaserSweep != null)
                    {
                        audioSource.PlayOneShot(clipLaserSweep, 1.0f);
                    }
                    if (ui != null)
                    {
                        ui.ShowActionNotice("Лазерный сектор");
                    }
                    break;

                case AtlasBossPhase.Phase2_CryoShockwave:
                    if (audioSource != null && clipSlam != null)
                    {
                        audioSource.PlayOneShot(clipSlam, 1.0f);
                    }
                    if (ui != null)
                    {
                        ui.ShowActionNotice("Крио-сброс");
                    }
                    break;

                case AtlasBossPhase.Phase3_CoreExposed:
                    if (audioSource != null && clipSiren != null)
                    {
                        audioSource.PlayOneShot(clipSiren, 0.9f);
                    }
                    if (ui != null)
                    {
                        ui.ShowActionNotice("Ядро открыто");
                    }
                    break;
            }
        }

        public void TakeDamage(float amount)
        {
            if (isDestroyed || isStaggered) return;

            currentHealth -= amount;

            // Phase Transitions
            if (currentPhase == AtlasBossPhase.Phase1_OrbitalSweep && currentHealth <= 450f)
            {
                TransitionToPhase(AtlasBossPhase.Phase2_CryoShockwave);
            }
            else if (currentPhase == AtlasBossPhase.Phase2_CryoShockwave && currentHealth <= 300f)
            {
                TransitionToPhase(AtlasBossPhase.Phase3_CoreExposed);
            }
            else if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                TriggerStaggeredShutdown();
            }
        }

        public void ApplyEmpStun(float duration = 6f)
        {
            if (isDestroyed || isStaggered) return;
            phaseTransitionTimer = Mathf.Max(phaseTransitionTimer, 3.5f);
            TakeDamage(40f);
            if (ui != null)
            {
                ui.ShowActionNotice("ЭМИ-разряд");
            }
        }

        private void TransitionToPhase(AtlasBossPhase newPhase)
        {
            currentPhase = newPhase;
            phaseTransitionTimer = 2.0f;
            attackTimer = 0f;

            if (audioSource != null && clipPhaseTransition != null)
            {
                audioSource.PlayOneShot(clipPhaseTransition, 1.0f);
            }

            if (coreLight != null)
            {
                coreLight.color = newPhase == AtlasBossPhase.Phase2_CryoShockwave ? new Color(0.2f, 0.7f, 1.0f) :
                                  newPhase == AtlasBossPhase.Phase3_CoreExposed ? new Color(1.0f, 0.2f, 0.1f) :
                                  Color.yellow;
                coreLight.intensity = 8.0f;
            }

            if (ui != null)
            {
                ui.ShowActionNotice($"ATLAS-0 · Фаза {(int)newPhase}");
            }
        }

        private void TriggerStaggeredShutdown()
        {
            isStaggered = true;
            isDestroyed = true;
            currentPhase = AtlasBossPhase.Phase4_StaggeredShutdown;

            if (audioSource != null)
            {
                audioSource.Stop();
                if (clipExplosion != null) audioSource.PlayOneShot(clipExplosion, 1.0f);
            }

            if (coreLight != null)
            {
                coreLight.color = new Color(0.3f, 1.0f, 0.4f);
                coreLight.intensity = 2.0f;
            }

            if (ui != null)
            {
                ui.ShowActionNotice("ATLAS-0 деактивирован · Доступ открыт");
            }
        }
    }
}
