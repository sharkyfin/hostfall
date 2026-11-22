using System;
using Hostfall.Core;
using UnityEngine;
namespace Hostfall.Player
{
    // The CC0 source's joint positions are retargeted onto the existing articulated body.
    // CharacterController owns translation; the imported root motion is never applied.
    [DefaultExecutionOrder(-40)]
    public sealed class OperatorMotion:MonoBehaviour
    {
        private sealed class Sampler
        {
            private readonly GameObject root;private readonly Transform[] joints;
            private readonly Animation animation;
            private readonly System.Collections.Generic.Dictionary<AnimationClip,AnimationState> states=new();
            private AnimationState active;
            public Sampler(Transform parent,OperatorMotionLibrary library,string id)
            {
                root=new GameObject(id);root.transform.SetParent(parent,false);
                joints=new Transform[library.joints.Length];
                for(int i=0;i<joints.Length;i++){joints[i]=new GameObject(library.joints[i]).transform;joints[i].SetParent(root.transform,false);}
                animation=root.AddComponent<Animation>();animation.playAutomatically=false;animation.enabled=false;animation.cullingType=AnimationCullingType.AlwaysAnimate;
                foreach(var entry in library.clips){animation.AddClip(entry.clip,entry.name);states.Add(entry.clip,animation[entry.name]);}
            }
            public void Sample(AnimationClip clip,float time)
            {
                if(clip==null)return;
                var state=states[clip];if(active!=state){if(active!=null)active.enabled=false;active=state;active.enabled=true;active.weight=1;active.speed=0;}
                active.time=time;animation.Sample();
            }
            public Vector3 At(int i)=>joints[i].localPosition;
        }
        private ControlState controls;
        private OperatorMotionLibrary library;private PlayerMotor player;private CharacterController controller;
        private Sampler a,b,c;private float phase,speed,air,actionAge=10,landAge=10;private bool wasGrounded;
        private AnimationClip action;private float actionDuration;private Vector3 previousAngles,rotationLag;
        public float Intensity=>player!=null?player.MotionIntensity:.5f;
        public Vector3 CameraOffset {get;private set;}
        public Quaternion CameraSway {get;private set;}=Quaternion.identity;
        public Quaternion ItemInertia {get;private set;}=Quaternion.identity;
        public string CurrentClip {get;private set;}="Idle_Loop";
        public bool HasLibrary=>library!=null;
        private void Awake()
        {
            controls=FindAnyObjectByType<ControlState>();player=GetComponent<PlayerMotor>();controller=GetComponent<CharacterController>();library=Resources.Load<OperatorMotionLibrary>("Motion/TypeIVMotionLibrary");
            previousAngles=transform.eulerAngles;
            if(library!=null){a=new Sampler(transform,library,"Motion sample A");b=new Sampler(transform,library,"Motion sample B");c=new Sampler(transform,library,"Motion action");var idle=library.Find("Idle_Loop");a.Sample(idle,0);b.Sample(idle,0);c.Sample(idle,0);wasGrounded=player.Grounded;}
        }
        public void PlayAction(string name,float duration=0)
        {if(library==null)return;action=library.Find(name);if(action==null)return;actionAge=0;actionDuration=duration>0?duration:action.length;}
        private void LateUpdate()
        {
            if(player==null||library==null||controls!=null&&!controls.CanExplore)return;
            float dt=Time.deltaTime;bool grounded=player.Grounded;
            float desired=player.PlanarVelocity.magnitude;speed=Mathf.MoveTowards(speed,desired,12*dt);phase+=dt*Mathf.Lerp(.8f,1.25f,Mathf.Clamp01(speed/5.4f));
            air=Mathf.MoveTowards(air,grounded?0:1,10*dt);
            if(grounded&&!wasGrounded)landAge=0;wasGrounded=grounded;landAge+=dt;actionAge+=dt;
            string first=speed<.2f?"Idle_Loop":"Walk_Loop",second=speed>4.2f?"Sprint_Loop":"Jog_Fwd_Loop";
            CurrentClip=air>.5f?"Jump_Loop":speed>4.2f?second:first;
            var ca=library.Find(first);var cb=library.Find(second);a.Sample(ca,(phase%1)*ca.length);b.Sample(cb,(phase%1)*cb.length);
            if(air>.1f){action=library.Find("Jump_Loop");c.Sample(action,(phase%1)*action.length);}
            else if(actionAge<actionDuration&&action!=null)c.Sample(action,Mathf.Clamp01(actionAge/actionDuration)*action.length);
            else if(landAge<.18f){action=library.Find("Jump_Land");c.Sample(action,landAge/.18f*action.length);}
            float gait=Mathf.Clamp01(speed/5.4f);
            CameraOffset=new Vector3(Mathf.Sin(phase*Mathf.PI*2)*.012f,Mathf.Sin(phase*Mathf.PI*4)*.012f,0)*gait*Intensity*(1-air);
            var angles=player.ViewRotation.eulerAngles;
            var delta=new Vector3(Mathf.DeltaAngle(previousAngles.x,angles.x),Mathf.DeltaAngle(previousAngles.y,angles.y),0);previousAngles=angles;
            rotationLag=Vector3.Lerp(rotationLag,new Vector3(Mathf.Clamp(-delta.x,-3,3),Mathf.Clamp(-delta.y,-4,4),0),1-Mathf.Exp(-14*dt));
#if UNITY_WEBGL && !UNITY_EDITOR
            CameraSway=Quaternion.Euler(0, 0, Mathf.Sin(phase*Mathf.PI*2)*gait*.4f*Intensity);
#else
            CameraSway=Quaternion.Euler(rotationLag.x*.12f*Intensity,rotationLag.y*.12f*Intensity,Mathf.Sin(phase*Mathf.PI*2)*gait*.4f*Intensity);
#endif
            ItemInertia=Quaternion.Euler(rotationLag.x*.65f,rotationLag.y*.65f,-rotationLag.y*.3f);
        }
        private Vector3 Blended(int index)
        {
            float blend=Mathf.Clamp01((speed-.5f)/3.7f);var p=Vector3.Lerp(a.At(index),b.At(index),blend);
            float actionWeight=air>.1f?air:actionAge<actionDuration?Mathf.Sin(Mathf.Clamp01(actionAge/actionDuration)*Mathf.PI):landAge<.18f?(1-landAge/.18f)*.5f:0;
            if(actionWeight>0)p=Vector3.Lerp(p,c.At(index),actionWeight);return p;
        }
        public Vector3 Joint(string name)
            =>library==null?Vector3.zero:Joint(Array.IndexOf(library.joints,name));
        public Vector3 Joint(int index)
        {
            if(library==null||index<0||index>=library.joints.Length)return Vector3.zero;
            var p=Blended(index);var pelvis=Blended(0);
            // Remove all source translation and fit the Type-IV service chassis.
            return Vector3.Scale(p-pelvis+new Vector3(0,.9167f,0),new Vector3(1.29f,.905f,1));
        }
    }
}
