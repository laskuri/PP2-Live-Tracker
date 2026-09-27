using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PP2_Live_Tracker.Services
{
    public static class FishNameCorrector
    {
        private static readonly List<string> FishNames = new()
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

        public static string Correct(string ocrName)
        {
            if (string.IsNullOrWhiteSpace(ocrName))
                return ocrName;

            string normalizedName =
                NormalizeName(ocrName);

            // Pelin lyhenteet ja niiden tavallisimmat OCR-virheet.
            if (IsNieriäAbbreviation(
                    normalizedName,
                    'H'))
            {
                return "Harmaanieriä";
            }

            if (IsNieriäAbbreviation(
                    normalizedName,
                    'P'))
            {
                return "Puronieriä";
            }

            if (IsMiekkasärkiAbbreviation(
                    normalizedName))
            {
                return "Miekkasärki";
            }

            var result = FishNames
                .Select(name => new
                {
                    Name = name,
                    Distance = Distance(
                        NormalizeName(name),
                        normalizedName)
                })
                .OrderBy(item => item.Distance)
                .First();

            if (result.Distance <= 3)
                return result.Name;

            return ocrName.Trim();
        }

        private static bool IsNieriäAbbreviation(
            string normalizedName,
            char initial)
        {
            if (string.IsNullOrWhiteSpace(
                    normalizedName))
            {
                return false;
            }

            char normalizedInitial =
                char.ToUpperInvariant(initial);

            if (char.ToUpperInvariant(
                    normalizedName[0]) !=
                normalizedInitial)
            {
                return false;
            }

            // Poistetaan lyhenteen erottimet.
            //
            // Esimerkiksi:
            // P.NIERIÄ  -> NIERIA
            // P-NIERIN  -> NIERIN
            // P NIIERIÄ -> NIIERIA
            string remainder =
                normalizedName[1..]
                    .Replace(".", string.Empty)
                    .Replace("-", string.Empty)
                    .Replace(" ", string.Empty);

            // Verrataan tavalliseen muotoon NIERIA.
            // Sallitaan enintään kaksi OCR-virhettä.
            //
            // Hyväksytään esimerkiksi:
            // NIERIA
            // NIERIH
            // NIERIN
            // NIERIK
            // NIIERIA
            // NIERI
            int distance =
                Distance(
                    remainder,
                    "NIERIA");

            return distance <= 2;
        }

        private static bool IsMiekkasärkiAbbreviation(
            string normalizedName)
        {
            string compactName =
                normalizedName
                    .Replace(".", string.Empty)
                    .Replace("-", string.Empty)
                    .Replace(" ", string.Empty);

            // Hyväksytään esimerkiksi:
            // MIEK.SÄRKI
            // MIEK.SARKI
            // MIEKSÄRKI
            // MIEKSARKI
            // MIEK.SÄRKl
            //
            // NormalizeName muuttaa Ä:n A:ksi.
            if (!compactName.StartsWith(
                    "MIEK",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            int distance =
                Distance(
                    compactName,
                    "MIEKSARKI");

            return distance <= 2;
        }

        private static string NormalizeName(
            string name)
        {
            string normalized =
                name.Trim()
                    .ToUpperInvariant();

            normalized =
                normalized
                    .Replace('Ä', 'A')
                    .Replace('Ö', 'O')
                    .Replace('Å', 'A');

            StringBuilder cleaned =
                new StringBuilder(
                    normalized.Length);

            foreach (char character in normalized)
            {
                if (char.IsLetter(character) ||
                    character == '.' ||
                    character == '-' ||
                    character == ' ')
                {
                    cleaned.Append(character);
                }
            }

            return cleaned.ToString();
        }

        private static int Distance(
            string first,
            string second)
        {
            int[,] matrix =
                new int[
                    first.Length + 1,
                    second.Length + 1];

            for (int i = 0;
                 i <= first.Length;
                 i++)
            {
                matrix[i, 0] = i;
            }

            for (int j = 0;
                 j <= second.Length;
                 j++)
            {
                matrix[0, j] = j;
            }

            for (int i = 1;
                 i <= first.Length;
                 i++)
            {
                for (int j = 1;
                     j <= second.Length;
                     j++)
                {
                    int cost =
                        first[i - 1] ==
                        second[j - 1]
                            ? 0
                            : 1;

                    matrix[i, j] =
                        Math.Min(
                            Math.Min(
                                matrix[i - 1, j] + 1,
                                matrix[i, j - 1] + 1),
                            matrix[i - 1, j - 1] +
                            cost);
                }
            }

            return matrix[
                first.Length,
                second.Length];
        }
    }
}