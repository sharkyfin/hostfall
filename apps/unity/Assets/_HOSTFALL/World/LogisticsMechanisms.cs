using Hostfall.Core;
using Hostfall.Player;
using Hostfall.Simulation.Computers;
using UnityEngine;

namespace Hostfall.World
{
    // Physical adapters for the three PAYLOAD milestones. All moving poses derive from saved clocks.
    [DefaultExecutionOrder(300)]
    public sealed class LogisticsMechanisms : MonoBehaviour
    {
        [SerializeField] private Transform lift, bridge;
        [SerializeField] private Vector3 liftBottom, bridgeParked, bridgeDeployed;
        [SerializeField] private Transform[] cargo;
        [SerializeField] private Vector3 conveyorStart, conveyorEnd;
        [SerializeField] private Light alarm;
        private ComputerCampaign campaign;
        private ControlState controls;
        private CharacterController passenger;
        private Collider liftFloor;
        private bool initialized;
        private AudioSource siren;
        private AudioClip tone;
        public Transform Lift => lift;
        public Transform Bridge => bridge;
        public void Configure(Transform platform, Transform span, Transform[] pallets, Light beacon,
            Vector3 lower, Vector3 parked, Vector3 deployed, Vector3 beltStart, Vector3 beltEnd)
        { lift=platform; bridge=span; cargo=pallets; alarm=beacon; liftBottom=lower; bridgeParked=parked; bridgeDeployed=deployed; conveyorStart=beltStart; conveyorEnd=beltEnd; }
        public void Initialize(ComputerCampaign model)
        {
            campaign=model; controls=FindAnyObjectByType<ControlState>(); passenger=FindAnyObjectByType<PlayerMotor>().GetComponent<CharacterController>();
            liftFloor=lift.GetComponent<Collider>();
            siren=alarm.gameObject.AddComponent<AudioSource>(); siren.spatialBlend=1; siren.minDistance=3; siren.maxDistance=24; siren.volume=.16f; siren.loop=true;
            const int count=24000;var samples=new float[count];
            for(int i=0;i<count;i++){float t=i/24000f;samples[i]=Mathf.Sin(2*Mathf.PI*(430*t+45*Mathf.Sin(t*2*Mathf.PI)/(2*Mathf.PI)))*.2f;}
            tone=AudioClip.Create("Logistics quarantine alarm",count,1,24000,false);tone.SetData(samples,0);siren.clip=tone;
            Apply(false);initialized=true;
        }
        public void RefreshState() { if(campaign!=null)Apply(false); }
        public void SynchronizeForSave() { if(initialized)Apply(true); }
        private void Update()
        {
            if(!initialized)return;
            Apply(controls.WorldRuns);
            bool audible=campaign.Logistics.AlarmEnabled&&controls.WorldRuns;
            if(audible&&!siren.isPlaying)siren.Play(); else if(!audible&&siren.isPlaying)siren.Stop();
        }
        private void Apply(bool carry)
        {
            var state=campaign.Logistics;var before=lift.position;var target=liftBottom+Vector3.up*(float)(state.LiftHeight*.95);
            bool aboard=false;
            if(carry&&passenger!=null&&passenger.enabled)
            {
                Bounds bounds=liftFloor.bounds;var feet=passenger.transform.position;
                aboard=feet.x>bounds.min.x-.05f&&feet.x<bounds.max.x+.05f&&feet.z>bounds.min.z-.05f&&feet.z<bounds.max.z+.05f&&Mathf.Abs(feet.y-bounds.max.y)<.2f;
            }
            lift.position=target;
            if(aboard){Physics.SyncTransforms();passenger.Move(target-before);}
            bridge.position=bridgeDeployed;
            bridge.localRotation=Quaternion.Euler(Mathf.Lerp(-90,0,(float)state.BridgeFraction),0,0);
            for(int i=0;i<cargo.Length;i++)cargo[i].position=Vector3.Lerp(conveyorStart,conveyorEnd,(float)((state.ConveyorPhase+i/(double)cargo.Length)%1));
            alarm.enabled=state.AlarmEnabled;alarm.intensity=state.AlarmPhase<.8?2:0;
        }
        private void OnDestroy(){if(tone!=null)Destroy(tone);}
    }
}
