using System;
using System.Linq;
using Hostfall.Core;
using Hostfall.Player;
using Hostfall.Simulation;
using UnityEngine;

namespace Hostfall.World
{
    // Simulation owns damage; this adapter owns hit geometry and bounded cosmetic debris.
    public sealed class RobotCombatController : MonoBehaviour
    {
        public RobotCombatState Model { get; private set; }
        public bool CompletingDeath { get; private set; }
        public float Mobility => Model?.Mobility ?? 1;
        private IDamageable backend;
        private ControlState controls;

        private Transform[] plates=Array.Empty<Transform>();
        private RobotHitbox[] hitboxes;
        private Renderer[] renderers;
        private Collider[] colliders;
        private Light[] lights;
        private AudioSource[] audioSources;
        private Transform[] hatches;
        private MeshRenderer[] glass;
        private int[] glassMasks;
        private MaterialPropertyBlock properties;
        private int appearanceMask=-1;
        private bool appearanceDead, appearanceExposed;

        public void Initialize(FacilitySimulation world, ControlState state)
        {
            controls=state;properties=new MaterialPropertyBlock();
            backend=GetComponents<MonoBehaviour>().OfType<IDamageable>().FirstOrDefault();
            if(backend==null)return;
            bool wasDead=backend.IsDestroyed;
            bool boss=backend is TrackedSentryController;
            Model=world.Combat.Get(backend.DamageableId,boss,boss?780:260);
            if (backend is WatcherController watcher) watcher.BindCombat(this);
            if (boss && world.InteractiveComputers)
                Model.Data.crawlerAttack ??= new CrawlerAttackData { version = CrawlerAttack.SaveVersion };
            if(wasDead)Model.Defeat();
            if(Model.Data.hasPose)transform.SetPositionAndRotation(new Vector3(Model.Data.x,Model.Data.y,Model.Data.z),Quaternion.Euler(0,Model.Data.yaw,0));
            plates=GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("ArmorL")||t.name.StartsWith("ArmorR")).ToArray();
            if(Model.Data.hasPose&&backend is WatcherController restored)restored.RestoreCombatTimers(Model.Data);
            BuildHitboxes(boss);
            hitboxes=GetComponentsInChildren<RobotHitbox>(true);
            renderers=GetComponentsInChildren<Renderer>(true);colliders=GetComponentsInChildren<Collider>(true);
            lights=GetComponentsInChildren<Light>(true);audioSources=GetComponentsInChildren<AudioSource>(true);
            hatches=GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("CoreHatch")).ToArray();
            glass=GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.StartsWith("Coolant glass")).ToArray();glassMasks=new int[glass.Length];
            for(int i=0;i<glass.Length;i++)
            {
                float nearest=float.PositiveInfinity;
                foreach(var box in hitboxes)
                {
                    if(box.Region!=RobotHitRegion.LeftCoolant&&box.Region!=RobotHitRegion.RightCoolant)continue;
                    float distance=(box.transform.position-glass[i].transform.position).sqrMagnitude;
                    if(distance<nearest){nearest=distance;glassMasks[i]=box.Region==RobotHitRegion.LeftCoolant?8:16;}
                }
            }
            Model.Changed+=Changed;

            ApplyAppearance(false);
            if(Model.IsDead)CompleteDeath();
        }
        public void RegisterLight(Light light)
        {
            if(light==null)return;
            if(CompletingDeath){light.enabled=false;return;}
            foreach(var known in lights)if(known==light)return;
            int count=lights.Length;Array.Resize(ref lights,count+1);lights[count]=light;
        }
        public void Hit(float damage,RobotHitRegion region,Vector3 point,RobotImpact impact=RobotImpact.Standard,
            float charge=0,Vector3 direction=default)
        {
            if(Model==null||Model.IsDead)return;
            Model.ApplyHit(damage,region,impact,charge);
            if(backend is WatcherController watcher && !Model.IsDead)
            {
                watcher.ReactToHit(point);
                if (impact is RobotImpact.Scatter or RobotImpact.Melee)
                    watcher.ReceiveKnockback(direction, impact == RobotImpact.Melee ? .48f : .25f);
            }
        }
        public void SyncState()
        {
            if(Model==null)return;
            var d=Model.Data;var p=transform.position;
            d.hasPose=true;d.x=p.x;d.y=p.y;d.z=p.z;d.yaw=transform.eulerAngles.y;
            if(backend is WatcherController watcher)watcher.CaptureCombatTimers(d);
        }
        private void Changed(){ApplyAppearance(true);if(Model.IsDead)CompleteDeath();}
        private void ApplyAppearance(bool debris)
        {
            int mask=Model.BrokenMask;
            bool exposed = Model.CoreExposed;
            if(appearanceMask==mask&&appearanceDead==Model.IsDead&&appearanceExposed==exposed)return;
            appearanceMask=mask;appearanceDead=Model.IsDead;appearanceExposed=exposed;
            foreach(var box in hitboxes)
                if(box.Region==RobotHitRegion.LeftCoolant||box.Region==RobotHitRegion.RightCoolant)
                {
                    bool broken=(mask&(box.Region==RobotHitRegion.LeftCoolant?8:16))!=0;
                    var renderer=box.GetComponentInParent<MeshRenderer>();
                    if(renderer!=null)renderer.enabled=!broken;
                    box.GetComponent<Collider>().enabled=!broken&&!Model.IsDead;
                }
            for(int i=0;i<plates.Length;i++)
            {
                bool removed=(mask&32)!=0&&i%3==0||(mask&64)!=0&&i%3==1||Model.IsDead;
                if(removed&&plates[i].gameObject.activeSelf){if(debris)Shed(plates[i]);plates[i].gameObject.SetActive(false);}
            }
            for(int i=0;i<glass.Length;i++)glass[i].enabled=(mask&glassMasks[i])==0&&!Model.IsDead;
            if(Model.IsDead)
                foreach(var renderer in renderers)
                {
                    renderer.GetPropertyBlock(properties);properties.SetColor("_EmissionColor",Color.black);renderer.SetPropertyBlock(properties);
                }
            foreach(var hatch in hatches)hatch.gameObject.SetActive(!exposed&&!Model.IsDead);

        }
        private void Shed(Transform part)
        {
            var mesh=part.GetComponent<MeshFilter>();var renderer=part.GetComponent<MeshRenderer>();
            if(mesh==null||renderer==null)return;
            var debris=new GameObject("Detached armor");debris.layer=2;
            debris.transform.SetPositionAndRotation(part.position,part.rotation);debris.transform.localScale=part.lossyScale;
            debris.AddComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;
            debris.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
            debris.AddComponent<RobotDebris>().Initialize(controls,(part.position-transform.position).normalized*1.2f+Vector3.up*1.2f);
        }
        private void CompleteDeath()
        {
            if(CompletingDeath)return;
            CompletingDeath=true;
            backend.TakeDamage(100000);
            foreach(var c in colliders)c.enabled=false;
            foreach(var light in lights)light.enabled=false;
            foreach(var audio in audioSources)audio.Stop();
        }
        private void BuildHitboxes(bool boss)
        {
            if(GetComponentsInChildren<RobotHitbox>().Length>0)return;
            if(boss)
            {
                foreach(var mf in GetComponentsInChildren<MeshFilter>())
                {
                    RobotHitRegion? region=mf.name.StartsWith("CoolingLeft")?RobotHitRegion.LeftCoolant:mf.name.StartsWith("CoolingRight")?RobotHitRegion.RightCoolant:mf.name=="Sensor"?RobotHitRegion.Sensor:mf.name=="Core"?RobotHitRegion.Core:null;
                    if(region.HasValue)AddBox(mf.transform,region.Value,mf.sharedMesh.bounds.center,mf.sharedMesh.bounds.size*1.1f);
                }
                AddBox(transform,RobotHitRegion.LeftDrive,new Vector3(-1.2f,.5f,0),new Vector3(.7f,.85f,3.1f));
                AddBox(transform,RobotHitRegion.RightDrive,new Vector3(1.2f,.5f,0),new Vector3(.7f,.85f,3.1f));
                AddBox(transform,RobotHitRegion.Hull,new Vector3(0,.6f,0),new Vector3(2.1f,1f,2.45f));
            }
            else
            {
                var sensor=GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name.Contains("Sensor")||t.name.Contains("Optic"));
                if(sensor!=null)AddBox(sensor,RobotHitRegion.Sensor,Vector3.zero,new Vector3(.3f,.28f,.32f)/Mathf.Max(.001f,sensor.lossyScale.x));
                else AddBox(transform,RobotHitRegion.Sensor,new Vector3(0,1.05f,.1f),new Vector3(.36f,.3f,.35f));
                AddBox(transform,RobotHitRegion.Hull,new Vector3(0,.6f,0),new Vector3(.65f,.8f,.65f));
                AddBox(transform,RobotHitRegion.LeftDrive,new Vector3(-.38f,.3f,0),new Vector3(.24f,.5f,.55f));
                AddBox(transform,RobotHitRegion.RightDrive,new Vector3(.38f,.3f,0),new Vector3(.24f,.5f,.55f));
            }
        }
        private void AddBox(Transform parent,RobotHitRegion region,Vector3 center,Vector3 size)
        {
            var go=new GameObject("Hit "+region);go.layer=CollisionLayers.RobotHitbox;go.transform.SetParent(parent,false);
            var box=go.AddComponent<BoxCollider>();box.center=center;box.size=size;
            go.AddComponent<RobotHitbox>().Region=region;
        }
        private void OnDestroy(){if(Model!=null)Model.Changed-=Changed;}
    }
    public sealed class RobotHitbox : MonoBehaviour { public RobotHitRegion Region; }
    public sealed class RobotDebris : MonoBehaviour
    {
        private ControlState controls;private Vector3 velocity;private float time;
        public void Initialize(ControlState state,Vector3 v){controls=state;velocity=v;}
        private void Update()
        {
            if(controls!=null&&!controls.WorldRuns)return;
            float dt=Time.deltaTime;time+=dt;
            if(time>=1.8f){Destroy(gameObject);return;}
            velocity+=Vector3.down*7*dt;transform.position+=velocity*dt;
            transform.Rotate(90*dt,130*dt,20*dt);if(time>1)transform.localScale*=Mathf.Max(0,1-dt*5);
        }
    }
    public static class RobotShot
    {
        private static RaycastHit[] hitBuffer=new RaycastHit[64];
        private static readonly System.Collections.Generic.IComparer<RaycastHit> nearFirst=System.Collections.Generic.Comparer<RaycastHit>.Create((a,b)=>a.distance.CompareTo(b.distance));
        // Reuse the query buffer; keep nearest-wall ordering and never accept a truncated query.
        public static bool Fire(Ray ray,float range,float damage,out RaycastHit impact,
            RobotImpact profile=RobotImpact.Standard,float charge=0)
        {
            int count=Physics.RaycastNonAlloc(ray,hitBuffer,range,CollisionLayers.ShotMask,QueryTriggerInteraction.Ignore);
            while(count==hitBuffer.Length&&hitBuffer.Length<1024){hitBuffer=new RaycastHit[hitBuffer.Length*2];count=Physics.RaycastNonAlloc(ray,hitBuffer,range,CollisionLayers.ShotMask,QueryTriggerInteraction.Ignore);}
            if(count==hitBuffer.Length){hitBuffer=Physics.RaycastAll(ray,range,CollisionLayers.ShotMask,QueryTriggerInteraction.Ignore);count=hitBuffer.Length;}
            System.Array.Sort(hitBuffer,0,count,nearFirst);
            for(int i=0;i<count;i++)
            {
                var hit=hitBuffer[i];
                var combat=hit.collider.GetComponentInParent<RobotCombatController>();
                var zone=hit.collider.GetComponent<RobotHitbox>();
                if(combat!=null&&combat.Model!=null)
                {
                    if(combat.Model.IsDead)continue;
                    if(zone==null)continue; // movement capsules are not damage volumes
                    var response = profile == RobotImpact.Scatter && hit.distance > 4.5f ? RobotImpact.Standard : profile;
                    combat.Hit(damage,zone.Region,hit.point,response,charge,ray.direction);impact=hit;return true;
                }
                if (hit.collider.TryGetComponent<CoreAvatarHitbox>(out var avatarPart)) avatarPart.Hit(damage);
                else if (hit.collider.TryGetComponent<MinibossComponent>(out var minibossPart)) minibossPart.Hit(damage);
                else hit.collider.GetComponentInParent<IDamageable>()?.TakeDamage(damage);
                impact=hit;return true;
            }
            impact=default;return false;
        }
    }
}
