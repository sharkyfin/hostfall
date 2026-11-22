using System.Linq;
using Hostfall.Core;
using Hostfall.Player;
using Hostfall.Simulation;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.World
{
    // Owns the single exploration clock and spatial validation for authored abilities.
    // It never sends player text to the virtual shell or changes robot transforms directly.
    public sealed class FacilityActionController : MonoBehaviour
    {
        private FacilitySimulation world;
        private PlayerMotor player;
        private ControlState controls;
        private FacilityInput input;
        private FacilityUI ui;
        private WatcherController[] robots;
        public void Initialize(FacilitySimulation model,PlayerMotor motor,ControlState state,FacilityInput bindings,FacilityUI view,WatcherController[] enemies)
        { world=model; player=motor; controls=state; input=bindings; ui=view; robots=enemies; player.DashFeedback+=OnDash; }
        private void OnDash(bool succeeded)=>ui.ShowActionNotice(succeeded?"Рывок":"Рывок · дождитесь восстановления и приземления");
        private void OnDestroy() { if(player!=null) player.DashFeedback-=OnDash; }
        private void Update()
        {
            if(world==null||!controls.WorldRuns) return;
            world.Actions.Tick(Time.deltaTime);
            if(input.Pulse.WasPressedThisFrame()) TryPulse();
        }
        public bool TryPulse()
        {
            if(!controls.CanExplore) return false;
            if(!world.PulseUnlocked) { ui.ShowActionNotice("Импульс откроется на рабочем терминале"); return false; }
            if(world.Actions.PulseCooldown>0) { ui.ShowActionNotice("Импульс · перезарядка "+Mathf.CeilToInt(world.Actions.PulseCooldown)+" с"); return false; }
            WatcherController selected=null; float nearest=4.5f;
            Vector3 forward=player.ViewRotation*Vector3.forward;
            foreach(var robot in robots)
            {
                if(robot == null || robot.IsDestroyed || robot.State == null || robot.State.Mode==WatcherMode.Offline||robot.State.Mode==WatcherMode.Maintenance) continue;
                Vector3 aim=robot.transform.position+Vector3.up*.8f;
                Vector3 delta=aim-player.Eye;
                if(delta.magnitude>nearest||Vector3.Angle(forward,delta)>35) continue;
                if(Physics.Linecast(player.Eye,aim,out var hit,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<WatcherController>()!=robot) continue;
                selected=robot; nearest=delta.magnitude;
            }
            if(selected==null)
            {
                bool anyActive = robots != null && robots.Any(r => r != null && !r.IsDestroyed && r.State != null && r.State.Mode != WatcherMode.Offline && r.State.Mode != WatcherMode.Maintenance);
                ui.ShowActionNotice(anyActive ? "Наведитесь на цель" : "Нет активных целей");
                return false;
            }
            bool applied=world.Actions.TryPulse(selected.Id,true);
            if(applied) { selected.Tick(0); world.NotifyTerminalChanged(); ui.ShowActionNotice("Импульс успешен"); }
            return applied;
        }
    }
}
