using System;
using Hostfall.Core;
using Hostfall.Simulation;
using Hostfall.UI;
using Hostfall.World;
using UnityEngine;

namespace Hostfall.Player
{
    public sealed class PlayerWeapon : MonoBehaviour
    {
        [SerializeField] private bool hasWeapon = false;
        private PlayerMotor motor;
        private FacilityInput input;
        private ControlState controls;
        private FacilityUI ui;

        private Transform weaponRoot;
        private Light muzzleLight;
        private AudioSource audioSource;
        private static AudioClip clipFire;
        private static bool audioGenerated;

        private float cooldownTimer;
        private float recoilAmount;
        private float flashTimer;

        public bool HasWeapon => hasWeapon;

        public void Initialize(PlayerMotor playerMotor, FacilityInput facilityInput, ControlState controlState, FacilityUI facilityUI)
        {
            motor = playerMotor;
            input = facilityInput;
            controls = controlState;
            ui = facilityUI;

            EnsureAudio();
            BuildWeaponModel();
            SetEquipped(hasWeapon);
        }

        public void Equip()
        {
            hasWeapon = true;
            SetEquipped(true);
            if (audioSource != null && clipFire != null)
            {
                // Play ready / cocking sound
                audioSource.PlayOneShot(clipFire, 0.4f);
            }
        }

        public void Unequip()
        {
            hasWeapon = false;
            SetEquipped(false);
        }

        private void SetEquipped(bool equipped)
        {
            if (weaponRoot != null)
            {
                weaponRoot.gameObject.SetActive(equipped);
            }
        }

