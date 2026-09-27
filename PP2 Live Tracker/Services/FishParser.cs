using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PP2_Live_Tracker.Services
{
    public static class FishParser
    {
        private static readonly HashSet<string> KnownFishNames =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Ahven",
                "Harjus",
                "Harmaanieriä",
                "Hauki",
                "Isosimppu",
                "Kiiski",
                "Kirjolohi",
                "Kivinilkka",
                "Kolmipiikki",
                "Kuha",
                "Kuore",
                "Lahna",
                "Lohi",
                "Made",
                "Miekkasärki",
                "Muikku",
                "Pasuri",
                "Puronieriä",
                "Rautu",
                "Ruutana",
                "Salakka",
                "Särki",
                "Säyne",
                "Seipi",
                "Siika",
                "Silakka",
                "Sorva",
                "Sulkava",
                "Suutari",
                "Taimen",
                "Toutain",
                "Turpa"
            };

        // Hyväksytään esimerkiksi:
        //
        // Ahven 35g  -> 35
        // Ahven 35c  -> 35
        // Ahven 35&  -> 35
        // Ahven 356  -> 35
        // Ahven AAg  -> 44
        // Ahven AOg  -> 40
        // Kirjolohi 160gg -> 1606
        //
        // Painon ja yksikön välissä ei saa olla välilyöntiä.
        // Siksi esimerkiksi "Ahven 35 g" hylätään.
        private static readonly Regex FishRegex = new(
            @"((?:Miek\.\s*)?[A-Za-zÄÖÅäöå\.]+)\s+([0-9AaOoGg]+)([gc&6])(?![A-Za-z0-9])",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant |
            RegexOptions.Compiled);

        // Pelkkä kalan nimi omalla OCR-rivillään.
        private static readonly Regex FishNameOnlyRegex = new(
            @"^\s*([A-Za-zÄÖÅäöå\.]+)\s*$",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant |
            RegexOptions.Compiled);

        // Pelkkä paino omalla OCR-rivillään.
        //
        // Hyväksytään:
        // 44g
        // AAg
        // AOg
        // 160gg
        //
        // Hylätään:
        // 44 g
        private static readonly Regex WeightOnlyRegex = new(
            @"^\s*([0-9AaOoGg]+)([gc&6])\s*$",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant |
            RegexOptions.Compiled);

        /// <summary>
        /// Vanha tekstipohjainen parseri.
        /// Palauttaa esimerkiksi "Ahven 35 g".
        /// </summary>
        public static string? ParseFish(string ocrText)
        {
            if (string.IsNullOrWhiteSpace(ocrText))
                return null;

            string? lastFish = null;

            string[] lines = ocrText.Split(
                new[] { "\r\n", "\n", "\r" },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (string originalLine in lines)
            {
                string line =
                    NormalizeLine(originalLine);

                if (ShouldIgnoreLine(line))
                    continue;

                MatchCollection matches =
                    FishRegex.Matches(line);

                foreach (Match match in matches)
                {
                    if (!TryCreateFishResult(
                            match,
                            out string fishName,
                            out int weight))
                    {
                        continue;
                    }

                    lastFish =
                        $"{fishName} {weight} g";
                }
            }

            return lastFish;
        }

        /// <summary>
        /// Parsii Tesseractin palauttamat tekstirivit.
        ///
        /// Tukee sekä yhden rivin ilmoitusta:
        /// Ahven 44g
        ///
        /// että kahdelle OCR-riville jakautunutta ilmoitusta:
        /// Ahven
        /// 44g
        /// </summary>
        public static ParsedFishResult? ParseFishLine(
            IEnumerable<OcrTextLine> lines)
        {
            if (lines == null)
                return null;

            List<OcrTextLine> sourceLines = lines
                .Where(line =>
                    line != null &&
                    !string.IsNullOrWhiteSpace(line.Text))
                .OrderBy(line => line.Bounds.Top)
                .ThenBy(line => line.Bounds.Left)
                .ToList();

            ParsedFishResult? lastResult = null;

            // Ensin etsitään tavalliset yhden rivin ilmoitukset.
            foreach (OcrTextLine sourceLine in sourceLines)
            {
                string line =
                    NormalizeLine(sourceLine.Text);

                if (ShouldIgnoreLine(line))
                    continue;

                MatchCollection matches =
                    FishRegex.Matches(line);

                foreach (Match match in matches)
                {
                    if (!TryCreateFishResult(
                            match,
                            out string fishName,
                            out int ocrWeight))
                    {
                        continue;
                    }

                    lastResult =
                        new ParsedFishResult
                        {
                            FishName = fishName,
                            OcrWeight = ocrWeight,
                            SourceLine = sourceLine
                        };
                }
            }

            // Jos tavallinen ilmoitus löytyi,
            // käytetään sitä.
            if (lastResult != null)
                return lastResult;

            // Jos ilmoitusta ei löytynyt yhdeltä riviltä,
            // yritetään yhdistää kalan nimi ja paino
            // eri OCR-riveiltä.
            for (int fishIndex = 0;
                 fishIndex < sourceLines.Count;
                 fishIndex++)
            {
                OcrTextLine fishSourceLine =
                    sourceLines[fishIndex];

                string fishLineText =
                    NormalizeLine(fishSourceLine.Text);

                if (ShouldIgnoreLine(fishLineText))
                    continue;

                Match fishNameMatch =
                    FishNameOnlyRegex.Match(fishLineText);

                if (!fishNameMatch.Success)
                    continue;

                string rawFishName =
                    fishNameMatch.Groups[1].Value;

                string correctedFishName =
                    FishNameCorrector.Correct(rawFishName);

                if (!KnownFishNames.Contains(
                        correctedFishName))
                {
                    continue;
                }

                OcrTextLine? bestWeightLine = null;
                int bestDistance = int.MaxValue;

                for (int weightIndex = 0;
                     weightIndex < sourceLines.Count;
                     weightIndex++)
                {
                    if (weightIndex == fishIndex)
                        continue;

                    OcrTextLine weightSourceLine =
                        sourceLines[weightIndex];

                    string weightLineText =
                        NormalizeLine(
                            weightSourceLine.Text);

                    Match weightMatch =
                        WeightOnlyRegex.Match(
                            weightLineText);

                    if (!weightMatch.Success)
                        continue;

                    if (!TryParseOcrWeight(
                            weightMatch.Groups[1].Value,
                            out _))
                    {
                        continue;
                    }

                    if (!IsLikelyMatchingWeightLine(
                            fishSourceLine,
                            weightSourceLine,
                            out int distance))
                    {
                        continue;
                    }

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestWeightLine = weightSourceLine;
                    }
                }

                if (bestWeightLine == null)
                    continue;

                string bestWeightText =
                    NormalizeLine(
                        bestWeightLine.Text);

                Match bestWeightMatch =
                    WeightOnlyRegex.Match(
                        bestWeightText);

                if (!bestWeightMatch.Success)
                    continue;

                if (!TryParseOcrWeight(
                        bestWeightMatch.Groups[1].Value,
                        out int weight))
                {
                    continue;
                }

                if (weight < 8 ||
                    weight > 15000)
                {
                    continue;
                }

                Rectangle combinedBounds =
                    Rectangle.Union(
                        fishSourceLine.Bounds,
                        bestWeightLine.Bounds);

                OcrTextLine combinedSourceLine =
                    new OcrTextLine
                    {
                        Text =
                            $"{fishSourceLine.Text} " +
                            $"{bestWeightLine.Text}",

                        Bounds = combinedBounds
                    };

                lastResult =
                    new ParsedFishResult
                    {
                        FishName = correctedFishName,
                        OcrWeight = weight,
                        SourceLine = combinedSourceLine
                    };
            }

            return lastResult;
        }

        /// <summary>
        /// Tarkistaa, ovatko kalan nimirivi ja painorivi
        /// riittävän lähellä toisiaan kuuluakseen
        /// samaan ilmoitukseen.
        /// </summary>
        private static bool IsLikelyMatchingWeightLine(
            OcrTextLine fishLine,
            OcrTextLine weightLine,
            out int distance)
        {
            distance = int.MaxValue;

            Rectangle fishBounds =
                fishLine.Bounds;

            Rectangle weightBounds =
                weightLine.Bounds;

            int averageHeight =
                Math.Max(
                    1,
                    (fishBounds.Height +
                     weightBounds.Height) / 2);

            int verticalDistance;

            if (weightBounds.Top >=
                fishBounds.Top)
            {
                verticalDistance =
                    weightBounds.Top -
                    fishBounds.Bottom;
            }
            else
            {
                verticalDistance =
                    fishBounds.Top -
                    weightBounds.Bottom;
            }

            verticalDistance =
                Math.Max(
                    0,
                    verticalDistance);

            int maximumVerticalDistance =
                averageHeight * 2;

            if (verticalDistance >
                maximumVerticalDistance)
            {
                return false;
            }

            int fishCenterX =
                fishBounds.Left +
                fishBounds.Width / 2;

            int weightCenterX =
                weightBounds.Left +
                weightBounds.Width / 2;

            int horizontalDistance =
                Math.Abs(
                    fishCenterX -
                    weightCenterX);

            int maximumHorizontalDistance =
                Math.Max(
                    fishBounds.Width * 2,
                    averageHeight * 6);

            if (horizontalDistance >
                maximumHorizontalDistance)
            {
                return false;
            }

            distance =
                verticalDistance * 10 +
                horizontalDistance;

            return true;
        }

        private static bool TryCreateFishResult(
            Match match,
            out string fishName,
            out int weight)
        {
            fishName = string.Empty;
            weight = 0;

            if (match == null ||
                !match.Success)
            {
                return false;
            }

            string rawFishName =
                match.Groups[1].Value;

            fishName =
                FishNameCorrector.Correct(
                    rawFishName);

            if (!KnownFishNames.Contains(
                    fishName))
            {
                return false;
            }

            string rawWeightText =
                match.Groups[2].Value;

            if (!TryParseOcrWeight(
                    rawWeightText,
                    out weight))
            {
                return false;
            }

            if (weight < 8 ||
                weight > 15000)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Muuttaa painon OCR-virheet numeroiksi.
        ///
        /// A tai a -> 4
        /// O tai o -> 0
        /// g tai G -> 6
        ///
        /// Esimerkiksi:
        /// A4 -> 44
        /// AO -> 40
        /// 160g -> 1606
        /// </summary>
        private static bool TryParseOcrWeight(
            string rawWeightText,
            out int weight)
        {
            weight = 0;

            if (string.IsNullOrWhiteSpace(
                    rawWeightText))
            {
                return false;
            }

            StringBuilder correctedDigits =
                new StringBuilder(
                    rawWeightText.Length);

            foreach (char character
                     in rawWeightText)
            {
                char correctedCharacter =
                    character switch
                    {
                        'A' or 'a' => '4',
                        'O' or 'o' => '0',
                        'G' or 'g' => '6',
                        _ => character
                    };

                if (!char.IsDigit(
                        correctedCharacter))
                {
                    return false;
                }

                correctedDigits.Append(
                    correctedCharacter);
            }

            if (correctedDigits.Length < 1 ||
                correctedDigits.Length > 5)
            {
                return false;
            }

            return int.TryParse(
                correctedDigits.ToString(),
                out weight);
        }

        private static string NormalizeLine(
            string line)
        {
            if (string.IsNullOrWhiteSpace(
                    line))
            {
                return string.Empty;
            }

            string normalized =
                line.Trim();

            normalized =
                Regex.Replace(
                    normalized,
                    @"[ \t]+",
                    " ");

            return normalized;
        }

        private static bool ShouldIgnoreLine(
            string line)
        {
            if (string.IsNullOrWhiteSpace(
                    line))
            {
                return true;
            }

            return
                line.Contains(
                    "Suurin kala",
                    StringComparison.OrdinalIgnoreCase) ||

                line.Contains(
                    "Sijoitus",
                    StringComparison.OrdinalIgnoreCase) ||

                line.Contains(
                    "ennätyks",
                    StringComparison.OrdinalIgnoreCase) ||

                line.Contains(
                    "järven",
                    StringComparison.OrdinalIgnoreCase);
        }
    }
}