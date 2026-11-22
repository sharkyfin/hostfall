using System;
using System.Linq;
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
        [SerializeField] private int maxReserveAmmo = 72;
        [SerializeField] private int empGrenadeCount = 2;

        private int arc12Current = 6;
        private int arc12Reserve = 18;
        private int cirrusCurrent = 15;
        private int cirrusReserve = 45;
        private int titanCurrent = 3;
        private int titanReserve = 9;

        private WeaponWorldState worldWeapons;
        private readonly System.Collections.Generic.List<DroppedWeapon> drops=new();
        private readonly Transform[] cachedModels=new Transform[3];
        private WatcherController[] hearingTargets;
        private readonly Light[] impacts=new Light[3];private readonly float[] impactUntil=new float[3];private int impactIndex;
        private float heat,charge;private bool overheated,charging;
        public float Heat=>heat;public float Charge=>charge;public bool IsOverheated=>overheated;
        public float ReloadDuration=>activeWeapon==ActiveWeaponType.Arc12Shotgun?.48f:activeWeapon==ActiveWeaponType.CirrusPulseSidearm?1.1f:2.2f;
        public static string DisplayName(ActiveWeaponType kind)=>kind==ActiveWeaponType.Arc12Shotgun?"ARC-12":kind==ActiveWeaponType.CirrusPulseSidearm?"CIRRUS":"TITAN";
        private OperatorMotion motion;private float equipAge=10;
        private Camera aimCamera;
        private Hostfall.CameraSystem.PlayerCameraRig cameraRig;
        private PlayerMotor motor;
        private FacilityInput input;
        private ControlState controls;
        private FacilityUI ui;

        private Transform weaponRoot;
        private WeaponPresentation presentation;
        private OperatorVisual operatorVisual;
        public Transform HeldModel=>weaponRoot;
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
        private float nextMelee, guardUntil;
        private bool interactiveCombat;

        public bool HasWeapon => hasWeapon;
        public ActiveWeaponType ActiveWeapon => activeWeapon;
        public bool HasPulseSidearm => hasPulseSidearm;
        public bool HasTitanbreaker => hasTitanbreaker;
        public int CurrentAmmo => currentAmmo;
        public int ReserveAmmo => reserveAmmo;
        public int EmpGrenades => empGrenadeCount;
        public bool IsReloading => isReloading;
        public bool IsAmmoFull => reserveAmmo >= maxReserveAmmo && currentAmmo >= magazineCapacity;

        public bool TryCounter(Vector3 attacker)
        {
            var toAttacker = (attacker - motor.Eye).normalized;
            if (Time.time >= guardUntil || Vector3.Dot(motor.ViewRotation * Vector3.forward, toAttacker) < .45f) return false;
            guardUntil = 0;
            ui?.ShowActionNotice("Захват отражён");
            return true;
        }

        public bool TryMelee()
        {
            if (!interactiveCombat || !controls.CanExplore || Time.time < nextMelee) return false;
            nextMelee = Time.time + .85f;
            guardUntil = Time.time + .38f;
            var direction = motor.ViewRotation * Vector3.forward;
            operatorVisual?.ReachTo(motor.Eye - Vector3.up * .18f + direction, .26f);
            if (RobotShot.Fire(new Ray(motor.Eye, direction), 1.7f, 17, out var impact, RobotImpact.Melee))
                SpawnImpactFX(impact.point, impact.normal);
            return true;
        }

        public void UnlockPulseSidearm()=>PickupWeapon(ActiveWeaponType.CirrusPulseSidearm,15,45);
        public void UnlockTitanbreaker()=>PickupWeapon(ActiveWeaponType.TitanbreakerHeavyInductor,3,9);
        public void SwitchWeapon(ActiveWeaponType kind)
        {
            // One carried weapon. Old numeric-slot calls cannot summon a discarded weapon.
            if(kind==activeWeapon&&hasWeapon)return;
        }
        public bool PickupWeapon(ActiveWeaponType kind,int ammo,int reserve,string authoredId=null,float residualHeat=0,bool residualOverheat=false,float residualCooldown=0)
        {
            if(worldWeapons==null)return false;
            if(authoredId!=null&&!worldWeapons.CollectAuthored(authoredId))return false;
            bool replacing=hasWeapon;if(hasWeapon)DropCurrent();
            activeWeapon=kind;hasWeapon=true;hasPulseSidearm=kind==ActiveWeaponType.CirrusPulseSidearm;hasTitanbreaker=kind==ActiveWeaponType.TitanbreakerHeavyInductor;
            magazineCapacity=kind==ActiveWeaponType.Arc12Shotgun?6:kind==ActiveWeaponType.CirrusPulseSidearm?15:3;
            maxReserveAmmo=kind==ActiveWeaponType.Arc12Shotgun?72:kind==ActiveWeaponType.CirrusPulseSidearm?120:24;
            currentAmmo=Mathf.Clamp(ammo,0,magazineCapacity);reserveAmmo=Mathf.Clamp(reserve,0,maxReserveAmmo);heat=Mathf.Clamp01(residualHeat);overheated=residualOverheat||heat>=1;
            charging=false;charge=0;isReloading=false;cooldownTimer=Mathf.Max(.3f,residualCooldown);equipAge=0;motion?.PlayAction("PickUp_Table",.4f);RebuildModel();
            ui?.ShowActionNotice(DisplayName(kind)+(replacing?" · прежнее оружие оставлено рядом":""));return true;
        }
        public bool AuthoredWasCollected(string id)=>worldWeapons!=null&&worldWeapons.WasCollected(id);
        public bool TakeDropped(string id)
        {
            if(!worldWeapons.Take(id,out var data))return false;
            return PickupWeapon((ActiveWeaponType)data.kind,data.ammo,data.reserve,null,data.heat,data.overheated,data.cooldown);
        }
        private void DropCurrent()
        {
            Vector3 direction=motor.ViewRotation*Vector3.forward;
            var start=motor.transform.position+Vector3.up*.9f;
            Vector3 offset=motor.transform.right*.38f+direction*.4f;
            if(Physics.SphereCast(start,.18f,offset.normalized,out var hit,offset.magnitude,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore))offset=offset.normalized*Mathf.Max(0,hit.distance-.2f);
            var position=start+offset;
            var state=worldWeapons.Add(new WeaponItemData{kind=(int)activeWeapon,ammo=currentAmmo,reserve=reserveAmmo,heat=heat,overheated=overheated,cooldown=cooldownTimer,x=position.x,y=position.y,z=position.z,yaw=motor.transform.eulerAngles.y,roll=90,vx=direction.x*1.1f,vy=1.15f,vz=direction.z*1.1f,ax=1.5f,ay=2,az=1});
            SpawnDrop(state);
        }
        private void SpawnDrop(WeaponItemData data)
        {
            var model=Resources.Load<GameObject>("Combat/"+(data.kind==0?"ARC12":data.kind==1?"CIRRUS":"TITAN"));
            var go=Instantiate(model,new Vector3(data.x,data.y,data.z),Quaternion.Euler(data.pitch,data.yaw,data.roll));go.name="Dropped "+DisplayName((ActiveWeaponType)data.kind);
            foreach(var t in go.GetComponentsInChildren<Transform>())t.gameObject.layer=CollisionLayers.WeaponItem;
            Bounds b=default;bool first=true;
            foreach(var f in go.GetComponentsInChildren<MeshFilter>())
            {var local=f.sharedMesh.bounds;var matrix=go.transform.worldToLocalMatrix*f.transform.localToWorldMatrix;
             for(int i=0;i<8;i++){var v=matrix.MultiplyPoint3x4(local.center+Vector3.Scale(local.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));if(first){b=new Bounds(v,Vector3.zero);first=false;}else b.Encapsulate(v);}}
            var shape=go.AddComponent<BoxCollider>();shape.center=b.center;shape.size=b.size;
            var physics=go.AddComponent<Rigidbody>();physics.mass=data.kind==0?3.5f:data.kind==1?1.1f:7.5f;physics.interpolation=RigidbodyInterpolation.Interpolate;physics.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            var drop=go.AddComponent<DroppedWeapon>();drop.Configure(this,data,controls,physics);drops.Add(drop);ui?.AddWorldWeapon(drop);
        }
        public void SyncWorldDrops(){foreach(var drop in drops)if(drop!=null&&drop.gameObject.activeInHierarchy)drop.Sync();}
        public void RestoreWorldDrops(){foreach(var data in worldWeapons.Drops)SpawnDrop(data);}

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
            aimCamera=Camera.main;cameraRig=FindAnyObjectByType<Hostfall.CameraSystem.PlayerCameraRig>();
            motion=GetComponent<OperatorMotion>()??gameObject.AddComponent<OperatorMotion>();
            var simulation = FindAnyObjectByType<FacilityRoot>().Simulation;
            worldWeapons = simulation.Weapons;
            interactiveCombat = simulation.InteractiveComputers;
            hearingTargets=FindObjectsByType<WatcherController>(FindObjectsSortMode.None);
            for(int i=0;i<impacts.Length;i++){var spark=new GameObject("Pooled impact "+i);spark.transform.SetParent(transform,false);impacts[i]=spark.AddComponent<Light>();impacts[i].color=new Color(.35f,.85f,1);impacts[i].range=1.3f;impacts[i].intensity=2;impacts[i].enabled=false;}
            Physics.IgnoreLayerCollision(CollisionLayers.Player,CollisionLayers.WeaponItem,true);
            input = facilityInput;
            controls = controlState;
            ui = facilityUI;

            EnsureAudio();
            operatorVisual=motor.GetComponentInChildren<OperatorVisual>(true);
            var selected=activeWeapon;for(int i=0;i<3;i++){activeWeapon=(ActiveWeaponType)i;BuildWeaponModel();weaponRoot.gameObject.SetActive(false);}activeWeapon=selected;BuildWeaponModel();
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

            // Enter Play Mode can preserve managed statics while native clips have been
            // unloaded. Rebuild the cache from live objects, not only a stale flag.
            if (!audioGenerated||clipFire==null||clipPulseFire==null||clipTitanFire==null||clipDryFire==null||clipReload==null||clipThrow==null)
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
            if(cachedModels[(int)activeWeapon]!=null){weaponRoot=cachedModels[(int)activeWeapon];presentation=weaponRoot.GetComponent<WeaponPresentation>();muzzleLight=weaponRoot.GetComponentInChildren<Light>(true);muzzleLight.enabled=false;presentation.Ready();weaponRoot.gameObject.SetActive(hasWeapon);return;}
            var asset=Resources.Load<GameObject>("Combat/"+(activeWeapon==ActiveWeaponType.Arc12Shotgun?"ARC12":activeWeapon==ActiveWeaponType.CirrusPulseSidearm?"CIRRUS":"TITAN"));
            var go=asset!=null?Instantiate(asset):new GameObject("Weapon awaiting authored asset");
            weaponRoot=go.transform;weaponRoot.SetParent(transform,false);cachedModels[(int)activeWeapon]=weaponRoot;go.name="Held "+activeWeapon;go.SetActive(hasWeapon);
            foreach(var t in go.GetComponentsInChildren<Transform>())t.gameObject.layer=2;
            presentation=go.AddComponent<WeaponPresentation>();presentation.Configure(activeWeapon);
            var flash=new GameObject("Muzzle flash");flash.transform.SetParent(presentation.Muzzle!=null?presentation.Muzzle:weaponRoot,false);flash.transform.localPosition=Vector3.zero;
            muzzleLight=flash.AddComponent<Light>();muzzleLight.range=3;muzzleLight.intensity=4;muzzleLight.enabled=false;
        }
        private void RebuildModel(){if(weaponRoot!=null)weaponRoot.gameObject.SetActive(false);BuildWeaponModel();}
        public WeaponLoadoutData Capture()
        {
            var d=new WeaponLoadoutData{active=(int)activeWeapon,equipped=hasWeapon,pulse=hasPulseSidearm,heavy=hasTitanbreaker,arc=arc12Current,arcReserve=arc12Reserve,cirrus=cirrusCurrent,cirrusReserve=cirrusReserve,titan=titanCurrent,titanReserve=titanReserve,grenades=empGrenadeCount,carryVersion=1,heat=heat,cooldown=cooldownTimer,reloadRemaining=reloadTimer,reloading=isReloading,charge=charge,charging=charging,overheated=overheated};
            if(d.active==0){d.arc=currentAmmo;d.arcReserve=reserveAmmo;}else if(d.active==1){d.cirrus=currentAmmo;d.cirrusReserve=reserveAmmo;}else {d.titan=currentAmmo;d.titanReserve=reserveAmmo;}return d;
        }
        public void Restore(WeaponLoadoutData d)
        {
            WeaponLoadoutData.Validate(d);
            if(d.carryVersion==0&&d.equipped)
            {
                if(d.active!=0)SpawnLegacy(0,d.arc,d.arcReserve);
                if(d.pulse&&d.active!=1)SpawnLegacy(1,d.cirrus,d.cirrusReserve);
                if(d.heavy&&d.active!=2)SpawnLegacy(2,d.titan,d.titanReserve);
            }
            hasWeapon=d.equipped;hasPulseSidearm=d.equipped&&d.active==1;hasTitanbreaker=d.equipped&&d.active==2;activeWeapon=(ActiveWeaponType)d.active;
            arc12Current=d.arc;arc12Reserve=d.arcReserve;cirrusCurrent=d.cirrus;cirrusReserve=d.cirrusReserve;titanCurrent=d.titan;titanReserve=d.titanReserve;empGrenadeCount=d.grenades;
            currentAmmo=d.active==0?d.arc:d.active==1?d.cirrus:d.titan;reserveAmmo=d.active==0?d.arcReserve:d.active==1?d.cirrusReserve:d.titanReserve;
            magazineCapacity=d.active==0?6:d.active==1?15:3;maxReserveAmmo=d.active==0?72:d.active==1?120:24;isReloading=d.reloading;reloadTimer=d.reloadRemaining;heat=d.heat;overheated=d.overheated;cooldownTimer=d.cooldown;charge=d.charge;charging=d.charging;RebuildModel();
        }
        private void SpawnLegacy(int kind,int ammo,int reserve)
        {var p=motor.transform.position+Vector3.up*.8f+motor.transform.right*(kind-1)*.3f;worldWeapons.Add(new WeaponItemData{kind=kind,ammo=ammo,reserve=reserve,x=p.x,y=p.y,z=p.z,roll=90});}

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
            if(weaponRoot!=null)weaponRoot.gameObject.SetActive(hasWeapon&&controls!=null&&controls.CanExplore);
            if(operatorVisual!=null)operatorVisual.SetWeaponGrips(hasWeapon?presentation?.RightGrip:null,hasWeapon?presentation?.SupportGrip:null);
            if(controls==null||!controls.CanExplore)return;
            if (motor == null || controls == null) return;

            for(int i=0;i<impacts.Length;i++)if(impacts[i]!=null&&impacts[i].enabled&&Time.time>=impactUntil[i])impacts[i].enabled=false;
            equipAge+=Time.deltaTime;
            heat=Mathf.MoveTowards(heat,0,Time.deltaTime*.34f);if(overheated&&heat<.3f)overheated=false;
            if(charging)charge=Mathf.MoveTowards(charge,1,Time.deltaTime/0.8f);
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
                cooldownTimer=Mathf.Max(0,cooldownTimer-Time.deltaTime);
            }

            if (weaponRoot != null && hasWeapon)
            {
                // Align weapon rigidly with player's first-person view rotation and eye
                Vector3 viewOffset = activeWeapon==ActiveWeaponType.CirrusPulseSidearm?new Vector3(.14f,-.28f,.35f):activeWeapon==ActiveWeaponType.TitanbreakerHeavyInductor?new Vector3(.15f,-.36f,.30f):new Vector3(.13f,-.34f,.25f);
                viewOffset.z-=recoilAmount;
                // Small breathing motion; never a second disconnected arm model.
                float sway=motor.IsSprinting?.009f:.003f;
                viewOffset+=new Vector3(Mathf.Sin(Time.time*2.1f)*sway,Mathf.Sin(Time.time*2.8f)*sway*.5f,0);
                                viewOffset.y-=Mathf.Clamp01(1-equipAge/.3f)*.20f;
                var view=motor.ViewRotation*(motion!=null?motion.CameraSway:Quaternion.identity);
                weaponRoot.position = motor.Eye+(motion!=null?motion.CameraOffset:Vector3.zero) + view * viewOffset;
                float reloadCurve=isReloading?Mathf.Sin(Mathf.PI*Mathf.Clamp01(reloadTimer/ReloadDuration)):0;
                weaponRoot.rotation = view * (motion!=null?motion.ItemInertia:Quaternion.identity)*Quaternion.Euler(reloadCurve*12,0,reloadCurve*-9);
                presentation?.Tick(Time.deltaTime,isReloading,1-reloadTimer/ReloadDuration);
            }

            if (isReloading)
            {
                reloadTimer -= Time.deltaTime;
                if (reloadTimer <= 0f)
                {
                    int needed=activeWeapon==ActiveWeaponType.Arc12Shotgun?1:magazineCapacity-currentAmmo;
                    int transfer=Mathf.Min(needed,reserveAmmo);currentAmmo+=transfer;reserveAmmo-=transfer;
                    isReloading=activeWeapon==ActiveWeaponType.Arc12Shotgun&&currentAmmo<magazineCapacity&&reserveAmmo>0;
                    reloadTimer=isReloading?ReloadDuration:0;
                }
            }

            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && hasWeapon && controls.CanExplore)
            {
                if (kb.rKey.wasPressedThisFrame) StartReload();
                if (kb.gKey.wasPressedThisFrame) ThrowEmpGrenade();
            }

            if (input.Melee.WasPressedThisFrame()) TryMelee();
            if(hasWeapon&&controls.CanExplore&&input?.Fire!=null)
            {
                if(activeWeapon==ActiveWeaponType.TitanbreakerHeavyInductor)
                {if(input.Fire.WasPressedThisFrame())BeginCharge();if(input.Fire.WasReleasedThisFrame())ReleaseCharge();}
                else if (interactiveCombat && activeWeapon == ActiveWeaponType.CirrusPulseSidearm)
                {
                    if (input.Fire.IsPressed() && !overheated && cooldownTimer <= 0) TryFire();
                }
                else if(input.Fire.WasPressedThisFrame())TryFire();
            }
        }
        public bool BeginCharge(){if(!hasWeapon||activeWeapon!=ActiveWeaponType.TitanbreakerHeavyInductor||cooldownTimer>0||isReloading||currentAmmo<=0||!controls.CanExplore)return false;charging=true;charge=0;return true;}
        public bool ReleaseCharge(){if(!charging)return false;if(!controls.CanExplore)return false;charging=false;return TryFire();}
        public bool TryFire()
        {
            if(!hasWeapon||!controls.CanExplore||cooldownTimer>0)return false;
            if(isReloading){if(activeWeapon!=ActiveWeaponType.Arc12Shotgun||currentAmmo<=0)return false;isReloading=false;reloadTimer=0;}
            if(activeWeapon==ActiveWeaponType.CirrusPulseSidearm&&overheated){ui?.ShowActionNotice("CIRRUS · охлаждение");return false;}
            if(currentAmmo<=0){cooldownTimer=.25f;if(reserveAmmo>0)StartReload();else audioSource?.PlayOneShot(clipDryFire,.6f);return false;}
            currentAmmo--;presentation?.Fired();motion?.PlayAction("Pistol_Shoot",.18f);
            // Cinemachine renders later in LateUpdate. FPS shots use this frame's view,
            // avoiding a frame of aiming latency when the mouse moves while firing.
            var view=motor.ViewRotation*(motion!=null?motion.CameraSway:Quaternion.identity);
            Ray aim=cameraRig!=null&&cameraRig.ThirdPerson&&aimCamera!=null
                ?aimCamera.ViewportPointToRay(new Vector3(.5f,.5f,0))
                :new Ray(motor.Eye+(motion!=null?motion.CameraOffset:Vector3.zero),view*Vector3.forward);
            foreach(var watcher in hearingTargets)if(watcher!=null&&watcher.isActiveAndEnabled&&!watcher.IsDestroyed&&Vector3.Distance(watcher.transform.position,aim.origin)<9&&Mathf.Abs(watcher.transform.position.y-aim.origin.y)<3)watcher.HearShot(aim.origin);
            bool arc=activeWeapon==ActiveWeaponType.Arc12Shotgun,heavy=activeWeapon==ActiveWeaponType.TitanbreakerHeavyInductor;
            int pellets=arc?8:1;float damage=arc?10:heavy?Mathf.Lerp(55,140,charge):24;float range=arc?12:heavy?35:24;
            var rotation=Quaternion.LookRotation(aim.direction);
            for(int i=0;i<pellets;i++)
            {
                float angle=i*2.399963f,radius=i==0?0:interactiveCombat?.049f:.026f;var direction=rotation*new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,1).normalized;
                var profile = !interactiveCombat ? RobotImpact.Standard : heavy ? RobotImpact.Coil :
                    !arc ? RobotImpact.Pulse : i == 0 ? RobotImpact.Scatter : RobotImpact.Standard;
                if(RobotShot.Fire(new Ray(aim.origin,direction),range,damage,out var hit,profile,charge)&&i<3)SpawnImpactFX(hit.point,hit.normal);
            }
            if(!arc&&!heavy){heat=Mathf.Clamp01(heat+.23f);if(heat>=1)overheated=true;}
            cooldownTimer=heavy?1.1f:arc?.65f:.18f;recoilAmount=heavy?.14f:arc?.08f:.025f;charge=0;charging=false;
            motor.AddPitchKick(heavy?-2.2f:arc?-1.3f:-.35f);
            if(muzzleLight!=null){muzzleLight.color=heavy?new Color(.65f,.25f,1):new Color(.3f,.85f,1);muzzleLight.enabled=true;flashTimer=.07f;}
            audioSource?.PlayOneShot(heavy?clipTitanFire:arc?clipFire:clipPulseFire,.8f);return true;
        }

        public bool StartReload()
        {
            if (!hasWeapon || isReloading || currentAmmo >= magazineCapacity || reserveAmmo <= 0) return false;

            isReloading = true;
            reloadTimer = ReloadDuration;charge=0;charging=false;motion?.PlayAction("Pistol_Reload",ReloadDuration);
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

        private void SpawnImpactFX(Vector3 point,Vector3 normal)
        {
            int i=impactIndex++%impacts.Length;var light=impacts[i];light.transform.position=point+normal*.02f;light.enabled=true;impactUntil[i]=Time.time+.06f;
        }
    }
}
