using System;
using Hostfall.Core;
using Hostfall.Simulation;
using Hostfall.UI;
using Hostfall.World;
using UnityEngine;

namespace Hostfall.Player
{
    public enum ActiveWeaponType
    {
        Arc12Shotgun = 0,
        CirrusPulseSidearm = 1,
        TitanbreakerHeavyInductor = 2
    }

    public sealed class PlayerWeapon : MonoBehaviour
    {
        [SerializeField] private bool hasWeapon = false;
        [SerializeField] private ActiveWeaponType activeWeapon = ActiveWeaponType.Arc12Shotgun;
        [SerializeField] private bool hasPulseSidearm = false;
        [SerializeField] private bool hasTitanbreaker = false;
        [SerializeField] private int currentAmmo = 6;
        [SerializeField] private int magazineCapacity = 6;
        [SerializeField] private int reserveAmmo = 18;
        [SerializeField] private int maxReserveAmmo = 18;
        [SerializeField] private int empGrenadeCount = 2;

        private int arc12Current = 6;
        private int arc12Reserve = 18;
        private int cirrusCurrent = 15;
        private int cirrusReserve = 45;
        private int titanCurrent = 3;
        private int titanReserve = 9;

        private PlayerMotor motor;
        private FacilityInput input;
        private ControlState controls;
        private FacilityUI ui;

        private Transform weaponRoot;
        private Light muzzleLight;
        private AudioSource audioSource;
        private static AudioClip clipFire;
        private static AudioClip clipPulseFire;
        private static AudioClip clipTitanFire;
        private static AudioClip clipDryFire;
        private static AudioClip clipReload;
        private static AudioClip clipThrow;
        private static bool audioGenerated;

        private float cooldownTimer;
        private float recoilAmount;
        private float flashTimer;
        private bool isReloading;
        private float reloadTimer;

        public bool HasWeapon => hasWeapon;
        public ActiveWeaponType ActiveWeapon => activeWeapon;
        public bool HasPulseSidearm => hasPulseSidearm;
        public bool HasTitanbreaker => hasTitanbreaker;
        public int CurrentAmmo => currentAmmo;
        public int ReserveAmmo => reserveAmmo;
        public int EmpGrenades => empGrenadeCount;
        public bool IsReloading => isReloading;
        public bool IsAmmoFull => reserveAmmo >= maxReserveAmmo && currentAmmo >= magazineCapacity;

        public void UnlockPulseSidearm()
        {
            hasPulseSidearm = true;
            Equip();
            SwitchWeapon(ActiveWeaponType.CirrusPulseSidearm);
        }

        public void UnlockTitanbreaker()
        {
            hasTitanbreaker = true;
            Equip();
            SwitchWeapon(ActiveWeaponType.TitanbreakerHeavyInductor);
        }

        public void SwitchWeapon(ActiveWeaponType weaponType)
        {
            if (activeWeapon == weaponType) return;
            if (weaponType == ActiveWeaponType.CirrusPulseSidearm && !hasPulseSidearm) return;
            if (weaponType == ActiveWeaponType.TitanbreakerHeavyInductor && !hasTitanbreaker) return;

            if (activeWeapon == ActiveWeaponType.Arc12Shotgun)
            {
                arc12Current = currentAmmo;
                arc12Reserve = reserveAmmo;
            }
            else if (activeWeapon == ActiveWeaponType.CirrusPulseSidearm)
            {
                cirrusCurrent = currentAmmo;
                cirrusReserve = reserveAmmo;
            }
            else
            {
                titanCurrent = currentAmmo;
                titanReserve = reserveAmmo;
            }

            activeWeapon = weaponType;
            if (activeWeapon == ActiveWeaponType.Arc12Shotgun)
            {
                magazineCapacity = 6;
                maxReserveAmmo = 18;
                currentAmmo = arc12Current;
                reserveAmmo = arc12Reserve;
                if (ui != null) ui.ShowActionNotice($"Выбран ARC-12 Shockgun ({currentAmmo}/{reserveAmmo})");
            }
            else if (activeWeapon == ActiveWeaponType.CirrusPulseSidearm)
            {
                magazineCapacity = 15;
                maxReserveAmmo = 45;
                currentAmmo = cirrusCurrent;
                reserveAmmo = cirrusReserve;
                if (ui != null) ui.ShowActionNotice($"Выбран CIRRUS Pulse Sidearm ({currentAmmo}/{reserveAmmo})");
            }
            else
            {
                magazineCapacity = 3;
                maxReserveAmmo = 9;
                currentAmmo = titanCurrent;
                reserveAmmo = titanReserve;
                if (ui != null) ui.ShowActionNotice($"Выбран W10 TITANBREAKER Heavy Inductor ({currentAmmo}/{reserveAmmo})");
            }

            if (audioSource != null && clipReload != null) audioSource.PlayOneShot(clipReload, 0.4f);
        }

