using System;
using System.Collections.Generic;
using System.Linq;

namespace Hostfall.Simulation.Computers
{
    public enum DetectiveSource { Files, Mail, Photos, Notes, Recordings }
    public enum DetectiveEvent { SealArchive, Quarantine, RemoteSignature, PublishReport }
    public enum DetectiveResult { Incomplete, ChronologyConflict, UnsupportedEvidence, DateConflict, Verified }
    public enum DetectiveEvidence
    {
        ClockCertificate, PublicReport, EngineerMail, CorridorPhoto, ShiftNotebook,
        ArchivePlayback, SensorRegister, CustodyMemo, IdentityProtocol
    }

    public sealed class DetectiveDocument
    {
        public DetectiveEvidence Id { get; }
        public DetectiveSource Source { get; }
        public string Title { get; }
        public string Text { get; }

        internal DetectiveDocument(DetectiveEvidence id, DetectiveSource source, string title, string text)
        {
            Id = id;
            Source = source;
            Title = title;
            Text = text;
        }
    }

    [Serializable]
    public sealed class DetectiveData
    {
        public int version = 1, opened, collected;
        public int[] timeline = { -1, -1, -1, -1 }, citations = { -1, -1, -1, -1 };
        public int incidentDay, dateEvidence = -1, reportEvidence = -1;
        public bool verified, keyRecovered, highContrast, playing;
        public double playbackTime, stationPlaybackTime;
        public int[] selected = { -1, -1, -1, -1, -1 };
        public DetectiveResult result;
    }

    public sealed class DetectiveSession
    {
        public const int Slots = 4, RecordingFrames = 12;
        public const float RecordingLength = 6;
        private const int Count = 9, DocumentBits = (1 << Count) - 1;
        private static readonly DetectiveDocument[] documents =
        {
            new(DetectiveEvidence.ClockCertificate, DetectiveSource.Files, "Сверка часов станции", "17 ноября, 02:50. Камера С-08 и шкаф охраны SEC-07 сверены с эталонными часами. Погрешность меньше секунды. Аварийное питание часов продержалось до 05:00."),
            new(DetectiveEvidence.PublicReport, DetectiveSource.Files, "Сменный рапорт № 0317", "Составлен и распространён 18 ноября в 09:20. Официальная дата инцидента: 18 ноября, 03:17. Внесено в общий реестр после подписания в службе контроля."),
            new(DetectiveEvidence.EngineerMail, DetectiveSource.Mail, "Левин → Ли · закрываю архив", "17 ноября, 03:04. Пломба нейроархива установлена. Ключ заперт в кабинете. После моего выхода дверь под карантином. Если кто-то пришлёт запрос с пульта, сначала сверяйтесь с записью камеры."),
            new(DetectiveEvidence.CorridorPhoto, DetectiveSource.Photos, "Кадр С-08 · пломба", "17 ноября, 03:04:36. Дверь закрыта. На пломбе видна метка Левина. Индикатор общего карантина ещё погашен."),
            new(DetectiveEvidence.ShiftNotebook, DetectiveSource.Notes, "Заметка начальника смены", "03:07. Сигнал общей изоляции. Проходы погасли по цепочке. Рапорт обещали подготовить утром. Время внутри комплекса подтверждают оригинальные записи."),
            new(DetectiveEvidence.ArchivePlayback, DetectiveSource.Recordings, "С-08 · аварийная запись", "Локальный журнал от 17 ноября. В 03:07 общий карантин блокирует шлюзы. Первый удалённый запрос с подписью оператора получен в 03:17, когда рабочее место уже пусто."),
            new(DetectiveEvidence.SensorRegister, DetectiveSource.Files, "Регистр SEC-07", "17 ноября, 03:17. Подпись оператора получена по шине. За предыдущие десять минут ни одна секция пола не зарегистрировала шаг."),
            new(DetectiveEvidence.CustodyMemo, DetectiveSource.Mail, "Рапорт передан в реестр", "18 ноября, 09:20. На рассылку ушла версия после правки службы контроля. Первичные видеофайлы сохранены на закрытых мониторах поста наблюдения."),
            new(DetectiveEvidence.IdentityProtocol, DetectiveSource.Notes, "Скрытый протокол · преемственность допуска", "Идентификатор оператора можно восстановить по записи памяти. Карантин проверяет подпись, но не происхождение её носителя. Протокол позволяет восстановленному оператору подтверждать операции от имени прежнего. В документации SUPER такие подтверждения отмечены как штатные.")
        };
        private readonly int[] timeline = { -1, -1, -1, -1 }, citations = { -1, -1, -1, -1 };
        private readonly int[] selected = { -1, -1, -1, -1, -1 };
        private int opened, collected;
        public int IncidentDay { get; private set; }
        public int DateEvidence { get; private set; } = -1;
        public int ReportEvidence { get; private set; } = -1;
        public bool Verified { get; private set; }
        public bool KeyRecovered { get; private set; }
        public bool HighContrast { get; private set; }
        public bool Playing { get; private set; }
        public double PlaybackTime { get; private set; }
        public double StationPlaybackTime { get; private set; }
        public DetectiveResult Result { get; private set; }
        public event Action Changed, CaseVerified, KeyCollected;

