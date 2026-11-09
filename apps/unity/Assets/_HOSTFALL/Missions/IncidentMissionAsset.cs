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
            new(ObjectiveCondition.TerminalSequence, DeviceId.Ws07, "Сектор 00 · Операторская // Терминал WS-07"),
            new(ObjectiveCondition.TerminalSequence, DeviceId.Edge01, "Сектор 01 · Служебный проход // Терминал EDGE-01"),
            new(ObjectiveCondition.TerminalSequence, DeviceId.Core01, "Сектор 01 · Серверная // Терминал CORE-01"),
            new(ObjectiveCondition.TerminalSequence, DeviceId.Data01, "Сектор 02 · Вычислительный склеп // Терминал DATA-01"),
            new(ObjectiveCondition.TerminalSequence, DeviceId.Pwr01, "Сектор 03 · Энергоцентр // Распределитель PWR-01"),
            new(ObjectiveCondition.TerminalSequence, DeviceId.Qnt01, "Сектор 04 · Квантовый нексус // Релей QNT-01")
        };
        public MissionDefinition CreateDefinition()
        {
            var data = new ObjectiveDefinition[objectives.Length];
            for (int i = 0; i < data.Length; i++) data[i] = new ObjectiveDefinition(objectives[i].condition, objectives[i].device, objectives[i].description);
            return new MissionDefinition(completionMessage, data);
        }
    }
}
