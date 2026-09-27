using System;
using System.Collections.Generic;
using System.Drawing;

namespace PP2_Live_Tracker.Services
{
    internal static class WeightDigitsCropService
    {
        public static Bitmap CropDigits(Bitmap source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (source.Width <= 0 || source.Height <= 0)
                throw new ArgumentException(
                    "Painokuva on tyhjä.",
                    nameof(source));

            bool[] activeColumns = new bool[source.Width];

            // Etsitään sarakkeet, joissa on tummaa tekstiä.
            for (int x = 0; x < source.Width; x++)
            {
                int darkPixelCount = 0;

                for (int y = 0; y < source.Height; y++)
                {
                    Color pixel = source.GetPixel(x, y);

                    if (IsDark(pixel))
                    {
                        darkPixelCount++;

                        if (darkPixelCount >= 2)
                        {
                            activeColumns[x] = true;
                            break;
                        }
                    }
                }
            }

            List<(int Start, int End)> characters =
                FindCharacterRuns(activeColumns);

            // Tarvitaan vähintään yksi numero ja viimeinen g.
            if (characters.Count < 2)
                return source.Clone(
                    new Rectangle(
                        0,
                        0,
                        source.Width,
                        source.Height),
                    source.PixelFormat);

            // Viimeinen löydetty merkki jätetään pois.
            // Sen pitäisi olla g riippumatta siitä,
            // tunnistaisiko OCR sen g:ksi vai 6:ksi.
            int firstDigitX = characters[0].Start;
            int lastDigitX = characters[^2].End;

            int horizontalPadding =
                Math.Max(1, source.Width / 50);

            int left = Math.Max(
                0,
                firstDigitX - horizontalPadding);

            int right = Math.Min(
                source.Width,
                lastDigitX + horizontalPadding + 1);

            int top = FindTop(
                source,
                left,
                right);

            int bottom = FindBottom(
                source,
                left,
                right);

            int verticalPadding =
                Math.Max(1, source.Height / 15);

            top = Math.Max(
                0,
                top - verticalPadding);

            bottom = Math.Min(
                source.Height - 1,
                bottom + verticalPadding);

            int width = right - left;
            int height = bottom - top + 1;

            if (width <= 0 || height <= 0)
            {
                return source.Clone(
                    new Rectangle(
                        0,
                        0,
                        source.Width,
                        source.Height),
                    source.PixelFormat);
            }

            Rectangle area = new Rectangle(
                left,
                top,
                width,
                height);

            return source.Clone(
                area,
                source.PixelFormat);
        }

        private static List<(int Start, int End)> FindCharacterRuns(
            bool[] activeColumns)
        {
            List<(int Start, int End)> runs = new();

            int start = -1;

            for (int x = 0; x < activeColumns.Length; x++)
            {
                if (activeColumns[x])
                {
                    if (start == -1)
                        start = x;
                }
                else if (start != -1)
                {
                    runs.Add((start, x - 1));
                    start = -1;
                }
            }

            if (start != -1)
            {
                runs.Add((
                    start,
                    activeColumns.Length - 1));
            }

            return runs;
        }

        private static int FindTop(
            Bitmap source,
            int left,
            int right)
        {
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = left; x < right; x++)
                {
                    if (IsDark(source.GetPixel(x, y)))
                        return y;
                }
            }

            return 0;
        }

        private static int FindBottom(
            Bitmap source,
            int left,
            int right)
        {
            for (int y = source.Height - 1; y >= 0; y--)
            {
                for (int x = left; x < right; x++)
                {
                    if (IsDark(source.GetPixel(x, y)))
                        return y;
                }
            }

            return source.Height - 1;
        }

        private static bool IsDark(Color pixel)
        {
            int brightness =
                (pixel.R + pixel.G + pixel.B) / 3;

            return brightness < 190;
        }
    }
}