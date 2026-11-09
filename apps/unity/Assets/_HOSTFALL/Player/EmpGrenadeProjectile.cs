using System;
using System.Collections.Generic;
using Hostfall.Core;
using Hostfall.Simulation;
using Hostfall.UI;
using Hostfall.World;
using UnityEngine;

namespace Hostfall.Player
{
    public sealed class EmpGrenadeProjectile : MonoBehaviour
    {
        [SerializeField] private float fuseDuration = 1.8f;
        [SerializeField] private float blastRadius = 8.0f;
        [SerializeField] private float shockDamage = 40.0f;
        [SerializeField] private float stunDuration = 7.0f;

        private Rigidbody rb;
        private Light beaconLight;
        private AudioSource audioSource;
        private FacilityUI ui;

        private float timer;
        private float beepInterval = 0.45f;
        private float nextBeepTime;
        private bool hasDetonated;

        private static AudioClip clipBeep;
        private static AudioClip clipBlast;
        private static bool audioGenerated;

        public void Initialize(Vector3 throwVelocity, FacilityUI facilityUI = null)
        {
            ui = facilityUI;
            EnsureAudio();

            rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 0.85f;
            rb.linearDamping = 0.15f;
            rb.angularDamping = 0.5f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = throwVelocity;
            rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 12.0f;

            timer = fuseDuration;
            nextBeepTime = fuseDuration - beepInterval;
        }

        private void Awake()
        {
            EnsureAudio();
            EnsureVisuals();
        }

        private void EnsureVisuals()
        {
            if (beaconLight == null)
            {
                var lightObj = new GameObject("Beacon LED");
                lightObj.transform.SetParent(transform, false);
                lightObj.transform.localPosition = Vector3.up * 0.08f;
                beaconLight = lightObj.AddComponent<Light>();
                beaconLight.type = LightType.Point;
                beaconLight.color = new Color(0.2f, 0.75f, 1.0f);
                beaconLight.range = 2.5f;
                beaconLight.intensity = 2.0f;
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
                audioSource.maxDistance = 24.0f;
            }

            if (!audioGenerated)
            {
                clipBeep = CreateBeepSound();
                clipBlast = CreateBlastSound();
                audioGenerated = true;
            }
        }

        private void Update()
        {
            if (hasDetonated) return;

            timer -= Time.deltaTime;

            // Flash beacon light
            float normalizedTime = Mathf.Clamp01(timer / fuseDuration);
            beepInterval = Mathf.Lerp(0.12f, 0.45f, normalizedTime);

            if (timer <= nextBeepTime)
            {
                nextBeepTime = timer - beepInterval;
                if (audioSource != null && clipBeep != null)
                {
                    audioSource.PlayOneShot(clipBeep, 0.65f);
                }
                if (beaconLight != null)
                {
                    beaconLight.intensity = 5.0f;
                }
            }
            else if (beaconLight != null && beaconLight.intensity > 1.5f)
            {
                beaconLight.intensity = Mathf.MoveTowards(beaconLight.intensity, 1.5f, Time.deltaTime * 18f);
            }

            if (timer <= 0f)
            {
                Detonate();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // If grenade directly hits an active robot, detonate on impact!
            var target = collision.collider.GetComponentInParent<IDamageable>();
            if (target != null && !target.IsDestroyed && timer < fuseDuration - 0.2f)
            {
                Detonate();
            }
        }

        public void Detonate()
        {
            if (hasDetonated) return;
            hasDetonated = true;

            Vector3 blastCenter = transform.position;

            // Play blast sound in detached audio gameobject so it finishes playing after projectile is destroyed
            var blastAudioObj = new GameObject("EMP Blast Audio");
            blastAudioObj.transform.position = blastCenter;
            var blastSource = blastAudioObj.AddComponent<AudioSource>();
            blastSource.spatialBlend = 1.0f;
            blastSource.minDistance = 4.0f;
            blastSource.maxDistance = 35.0f;
            blastSource.PlayOneShot(clipBlast, 1.0f);
            Destroy(blastAudioObj, 1.8f);

            // Expanding EMP Shockwave VFX
            SpawnShockwaveVFX(blastCenter);

            // Affect all damageables in radius
            var hitDamageables = new HashSet<IDamageable>();
            var colliders = Physics.OverlapSphere(blastCenter, blastRadius, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore);

            foreach (var hitCol in colliders)
            {
                var damageable = hitCol.GetComponentInParent<IDamageable>();
                if (damageable != null && !damageable.IsDestroyed && hitDamageables.Add(damageable))
                {
                    damageable.ApplyEmpStun(stunDuration);
                    damageable.TakeDamage(shockDamage);

                    if (ui != null && damageable.IsDestroyed)
                    {
                        ui.ShowActionNotice("Угроза уничтожена импульсом: " + damageable.DamageableId.ToUpperInvariant());
                    }
                }

                var hitRb = hitCol.GetComponentInParent<Rigidbody>();
                if (hitRb != null && !hitRb.isKinematic)
                {
                    hitRb.AddExplosionForce(450f, blastCenter, blastRadius, 0.4f, ForceMode.Impulse);
                }
            }

            if (ui != null && hitDamageables.Count > 0)
            {
                ui.ShowActionNotice($"[EMP] Электромагнитный импульс нейтрализовал {hitDamageables.Count} целей в радиусе {blastRadius:F0}м");
            }

            Destroy(gameObject);
        }

        private void SpawnShockwaveVFX(Vector3 center)
        {
            var fxObj = new GameObject("EMP Shockwave Visual");
            fxObj.transform.position = center;

            var light = fxObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.2f, 0.85f, 1.0f);
            light.range = blastRadius * 1.3f;
            light.intensity = 8.0f;

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(fxObj.transform, false);
            sphere.transform.localScale = Vector3.one * 0.2f;
            var col = sphere.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var mr = sphere.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(0.1f, 0.8f, 1.0f, 0.4f));
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.2f, 0.9f, 1.0f) * 4.0f);
            mr.sharedMaterial = mat;

            // Expand and fade out over 0.35s
            var expander = fxObj.AddComponent<ShockwaveExpander>();
            expander.Initialize(sphere.transform, light, blastRadius * 1.6f, 0.35f);
        }

        private static AudioClip CreateBeepSound()
        {
            const int rate = 22050;
            float duration = 0.08f;
            int samples = (int)(rate * duration);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)rate;
                data[i] = Mathf.Sin(2f * Mathf.PI * 1850f * t) * Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI) * 0.5f;
            }
            var clip = AudioClip.Create("SFX_EMP_Beep", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateBlastSound()
        {
            const int rate = 22050;
            float duration = 1.1f;
            int samples = (int)(rate * duration);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)rate;
                float bass = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(85f, 25f, t) * t) * Mathf.Exp(-t * 5f);
                float crack = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 14f);
                float hum = Mathf.Sin(2f * Mathf.PI * 1200f * t) * Mathf.Exp(-t * 7f) * 0.35f;
                data[i] = Mathf.Clamp(bass * 0.8f + crack * 0.5f + hum, -1f, 1f);
            }
            var clip = AudioClip.Create("SFX_EMP_Blast", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
