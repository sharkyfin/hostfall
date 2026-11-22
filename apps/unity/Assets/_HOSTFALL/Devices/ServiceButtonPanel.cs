using Hostfall.Interaction;
using Hostfall.Simulation;
using UnityEngine;
namespace Hostfall.Devices
{
    // A local task-light switch, not another door key or permanent HUD indicator.
    public sealed class ServiceButtonPanel:MonoBehaviour,IInteractable
    {
        [SerializeField] private Light taskLight;
        [SerializeField] private string stableId;
        private FacilitySimulation simulation;
        public string Prompt=>taskLight!=null&&taskLight.enabled?"[E] Выключить рабочий свет":"[E] Включить рабочий свет";
        public void Configure(string id,Light light){stableId=id;taskLight=light;}
        public void Initialize(FacilitySimulation model){simulation=model;if(taskLight!=null)taskLight.enabled=!model.Switches.IsOff(stableId);}
        public void Interact(){if(taskLight==null||simulation==null)return;taskLight.enabled=!taskLight.enabled;simulation.Switches.SetOff(stableId,!taskLight.enabled);}
    }
}
