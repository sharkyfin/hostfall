using Hostfall.Simulation.Computers;
using UnityEngine;

namespace Hostfall.UI
{
    public static class NeuroArchiveMedia
    {
        private const int Columns = 4, Rows = 3;
        private static Texture2D picture;
        public static Texture2D Picture => picture != null ? picture : picture = Resources.Load<Texture2D>("Investigation/ArchiveS08");
        public static Rect Frame(int frame)
        {
            int index = Mathf.Clamp(frame, 0, DetectiveSession.RecordingFrames - 1);
            return new Rect((float)(index % Columns) / Columns, (float)(Rows - 1 - index / Columns) / Rows,
                1f / Columns, 1f / Rows);
        }
    }
}
