using Hostfall.Core;
using Hostfall.Player;
using Hostfall.Simulation;
using UnityEngine;

namespace Hostfall.World
{
    public sealed partial class CrawlerMinibossController : MonoBehaviour
    {
        private PlayerMotor player;private ControlState controls;private RobotCombatController combat;
        private RobotNavigation navigation;private Vector3 home,goal;private float phase;
        private Transform sensor;private Light lamp;private AudioSource engine;private bool soundPaused;
        public bool WindingUp => encounter != null ?
            encounter.Phase == CrawlerAttackPhase.Telegraph : combat?.Model?.Data.windup > 0;
        public void Initialize(PlayerMotor target,ControlState state,bool interactiveCampaign = false)
        {
            player=target;controls=state;combat=GetComponent<RobotCombatController>();
            engine=GetComponent<AudioSource>();
            navigation=new RobotNavigation(transform,1.1f);home=transform.position;goal=home;
            foreach(var t in GetComponentsInChildren<Transform>())if(t.name=="Sensor")sensor=t;
            if(sensor!=null){lamp=sensor.gameObject.AddComponent<Light>();lamp.type=LightType.Point;lamp.range=3;lamp.intensity=1;lamp.color=Color.red;combat.RegisterLight(lamp);}
            if (interactiveCampaign) InitializeEncounter();
        }
        private void Update()
        {
            if(player==null||combat?.Model==null)return;
            if(combat.Model.IsDead){if(aimLine!=null)aimLine.enabled=false;return;}
            if(!controls.WorldRuns){if(engine!=null&&!soundPaused){engine.Pause();soundPaused=true;}return;}
            if(soundPaused){engine?.UnPause();soundPaused=false;}
            var d=combat.Model.Data;float dt=Time.deltaTime;
            if (encounter != null)
            {
                UpdateEncounter(d, dt);
                AnimateEncounter(dt);
                return;
            }
            d.stun=Mathf.Max(0,d.stun-dt);d.attackCooldown=Mathf.Max(0,d.attackCooldown-dt);
            if(d.stun>0){d.windup=0;return;}
            Vector3 eye=transform.position+Vector3.up*1.35f;
            float distance=Vector3.Distance(transform.position,player.transform.position);
            bool sees=distance<15&&Mathf.Abs(player.transform.position.y-transform.position.y)<2&&navigation.Visible(eye,player.Eye);
            if(sees){d.memoryTime=6;d.memoryX=player.transform.position.x;d.memoryZ=player.transform.position.z;}
            else d.memoryTime=Mathf.Max(0,d.memoryTime-dt);
            if(d.windup>0)
            {
                d.windup-=dt;if(lamp!=null)lamp.intensity=4+Mathf.Sin(Time.time*28)*2;
                if(d.windup<=0)
                {
                    // The committed firing direction gives a real sidestep window.
                    if(sees&&Vector3.Angle(transform.forward,player.Eye-eye)<13)
                        player.GetComponent<PlayerHealth>()?.TakeDamage(30,"Дуга MANTIS-6");
                    d.attackCooldown=2.4f;
                }
                return;
            }
            if(lamp!=null)lamp.intensity=1;
            Vector2 xb=home.x<0?new Vector2(-23,-9):new Vector2(-3,3);
            Vector2 zb=home.x<0?new Vector2(21,35):new Vector2(67,88);
            if(sees&&distance<10&&d.attackCooldown<=0)
            {
                var aim=player.transform.position-transform.position;aim.y=0;
                navigation.Face(aim,dt);
                if(Vector3.Angle(transform.forward,aim)<12){d.windup=.85f;return;}
            }
            if(d.memoryTime>0)goal=new Vector3(d.memoryX,home.y,d.memoryZ);
            else {phase+=dt*.3f;goal=home+new Vector3(Mathf.Sin(phase)*2,0,Mathf.Cos(phase)*3);}
            if(distance>4||!sees)navigation.Move(goal,1.6f*combat.Mobility,dt,xb,zb);
        }
    }
}