        public static IReadOnlyList<DetectiveDocument> Documents => documents;
        public static DetectiveDocument Find(DetectiveEvidence id) => IdBit(id) != 0 ? documents[(int)id] : null;
        public int Selected(DetectiveSource source) => (int)source >= 0 && (int)source < selected.Length ? selected[(int)source] : -1;

        public void SetHighContrast(bool enabled)
        {
            if (HighContrast == enabled)
                return;
            HighContrast = enabled;
            Changed?.Invoke();
        }
        public int TimelineAt(int slot) => ValidSlot(slot) ? timeline[slot] : -1;
        public int CitationAt(int slot) => ValidSlot(slot) ? citations[slot] : -1;
        public bool IsAvailable(DetectiveEvidence id) => IdBit(id) != 0 && (id != DetectiveEvidence.IdentityProtocol || Verified);
        public bool IsOpened(DetectiveEvidence id) => (opened & IdBit(id)) != 0;
        public bool IsCollected(DetectiveEvidence id) => (collected & IdBit(id)) != 0;

        public void ToggleRecording()
        {
            if (!IsOpened(DetectiveEvidence.ArchivePlayback))
                return;
            if (!Playing && PlaybackTime >= RecordingLength)
                PlaybackTime = 0;
            Playing = !Playing;
            Changed?.Invoke();
        }

        public void SeekRecording(double seconds)
        {
            if (!IsOpened(DetectiveEvidence.ArchivePlayback) || !double.IsFinite(seconds))
                return;
            PlaybackTime = Math.Clamp(seconds, 0, RecordingLength);
            if (PlaybackTime >= RecordingLength)
                Playing = false;
            Changed?.Invoke();
        }

        public void AdvanceRecording(double seconds)
        {
            if (!Playing || !double.IsFinite(seconds) || seconds <= 0)
                return;
            int frame = (int)(PlaybackTime * 2);
            PlaybackTime = Math.Min(RecordingLength, PlaybackTime + Math.Min(seconds, .25));
            if (PlaybackTime >= RecordingLength)
                Playing = false;
            if (!Playing || (int)(PlaybackTime * 2) != frame)
                Changed?.Invoke();
        }

        public void AdvanceStationPlayback(double seconds)
        {
            if (Verified && double.IsFinite(seconds) && seconds > 0)
                StationPlaybackTime = (StationPlaybackTime + Math.Min(.25, seconds)) % RecordingLength;
        }

        public bool Open(DetectiveEvidence id)
        {
            if (!IsAvailable(id))
                return false;
            opened |= IdBit(id);
            selected[(int)Find(id).Source] = (int)id;
            Changed?.Invoke();
            return true;
        }

        public bool Collect(DetectiveEvidence id)
        {
            if (!IsOpened(id))
                return false;
            collected |= IdBit(id);
            Changed?.Invoke();
            return true;
        }

