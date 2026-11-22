using Hostfall.Core;
using Hostfall.Devices;
using Hostfall.Player;
using Unity.Cinemachine;
using UnityEngine;

namespace Hostfall.CameraSystem
{
    [DefaultExecutionOrder(100)]
    public sealed class PlayerCameraRig : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float thirdPersonDistance = 2.7f;
        [SerializeField, Min(0.05f)] private float collisionRadius = 0.22f;
        [SerializeField] private Vector2 shoulderOffset = new(.45f, .3f);
        private PlayerMotor player;private OperatorMotion motion;
        private FacilityInput input;
        private ControlState controls;
        private CinemachineCamera first;
        private CinemachineCamera third;
        private CinemachineBrain brain;
        private OperatorVisual body;
        private float distance;
        private WorkstationSurface focus;
        private Vector3 blendPosition;private Quaternion blendRotation;private float blendFov,blendTime;
        private bool returning;
        private bool cinematic,showCinematicBody;private Vector3 cinematicPosition;private Quaternion cinematicRotation;private float cinematicFov;
        public void SetCinematicPose(Vector3 p,Quaternion q,float fov,bool showBody){cinematic=true;cinematicPosition=p;cinematicRotation=q;cinematicFov=fov;showCinematicBody=showBody;}
        public void ClearCinematicPose(){cinematic=false;body.SetView(!ThirdPerson,true);}

        public bool FocusReady=>focus!=null&&blendTime>=.45f;
        public WorkstationSurface FocusedSurface=>focus;
        public bool ThirdPerson { get; private set; }
        public void Focus(WorkstationSurface surface)
        {
            var camera=brain.GetComponent<Camera>();focus=surface;returning=false;blendTime=0;
            blendPosition=camera.transform.position;blendRotation=camera.transform.rotation;blendFov=camera.fieldOfView;
        }
        public void ReleaseFocus()
        {
            if(focus==null)return;
            var camera=brain.GetComponent<Camera>();focus.SetNativeOverlay(false);focus=null;returning=true;blendTime=0;
            blendPosition=camera.transform.position;blendRotation=camera.transform.rotation;blendFov=camera.fieldOfView;
        }
        private void PresentFocus()
        {
            if(cinematic){var c=brain.GetComponent<Camera>();c.transform.SetPositionAndRotation(cinematicPosition,cinematicRotation);c.fieldOfView=cinematicFov;body.SetView(false,showCinematicBody);return;}
            if(focus==null&&!returning)return;
            var camera=brain.GetComponent<Camera>();var position=camera.transform.position;var rotation=camera.transform.rotation;float fov=camera.fieldOfView;
            if(focus!=null)focus.FocusPose(camera.aspect,out position,out rotation,out fov);
            blendTime+=Time.unscaledDeltaTime;float t=Mathf.SmoothStep(0,1,blendTime/.45f);
            camera.transform.SetPositionAndRotation(Vector3.Lerp(blendPosition,position,t),Quaternion.Slerp(blendRotation,rotation,t));
            camera.fieldOfView=Mathf.Lerp(blendFov,fov,t);body.SetView(true,false);
            if(returning&&t>=1){returning=false;body.SetView(!ThirdPerson,true);}
        }
        public void Initialize(PlayerMotor motor, FacilityInput bindings, ControlState state, GameObject visual,
            CinemachineCamera fps, CinemachineCamera tps, CinemachineBrain cameraBrain)
        {
            player = motor; input = bindings; controls = state; body = visual.GetComponent<OperatorVisual>(); first = fps; third = tps; brain = cameraBrain;
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
            // A cut avoids interpolating the camera through a wall during mode switches.
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0);
            ApplyMode();
        }
        private void LateUpdate()
        {
            if (player == null) return;
            if (controls.CanExplore && input.SwitchCamera.WasPressedThisFrame()) { ThirdPerson = !ThirdPerson; ApplyMode(); }
            var rotation = player.ViewRotation; var eye = player.Eye;
            if(motion==null)motion=player.GetComponent<OperatorMotion>();
            float startle = controls.CanExplore ? player.Startle : 0;
            first.transform.SetPositionAndRotation(eye+(motion!=null?motion.CameraOffset:Vector3.zero) - rotation * Vector3.forward * (.032f * startle),
                rotation*(motion!=null?motion.CameraSway:Quaternion.identity)*Quaternion.Euler(1.1f * startle, 0, -.8f * startle));
            if(!ThirdPerson){body.SetView(true,true);brain.ManualUpdate();PresentFocus();return;}
            Vector3 offset = rotation * new Vector3(shoulderOffset.x, shoulderOffset.y, -thirdPersonDistance);
            Vector3 back = offset.normalized;
            float allowed = offset.magnitude;
            if (Physics.SphereCast(eye, collisionRadius, back, out var hit, allowed, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore))
                allowed = Mathf.Max(0, hit.distance - 0.04f);
            distance = allowed < distance ? allowed : Mathf.MoveTowards(distance, allowed, 5f * Time.unscaledDeltaTime);
            third.transform.SetPositionAndRotation(eye + back * distance, rotation);
            body.SetView(!ThirdPerson, !ThirdPerson || distance > .65f);
            brain.ManualUpdate();PresentFocus();
        }
        public void RestoreMode(bool thirdPerson) { ThirdPerson = thirdPerson; distance = 0; ApplyMode(); }
        private void ApplyMode() { first.Priority = ThirdPerson ? 0 : 20; third.Priority = ThirdPerson ? 20 : 0; }
    }
}
