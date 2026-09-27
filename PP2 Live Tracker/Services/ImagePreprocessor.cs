using System.Drawing;
using System.Drawing.Imaging;

namespace PP2_Live_Tracker.Services
{
    public static class ImagePreprocessor
    {
        public static Bitmap Process(Bitmap original)
        {
            // Suurennetaan kuva 2-kertaiseksi
            Bitmap enlarged = new Bitmap(
                original.Width * 2,
                original.Height * 2);

            using (Graphics graphics = Graphics.FromImage(enlarged))
            {
                graphics.DrawImage(
                    original,
                    new Rectangle(
                        0,
                        0,
                        enlarged.Width,
                        enlarged.Height));
            }

            // Muutetaan harmaasävyksi
            Bitmap gray = new Bitmap(
                enlarged.Width,
                enlarged.Height);

            for (int x = 0; x < enlarged.Width; x++)
            {
                for (int y = 0; y < enlarged.Height; y++)
                {
                    Color pixel = enlarged.GetPixel(x, y);

                    int value =
                        (pixel.R + pixel.G + pixel.B) / 3;

                    Color grayColor =
                        Color.FromArgb(
                            value,
                            value,
                            value);

                    gray.SetPixel(x, y, grayColor);
                }
            }

            enlarged.Dispose();

            return gray;
        }
    }
}