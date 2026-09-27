using System.Drawing;

namespace PP2_Live_Tracker.Services
{
    public static class CropService
    {
        // Oletusarvot
        public const double DefaultCropX = 0.49;
        public const double DefaultCropY = 0.04;
        public const double DefaultCropWidth = 0.12;
        public const double DefaultCropHeight = 0.16;

        // Käytössä olevat arvot (näitä kalibrointi muuttaa)
        public static double CropX = DefaultCropX;
        public static double CropY = DefaultCropY;
        public static double CropWidth = DefaultCropWidth;
        public static double CropHeight = DefaultCropHeight;

        public static Rectangle GetFishNotificationArea(int windowWidth, int windowHeight)
        {
            int x = (int)(windowWidth * CropX);
            int y = (int)(windowHeight * CropY);

            int width = (int)(windowWidth * CropWidth);
            int height = (int)(windowHeight * CropHeight);

            return new Rectangle(x, y, width, height);
        }
    }
}