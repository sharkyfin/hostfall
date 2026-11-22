using Hostfall.Core;
using UnityEngine;
namespace Hostfall.World
{
    [DefaultExecutionOrder(-150)]
    public sealed class RobotNavigationScheduler : MonoBehaviour
    {
        private ControlState controls;
        private void Awake(){RobotNavigation.ResetSearchQueue();controls=FindAnyObjectByType<ControlState>();}
        private void Update(){if(controls!=null&&controls.WorldRuns)RobotNavigation.ProcessSearchFrame();}
        private void OnDestroy()=>RobotNavigation.ResetSearchQueue();
    }
}
