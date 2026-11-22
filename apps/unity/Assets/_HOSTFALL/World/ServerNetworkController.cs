using System;
using Hostfall.Core;
using Hostfall.Mapping;
using Hostfall.Simulation.Computers;
using UnityEngine;

namespace Hostfall.World
{
    // Physical network, bounded relay of sightings, and authored reinforcement emergence.
    public sealed class ServerNetworkController:MonoBehaviour
    {
        [SerializeField] private WatcherController[] members, responders;
        [SerializeField] private Transform grate;
        [SerializeField] private Vector3[] hidden, exits;
        [SerializeField] private Light relayLamp;
        private ComputerCampaign campaign;
        private ControlState controls;
        private RobotPresentation[] presentations;
        private Collider[] grilleColliders;
        private Vector3 grilleClosed;
        
        private AudioSource sound;
        private int warningIndex=-1;
        public bool CommandOnline=>campaign==null||campaign.NetworkCommandOnline;
        public bool Disconnected=>campaign?.NetworkSecurity.OutageRemaining>0;
        public WatcherController[] Responders=>responders;
        public void Configure(WatcherController[] patrols,WatcherController[] reserves,Transform grille,Vector3[] starts,Vector3[] ends,Light lamp)
        { members=patrols;responders=reserves;grate=grille;hidden=starts;exits=ends;relayLamp=lamp; }
        public void Initialize(ComputerCampaign model)
        {
            campaign=model;
            sound=gameObject.AddComponent<AudioSource>();sound.spatialBlend=1;sound.minDistance=2;sound.maxDistance=18;sound.playOnAwake=false;
            controls=FindAnyObjectByType<ControlState>();grilleClosed=grate.localPosition;grilleColliders=grate.GetComponentsInChildren<Collider>();
            if(responders.Length!=2||hidden.Length!=2||exits.Length!=2)throw new InvalidOperationException("Network reserve binding must have two authored actors/lanes");
            presentations=new RobotPresentation[responders.Length];
            foreach(var member in members)member.BindNetwork(this);
            for(int i=0;i<responders.Length;i++)
            {
                responders[i].BindNetwork(this);presentations[i]=new RobotPresentation(responders[i]);
                bool released=i<model.NetworkSecurity.Released;
                if(!released)responders[i].transform.position=hidden[i];
                presentations[i].SetActive(released);
            }
            campaign.NetworkSecurity.RelayRequested+=RelaySightings;
            campaign.NetworkSecurity.ReleaseChanged+=Released;RefreshState();
        }
        public void RefreshState()
        {
            if(campaign==null)return;
            if(!CommandOnline)
            {
                foreach(var actor in members)if(actor!=null)actor.ForgetNetworkTarget();
                foreach(var actor in responders)if(actor!=null)actor.ForgetNetworkTarget();
            }
            relayLamp.color=Disconnected?new Color(.25f,.5f,1):CommandOnline?new Color(1,.55f,.1f):new Color(.15f,.8f,.65f);
            relayLamp.intensity=1.4f;
        }
        private void Released(int count)
        {
            int index=count-1;CheckLane(index);
            responders[index].transform.position=exits[index];presentations[index].SetActive(true);Physics.SyncTransforms();
        }
        private void CheckLane(int index)
        {
            var direction=exits[index]-hidden[index];
            foreach(var hit in Physics.CapsuleCastAll(hidden[index]+Vector3.up*.35f,hidden[index]+Vector3.up, .28f,direction.normalized,direction.magnitude,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore))
            {
                if(hit.collider.transform.IsChildOf(transform)||hit.collider.GetComponentInParent<IDamageable>()!=null)continue;
                throw new InvalidOperationException("Server reserve lane intersects "+hit.collider.name);
            }
        }
        private void Update()
        {
            if(campaign==null||!controls.WorldRuns)return;
            RefreshState();
            var state=campaign.NetworkSecurity;
            bool opening=state.HasPending&&state.ReleaseClock>=10.8;
            foreach(var box in grilleColliders)box.enabled=!opening;
            grate.localPosition=grilleClosed+Vector3.up*(opening?Mathf.SmoothStep(0,1,(float)((state.ReleaseClock-10.8)/1.2))*1.8f:0);
            if (!opening && warningIndex >= 0)
            {
                if (warningIndex >= state.Released)
                {
                    responders[warningIndex].transform.position = hidden[warningIndex];
                    presentations[warningIndex].SetActive(false);
                }
                warningIndex = -1;
            }
            if(opening)
            {
                int index=state.Released;
                if(warningIndex!=index){warningIndex=index;CheckLane(index);sound.PlayOneShot(VentReleaseController.MetalScrape,.75f);}
                presentations[index].ShowEmerging();
                responders[index].transform.position=Vector3.Lerp(hidden[index],exits[index],Mathf.SmoothStep(0,1,(float)((state.ReleaseClock-10.8)/1.2)));
            }

        }
        public void RelaySightings()
        {
            if(!CommandOnline||Disconnected)return;
            foreach(var observer in members)
            {
                if(observer==null||!observer.isActiveAndEnabled||observer.IsDestroyed||observer.State.Mode is not (Hostfall.Simulation.WatcherMode.Alert or Hostfall.Simulation.WatcherMode.AttackWindup))continue;
                foreach(var receiver in members)
                    if(receiver!=observer&&receiver!=null&&Vector3.Distance(receiver.transform.position,observer.transform.position)<14)receiver.ReceiveNetworkTarget(observer.LastKnownPosition);
                foreach(var receiver in responders)
                    if(receiver!=null&&Vector3.Distance(receiver.transform.position,observer.transform.position)<14)receiver.ReceiveNetworkTarget(observer.LastKnownPosition);
            }
        }
        private void OnDestroy(){if(campaign!=null){campaign.NetworkSecurity.ReleaseChanged-=Released;campaign.NetworkSecurity.RelayRequested-=RelaySightings;}}
    }
}
