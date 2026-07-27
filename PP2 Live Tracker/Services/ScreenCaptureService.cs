using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace PP2_Live_Tracker.Services
{
    public static class ScreenCaptureService
    {
        public static BitmapSource CaptureWindow(int left, int top, int width, int height)
        {
            using Bitmap bitmap = new Bitmap(width, height);

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(
                    left,
                    top,
                    0,
                    0,
                    new Size(width, height));
            }

            using MemoryStream stream = new MemoryStream();

            bitmap.Save(stream, ImageFormat.Png);

            stream.Position = 0;

            BitmapImage image = new BitmapImage();

            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();

            return image;

        }
        public static BitmapSource CaptureArea(
    int windowLeft,
    int windowTop,
    int windowWidth,
    int windowHeight)
        {
            var crop = CropService.GetFishNotificationArea(windowWidth, windowHeight);

            return CaptureWindow(
                windowLeft + crop.X,
                windowTop + crop.Y,
                crop.Width,
                crop.Height);
        }
    }
}