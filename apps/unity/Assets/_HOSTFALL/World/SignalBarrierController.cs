using System;
using Hostfall.Core;
using Hostfall.Interaction;
using Hostfall.Player;
using Hostfall.Simulation.Computers;
using UnityEngine;
namespace Hostfall.World
{
    public sealed class SignalBarrierController:MonoBehaviour,IInteractable
    {
        [SerializeField]private FacilityDoor door;
        [SerializeField]private BoxCollider barrier;
        [SerializeField]private Renderer field;
        [SerializeField]private Light plantLamp,gateLamp;
        [SerializeField]private Transform workstation;
        [SerializeField]private WatcherController approaching;
        [SerializeField]private TextMesh countdown;
        [SerializeField]private LineRenderer[] sparks;
        private SignalEquipment plant;
        private ControlState controls;
        private PlayerMotor player;
        private CharacterController body;
        private MaterialPropertyBlock properties;
        private int caption=-1;
        private bool wasOpen;
        public bool IsBlocking=>barrier!=null&&barrier.enabled;
        public FacilityDoor Door=>door;
        public bool SafetyHeld{get;private set;}
        public string Prompt=>player!=null&&player.transform.position.z>transform.position.z+.4f?"[E] Аварийный возврат · открыть поле на 8 с":"Энергетическое поле · пульт QNT-01";
        public void Configure(FacilityDoor gate,BoxCollider collider,Renderer art,Light equipment,Light signal,Transform pc,WatcherController guard,TextMesh timer,LineRenderer[] arcs)
        {door=gate;barrier=collider;field=art;plantLamp=equipment;gateLamp=signal;workstation=pc;approaching=guard;countdown=timer;sparks=arcs;}
        public void Initialize(ComputerCampaign campaign,ControlState state)
        {
            if(door==null||barrier==null||field==null||workstation==null||approaching==null||countdown==null)throw new InvalidOperationException("Incomplete SIGNAL SYNC physical bindings");
            plant=campaign.SignalPlant;controls=state;player=FindAnyObjectByType<PlayerMotor>();body=player.GetComponent<CharacterController>();properties=new MaterialPropertyBlock();
            wasOpen=door.Capture().opened;plant.FirstStarted+=Approach;plant.Changed+=RefreshState;RefreshState();
        }
        private void Approach(){approaching.HearShot(workstation.position);}
        public void Interact()
        {
            if(player==null||!controls.CanExplore||player.transform.position.z<=transform.position.z+.4f)return;
            plant.ReleaseReturn();
        }
        public void RefreshState()
        {
            if(plant==null)return;
            Vector3 size=Vector3.Scale(barrier.size*.5f,barrier.transform.lossyScale);size.z=Mathf.Max(size.z,.45f);
            // A just-moved CCT was returned by OverlapBox even with its reported bounds
            // clear of the portal. Project the current real body onto the portal axes.
            Vector3 delta=body.bounds.center-barrier.transform.TransformPoint(barrier.center),extents=body.bounds.extents;
            bool Axis(Vector3 axis,float half)=>Mathf.Abs(Vector3.Dot(delta,axis))<=half+Mathf.Abs(axis.x)*extents.x+Mathf.Abs(axis.y)*extents.y+Mathf.Abs(axis.z)*extents.z;
            SafetyHeld=wasOpen&&!plant.BarrierRequestedOpen&&Axis(barrier.transform.right,size.x)&&Axis(barrier.transform.up,size.y)&&Axis(barrier.transform.forward,size.z);
            bool opened=plant.BarrierRequestedOpen||SafetyHeld;
            wasOpen=opened;barrier.enabled=!opened;field.enabled=!opened;door.SetComputerPower(opened);
            if(gateLamp!=null)gateLamp.color=opened?new Color(.2f,.9f,.6f):new Color(.15f,.55f,1);
            field.GetPropertyBlock(properties);properties.SetFloat("_Heat",(float)plant.Heat);field.SetPropertyBlock(properties);
            int remaining=(int)Math.Ceiling(Math.Max(plant.BarrierWindow,plant.ReturnWindow));int token=remaining+(opened?128:0);
            if(token!=caption){caption=token;countdown.text=opened?SafetyHeld&&remaining==0?"ПРОЁМ ЗАНЯТ":"ПОЛЕ СНЯТО / "+remaining+" с":"ПОЛЕ АКТИВНО";}
        }
        private void LateUpdate()
        {
            if(plant==null)return;RefreshState();if(!controls.WorldRuns)return;
            float heat=(float)plant.Heat;
            if(plantLamp!=null){plantLamp.color=Color.Lerp(new Color(.15f,.55f,.9f),new Color(1,.19f,.025f),heat);plantLamp.intensity=.7f+heat*1.4f;}
            if(sparks!=null)for(int i=0;i<sparks.Length;i++)sparks[i].enabled=heat>.6f&&Mathf.Repeat(Time.time*5+i*.21f,1)<.12f;
        }
        private void OnDestroy(){if(plant!=null){plant.FirstStarted-=Approach;plant.Changed-=RefreshState;}}
    }
}
