using System.Linq;
using UnityEngine;

namespace Hostfall.Player
{
    // The authored sockets and mechanical groups are shared by held and world models.
    public sealed class WeaponPresentation : MonoBehaviour
    {
        private Transform pump,bolt,slide,magazine;
        private Vector3 pumpRest,boltRest,slideRest,magazineRest;
        private float shotAge=10;
        private Transform reloadGrip;
        private bool supportReload;
        public Transform SupportGrip=>supportReload&&reloadGrip!=null?reloadGrip:LeftGrip;
        private ActiveWeaponType kind;
        public Transform RightGrip { get; private set; }
        public Transform LeftGrip { get; private set; }
        public Transform Muzzle { get; private set; }
        public float ShotAge=>shotAge;
        public void Configure(ActiveWeaponType weapon)
        {
            kind=weapon;RightGrip=Find("GripRight");LeftGrip=Find("GripLeft");Muzzle=Find("Muzzle");
            pump=Find("Pump");bolt=Find("Bolt");slide=Find("Slide");magazine=Find("Magazine");
            pumpRest=Position(pump);boltRest=Position(bolt);slideRest=Position(slide);magazineRest=Position(magazine);
            if(magazine!=null&&LeftGrip!=null)
            {
                var renderer=magazine.GetComponentInChildren<Renderer>();
                if(renderer!=null)
                {
                    reloadGrip=new GameObject("ReloadGrip").transform;reloadGrip.SetParent(magazine,false);
                    reloadGrip.position=renderer.bounds.center-transform.right*(renderer.bounds.extents.x+.015f);
                    reloadGrip.rotation=LeftGrip.rotation;
                }
            }
        }
        private Transform Find(string name)=>GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
        private static Vector3 Position(Transform part)=>part!=null?part.localPosition:Vector3.zero;
        public void Fired()=>shotAge=0;
        public void Ready(){shotAge=10;Tick(0,false,0);}
        public void Tick(float dt,bool reloading,float reloadProgress)
        {
            shotAge+=dt;
            supportReload=reloading&&reloadProgress>.08f&&reloadProgress<.88f;
            float boltStroke=Mathf.Clamp01(1-shotAge/.13f);
            float pumpStroke=shotAge<.09f?0:Mathf.Sin(Mathf.PI*Mathf.Clamp01((shotAge-.09f)/.36f));
            Offset(bolt,boltRest,Vector3.back*boltStroke*.034f);
            Offset(slide,slideRest,Vector3.back*boltStroke*.032f);
            float cycle=reloading?Mathf.Sin(Mathf.PI*Mathf.Clamp01(reloadProgress)):0;
            Offset(pump,pumpRest,Vector3.back*Mathf.Max(pumpStroke,cycle*.6f)*.075f);
            Offset(magazine,magazineRest,(Vector3.down*.15f+Vector3.right*.035f)*cycle);
        }
        private void Offset(Transform part,Vector3 rest,Vector3 displacement)
        {
            if(part!=null)part.localPosition=rest+part.parent.InverseTransformVector(transform.TransformVector(displacement));
        }
    }
}
