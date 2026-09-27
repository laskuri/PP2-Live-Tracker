namespace PP2_Live_Tracker.Models
{
    public class FishCatch
    {
        public string Name { get; set; } = "";
        public int Weight { get; set; }
        public DateTime Time { get; set; }

        public override string ToString()
        {
            return $"{Time:HH:mm:ss} - {Name} {Weight} g";
        }
    }
}