        public bool Place(int slot, DetectiveEvent fact)
        {
            if (Verified || !ValidSlot(slot) || !ValidSlot((int)fact))
                return false;
            if (timeline[slot] == (int)fact)
                return true;
            int previous = Array.IndexOf(timeline, (int)fact);
            if (previous != slot && previous >= 0)
            {
                timeline[previous] = -1;
                citations[previous] = -1;
            }
            timeline[slot] = (int)fact;
            citations[slot] = -1;
            ResetFeedback();
            return true;
        }

        public bool Cite(int slot, DetectiveEvidence id)
        {
            if (Verified || !ValidSlot(slot) || timeline[slot] < 0 || !IsCollected(id))
                return false;
            citations[slot] = (int)id;
            ResetFeedback();
            return true;
        }

        public bool SetDate(int day, DetectiveEvidence primary, DetectiveEvidence reported)
        {
            if (Verified || day < 16 || day > 19 || !IsCollected(primary) || !IsCollected(reported) || primary == reported)
                return false;
            IncidentDay = day;
            DateEvidence = (int)primary;
            ReportEvidence = (int)reported;
            ResetFeedback();
            return true;
        }

        public bool ChooseDay(int day)
        {
            if (Verified || day < 16 || day > 19)
                return false;
            IncidentDay = day;
            ResetFeedback();
            return true;
        }

        public bool ChooseDateSource(bool officialReport, DetectiveEvidence id)
        {
            if (Verified || !IsCollected(id))
                return false;
            if (officialReport)
            {
                ReportEvidence = (int)id;
                if (DateEvidence == ReportEvidence)
                    DateEvidence = -1;
            }
            else
            {
                DateEvidence = (int)id;
                if (DateEvidence == ReportEvidence)
                    ReportEvidence = -1;
            }
            ResetFeedback();
            return true;
        }

        public bool HasProgress => opened != 0 || collected != 0 || timeline.Any(value => value >= 0) ||
            IncidentDay != 0 || Verified || HighContrast;

        public DetectiveResult Submit()
        {
            if (Verified)
                return Result;
            Result = Evaluate();
            if (Result == DetectiveResult.Verified)
            {
                Verified = true;
                CaseVerified?.Invoke();
            }
            Changed?.Invoke();
            return Result;
        }

        public bool RecoverKey()
        {
            if (!Verified || KeyRecovered)
                return false;
            KeyRecovered = true;
            KeyCollected?.Invoke();
            Changed?.Invoke();
            return true;
        }

        private DetectiveResult Evaluate()
        {
            if (timeline.Any(value => value < 0) || citations.Any(value => value < 0) ||
                IncidentDay == 0 || DateEvidence < 0 || ReportEvidence < 0)
                return DetectiveResult.Incomplete;
            for (int slot = 0; slot < Slots; slot++)
                if (timeline[slot] != slot)
                    return DetectiveResult.ChronologyConflict;
            for (int slot = 0; slot < Slots; slot++)
                if (!Supports((DetectiveEvent)timeline[slot], (DetectiveEvidence)citations[slot]))
                    return DetectiveResult.UnsupportedEvidence;
            bool primaryRecord = (DetectiveEvidence)DateEvidence is DetectiveEvidence.ArchivePlayback or DetectiveEvidence.SensorRegister;
            if (IncidentDay != 17 || !primaryRecord || ReportEvidence != (int)DetectiveEvidence.PublicReport)
                return DetectiveResult.DateConflict;
            return DetectiveResult.Verified;
        }

        private static bool Supports(DetectiveEvent fact, DetectiveEvidence id) => fact switch
        {
            DetectiveEvent.SealArchive => id is DetectiveEvidence.EngineerMail or DetectiveEvidence.CorridorPhoto,
            DetectiveEvent.Quarantine => id is DetectiveEvidence.ArchivePlayback or DetectiveEvidence.ShiftNotebook,
            DetectiveEvent.RemoteSignature => id is DetectiveEvidence.ArchivePlayback or DetectiveEvidence.SensorRegister,
            DetectiveEvent.PublishReport => id is DetectiveEvidence.PublicReport or DetectiveEvidence.CustodyMemo,
            _ => false
        };

        private void ResetFeedback()
        {
            Result = DetectiveResult.Incomplete;
            Changed?.Invoke();
        }

