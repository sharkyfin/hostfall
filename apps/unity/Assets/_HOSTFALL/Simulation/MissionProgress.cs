using System;
using System.Collections.Generic;

namespace Hostfall.Simulation
{
    public enum ObjectiveCondition { InspectProcesses, IsolateCacheSource, TerminalSequence }
    public readonly struct ObjectiveDefinition
    {
        public ObjectiveCondition Condition { get; }
        public DeviceId Device { get; }
        public string Description { get; }
        public ObjectiveDefinition(ObjectiveCondition condition, DeviceId device, string description)
        {
            if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Objective needs instructions", nameof(description));
            Condition = condition; Device = device; Description = description;
        }
    }
    public sealed class MissionDefinition
    {
        public string CompletionMessage { get; }
        public IReadOnlyList<ObjectiveDefinition> Objectives { get; }
        public MissionDefinition(string completionMessage, params ObjectiveDefinition[] objectives)
        {
            if (objectives == null || objectives.Length == 0) throw new ArgumentException("Mission needs objectives", nameof(objectives));
            CompletionMessage = completionMessage;
            Objectives = Array.AsReadOnly((ObjectiveDefinition[])objectives.Clone());
        }
        public static MissionDefinition NightShift => new("Инцидент локализован.",
            new ObjectiveDefinition(ObjectiveCondition.InspectProcesses, DeviceId.Ws07, "Выясните, что управляет наблюдателем."),
            new ObjectiveDefinition(ObjectiveCondition.IsolateCacheSource, DeviceId.Ws07, "Отключите источник управляющих заданий."),
            new ObjectiveDefinition(ObjectiveCondition.InspectProcesses, DeviceId.Core01, "Проверьте оставшийся контур управления в серверной."));
        public static MissionDefinition ActionRoute => new("Данные участка у вас. Маршрут до серверной открыт.",
            new ObjectiveDefinition(ObjectiveCondition.TerminalSequence,DeviceId.Ws07,"Доберитесь до рабочего терминала в операторской."),
            new ObjectiveDefinition(ObjectiveCondition.TerminalSequence,DeviceId.Edge01,"Прорвитесь к терминалу шлюза в проходе."),
            new ObjectiveDefinition(ObjectiveCondition.TerminalSequence,DeviceId.Core01,"Доберитесь до терминала серверной и заберите карту участка."));
        public static MissionDefinition FacilityCampaign => new("Комплекс полностью разблокирован. Служебный контур доступен.",
            new ObjectiveDefinition(ObjectiveCondition.TerminalSequence,DeviceId.Ws07,"Доберитесь до рабочего терминала в операторской."),
            new ObjectiveDefinition(ObjectiveCondition.TerminalSequence,DeviceId.Edge01,"Прорвитесь к терминалу шлюза в проходе."),
            new ObjectiveDefinition(ObjectiveCondition.TerminalSequence,DeviceId.Core01,"Доберитесь до терминала серверной и отключите защитные ворота."),
            new ObjectiveDefinition(ObjectiveCondition.TerminalSequence,DeviceId.Data01,"Пройдите в Сектор 02 и разблокируйте энергоцентр на терминале DATA-01."),
            new ObjectiveDefinition(ObjectiveCondition.TerminalSequence,DeviceId.Pwr01,"Проникните в Сектор 03 и активируйте подстанцию на распределителе PWR-01."),
            new ObjectiveDefinition(ObjectiveCondition.TerminalSequence,DeviceId.Qnt01,"Доберитесь до квантового релея QNT-01 в Секторе 04 и откройте служебный контур."));
    }
    public sealed class MissionProgress
    {
        private readonly MissionDefinition definition;
        public int CompletedObjectives { get; private set; }
        public bool IsComplete => CompletedObjectives == definition.Objectives.Count;
        public bool UsesTerminalSequences => definition.Objectives[0].Condition==ObjectiveCondition.TerminalSequence;
        public string Description => IsComplete ? definition.CompletionMessage : definition.Objectives[CompletedObjectives].Description;
        public MissionProgress(MissionDefinition definition) { this.definition = definition ?? throw new ArgumentNullException(nameof(definition)); }
        internal void Restore(int completed)
        {
            SaveValidation.Require(completed >= 0 && completed <= definition.Objectives.Count, "Invalid saved mission progress");
            CompletedObjectives = completed;
        }
        public void Record(ObjectiveCondition condition, DeviceId device)
        {
            if (IsComplete) return;
            var objective = definition.Objectives[CompletedObjectives];
            if (objective.Condition == condition && objective.Device == device) CompletedObjectives++;
        }
    }
}
