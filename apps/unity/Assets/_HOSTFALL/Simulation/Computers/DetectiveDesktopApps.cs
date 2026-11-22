using System;
using System.Collections.Generic;

namespace Hostfall.Simulation.Computers
{
    public static class DetectiveDesktopApps
    {
        private static readonly (string id, string title, DetectiveSource? source)[] tools =
        {
            ("files", "Архив", DetectiveSource.Files),
            ("mail", "Почта", DetectiveSource.Mail),
            ("photos", "Кадры", DetectiveSource.Photos),
            ("notes", "Заметки", DetectiveSource.Notes),
            ("recordings", "Записи", DetectiveSource.Recordings),
            ("settings", "Экран", null)
        };

        public static IReadOnlyList<(string id, string title, DetectiveSource? source)> Tools => tools;
        public static bool IsTool(string id) => Array.Exists(tools, tool => tool.id == id);
        public static string Title(string id) => Array.Find(tools, tool => tool.id == id).title;
        public static DetectiveSource? Source(string id) => Array.Find(tools, tool => tool.id == id).source;
    }
}
