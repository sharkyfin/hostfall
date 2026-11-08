using System;
using Hostfall.Simulation;
using UnityEngine;

namespace Hostfall.Missions
{
    [CreateAssetMenu(menuName = "HOSTFALL/Incident Mission")]
    public sealed class IncidentMissionAsset : ScriptableObject
    {
        [Serializable]
        private struct ObjectiveConfig
        {
            public ObjectiveCondition condition;
            public DeviceId device;
            [TextArea] public string description;
            public ObjectiveConfig(ObjectiveCondition kind, DeviceId target, string instructions)
            { condition = kind; device = target; description = instructions; }
        }
        [SerializeField, TextArea] private string completionMessage = "Комплекс полностью разблокирован. Служебный контур доступен.";
        [SerializeField] private ObjectiveConfig[] objectives =
        {
            new(ObjectiveCondition.TerminalSequence, DeviceId.Ws07, "Доберитесь до рабочего терминала в операторской."),
            new(ObjectiveCondition.TerminalSequence, DeviceId.Edge01, "Прорвитесь к терминалу шлюза в проходе."),
            new(ObjectiveCondition.TerminalSequence, DeviceId.Core01, "Доберитесь до терминала серверной и отключите защитные ворота."),
            new(ObjectiveCondition.TerminalSequence, DeviceId.Data01, "Пройдите в Сектор 02 и разблокируйте энергоцентр на терминале DATA-01."),
            new(ObjectiveCondition.TerminalSequence, DeviceId.Pwr01, "Проникните в Сектор 03 и активируйте подстанцию на распределителе PWR-01."),
            new(ObjectiveCondition.TerminalSequence, DeviceId.Qnt01, "Доберитесь до квантового релея QNT-01 в Секторе 04 и откройте служебный контур.")
        };
        public MissionDefinition CreateDefinition()
        {
            var data = new ObjectiveDefinition[objectives.Length];
            for (int i = 0; i < data.Length; i++) data[i] = new ObjectiveDefinition(objectives[i].condition, objectives[i].device, objectives[i].description);
            return new MissionDefinition(completionMessage, data);
        }
    }
}
