using System;
using System.Collections.Generic;

namespace Hostfall.Simulation.Computers
{
    public static class VentflowArchive
    {
        public const string PhotographId = "handover-photo";
        public const string PhotographResource = "Investigation/VentflowHandover";

        public sealed class Entry
        {
            public readonly string Id, Name, Text;
            public readonly bool Hidden;

            public Entry(string id, string name, string text, bool hidden = false)
            {
                Id = id;
                Name = name;
                Text = text;
                Hidden = hidden;
            }
        }

        private static readonly Entry[] entries =
        {
            new("handover-letter", "handover.eml",
                "02:52 · От Левина дежурной смене\n\nСмена, оставьте вентиляцию на локальном реле. " +
                "Если сеть снова перестанет отвечать, аварийный пульт в операторской всё ещё сможет закрыть заслонку. " +
                "Сделайте контрольный снимок перед передачей смены."),
            new("unsent-letter", "draft_to_lee.eml",
                "03:09 · Черновик Левина для Ли\n\nПульт подтвердил подпись, хотя у стола никого не было. " +
                "Я отключил общую шину и оставил привод на местном питании. " +
                "Если журнал восстановится, сравните время записи с рапортом."),
            new(PhotographId, "handover_cam02.png",
                "03:08 · Камера 02 · Операторская\nНижняя часть кадра повреждена. Контрольный снимок сохранён до отключения общей шины."),
            new("relay-service", "relay_boundary.sys",
                "VENTFLOW / СЛУЖЕБНЫЕ ГРАНИЦЫ\n\nЛокальное реле отвечает на сервисный допуск. " +
                "Полномочия на общей шине отозваны. Следующий проверяемый узел — распределитель питания.\n\n" +
                "Последний читатель: подпись оператора подтверждена. Состояние рабочего места: пусто.", true)
        };

        public static IReadOnlyList<Entry> Entries { get; } = Array.AsReadOnly(entries);

        public static Entry Find(string id) => Array.Find(entries, entry => entry.Id == id);
    }
}
