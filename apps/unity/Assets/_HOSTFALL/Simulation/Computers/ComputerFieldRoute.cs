using System;

namespace Hostfall.Simulation.Computers
{
    [Flags]
    internal enum FieldRouteStep
    {
        None = 0, Camera = 1, Signal = 2, Cargo = 4, Floor = 8, Voice = 16, Key = 32, Home = 64, Armory = 128,
        All = Camera | Signal | Cargo | Floor | Voice | Key | Home | Armory
    }

    [Serializable]
    public sealed class FieldRouteData
    {
        public int version = 1, completed, cameraRead, voiceRead;

        internal bool Has(FieldRouteStep step) => (completed & (int)step) != 0;
        internal void Mark(FieldRouteStep step) => completed |= (int)step;
        internal FieldRouteData Copy() => (FieldRouteData)MemberwiseClone();

        internal static FieldRouteData Restore(FieldRouteData data)
        {
            if (data == null) return new FieldRouteData();
            SaveValidation.Require(data.version == 1 && (data.completed & ~(int)FieldRouteStep.All) == 0 &&
                FieldCase.Camera.ValidMask(data.cameraRead) && FieldCase.Voice.ValidMask(data.voiceRead) &&
                (!data.Has(FieldRouteStep.Camera) || FieldCase.Camera.HasProof(data.cameraRead)) &&
                (!data.Has(FieldRouteStep.Voice) || FieldCase.Voice.HasProof(data.voiceRead)), "Invalid field procedure");
            return data.Copy();
        }
    }

    public enum FieldCaseResult { Unavailable, CompareSources, Incorrect, Confirmed }

    public sealed partial class ComputerCampaign
    {
        private FieldRouteData fieldRoute = new();

        public bool CameraIsolated => Network.Stage == 3 || fieldRoute.Has(FieldRouteStep.Camera);
        public bool PhaseCalibrated => Signal.Stage == 3 || fieldRoute.Has(FieldRouteStep.Signal);
        public bool CargoReleased => Payload.Stage == 3 || fieldRoute.Has(FieldRouteStep.Cargo);
        public bool FloorServiced => Minefield.Phase == MinefieldPhase.Complete || fieldRoute.Has(FieldRouteStep.Floor);
        public bool VoiceVerified => Investigation.Verified || UsesVoiceCase;
        public bool UsesVoiceCase => fieldRoute.Has(FieldRouteStep.Voice);
        public bool ArmoryAccessible => Fragments.Complete || fieldRoute.Has(FieldRouteStep.Armory);
        public bool ArchiveKeyTaken => Investigation.KeyRecovered || fieldRoute.Has(FieldRouteStep.Key);
        public bool RecoveryAuthorized => Recovery.Complete || fieldRoute.Has(FieldRouteStep.Home);

        public bool CaseAvailable(string station) => station switch
        {
            NetworkStation => FirstTaskComplete,
            DetectiveStation => InvestigationAccessible,
            _ => false
        };

        public bool CaseConfirmed(string station) => station switch
        {
            NetworkStation => CameraIsolated,
            DetectiveStation => VoiceVerified,
            _ => false
        };

        public int CaseReadMask(string station) => station switch
        {
            NetworkStation => fieldRoute.cameraRead,
            DetectiveStation => fieldRoute.voiceRead,
            _ => 0
        };

        public bool ReadCaseEvidence(string station, int index)
        {
            var dossier = FieldCase.Find(station);
            if (dossier == null || !CaseAvailable(station) || index < 0 || index >= dossier.Evidence.Count)
                return false;
            int read = CaseReadMask(station);
            int mask = read | (1 << index);
            if (mask == read) return true;
            if (station == NetworkStation) fieldRoute.cameraRead = mask;
            else fieldRoute.voiceRead = mask;
            Changed?.Invoke();
            return true;
        }

        public FieldCaseResult SubmitCase(string station, int suspect, int evidence)
        {
            var dossier = FieldCase.Find(station);
            if (dossier == null || !CaseAvailable(station)) return FieldCaseResult.Unavailable;
            if (CaseConfirmed(station)) return FieldCaseResult.Confirmed;
            if (suspect < 0 || suspect >= dossier.Suspects.Count || !dossier.ValidMask(evidence) ||
                (CaseReadMask(station) & evidence) != evidence || !FieldCase.IsPair(evidence))
                return FieldCaseResult.CompareSources;
            if (!dossier.Matches(suspect, evidence))
            {
                if (station == NetworkStation) NetworkSecurity.Start();
                return FieldCaseResult.Incorrect;
            }

            if (station == NetworkStation)
            {
                NetworkSecurity.Start();
                fieldRoute.Mark(FieldRouteStep.Camera);
                for (int stage = Network.Stage + 1; stage <= 3; stage++) AwardNetworkStage(stage);
            }
            else
            {
                fieldRoute.Mark(FieldRouteStep.Voice);
                AwardVerifiedCase();
            }

            return FieldCaseResult.Confirmed;
        }

