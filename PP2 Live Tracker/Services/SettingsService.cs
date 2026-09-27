using System;
using System.IO;
using System.Text.Json;

namespace PP2_Live_Tracker.Services
{
    public static class SettingsService
    {
        private static string FilePath =>
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "settings.json");


        public class CropSettings
        {
            public double CropX { get; set; }
            public double CropY { get; set; }
            public double CropWidth { get; set; }
            public double CropHeight { get; set; }
        }


        public static void SaveCropSettings()
        {
            CropSettings settings = new CropSettings
            {
                CropX = CropService.CropX,
                CropY = CropService.CropY,
                CropWidth = CropService.CropWidth,
                CropHeight = CropService.CropHeight
            };


            string json = JsonSerializer.Serialize(
                settings,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });


            File.WriteAllText(FilePath, json);
        }


        public static void LoadCropSettings()
        {
            if (!File.Exists(FilePath))
                return;


            string json = File.ReadAllText(FilePath);

            CropSettings? settings =
                JsonSerializer.Deserialize<CropSettings>(json);


            if (settings == null)
                return;


            CropService.CropX = settings.CropX;
            CropService.CropY = settings.CropY;
            CropService.CropWidth = settings.CropWidth;
            CropService.CropHeight = settings.CropHeight;
        }
    }
}