        private void EnsureAudio()
        {
            if (audioSource == null)
            {
                if (!TryGetComponent<AudioSource>(out audioSource))
                    audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0f; // 2D punch for the player
            }

            if (!audioGenerated)
            {
                const int rate = 22050;
                float duration = 0.38f;
                int samples = (int)(rate * duration);
                var data = new float[samples];
                for (int i = 0; i < samples; i++)
                {
                    float t = i / (float)rate;
                    // Punchy low transient + electrical discharge crackle
                    float punch = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(120f, 40f, t / 0.15f) * t) * Mathf.Exp(-t * 18f);
                    float noise = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 22f);
                    float arc = Mathf.Sin(2f * Mathf.PI * 1800f * t) * Mathf.Exp(-t * 9f) * 0.25f;
                    data[i] = Mathf.Clamp(punch * 0.7f + noise * 0.5f + arc, -1f, 1f);
                }
                clipFire = AudioClip.Create("ARC12_Fire", samples, 1, rate, false);
                clipFire.SetData(data, 0);
                audioGenerated = true;
            }
        }

        private void BuildWeaponModel()
        {
            if (weaponRoot != null) return;

            var rootObj = new GameObject("NORTHLINE ARC-12 Shotgun");
            weaponRoot = rootObj.transform;
            weaponRoot.SetParent(transform, false);

            var darkMat = MakeLitMat(new Color(0.12f, 0.13f, 0.15f), 0.75f, 0.35f);
            var metalMat = MakeLitMat(new Color(0.48f, 0.52f, 0.56f), 0.88f, 0.75f);
            var cyanMat = MakeLitMat(new Color(0.1f, 0.85f, 1.0f), 0.95f, 0.1f, new Color(0.15f, 0.90f, 1.0f) * 2.8f);
            var redMat = MakeLitMat(new Color(1.0f, 0.15f, 0.1f), 0.95f, 0.1f, new Color(1.0f, 0.18f, 0.12f) * 2.5f);

            // 1. Receiver Chassis
            CreatePart("Receiver", new Vector3(0, 0, 0.05f), new Vector3(0.065f, 0.085f, 0.38f), darkMat, weaponRoot);

            // 2. Heavy Vented Barrel
            CreatePart("Heavy Barrel", new Vector3(0, 0.02f, 0.38f), new Vector3(0.045f, 0.045f, 0.34f), metalMat, weaponRoot);
            CreatePart("Barrel Shroud", new Vector3(0, 0.02f, 0.30f), new Vector3(0.058f, 0.058f, 0.18f), darkMat, weaponRoot);

            // 3. Capacitive Arc Coils (Cyan Emissive Rings)
            CreatePart("Arc Coil Front", new Vector3(0, 0.02f, 0.36f), new Vector3(0.062f, 0.062f, 0.025f), cyanMat, weaponRoot);
            CreatePart("Arc Coil Rear", new Vector3(0, 0.02f, 0.25f), new Vector3(0.062f, 0.062f, 0.025f), cyanMat, weaponRoot);

            // 4. Ergonomic Grip & Stock
            CreatePart("Pistol Grip", new Vector3(0, -0.08f, -0.05f), new Vector3(0.045f, 0.12f, 0.055f), darkMat, weaponRoot);
            CreatePart("Tactical Stock", new Vector3(0, -0.02f, -0.22f), new Vector3(0.055f, 0.075f, 0.22f), darkMat, weaponRoot);

            // 5. Reflex Optical Sight
            CreatePart("Sight Rail", new Vector3(0, 0.055f, 0.08f), new Vector3(0.035f, 0.02f, 0.12f), metalMat, weaponRoot);
            CreatePart("Sight Housing", new Vector3(0, 0.085f, 0.08f), new Vector3(0.045f, 0.045f, 0.04f), darkMat, weaponRoot);
            CreatePart("Sight Reticle Glass", new Vector3(0, 0.085f, 0.08f), new Vector3(0.032f, 0.032f, 0.005f), redMat, weaponRoot);

            // 6. Muzzle Flash Light
            var flashObj = new GameObject("Muzzle Flash Light");
            flashObj.transform.SetParent(weaponRoot, false);
            flashObj.transform.localPosition = new Vector3(0, 0.02f, 0.58f);
            muzzleLight = flashObj.AddComponent<Light>();
            muzzleLight.type = LightType.Point;
            muzzleLight.color = new Color(0.45f, 0.85f, 1.0f);
            muzzleLight.range = 7f;
            muzzleLight.intensity = 3.5f;
            muzzleLight.enabled = false;
        }

        private static Material MakeLitMat(Color baseColor, float smoothness = 0.5f, float metallic = 0.2f, Color? emission = null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
            }
            return mat;
        }

        private static GameObject CreatePart(string name, Vector3 pos, Vector3 size, Material mat, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            var mr = go.GetComponent<MeshRenderer>();
            if (mat != null) mr.sharedMaterial = mat;
            var col = go.GetComponent<BoxCollider>();
            if (col != null) UnityEngine.Object.Destroy(col);
            return go;
        }

        private void LateUpdate()
        {
            if (motor == null || controls == null) return;

            // Manage flash and recoil
            if (flashTimer > 0)
            {
                flashTimer -= Time.deltaTime;
                if (flashTimer <= 0 && muzzleLight != null) muzzleLight.enabled = false;
            }

            if (recoilAmount > 0)
            {
                recoilAmount = Mathf.MoveTowards(recoilAmount, 0f, Time.deltaTime * 1.4f);
            }

            if (cooldownTimer > 0)
            {
                cooldownTimer -= Time.deltaTime;
            }

            if (weaponRoot != null && hasWeapon)
            {
                // Align weapon rigidly with player's first-person view rotation and eye
                Vector3 viewOffset = new Vector3(0.22f, -0.22f, 0.36f - recoilAmount);
                weaponRoot.position = motor.Eye + motor.ViewRotation * viewOffset;
                weaponRoot.rotation = motor.ViewRotation;
            }

            // Handle Firing Input
            if (hasWeapon && controls.CanExplore && input != null && input.Fire != null && input.Fire.WasPressedThisFrame())
            {
                TryFire();
            }
        }

        public bool TryFire()
        {
            if (!hasWeapon || !controls.CanExplore || cooldownTimer > 0) return false;

            cooldownTimer = 0.65f;
            recoilAmount = 0.12f;
            motor.AddPitchKick(-1.8f);

            if (muzzleLight != null)
            {
                muzzleLight.enabled = true;
                flashTimer = 0.06f;
            }

            if (audioSource != null && clipFire != null)
            {
                audioSource.PlayOneShot(clipFire, 0.95f);
            }

            // 5-Pellet Cone Spread Raycast (effective range 8.5m)
            const int pelletCount = 5;
            const float maxRange = 8.5f;
            const float damage = 55f; // 2 shots = 110 dmg -> destroys 100 HP robot!

            Vector3 origin = motor.Eye;
            var hitRobots = new System.Collections.Generic.HashSet<WatcherController>();

            for (int i = 0; i < pelletCount; i++)
            {
                Vector3 spread = i == 0 ? Vector3.zero : UnityEngine.Random.insideUnitSphere * 0.045f;
                Vector3 dir = (motor.ViewRotation * (Vector3.forward + spread)).normalized;

                if (Physics.Raycast(origin, dir, out var hit, maxRange, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore))
                {
                    var watcher = hit.collider.GetComponentInParent<WatcherController>();
                    if (watcher != null)
                    {
                        hitRobots.Add(watcher);
                    }
                    SpawnImpactFX(hit.point, hit.normal);
                }
            }

            // Apply 55 damage to each hit robot (2 shots = 110 dmg -> kills 100 HP robot!)
            foreach (var watcher in hitRobots)
            {
                watcher.TakeDamage(damage);
                if (ui != null && watcher.IsDestroyed)
                {
                    ui.ShowActionNotice("Угроза нейтрализована: " + watcher.Id.ToUpperInvariant());
                }
            }

            return true;
        }

        private static void SpawnImpactFX(Vector3 point, Vector3 normal)
        {
            var spark = new GameObject("Shotgun Impact Spark");
            spark.transform.position = point + normal * 0.02f;
            var light = spark.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.35f, 0.85f, 1.0f);
            light.range = 2.0f;
            light.intensity = 2.5f;
            UnityEngine.Object.Destroy(spark, 0.08f);
        }
    }
}
