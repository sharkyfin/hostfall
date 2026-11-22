using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hostfall.Player
{
    [DefaultExecutionOrder(90)]
    public sealed class OperatorVisual : MonoBehaviour
    {
        [SerializeField] private CharacterController motor;
        [SerializeField] private Transform[] arms,legs;
        [SerializeField] private Renderer[] head;
        private float phase,stride;
        private OperatorMotion motion;private Transform[][] legParts;private bool? previousFirst,previousVisible;
        private Transform rightGrip,leftGrip,reachGrip;private Vector3 reachTarget;private float reachUntil, reachDuration;
        public void ReachTo(Vector3 target, float duration = .45f)
        { reachTarget = target; reachDuration = duration; reachUntil = Time.time + duration; }
        private Transform[][] parts;
        private Vector3[][] restPositions,restScales;
        private Quaternion[][] restRotations;
        public void Configure(CharacterController controller,Transform[] armJoints,Transform[] legJoints,Renderer[] helmet)
        {motor=controller;arms=armJoints;legs=legJoints;head=helmet;}
        public void SetView(bool first,bool visible)
        {
            if(previousFirst==first&&previousVisible==visible)return;previousFirst=first;previousVisible=visible;
            gameObject.SetActive(visible);
            foreach(var renderer in head)renderer.shadowCastingMode=first?ShadowCastingMode.ShadowsOnly:ShadowCastingMode.On;
            foreach(var arm in arms)arm.gameObject.SetActive(true);
        }
        public void SetWeaponGrips(Transform right,Transform left){rightGrip=right;leftGrip=left;}
        private void CacheArmGeometry()
        {
            if(parts!=null)return;
            motion=motor.GetComponent<OperatorMotion>();
            legParts=legs.Select(l=>new[]{l.Find("Trouser thigh"),l.Find("Trouser calf"),l.Find("Knee protection"),l.Find("Boot"),l.Find("Boot sole")}).ToArray();
            for(int i=0;i<legs.Length;i++)foreach(var t in legs[i].GetComponentsInChildren<Transform>())if(t.name=="Boot lace"||t.name=="Boot sole")t.SetParent(legParts[i][3],true);
            parts=arms.Select(a=>new[]{a.Find("Upper sleeve"),a.Find("Forearm sleeve"),a.Find("Gloved hand")}).ToArray();
            restPositions=parts.Select(p=>p.Select(t=>t.localPosition).ToArray()).ToArray();
            restScales=parts.Select(p=>p.Select(t=>t.localScale).ToArray()).ToArray();
            restRotations=parts.Select(p=>p.Select(t=>t.localRotation).ToArray()).ToArray();
        }
        private void LateUpdate()
        {
            if(motor==null)return;CacheArmGeometry();
            float speed=new Vector2(motor.velocity.x,motor.velocity.z).magnitude;
            stride=Mathf.MoveTowards(stride,motor.isGrounded?Mathf.Clamp01(speed/4):0,7*Time.deltaTime);
            phase+=speed*Time.deltaTime*2.1f;
            for(int i=0;i<legs.Length;i++)
            {
                float angle=Mathf.Sin(phase+i*Mathf.PI)*24*stride;
                if(motion!=null&&motion.HasLibrary)PoseLeg(i);
                else legs[i].localRotation=Quaternion.Euler(angle,0,0);
                var grip=i==0?leftGrip:rightGrip;
                if(i==0&&Time.time<reachUntil)
                {
                    if(reachGrip==null){reachGrip=new GameObject("Interaction reach").transform;reachGrip.SetParent(transform,false);}
                    var start=leftGrip!=null?leftGrip.position:arms[i].position+Vector3.down*.55f;
                    var target=arms[i].position+Vector3.ClampMagnitude(reachTarget-arms[i].position,.76f);
                    reachGrip.position=Vector3.Lerp(start,target,Mathf.Sin((1-(reachUntil-Time.time)/reachDuration)*Mathf.PI));reachGrip.rotation=leftGrip!=null?leftGrip.rotation:transform.rotation;grip=reachGrip;
                }
                if(grip!=null&&grip.gameObject.activeInHierarchy)
                {
                    arms[i].localRotation=Quaternion.identity;
                    PoseArm(i,grip);
                }
                else
                {
                    arms[i].localRotation=Quaternion.Euler(-angle*.7f,0,-5+i*10);
                    for(int p=0;p<3;p++){parts[i][p].localPosition=restPositions[i][p];parts[i][p].localRotation=restRotations[i][p];parts[i][p].localScale=restScales[i][p];}
                    if(motion!=null&&motion.HasLibrary)PoseUnarmed(i);
                }
            }
        }
        private void PoseLeg(int i)
        {
            int first=i==0?2:9;
            var hip=transform.TransformPoint(motion.Joint(first));var knee=transform.TransformPoint(motion.Joint(first+1));var foot=transform.TransformPoint(motion.Joint(first+2));var toe=transform.TransformPoint(motion.Joint(first+3));
            Segment(legParts[i][0],hip,knee,.19f,.21f);Segment(legParts[i][1],knee,foot,.16f,.18f);
            var forward=Vector3.ProjectOnPlane(toe-foot,Vector3.up).normalized;if(forward.sqrMagnitude<.1f)forward=transform.forward;
            var boot=legParts[i][3];boot.position=foot-Vector3.up*.035f+forward*.05f;boot.rotation=Quaternion.LookRotation(forward,Vector3.up);
            legParts[i][2].position=knee+forward*.07f;legParts[i][2].rotation=boot.rotation;
        }
        private void PoseUnarmed(int i)
        {
            int first=i==0?6:13;var shoulder=arms[i].position;
            var upper=transform.TransformVector(motion.Joint(first+1)-motion.Joint(first)).normalized;
            var lower=transform.TransformVector(motion.Joint(first+2)-motion.Joint(first+1)).normalized;
            var elbow=shoulder+upper*.32f;var wrist=elbow+lower*.33f;
            Segment(parts[i][0],shoulder,elbow,.15f,.16f);Segment(parts[i][1],elbow,wrist,.13f,.14f);parts[i][2].position=wrist;
        }
        private void PoseArm(int i,Transform socket)
        {
            var shoulder=arms[i].position;
            var target=socket.position;
            // The Type-IV service chassis has long articulated manipulators. The real
            // upper/forearm sleeves stay connected; no camera-only arms are spawned.
            const float upper=.41f,lower=.46f;
            var vector=target-shoulder;float distance=Mathf.Clamp(vector.magnitude,.12f,upper+lower-.005f);
            var forward=vector.normalized;
            var pole=(transform.right*(i==0?-1:1)*.65f-Vector3.up*.8f-transform.forward*.15f);
            var bend=Vector3.ProjectOnPlane(pole,forward).normalized;
            float along=(upper*upper-lower*lower+distance*distance)/(2*distance);
            float height=Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            var elbow=shoulder+forward*along+bend*height;
            var wrist=shoulder+forward*distance;
            Segment(parts[i][0],shoulder,elbow,.15f,.16f);
            Segment(parts[i][1],elbow,wrist,.13f,.14f);
            var hand=parts[i][2];hand.position=wrist;hand.rotation=socket.rotation;
        }
        private static void Segment(Transform sleeve,Vector3 a,Vector3 b,float width,float depth)
        {
            sleeve.position=(a+b)/2;sleeve.rotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);
            sleeve.localScale=new Vector3(width,Vector3.Distance(a,b)/2,depth);
        }
    }
}
