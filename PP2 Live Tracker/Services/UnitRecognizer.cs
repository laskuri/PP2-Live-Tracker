using OpenCvSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PP2_Live_Tracker.Services
{
    internal class UnitRecognizer
    {
        private readonly GlyphSegmenter _glyphSegmenter;

        // false = ei debug-tulostuksia
        // true = debug-tulostukset käyttöön
        private static readonly bool DebugOcr = false;

        // Kuinka monta pikseliä alempaa g:n yläreunan
        // pitää alkaa numeroihin verrattuna.
        private const int GTopDifferencePixels = 2;

        // Kuinka paljon g:n ja numeroiden alareunat
        // saavat erota toisistaan.
        private const int BottomTolerancePixels = 2;

        // Suurin sallittu pikseliväli viimeisen numeron
        // ja painoyksikön välillä.
        private const int MaxUnitGapPixels = 5;

        public UnitRecognizer()
        {
            _glyphSegmenter = new GlyphSegmenter();
        }

        public char RecognizeLastCharacter(
            System.Drawing.Bitmap weightImage)
        {
            if (weightImage == null)
                return '?';

            using MemoryStream stream =
                new MemoryStream();

            weightImage.Save(
                stream,
                System.Drawing.Imaging.ImageFormat.Png);

            byte[] imageBytes =
                stream.ToArray();

            using Mat weightMat =
                Cv2.ImDecode(
                    imageBytes,
                    ImreadModes.Color);

            if (weightMat.Empty())
                return '?';

            return RecognizeLastCharacter(
                weightMat);
        }

        public char RecognizeLastCharacter(
            Mat weightImage)
        {
            if (weightImage == null ||
                weightImage.Empty())
            {
                return '?';
            }

            var glyphs =
                _glyphSegmenter.Segment(
                    weightImage);

            try
            {
                if (glyphs.Count < 2)
                    return '?';

                SegmentedGlyph previousGlyph =
                    glyphs[^2];

                SegmentedGlyph lastGlyph =
                    glyphs[^1];

                int previousRight =
                    previousGlyph.Bounds.X +
                    previousGlyph.Bounds.Width;

                int unitGap =
                    lastGlyph.Bounds.X -
                    previousRight;

                if (DebugOcr)
                {
                    Debug.WriteLine(
                        $"Numero-yksikkö-väli: {unitGap} px");
                }

                // Esimerkiksi "124 g" hylätään.
                if (unitGap >
                    MaxUnitGapPixels)
                {
                    if (DebugOcr)
                    {
                        Debug.WriteLine(
                            "Viimeinen merkki hylättiin suuren välin vuoksi.");
                    }

                    return '?';
                }

                // Otetaan kaikki viimeistä merkkiä
                // edeltävät merkit.
                var digitGlyphs =
                    glyphs
                        .Take(glyphs.Count - 1)
                        .ToList();

                // Numeroiden tavallinen yläreuna.
                double averageDigitTop =
                    digitGlyphs.Average(
                        glyph =>
                            glyph.Bounds.Y);

                // Numeroiden tavallinen alareuna.
                double averageDigitBottom =
                    digitGlyphs.Average(
                        glyph =>
                            glyph.Bounds.Y +
                            glyph.Bounds.Height);

                int lastTop =
                    lastGlyph.Bounds.Y;

                int lastBottom =
                    lastGlyph.Bounds.Y +
                    lastGlyph.Bounds.Height;

                bool startsLower =
                    lastTop >=
                    averageDigitTop +
                    GTopDifferencePixels;

                bool sameBottom =
                    Math.Abs(
                        lastBottom -
                        averageDigitBottom)
                    <= BottomTolerancePixels;

                if (DebugOcr)
                {
                    Debug.WriteLine(
                        $"Viimeinen merkki: " +
                        $"yläreuna={lastTop}, " +
                        $"alareuna={lastBottom}, " +
                        $"numeroiden yläreuna={averageDigitTop:F1}, " +
                        $"numeroiden alareuna={averageDigitBottom:F1}, " +
                        $"alkaa alempaa={startsLower}, " +
                        $"sama alareuna={sameBottom}");
                }

                // PP2-fontissa g alkaa numeroita alempaa,
                // mutta sen alareuna on lähes samalla tasolla.
                if (startsLower &&
                    sameBottom)
                {
                    if (DebugOcr)
                    {
                        Debug.WriteLine(
                            "Viimeinen merkki tunnistettiin g:ksi sijainnin perusteella.");
                    }

                    return 'g';
                }

                return '?';
            }
            finally
            {
                foreach (SegmentedGlyph glyph in glyphs)
                {
                    glyph.Image.Dispose();
                }
            }
        }
    }
}