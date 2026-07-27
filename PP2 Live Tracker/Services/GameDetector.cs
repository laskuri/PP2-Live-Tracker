using System.Diagnostics;

namespace PP2_Live_Tracker.Services
{
    public class GameDetector
    {
        public bool IsGameRunning()
        {
            Process[] processes = Process.GetProcessesByName("Propilkki2");

            return processes.Length > 0;
        }
    }
}