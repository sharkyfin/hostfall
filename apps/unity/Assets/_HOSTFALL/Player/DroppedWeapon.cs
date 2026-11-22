using Hostfall.Core;
using Hostfall.Interaction;
using Hostfall.Simulation;
using UnityEngine;
namespace Hostfall.Player
{
    public sealed class DroppedWeapon : MonoBehaviour,IInteractable
    {
        private PlayerWeapon owner;
        private WeaponItemData data;
        private Rigidbody body;
        private ControlState controls;
        private Vector3 velocity,angular;
        private Vector3 safePosition;
        public string Id=>data.id;
        public ActiveWeaponType Kind=>(ActiveWeaponType)data.kind;
        public string Prompt=>"[E] Заменить оружие · "+PlayerWeapon.DisplayName((ActiveWeaponType)data.kind)+" · "+data.ammo+" / "+data.reserve;
        public void Configure(PlayerWeapon holder,WeaponItemData state,ControlState control,Rigidbody physics)
        {owner=holder;data=state;controls=control;body=physics;safePosition=transform.position;velocity=new Vector3(data.vx,data.vy,data.vz);angular=new Vector3(data.ax,data.ay,data.az);body.linearVelocity=velocity;body.angularVelocity=angular;}
        private void Update()
        {if(controls!=null&&controls.CanExplore){data.heat=Mathf.MoveTowards(data.heat,0,Time.deltaTime*.34f);data.cooldown=Mathf.Max(0,data.cooldown-Time.deltaTime);if(data.overheated&&data.heat<.3f)data.overheated=false;}}
        private void FixedUpdate()
        {
            bool paused=controls==null||!controls.CanExplore;
            if(paused&&!body.isKinematic){velocity=body.linearVelocity;angular=body.angularVelocity;body.isKinematic=true;}
            else if(!paused&&body.isKinematic){body.isKinematic=false;body.linearVelocity=velocity;body.angularVelocity=angular;}
            if(transform.position.y< -2.5f){transform.position=safePosition;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
        }
        public void Sync()
        {
            var p=transform.position;var r=transform.eulerAngles;var v=body.isKinematic?velocity:body.linearVelocity;var a=body.isKinematic?angular:body.angularVelocity;
            data.x=p.x;data.y=p.y;data.z=p.z;data.pitch=r.x;data.yaw=r.y;data.roll=r.z;data.vx=v.x;data.vy=v.y;data.vz=v.z;data.ax=a.x;data.ay=a.y;data.az=a.z;
        }
        public void Interact(){if(owner!=null&&owner.TakeDropped(data.id)){gameObject.SetActive(false);Destroy(gameObject);}}
        public static string AuthoredId(Transform t)
        {string p=t.name;while(t.parent!=null){t=t.parent;p=t.name+"/"+p;}return "weapon."+p;}
    }
}
