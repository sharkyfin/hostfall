using UnityEngine;

namespace Hostfall.World
{
    internal sealed class RobotPresentation
    {
        private readonly WatcherController actor;private readonly RobotCombatController combat;
        private readonly Renderer[] renderers;private readonly bool[] visible;private readonly Collider[] colliders;private readonly bool[] solid;
        private readonly Animator animator;private readonly Light[] lights;
        internal RobotPresentation(WatcherController source)
        {
            actor=source;combat=source.GetComponent<RobotCombatController>();animator=source.GetComponentInChildren<Animator>();
            renderers=source.GetComponentsInChildren<Renderer>(true);visible=new bool[renderers.Length];for(int i=0;i<visible.Length;i++)visible[i]=renderers[i].enabled;
            colliders=source.GetComponentsInChildren<Collider>(true);solid=new bool[colliders.Length];for(int i=0;i<solid.Length;i++)solid[i]=colliders[i].enabled;
            lights=source.GetComponentsInChildren<Light>(true);
        }
        internal void SetActive(bool active)
        {
            actor.enabled=active;if(combat!=null)combat.enabled=active;if(animator!=null)animator.enabled=active;
            for(int i=0;i<renderers.Length;i++)renderers[i].enabled=active&&visible[i];
            for(int i=0;i<colliders.Length;i++)colliders[i].enabled=active&&solid[i]&&!actor.IsDestroyed;
            foreach(var light in lights)light.enabled=active&&!actor.IsDestroyed;
        }
        internal void ShowStaged()
        {
            for(int i=0;i<renderers.Length;i++)renderers[i].enabled=visible[i];
        }
        internal void ShowEmerging()
        {
            ShowStaged();
            for(int i=0;i<colliders.Length;i++)if(colliders[i].GetComponent<RobotHitbox>()!=null)colliders[i].enabled=solid[i]&&!actor.IsDestroyed;
            if(animator!=null){if(!animator.enabled){animator.enabled=true;animator.CrossFadeInFixedTime("Run",.1f);}}
        }
    }
}