        private static int IdBit(DetectiveEvidence id) => (int)id >= 0 && (int)id < Count ? 1 << (int)id : 0;
        private static bool ValidSlot(int slot) => slot >= 0 && slot < Slots;

        public DetectiveData Capture() => new()
        {
            opened = opened, collected = collected, timeline = (int[])timeline.Clone(), citations = (int[])citations.Clone(),
            incidentDay = IncidentDay, dateEvidence = DateEvidence, reportEvidence = ReportEvidence,
            verified = Verified, keyRecovered = KeyRecovered, result = Result, highContrast = HighContrast,
            selected = (int[])selected.Clone(), playing = Playing, playbackTime = PlaybackTime, stationPlaybackTime = StationPlaybackTime
        };

        public static DetectiveSession Restore(DetectiveData data)
        {
            SaveValidation.Require(data != null && data.version == 1 && data.timeline?.Length == Slots &&
                data.citations?.Length == Slots && data.selected?.Length == 5 && Enum.IsDefined(typeof(DetectiveResult), data.result),
                "Invalid detective state");
            SaveValidation.Require(((data.opened | data.collected) & ~DocumentBits) == 0 &&
                (data.collected & ~data.opened) == 0 &&
                (data.verified || (data.opened & IdBit(DetectiveEvidence.IdentityProtocol)) == 0) &&
                (!data.keyRecovered || data.verified), "Invalid detective documents");
            var session = new DetectiveSession
            {
                opened = data.opened, collected = data.collected, IncidentDay = data.incidentDay,
                DateEvidence = data.dateEvidence, ReportEvidence = data.reportEvidence,
                Verified = data.verified, KeyRecovered = data.keyRecovered, Result = data.result, HighContrast = data.highContrast, Playing = data.playing, PlaybackTime = data.playbackTime, StationPlaybackTime = data.stationPlaybackTime
            };
            SaveValidation.Require(data.timeline.All(value => value == -1 || ValidSlot(value)) &&
                data.timeline.Where(value => value >= 0).Distinct().Count() == data.timeline.Count(value => value >= 0),
                "Duplicate or unknown detective event");
            for (int slot = 0; slot < Slots; slot++)
                SaveValidation.Require(data.citations[slot] == -1 ||
                    data.timeline[slot] >= 0 && session.IsCollected((DetectiveEvidence)data.citations[slot]),
                    "Unknown detective citation");
            SaveValidation.Require((data.incidentDay == 0 || data.incidentDay >= 16 && data.incidentDay <= 19) &&
                (data.dateEvidence == -1 || session.IsCollected((DetectiveEvidence)data.dateEvidence)) &&
                (data.reportEvidence == -1 || session.IsCollected((DetectiveEvidence)data.reportEvidence)) &&
                (data.dateEvidence == -1 || data.dateEvidence != data.reportEvidence), "Invalid date comparison");
            SaveValidation.Require(double.IsFinite(data.playbackTime) && data.playbackTime >= 0 && data.playbackTime <= RecordingLength &&
                (data.playbackTime == 0 && !data.playing || session.IsOpened(DetectiveEvidence.ArchivePlayback)) &&
                (!data.playing || data.playbackTime < RecordingLength), "Invalid archive playback state");
            SaveValidation.Require(double.IsFinite(data.stationPlaybackTime) &&
                data.stationPlaybackTime >= 0 && data.stationPlaybackTime < RecordingLength &&
                (data.verified || data.stationPlaybackTime == 0), "Invalid monitor playback state");
            for (int index = 0; index < session.selected.Length; index++)
            {
                int id = data.selected[index];
                var document = Find((DetectiveEvidence)id);
                SaveValidation.Require(id == -1 || document != null && (int)document.Source == index &&
                    session.IsOpened(document.Id), "Invalid open detective document");
            }
            Array.Copy(data.selected, session.selected, session.selected.Length);
            Array.Copy(data.timeline, session.timeline, Slots);
            Array.Copy(data.citations, session.citations, Slots);
            SaveValidation.Require(data.verified == (data.result == DetectiveResult.Verified) &&
                (data.result == DetectiveResult.Incomplete || data.result == session.Evaluate()), "Invalid detective verdict");
            return session;
        }
    }
}
