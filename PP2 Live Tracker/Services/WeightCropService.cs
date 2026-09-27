using System;
using System.Collections.Generic;
using System.Drawing;

namespace PP2_Live_Tracker.Services
{
    internal static class WeightCropService
    {
        public static Bitmap CropWeight(Bitmap source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (source.Width <= 0 || source.Height <= 0)
                throw new ArgumentException("Lähdekuva on tyhjä.", nameof(source));

            // Jätetään kuvan alin osa tutkimatta, jotta ilmoituksen
            // alareunan tumma viiva ei sotke tekstin löytämistä.
            int scanHeight = Math.Max(1, (int)(source.Height * 0.80));

            bool[] activeColumns = new bool[source.Width];

            // Etsitään sarakkeet, joissa on tummaa tekstiä.
            for (int x = 0; x < source.Width; x++)
            {
                int darkPixelCount = 0;

                for (int y = 0; y < scanHeight; y++)
                {
                    Color pixel = source.GetPixel(x, y);

                    int brightness =
                        (pixel.R + pixel.G + pixel.B) / 3;

                    if (brightness < 190)
                    {
                        darkPixelCount++;

                        // Muutama tumma pikseli riittää osoittamaan,
                        // että sarakkeessa on tekstiä.
                        if (darkPixelCount >= 2)
                        {
                            activeColumns[x] = true;
                            break;
                        }
                    }
                }
            }

            List<(int Start, int End)> textGroups = FindTextGroups(activeColumns);

            if (textGroups.Count == 0)
            {
                return CreateFallbackCrop(source);
            }

            // Paino on ilmoituksen oikeanpuoleisin tekstiryhmä,
            // esimerkiksi "38g" tai "112g".
            var weightGroup = textGroups[^1];

            int paddingX = Math.Max(2, source.Width / 100);
            int paddingY = Math.Max(2, source.Height / 20);

            int left = Math.Max(0, weightGroup.Start - paddingX);
            int right = Math.Min(source.Width, weightGroup.End + paddingX + 1);

            int top = FindTop(source, left, right, scanHeight);
            int bottom = FindBottom(source, left, right, scanHeight);

            top = Math.Max(0, top - paddingY);
            bottom = Math.Min(source.Height - 1, bottom + paddingY);

            int width = right - left;
            int height = bottom - top + 1;

            if (width <= 0 || height <= 0)
            {
                return CreateFallbackCrop(source);
            }

            Rectangle area = new Rectangle(
                left,
                top,
                width,
                height);

            return source.Clone(area, source.PixelFormat);
        }

        private static List<(int Start, int End)> FindTextGroups(
            bool[] activeColumns)
        {
            List<(int Start, int End)> characterRuns = new();

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
                    characterRuns.Add((start, x - 1));
                    start = -1;
                }
            }

            if (start != -1)
            {
                characterRuns.Add((start, activeColumns.Length - 1));
            }

            if (characterRuns.Count == 0)
                return new List<(int Start, int End)>();

            // Yhdistetään lähekkäiset merkit sanoiksi.
            // Suurempi väli tarkoittaa kalan nimen ja painon välistä tyhjää tilaa.
            int wordGap = Math.Max(5, activeColumns.Length / 50);

            List<(int Start, int End)> groups = new();

            int groupStart = characterRuns[0].Start;
            int groupEnd = characterRuns[0].End;

            for (int i = 1; i < characterRuns.Count; i++)
            {
                int gap = characterRuns[i].Start - groupEnd - 1;

                if (gap <= wordGap)
                {
                    groupEnd = characterRuns[i].End;
                }
                else
                {
                    groups.Add((groupStart, groupEnd));

                    groupStart = characterRuns[i].Start;
                    groupEnd = characterRuns[i].End;
                }
            }

            groups.Add((groupStart, groupEnd));

            return groups;
        }

        private static int FindTop(
            Bitmap source,
            int left,
            int right,
            int scanHeight)
        {
            for (int y = 0; y < scanHeight; y++)
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
            int right,
            int scanHeight)
        {
            for (int y = scanHeight - 1; y >= 0; y--)
            {
                for (int x = left; x < right; x++)
                {
                    if (IsDark(source.GetPixel(x, y)))
                        return y;
                }
            }

            return scanHeight - 1;
        }

        private static bool IsDark(Color pixel)
        {
            int brightness =
                (pixel.R + pixel.G + pixel.B) / 3;

            return brightness < 190;
        }

        private static Bitmap CreateFallbackCrop(Bitmap source)
        {
            // Vararatkaisu: otetaan oikeanpuoleinen 45 % kuvasta.
            int left = (int)(source.Width * 0.55);

            Rectangle area = new Rectangle(
                left,
                0,
                source.Width - left,
                source.Height);

            return source.Clone(area, source.PixelFormat);
        }
    }
}