        public bool TakeArchiveKey()
        {
            if (!VoiceVerified || ArchiveKeyTaken) return false;
            if (!UsesVoiceCase) return Investigation.RecoverKey();
            fieldRoute.Mark(FieldRouteStep.Key);
            AwardArchiveKey();
            return true;
        }

        public bool AuthorizeRecovery()
        {
            if (!RecoveryAccessible || RecoveryAuthorized) return false;
            fieldRoute.Mark(FieldRouteStep.Home);
            AwardRecovery();
            return true;
        }

        public string ExecuteFieldUtility(string station, FieldUtilityId utility)
        {
            if (FieldComputer.Find(station)?.Offers(utility) != true)
                return "Файл выполнения отсутствует на этом пульте.";

            switch (utility)
            {
                case FieldUtilityId.PowerLight:
                case FieldUtilityId.PowerGuard:
                    if (!CircuitAccessible) return "Шина недоступна. Сначала проверьте камеры на CORE-01.";
                    bool light = utility == FieldUtilityId.PowerLight;
                    if (!Circuit.ApplyServicePreset(light)) return "Автомат остывает. Повторите запуск через несколько секунд.";
                    return light ? "Шлюз запитан. Освещение и служебный обход включены." : "Шлюз запитан. Турель активна, освещение отключено.";

                case FieldUtilityId.PhaseBypass:
                    if (!Circuit.Powered(CircuitLoad.MainDoor)) return "Нет питания. Проверьте PWR-01.";
                    if (!SignalPlant.Start()) return "Реле остывает. Повторите запуск через несколько секунд.";
                    if (!PhaseCalibrated)
                    {
                        fieldRoute.Mark(FieldRouteStep.Signal);
                        for (int stage = Signal.Stage + 1; stage <= 3; stage++) AwardSignalStage(stage);
                    }
                    SignalPlant.OpenBarrier();
                    return "Поле снято на 20 секунд.";

                case FieldUtilityId.CargoTransfer:
                    if (!PhaseCalibrated) return "Нет разрешения от квантового узла.";
                    if (CargoReleased) return "Грузовой цикл уже выполнен.";
                    fieldRoute.Mark(FieldRouteStep.Cargo);
                    for (int stage = Payload.Stage + 1; stage <= 3; stage++) AwardPayloadStage(stage);
                    return "Грузовой мост запущен. Сработали ворота карантина.";

                case FieldUtilityId.FloorService:
                    if (!CargoReleased || LogisticsDefense.Active) return "Датчики под охраной грузового шлюза.";
                    if (FloorServiced) return "Датчики уже выведены из контура.";
                    fieldRoute.Mark(FieldRouteStep.Floor);
                    for (int stage = Minefield.Stage + 1; stage <= MinefieldSession.LevelCount; stage++)
                        AwardMinefieldStage(stage);
                    return "Пол переведён в режим обслуживания. Проход в архив разблокирован.";

                case FieldUtilityId.ArmoryPermit:
                    if (!CameraIsolated) return "Требуется локальный допуск от CORE-01.";
                    if (ArmoryAccessible) return "Допуск уже выдан.";
                    fieldRoute.Mark(FieldRouteStep.Armory);
                    AwardFragmentDock();
                    return "Резервный ключ принят. Дверь оружейной можно открыть вручную.";

                case FieldUtilityId.QuarantineRecall:
                    if (!ContainmentAccessible) return "Требуется физический ключ нейроархива.";
                    if (Containment.Complete) return "Карантин уже изолирован.";
                    Containment.Start();
                    Containment.Release(ContainmentShutter.South);
                    Containment.Release(ContainmentShutter.North);
                    Containment.Send(SentryOrder.Capture);
                    return "R26 получил команду возврата. Следите за датчиком камеры.";

                case FieldUtilityId.QuarantineSeal:
                    if (!ContainmentAccessible) return "Требуется физический ключ нейроархива.";
                    if (Containment.Complete) return "Карантин уже изолирован.";
                    var sensors = Containment.Sensors;
                    if (sensors.PlayerInside || sensors.SouthBlocked || sensors.NorthBlocked)
                        return "Створки заблокированы датчиком присутствия.";
                    if (sensors.Zone != SentryZone.Offline &&
                        (sensors.Zone != SentryZone.Bay || Containment.Order != SentryOrder.Capture))
                        return "R26 вне камеры. Отзовите его или выведите из строя.";
                    Containment.Start();
                    foreach (var shutter in new[] { ContainmentShutter.South, ContainmentShutter.North })
                        if (Containment.RequestedOpen(shutter)) Containment.Toggle(shutter);
                    if (!Containment.Isolate())
                        return "Створки опускаются. Дождитесь подтверждения и повторите запуск.";
                    return "Контур карантина изолирован. Путь к Центральному кольцу открыт.";

                default:
                    return "Утилита недоступна.";
            }
        }
    }
}
