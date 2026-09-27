using OpenCvSharp;

namespace PP2_Live_Tracker.Services
{
    internal class SegmentedGlyph
    {
        public Mat Image { get; set; } = new Mat();

        public Rect Bounds { get; set; }
    }
}