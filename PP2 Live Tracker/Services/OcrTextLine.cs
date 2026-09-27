using System.Drawing;

namespace PP2_Live_Tracker.Services
{
    public class OcrTextLine
    {
        public string Text { get; init; } = string.Empty;

        public Rectangle Bounds { get; init; }
    }
}