        public int AddAmmo(int amount)
        {
            if (amount <= 0) return 0;
            int space = maxReserveAmmo - reserveAmmo;
            int toAdd = Mathf.Min(space, amount);
            reserveAmmo += toAdd;
            return toAdd;
        }

        public void AddEmpGrenades(int count)
        {
            empGrenadeCount = Mathf.Min(4, empGrenadeCount + count);
        }

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
                    float punch = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(120f, 40f, t / 0.15f) * t) * Mathf.Exp(-t * 18f);
                    float noise = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 22f);
                    float arc = Mathf.Sin(2f * Mathf.PI * 1800f * t) * Mathf.Exp(-t * 9f) * 0.25f;
                    data[i] = Mathf.Clamp(punch * 0.7f + noise * 0.5f + arc, -1f, 1f);
                }
                clipFire = AudioClip.Create("ARC12_Fire", samples, 1, rate, false);
                clipFire.SetData(data, 0);

                // CIRRUS Pulse Sidearm Fire (High-tech discharge)
                int pulseSamples = (int)(rate * 0.18f);
                var pulseData = new float[pulseSamples];
                for (int i = 0; i < pulseSamples; i++)
                {
                    float t = (float)i / rate;
                    float freq = Mathf.Lerp(1900f, 420f, t / 0.18f);
                    float wave = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-t * 22f);
                    pulseData[i] = wave * 0.75f;
                }
                clipPulseFire = AudioClip.Create("CIRRUS_Pulse_Fire", pulseSamples, 1, rate, false);
                clipPulseFire.SetData(pulseData, 0);

                // TITANBREAKER Heavy Inductor Fire (Deep inductive thunder charge)
                int titanSamples = (int)(rate * 0.52f);
                var titanData = new float[titanSamples];
                for (int i = 0; i < titanSamples; i++)
                {
                    float t = (float)i / rate;
                    float subBass = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(75f, 28f, t / 0.52f) * t) * Mathf.Exp(-t * 8f);
                    float crack = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 26f);
                    float buzz = Mathf.Sin(2f * Mathf.PI * 880f * t) * Mathf.Exp(-t * 15f) * 0.4f;
                    titanData[i] = Mathf.Clamp(subBass * 0.8f + crack * 0.6f + buzz, -1f, 1f);
                }
                clipTitanFire = AudioClip.Create("TITANBREAKER_Fire", titanSamples, 1, rate, false);
                clipTitanFire.SetData(titanData, 0);

                // Dry Fire Click
                int drySamples = (int)(rate * 0.12f);
                var dryData = new float[drySamples];
                for (int i = 0; i < drySamples; i++)
                {
                    float t = i / (float)rate;
                    dryData[i] = Mathf.Sin(2f * Mathf.PI * 1200f * t) * Mathf.Exp(-t * 60f) * 0.5f;
                }
                clipDryFire = AudioClip.Create("ARC12_DryClick", drySamples, 1, rate, false);
                clipDryFire.SetData(dryData, 0);

                // Reload Pump & Chambering
                int reloadSamples = (int)(rate * 0.85f);
                var reloadData = new float[reloadSamples];
                for (int i = 0; i < reloadSamples; i++)
                {
                    float t = i / (float)rate;
                    float click1 = t < 0.3f ? Mathf.Sin(2f * Mathf.PI * 450f * t) * Mathf.Exp(-t * 25f) : 0f;
                    float click2 = t > 0.45f ? Mathf.Sin(2f * Mathf.PI * 850f * (t - 0.45f)) * Mathf.Exp(-(t - 0.45f) * 35f) : 0f;
                    float rattle = (Mathf.PerlinNoise(t * 800f, 0f) - 0.5f) * Mathf.Exp(-t * 6f) * 0.2f;
                    reloadData[i] = Mathf.Clamp(click1 * 0.6f + click2 * 0.7f + rattle, -1f, 1f);
                }
                clipReload = AudioClip.Create("ARC12_Reload", reloadSamples, 1, rate, false);
                clipReload.SetData(reloadData, 0);

                // Throw Whoosh
                int throwSamples = (int)(rate * 0.25f);
                var throwData = new float[throwSamples];
                for (int i = 0; i < throwSamples; i++)
                {
                    float t = i / (float)rate;
                    throwData[i] = Mathf.Sin(2f * Mathf.PI * 180f * t) * Mathf.Sin(Mathf.Clamp01(t / 0.25f) * Mathf.PI) * 0.4f;
                }
                clipThrow = AudioClip.Create("SFX_Grenade_Throw", throwSamples, 1, rate, false);
                clipThrow.SetData(throwData, 0);

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

            if (isReloading)
            {
                reloadTimer -= Time.deltaTime;
                if (reloadTimer <= 0f)
                {
                    int needed = magazineCapacity - currentAmmo;
                    int transfer = Mathf.Min(needed, reserveAmmo);
                    currentAmmo += transfer;
                    reserveAmmo -= transfer;
                    isReloading = false;
                    if (ui != null)
                    {
                        string wName = activeWeapon == ActiveWeaponType.Arc12Shotgun ? "ARC-12" : activeWeapon == ActiveWeaponType.CirrusPulseSidearm ? "CIRRUS" : "TITANBREAKER";
                        ui.ShowActionNotice($"{wName} готов к бою ({currentAmmo}/{reserveAmmo})");
                    }
                }
            }

            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && hasWeapon && controls.CanExplore)
            {
                if (kb.digit1Key.wasPressedThisFrame) SwitchWeapon(ActiveWeaponType.Arc12Shotgun);
                if (kb.digit2Key.wasPressedThisFrame && hasPulseSidearm) SwitchWeapon(ActiveWeaponType.CirrusPulseSidearm);
                if (kb.digit3Key.wasPressedThisFrame && hasTitanbreaker) SwitchWeapon(ActiveWeaponType.TitanbreakerHeavyInductor);
                if (kb.tabKey.wasPressedThisFrame)
                {
                    if (hasTitanbreaker)
                    {
                        SwitchWeapon(activeWeapon switch
                        {
                            ActiveWeaponType.Arc12Shotgun => hasPulseSidearm ? ActiveWeaponType.CirrusPulseSidearm : ActiveWeaponType.TitanbreakerHeavyInductor,
                            ActiveWeaponType.CirrusPulseSidearm => ActiveWeaponType.TitanbreakerHeavyInductor,
                            _ => ActiveWeaponType.Arc12Shotgun
                        });
                    }
                    else if (hasPulseSidearm)
                    {
                        SwitchWeapon(activeWeapon == ActiveWeaponType.Arc12Shotgun ? ActiveWeaponType.CirrusPulseSidearm : ActiveWeaponType.Arc12Shotgun);
                    }
                }
                if (kb.rKey.wasPressedThisFrame) StartReload();
                if (kb.gKey.wasPressedThisFrame) ThrowEmpGrenade();
            }

            // Handle Firing Input
            if (hasWeapon && controls.CanExplore && input != null && input.Fire != null && input.Fire.WasPressedThisFrame())
            {
                TryFire();
            }
        }

        public bool TryFire()
        {
            if (!hasWeapon || !controls.CanExplore || cooldownTimer > 0 || isReloading) return false;

            if (currentAmmo <= 0)
            {
                cooldownTimer = 0.35f;
                if (audioSource != null && clipDryFire != null)
                {
                    audioSource.PlayOneShot(clipDryFire, 0.75f);
                }
                if (reserveAmmo > 0)
                {
                    StartReload();
                }
                else if (ui != null)
                {
                    string wName = activeWeapon == ActiveWeaponType.Arc12Shotgun ? "ARC-12" : activeWeapon == ActiveWeaponType.CirrusPulseSidearm ? "CIRRUS" : "TITANBREAKER";
                    ui.ShowActionNotice($"[!] НЕТ ПАТРОНОВ ДЛЯ {wName} · НАЙДИТЕ БОЕПРИПАСЫ В ОРУЖЕЙНОЙ");
                }
                return false;
            }

            currentAmmo--;

            if (activeWeapon == ActiveWeaponType.TitanbreakerHeavyInductor)
            {
                cooldownTimer = 1.1f;
                recoilAmount = 0.24f;
                motor.AddPitchKick(-3.2f);

                if (muzzleLight != null)
                {
                    muzzleLight.color = new Color(0.6f, 0.25f, 1.0f);
                    muzzleLight.enabled = true;
                    flashTimer = 0.12f;
                }

                if (audioSource != null && clipTitanFire != null)
                {
                    audioSource.PlayOneShot(clipTitanFire, 1.0f);
                }

                // Heavy Inductor High-Penetration Beam (range 35m, 120 damage)
                Vector3 origin = motor.Eye;
                Vector3 dir = motor.ViewRotation * Vector3.forward;
                if (Physics.Raycast(origin, dir, out var hit, 35.0f, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore))
                {
                    var target = hit.collider.GetComponentInParent<IDamageable>();
                    if (target != null)
                    {
                        target.TakeDamage(120f);
                        if (ui != null)
                        {
                            if (target.IsDestroyed) ui.ShowActionNotice("Цель нейтрализована");
                            else ui.ShowActionNotice("Импульс индуктора");
                        }
                    }
                    SpawnImpactFX(hit.point, hit.normal);
                }
            }
            else if (activeWeapon == ActiveWeaponType.CirrusPulseSidearm)
            {
                cooldownTimer = 0.22f;
                recoilAmount = 0.045f;
                motor.AddPitchKick(-0.45f);

                if (muzzleLight != null)
                {
                    muzzleLight.color = new Color(0.2f, 0.85f, 1.0f);
                    muzzleLight.enabled = true;
                    flashTimer = 0.05f;
                }

                if (audioSource != null && clipPulseFire != null)
                {
                    audioSource.PlayOneShot(clipPulseFire, 0.85f);
                }

                // Single Precision Pulse Beam Raycast (range 24m, 30 damage)
                Vector3 origin = motor.Eye;
                Vector3 dir = motor.ViewRotation * Vector3.forward;
                if (Physics.Raycast(origin, dir, out var hit, 24.0f, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore))
                {
                    var target = hit.collider.GetComponentInParent<IDamageable>();
                    if (target != null)
                    {
                        target.TakeDamage(30f);
                        if (ui != null && target.IsDestroyed)
                        {
                            ui.ShowActionNotice("Цель нейтрализована");
                        }
                    }
                    SpawnImpactFX(hit.point, hit.normal);
                }
            }
            else
            {
                // ARC-12 Shockgun
                cooldownTimer = 0.65f;
                recoilAmount = 0.12f;
                motor.AddPitchKick(-1.8f);

                if (muzzleLight != null)
                {
                    muzzleLight.color = new Color(0.45f, 0.85f, 1.0f);
                    muzzleLight.enabled = true;
                    flashTimer = 0.06f;
                }

                if (audioSource != null && clipFire != null)
                {
                    audioSource.PlayOneShot(clipFire, 0.95f);
                }

                // 6-Pellet Cone Spread Raycast (effective range 9.0m)
                const int pelletCount = 6;
                const float maxRange = 9.0f;
                const float damage = 55f;

                Vector3 origin = motor.Eye;
                var hitTargets = new System.Collections.Generic.HashSet<IDamageable>();

                for (int i = 0; i < pelletCount; i++)
                {
                    Vector3 spread = i == 0 ? Vector3.zero : UnityEngine.Random.insideUnitSphere * 0.042f;
                    Vector3 dir = (motor.ViewRotation * (Vector3.forward + spread)).normalized;

                    if (Physics.Raycast(origin, dir, out var hit, maxRange, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore))
                    {
                        var target = hit.collider.GetComponentInParent<IDamageable>();
                        if (target != null)
                        {
                            hitTargets.Add(target);
                        }
                        SpawnImpactFX(hit.point, hit.normal);
                    }
                }

                // Apply 55 damage to each hit target
                foreach (var target in hitTargets)
                {
                    target.TakeDamage(damage);
                    if (ui != null && target.IsDestroyed)
                    {
                        ui.ShowActionNotice("Цель нейтрализована");
                    }
                }
            }

            if (ui != null && currentAmmo <= 2 && reserveAmmo > 0)
            {
                ui.ShowActionNotice("Мало патронов · R");
            }

            return true;
        }

        public bool StartReload()
        {
            if (!hasWeapon || isReloading || currentAmmo >= magazineCapacity || reserveAmmo <= 0) return false;

            isReloading = true;
            reloadTimer = 1.35f;
            if (audioSource != null && clipReload != null)
            {
                audioSource.PlayOneShot(clipReload, 0.9f);
            }
            if (ui != null)
            {
                ui.ShowActionNotice("Перезарядка");
            }
            return true;
        }

        public bool ThrowEmpGrenade()
        {
            if (!controls.CanExplore || cooldownTimer > 0) return false;

            if (empGrenadeCount <= 0)
            {
                if (ui != null) ui.ShowActionNotice("Нет гранат");
                return false;
            }

            empGrenadeCount--;
            cooldownTimer = 0.5f;

            if (audioSource != null && clipThrow != null)
            {
                audioSource.PlayOneShot(clipThrow, 0.85f);
            }

            Vector3 spawnPos = motor.Eye + motor.ViewRotation * new Vector3(0.15f, -0.1f, 0.4f);
            Vector3 throwVel = motor.ViewRotation * Vector3.forward * 14.5f + Vector3.up * 2.2f;

            var grenadeObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            grenadeObj.name = "EMP Grenade";
            grenadeObj.transform.position = spawnPos;
            grenadeObj.transform.localScale = new Vector3(0.12f, 0.14f, 0.12f);

            var darkMat = MakeLitMat(new Color(0.15f, 0.18f, 0.22f), 0.7f, 0.6f);
            var mr = grenadeObj.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = darkMat;

            var proj = grenadeObj.AddComponent<EmpGrenadeProjectile>();
            proj.Initialize(throwVel, ui);

            if (ui != null)
            {
                ui.ShowActionNotice($"Граната EMP ({empGrenadeCount})");
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
