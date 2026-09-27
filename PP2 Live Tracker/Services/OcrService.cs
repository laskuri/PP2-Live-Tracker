using System;
using System.Drawing;
using System.IO;
using Tesseract;
using System.Collections.Generic;

namespace PP2_Live_Tracker.Services
{
    internal class OcrService : IDisposable
    {
        private readonly TesseractEngine _textEngine;
        private readonly TesseractEngine _digitEngine;

        public OcrService()
        {
            string tessDataPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Assets",
                "tessdata");

            _textEngine = new TesseractEngine(
                tessDataPath,
                "fin",
                EngineMode.Default);

            _digitEngine = new TesseractEngine(
                tessDataPath,
                "eng",
                EngineMode.Default);

            // Numerotunnistin saa palauttaa vain numeroita.
            _digitEngine.SetVariable(
                "tessedit_char_whitelist",
                "0123456789");
        }

        public string ReadText(Bitmap bitmap)
        {
            using Pix pix = PixConverter.ToPix(bitmap);

            using Page page = _textEngine.Process(
                pix,
                PageSegMode.SparseText);

            return page.GetText().Trim();
        }
        public List<OcrTextLine> ReadLines(Bitmap bitmap)
        {
            List<OcrTextLine> lines = new();

            using Pix pix =
                PixConverter.ToPix(bitmap);

            using Page page =
                _textEngine.Process(
                    pix,
                    PageSegMode.SparseText);

            using ResultIterator iterator =
                page.GetIterator();

            iterator.Begin();

            do
            {
                string? text =
                    iterator.GetText(
                        PageIteratorLevel.TextLine);

                if (string.IsNullOrWhiteSpace(text))
                    continue;

                if (!iterator.TryGetBoundingBox(
                        PageIteratorLevel.TextLine,
                        out Rect bounds))
                {
                    continue;
                }

                lines.Add(
                    new OcrTextLine
                    {
                        Text = text.Trim(),

                        Bounds = new Rectangle(
                            bounds.X1,
                            bounds.Y1,
                            bounds.X2 - bounds.X1,
                            bounds.Y2 - bounds.Y1)
                    });
            }
            while (iterator.Next(
                PageIteratorLevel.TextLine));

            return lines;
        }
        public string ReadDigits(Bitmap bitmap)
        {
            using Pix pix =
                PixConverter.ToPix(bitmap);

            using Page page =
                _digitEngine.Process(
                    pix,
                    PageSegMode.SingleWord);

            string result =
                page.GetText().Trim();

            return KeepOnlyDigits(result);
        }

        private static string KeepOnlyDigits(string text)
        {
            char[] digits = new char[text.Length];
            int count = 0;

            foreach (char character in text)
            {
                if (char.IsDigit(character))
                {
                    digits[count] = character;
                    count++;
                }
            }

            return new string(
                digits,
                0,
                count);
        }

        public void Dispose()
        {
            _textEngine.Dispose();
            _digitEngine.Dispose();
        }
    }
}