using System;
using System.Collections.Generic;
using System.Linq;

namespace Hostfall.Simulation
{
    [Serializable] public sealed class TerminalLineData { public string text, style; }
    [Serializable] public sealed class TerminalMemoryData
    {
        public DeviceId host;
        public bool initialized;
        public string draft = "";
        public string[] history = Array.Empty<string>();
        public TerminalLineData[] lines = Array.Empty<TerminalLineData>();
        public float scroll;
    }
    public sealed class TerminalMemory
    {
        private readonly List<TerminalLineData> lines = new();
        private readonly List<string> history = new();
        public bool Initialized { get; set; }
        public DeviceId Host { get; set; }
        public string Draft { get; set; } = "";
        public float Scroll { get; set; }
        public IReadOnlyList<TerminalLineData> Lines => lines;
        public IReadOnlyList<string> History => history;
        public int Revision { get; private set; }
        public TerminalMemory(DeviceId host) { Host = host; }
        public void AddLine(string text, string style)
        {
            if (text.Length > 8192) text = text.Substring(0,8192);
            lines.Add(new TerminalLineData { text = text, style = style });
            // Bound both the entry count and total text so three terminals fit safely into a save.
            int length = lines.Sum(l => l.text.Length);
            while (lines.Count > 200 || length > 65536) { length -= lines[0].text.Length; lines.RemoveAt(0); }
            Revision++;
        }
        public void AddCommand(string text) { history.Add(text); if (history.Count > 100) history.RemoveAt(0); }
        public void Clear() { lines.Clear(); Scroll = 0; Revision++; }
        // Unity can materialize a missing nested JSON object as an empty instance.
        public static bool IsLegacyEmpty(TerminalMemoryData data) => data==null ||
            ((uint)data.host==0 && !data.initialized && string.IsNullOrEmpty(data.draft) &&
             (data.history==null || data.history.Length==0) && (data.lines==null || data.lines.Length==0) && data.scroll==0);
        public TerminalMemoryData Capture() => new()
        { initialized=Initialized, host = Host, draft = Draft, scroll = Scroll, history = history.ToArray(), lines = lines.Select(l => new TerminalLineData { text = l.text, style = l.style }).ToArray() };
        public static TerminalMemory Restore(TerminalMemoryData data, DeviceId origin)
        {
            var memory = new TerminalMemory(origin); if (IsLegacyEmpty(data)) return memory;
            SaveValidation.Require(Enum.IsDefined(typeof(DeviceId),data.host),"Unknown terminal host");
            SaveValidation.Text(data.draft,4096,"Invalid terminal draft");
            SaveValidation.Require(data.history != null && data.history.Length <= 100 && data.lines != null && data.lines.Length <= 200,"Invalid terminal memory");
            SaveValidation.Require(!float.IsNaN(data.scroll) && !float.IsInfinity(data.scroll) && data.scroll >= 0,"Invalid terminal scroll");
            foreach (string line in data.history) SaveValidation.Text(line,4096,"Invalid terminal history");
            int length = 0;
            foreach (var line in data.lines)
            {
                SaveValidation.Require(line != null && new[] { "muted", "command-line", "result", "error" }.Contains(line.style),"Invalid terminal line");
                SaveValidation.Text(line.text,8192,"Invalid terminal output"); length += line.text.Length;
            }
            SaveValidation.Require(length <= 65536,"Terminal memory exceeds limit");
            memory.Initialized=data.initialized; memory.Host = data.host; memory.Draft = data.draft; memory.Scroll = data.scroll;
            memory.history.AddRange(data.history);
            memory.lines.AddRange(data.lines.Select(l => new TerminalLineData { text = l.style=="muted"&&l.text.StartsWith("Сеанс ",StringComparison.Ordinal)?l.text.Replace("\nhelp — команды с объяснениями",""):l.text, style = l.style })); return memory;
        }
    }
}
