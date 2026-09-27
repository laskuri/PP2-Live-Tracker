namespace PP2_Live_Tracker.Services
{
    public class ParsedFishResult
    {
        public string FishName { get; init; } = string.Empty;

        public int OcrWeight { get; init; }

        public OcrTextLine SourceLine { get; init; } = new();
    }
}