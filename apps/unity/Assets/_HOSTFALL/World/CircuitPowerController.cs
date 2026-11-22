using System;
using Hostfall.Core;
using Hostfall.Simulation.Computers;
using UnityEngine;

namespace Hostfall.World
{
    // Projection of the saved power graph. It never completes the puzzle or owns logical timers.
    public sealed class CircuitPowerController:MonoBehaviour
    {
        [SerializeField] private FacilityDoor mainDoor,shortcutDoor;
        [SerializeField] private Light[] routeLights;
        [SerializeField] private Transform[] louvers;
        [SerializeField] private Transform turretHead,muzzle;
        [SerializeField] private Light turretLamp;
        [SerializeField] private LineRenderer pulse;
        private WatcherController[] threats;
        private CircuitSession circuit;
        private ControlState controls;
        private WatcherController target;
        private float flash;
        private AudioSource audioSource;
        private static AudioClip pulseClip;
        public string LastPulseTargetId{get;private set;}="";
        public FacilityDoor MainDoor=>mainDoor;
        public FacilityDoor ShortcutDoor=>shortcutDoor;
        public Light[] RouteLights=>routeLights;
        public Transform[] Louvers=>louvers;
        public Transform Muzzle=>muzzle;
        public void Configure(FacilityDoor main,FacilityDoor shortcut,Light[] lights,Transform[] shutters,Transform head,Transform lens,Light indicator,LineRenderer beam)
        {mainDoor=main;shortcutDoor=shortcut;routeLights=lights;louvers=shutters;turretHead=head;muzzle=lens;turretLamp=indicator;pulse=beam;}
        public void Initialize(ComputerCampaign campaign,ControlState state)
        {
            if(mainDoor==null||shortcutDoor==null||routeLights==null||routeLights.Length==0||louvers==null||louvers.Length==0||turretHead==null||muzzle==null||pulse==null)
                throw new InvalidOperationException("Incomplete physical CIRCUIT BREAKER bindings");
            circuit=campaign.Circuit;controls=state;threats=FindObjectsByType<WatcherController>(FindObjectsSortMode.None);
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.spatialBlend=1;audioSource.maxDistance=22;audioSource.rolloffMode=AudioRolloffMode.Linear;
            if(pulseClip==null)
            {
                const int rate=22050;var samples=new float[rate/5];
                for(int i=0;i<samples.Length;i++){float t=i/(float)rate;samples[i]=Mathf.Sin(2*Mathf.PI*(920-600*t)*t)*Mathf.Exp(-20*t)*.22f;}
                pulseClip=AudioClip.Create("Local defensive induction pulse",samples.Length,1,rate,false);pulseClip.SetData(samples,0);
            }
            circuit.Changed+=RefreshState;circuit.TurretPulseRequested+=FirePulse;pulse.enabled=false;RefreshState();ApplyLouvers();
        }
        public void RefreshState()
        {
            if(circuit==null)return;
            mainDoor.SetComputerPower(circuit.Powered(CircuitLoad.MainDoor));shortcutDoor.SetComputerPower(circuit.Powered(CircuitLoad.Shortcut));
            bool lit=circuit.Powered(CircuitLoad.Lighting);foreach(var lamp in routeLights)if(lamp!=null)lamp.enabled=lit;
            bool defence=circuit.Powered(CircuitLoad.Turret);if(turretLamp!=null)turretLamp.enabled=defence;
            if(!defence){target=null;pulse.enabled=false;flash=0;}
        }
        private void ApplyLouvers()
        {foreach(var louver in louvers)louver.localRotation=Quaternion.Euler((float)circuit.VentOpening*82,0,0);}
        private void LateUpdate()
        {
            if(circuit==null)return;ApplyLouvers();if(!controls.WorldRuns)return;
            if(target!=null&&!target.IsDestroyed)
            {
                Vector3 direction=target.transform.position+Vector3.up*.7f-turretHead.position;
                if(direction.sqrMagnitude>.001f)turretHead.rotation=Quaternion.RotateTowards(turretHead.rotation,Quaternion.LookRotation(direction),180*Time.deltaTime);
            }
            if(flash>0){flash=Mathf.Max(0,flash-Time.deltaTime);pulse.enabled=flash>0;}
        }
        private void FirePulse()
        {
            if(!controls.WorldRuns||!circuit.Powered(CircuitLoad.Turret))return;
            target=null;float nearest=12*12;Vector3 origin=muzzle.position;
            foreach(var actor in threats)
            {
                if(actor==null||!actor.isActiveAndEnabled||actor.IsDestroyed)continue;
                Vector3 destination=actor.transform.position+Vector3.up*.65f;float distance=(destination-origin).sqrMagnitude;
                if(distance>=nearest)continue;
                if(Physics.Linecast(origin,destination,out var hit,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<WatcherController>()!=actor)continue;
                nearest=distance;target=actor;
            }
            if(target==null)return;
            LastPulseTargetId=target.Id;target.ApplyEmpStun(1.6f);
            pulse.SetPosition(0,origin);pulse.SetPosition(1,target.transform.position+Vector3.up*.65f);pulse.enabled=true;flash=.14f;
            audioSource.PlayOneShot(pulseClip,.8f);
        }
        private void OnDestroy(){if(circuit!=null){circuit.Changed-=RefreshState;circuit.TurretPulseRequested-=FirePulse;}}
    }
}
