using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace PP2_Live_Tracker.Services
{
    internal class GlyphSegmenter
    {
        // false = ei debug-tulostuksia eikä debug-kuvia
        // true = debug käyttöön
        private static readonly bool DebugOcr = false;

        public List<SegmentedGlyph> Segment(Mat image)
        {
            List<SegmentedGlyph> glyphs = new();

            // Harmaasävy
            using Mat gray = image.Channels() == 1
                ? image.Clone()
                : image.CvtColor(ColorConversionCodes.BGR2GRAY);

            // Mustavalkoinen
            using Mat binary = new();

            Cv2.Threshold(
                gray,
                binary,
                0,
                255,
                ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

            // Etsitään ulkoiset ääriviivat
            Cv2.FindContours(
                binary,
                out Point[][] contours,
                out _,
                RetrievalModes.External,
                ContourApproximationModes.ApproxSimple);

            List<Rect> rects = new();

            foreach (Point[] contour in contours)
            {
                Rect rect = Cv2.BoundingRect(contour);

                // Suodatetaan pienet roskat pois
                if (rect.Width < 6)
                    continue;

                if (rect.Height < 15)
                    continue;

                rects.Add(rect);
            }

            // Vasemmalta oikealle
            rects.Sort((a, b) => a.X.CompareTo(b.X));

            string? debugFolder = null;

            if (DebugOcr)
            {
                debugFolder = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "DebugGlyphs");

                Directory.CreateDirectory(debugFolder);
            }

            int i = 0;

            foreach (Rect rect in rects)
            {
                Mat glyph = new Mat(binary, rect).Clone();

                if (DebugOcr)
                {
                    Debug.WriteLine(
                        $"Glyph {i}: X={rect.X} Y={rect.Y} W={rect.Width} H={rect.Height}");

                    string fileName = Path.Combine(
                        debugFolder!,
                        $"segment_{i}.png");

                    Debug.WriteLine(fileName);

                    Cv2.ImWrite(fileName, glyph);
                }

                glyphs.Add(new SegmentedGlyph
                {
                    Image = glyph,
                    Bounds = rect
                });

                i++;
            }

            return glyphs;
        }
    }
}