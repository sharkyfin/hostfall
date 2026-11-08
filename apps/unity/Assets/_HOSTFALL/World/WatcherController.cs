using Hostfall.Core;
using Hostfall.Persistence;
using Hostfall.Player;
using Hostfall.Simulation;
using UnityEngine;

namespace Hostfall.World
{
    public sealed class WatcherController : MonoBehaviour
    {
        public const string CoreProcess="super-node --leader --replicate";
        [SerializeField] private string enemyId="watcher-r17";
        [SerializeField] private Light indicator;
        [SerializeField] private Transform sensor;
        [SerializeField] private Animator animator;
        [SerializeField] private Vector3 patrolA=new(-.4f,0,2.2f),patrolB=new(-.4f,0,5.6f);
        [SerializeField] private Vector2 xBounds=new(-.58f,.58f),zBounds=new(1.65f,5.8f);
        private FacilitySimulation simulation;
        private PlayerMotor player;
        private ControlState controls;
        private float scan,contactCooldown;
        private int direction=1;
        private string animationState="";
        public string Id=>enemyId;
        public string AnimationState=>animationState;
        public WatcherState State { get; }=new();
        public void Configure(Transform eye,Light lamp,Animator model=null,string id="watcher-r17",Vector3? first=null,Vector3? last=null)
        {
            sensor=eye; indicator=lamp; animator=model; enemyId=id;
            if(first.HasValue) patrolA=first.Value; if(last.HasValue) patrolB=last.Value;
            if(id=="sentry-r18") { xBounds=new Vector2(-1.5f,1.5f); zBounds=new Vector2(8.2f,14.4f); }
        }
        public void Initialize(FacilitySimulation model,PlayerMotor target,ControlState state)
        { simulation=model; player=target; controls=state; }
        public RobotStateData Capture()=>new()
        { id=Id,position=transform.position,sensorEuler=(Quaternion.Inverse(transform.rotation)*sensor.rotation).eulerAngles,bodyYaw=transform.eulerAngles.y,scan=scan,direction=direction,contactCooldown=Mathf.Clamp(contactCooldown,0,3) };
        public void Restore(RobotStateData state)
        {
            transform.SetPositionAndRotation(state.position,Quaternion.Euler(0,state.bodyYaw,0)); sensor.rotation=transform.rotation*Quaternion.Euler(state.sensorEuler);
            scan=state.scan; direction=state.direction; contactCooldown=state.contactCooldown;
            ResolveSavedOverlap();
        }
        private void ResolveSavedOverlap()
        {
            // Older scenes allowed the robot to enter furniture. Recover only actual overlap,
            // rather than resetting a valid saved patrol position to an authored spawn point.
            var capsule=GetComponent<CapsuleCollider>();
            for(int pass=0;pass<4;pass++)
            {
                var center=transform.TransformPoint(capsule.center);
                float half=Mathf.Max(0,capsule.height/2-capsule.radius); bool moved=false;
                foreach(var obstacle in Physics.OverlapCapsule(center+Vector3.up*half,center-Vector3.up*half,capsule.radius,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore))
                {
                    if(obstacle.transform.IsChildOf(transform)) continue;
                    if(Physics.ComputePenetration(capsule,transform.position,transform.rotation,obstacle,obstacle.transform.position,obstacle.transform.rotation,out var normal,out var depth) && depth>.0001f)
                    { transform.position+=normal*(depth+.005f); moved=true; }
                }
                if(!moved) break;
            }
        }
        public bool Sees(Vector3 target)
        {
            if(simulation!=null&&simulation.Actions.Stun(Id)>0) return false;
            Vector3 delta=target-sensor.position;
            return delta.magnitude<5 && Vector3.Angle(Vector3.ProjectOnPlane(sensor.forward,Vector3.up),Vector3.ProjectOnPlane(delta,Vector3.up))<50 &&
                !Physics.Linecast(sensor.position,target,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore);
        }
        private void Update()=>Tick(Time.deltaTime);
        public void Tick(float dt)
        {
            if(simulation==null) return;
            bool relay=Id=="watcher-r17";
            State.Observe(relay?(!simulation.AutomatedRoute&&simulation.SourceIsolated):!simulation[DeviceId.Core01].HasProcess(CoreProcess),
                (relay&&simulation.AutomatedRoute?true:simulation[relay?DeviceId.Edge01:DeviceId.Core01].Powered),Sees(player.Eye),simulation.Actions.Stun(Id)>0);
            indicator.color=State.Mode==WatcherMode.Maintenance?new Color(.35f,1,.7f):State.Mode==WatcherMode.Offline||State.Mode==WatcherMode.Jammed?new Color(.35f,.55f,1):State.Mode==WatcherMode.Alert?new Color(1,.15f,.08f):new Color(1,.6f,.2f);
            bool moving=false,contact=false;
            if(controls.CanExplore && dt>0)
            {
                contactCooldown=Mathf.Max(0,contactCooldown-dt);
                if((State.Mode==WatcherMode.Patrol||State.Mode==WatcherMode.Alert)&&contactCooldown<=2.4f)
                {
                    scan+=dt; Vector3 position=transform.position; Vector3 destination;
                    if(State.Mode==WatcherMode.Alert)
                        destination=new Vector3(Mathf.Clamp(player.transform.position.x,xBounds.x,xBounds.y),position.y,Mathf.Clamp(player.transform.position.z,zBounds.x,zBounds.y));
                    else
                    {
                        var end=direction>0?patrolB:patrolA;
                        if(Vector3.Distance(position,end)<.1f) direction=-direction;
                        destination=direction>0?patrolB:patrolA;
                        // Rejoin the clear lane sideways before walking past service furniture.
                        // A legacy saved pose can be collision-free yet sit at a blocked corner.
                        if(Mathf.Abs(position.x-destination.x)>.02f)
                            destination=new Vector3(destination.x,position.y,position.z);
                    }
                    float multiplier=simulation.Actions.Slow(Id)>0?.4f:1;
                    var desired=Vector3.MoveTowards(position,destination,(State.Mode==WatcherMode.Alert?1.6f:.65f)*multiplier*dt);
                    Vector3 delta=desired-position; float distance=delta.magnitude;
                    if(distance>.0001f)
                    {
                        var capsule=GetComponent<CapsuleCollider>(); Vector3 center=position+capsule.center;
                        float half=Mathf.Max(0,capsule.height/2-capsule.radius);
                        foreach(var hit in Physics.CapsuleCastAll(center+Vector3.up*half,center-Vector3.up*half,capsule.radius,delta.normalized,distance+.025f,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore))
                            if(!hit.collider.transform.IsChildOf(transform)) distance=Mathf.Min(distance,Mathf.Max(0,hit.distance-.025f));
                        transform.position=position+delta.normalized*distance; moving=distance>.001f;
                        if(moving) transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(delta),120*dt);
                    }
                    sensor.rotation=State.Mode==WatcherMode.Alert?Quaternion.LookRotation(player.Eye-sensor.position):transform.rotation*Quaternion.Euler(0,Mathf.Sin(scan*.8f)*60,0);
                    if(contactCooldown<=0 && State.Mode==WatcherMode.Alert && Vector3.Distance(player.transform.position,transform.position)<.9f && Sees(player.Eye))
                    { player.ReturnToPost(); contactCooldown=3; simulation.RecordWatcherContact(); contact=true; }
                }
            }
            string clip=State.Mode==WatcherMode.Maintenance||State.Mode==WatcherMode.Offline||State.Mode==WatcherMode.Jammed?"TurnOff":contact||contactCooldown>2.4f?"Attack":moving?State.Mode==WatcherMode.Alert?"Run":"Walk":"Idle";
            if(animator!=null)
            {
                animator.speed=controls.CanExplore?1:0;
                if(animationState!=clip) { animator.CrossFadeInFixedTime(clip,.12f); animationState=clip; }
            }
        }
    }
}
