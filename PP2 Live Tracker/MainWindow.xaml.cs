using PP2_Live_Tracker.Helpers;
using PP2_Live_Tracker.Models;
using PP2_Live_Tracker.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Media;
using OpenCvSharp;
using System.Linq;

namespace PP2_Live_Tracker
{
    public partial class MainWindow : System.Windows.Window
    {
        private readonly List<FishCatch> _fishHistory = new();
        private string? _lastFish;

        private DateTime _lastFishAddedAt =
    DateTime.MinValue;

        private readonly DispatcherTimer _timer = new DispatcherTimer();

        private readonly OcrService _ocrService;

        private readonly UnitRecognizer _unitRecognizer;

        private static readonly bool DebugOcr = false;

        private string _selectedGameMode = "Kaikki lajit";

        private bool _ocrCalibrationMode = false;

        private System.Windows.Point _cropStartPoint;
        private System.Windows.Point _cropEndPoint;

        private System.Windows.Shapes.Rectangle? _calibrationRectangle;

        private string? _lastOcrText;
        private int _sameTextCount;

        private static readonly Dictionary<string, int> MinimumFishWeights = new()
{
    { "Kuha", 400 },
    { "Taimen", 650 }
};

        public MainWindow()
        {
            InitializeComponent();

            _ocrService = new OcrService();

            _unitRecognizer = new UnitRecognizer();

            SettingsService.LoadCropSettings();

            _timer.Interval = TimeSpan.FromMilliseconds(300);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private static Mat ConvertBitmapToMat(Bitmap bitmap)
        {
            using MemoryStream stream = new();

            bitmap.Save(stream, ImageFormat.Png);

            return Cv2.ImDecode(
                stream.ToArray(),
                ImreadModes.Color);
        }


        private void StartButton_Click(object sender, RoutedEventArgs e)
        {

            SetStatus(System.Windows.Media.Brushes.Gold, "Etsitään Pro Pilkki 2");

            IntPtr gameWindow = WindowFinder.FindProPilkkiWindow();

            if (gameWindow != IntPtr.Zero)
            {
                SetStatus(System.Windows.Media.Brushes.LimeGreen, "Pro Pilkki 2 löytyi");
            }
            else
            {
                SetStatus(System.Windows.Media.Brushes.Red, "Peliä ei löytynyt");
            }
        }
        private static bool TryParseRecognizedWeight(
    string recognizedWeight,
    out int weight)
        {
            weight = 0;

            if (string.IsNullOrWhiteSpace(recognizedWeight))
                return false;

            recognizedWeight =
                recognizedWeight.Trim().ToLowerInvariant();

            if (!recognizedWeight.EndsWith("g"))
                return false;

            string numberPart =
                recognizedWeight[..^1];

            if (numberPart.Length < 1 ||
                numberPart.Length > 5)
            {
                return false;
            }

            foreach (char character in numberPart)
            {
                if (!char.IsDigit(character))
                    return false;
            }

            if (!int.TryParse(numberPart, out weight))
                return false;

            return weight > 0 && weight <= 100000;
        }
        private void NewCompetitionButton_Click(object sender, RoutedEventArgs e)
        {
            StartNewCompetition();
        }

        private bool IsFishAllowedForGameMode(
    string fishName)
        {
            return _selectedGameMode switch
            {
                "Kaikki lajit" =>
                    true,

                "Normaali" =>
                    !fishName.Equals(
                        "Kiiski",
                        StringComparison.OrdinalIgnoreCase),

                "Vain ahven" =>
                    fishName.Equals(
                        "Ahven",
                        StringComparison.OrdinalIgnoreCase),

                "Vain made" =>
                    fishName.Equals(
                        "Made",
                        StringComparison.OrdinalIgnoreCase),

                "Vain kuha" =>
                    fishName.Equals(
                        "Kuha",
                        StringComparison.OrdinalIgnoreCase),

                "Vain kiiski" =>
                    fishName.Equals(
                        "Kiiski",
                        StringComparison.OrdinalIgnoreCase),

                "Vain hauki" =>
                    fishName.Equals(
                        "Hauki",
                        StringComparison.OrdinalIgnoreCase),

                "Vain pasuri" =>
                    fishName.Equals(
                        "Pasuri",
                        StringComparison.OrdinalIgnoreCase),

                "Vain siika" =>
                    fishName.Equals(
                        "Siika",
                        StringComparison.OrdinalIgnoreCase),

                "Kirjo ja taimen" =>
                    fishName.Equals(
                        "Kirjolohi",
                        StringComparison.OrdinalIgnoreCase) ||
                    fishName.Equals(
                        "Taimen",
                        StringComparison.OrdinalIgnoreCase),

                "Vain särkikalat" =>
                    IsCyprinidFish(fishName),

                // Näissä kaikki lajit voidaan ensin ottaa historiaan.
                // Tilastoihin jätetään myöhemmin vain suurimmat 3 tai 5.
                "3 suurinta kalaa" =>
                    true,

                "5 suurinta kalaa" =>
                    true,

                _ =>
                    true
            };
        }

        private static bool IsCyprinidFish(
    string fishName)
        {
            return fishName.Equals(
                       "Lahna",
                       StringComparison.OrdinalIgnoreCase) ||
                   fishName.Equals(
                       "Miekkasärki",
                       StringComparison.OrdinalIgnoreCase) ||
                   fishName.Equals(
                       "Pasuri",
                       StringComparison.OrdinalIgnoreCase) ||
                   fishName.Equals(
                       "Ruutana",
                       StringComparison.OrdinalIgnoreCase) ||
                   fishName.Equals(
                       "Salakka",
                       StringComparison.OrdinalIgnoreCase) ||
                   fishName.Equals(
                       "Särki",
                       StringComparison.OrdinalIgnoreCase) ||
                   fishName.Equals(
                       "Säyne",
                       StringComparison.OrdinalIgnoreCase) ||
                   fishName.Equals(
                       "Seipi",
                       StringComparison.OrdinalIgnoreCase) ||
                   fishName.Equals(
                       "Sorva",
                       StringComparison.OrdinalIgnoreCase) ||
                   fishName.Equals(
                       "Sulkava",
                       StringComparison.OrdinalIgnoreCase) ||
                   fishName.Equals(
                       "Suutari",
                       StringComparison.OrdinalIgnoreCase) ||
                   fishName.Equals(
                       "Toutain",
                       StringComparison.OrdinalIgnoreCase) ||
                   fishName.Equals(
                       "Turpa",
                       StringComparison.OrdinalIgnoreCase);
        }

        private void GameModeComboBox_SelectionChanged(
    object sender,
    SelectionChangedEventArgs e)
        {
            if (GameModeComboBox.SelectedItem
                is ComboBoxItem selectedItem &&
                selectedItem.Content is string selectedMode)
            {
                _selectedGameMode = selectedMode;
            }
        }

        private void StartNewCompetition()
        {
            _fishHistory.Clear();

            LogList.Items.Clear();

            _lastFish = null;
            _lastOcrText = null;
            _sameTextCount = 0;

            FishNameText.Text = "Ei kalaa";
            FishWeightText.Text = "0 g";

            UpdateStatistics();

            SetStatus(System.Windows.Media.Brushes.LimeGreen, "Uusi kilpailu aloitettu");
        }
        private void ResetCalibrationButton_Click(object sender, RoutedEventArgs e)
        {
            CropService.CropX = CropService.DefaultCropX;
            CropService.CropY = CropService.DefaultCropY;
            CropService.CropWidth = CropService.DefaultCropWidth;
            CropService.CropHeight = CropService.DefaultCropHeight;

            SettingsService.SaveCropSettings();

            StatusText.Text = "OCR-alue palautettu oletusarvoihin.";
        }

        private static bool IsLikelySameWeightEnding(
    string fullDigits,
    string recognizedDigits)
        {
            if (string.IsNullOrWhiteSpace(fullDigits) ||
                string.IsNullOrWhiteSpace(recognizedDigits))
            {
                return false;
            }

            if (recognizedDigits.Length > fullDigits.Length)
                return false;

            string fullEnding =
                fullDigits[^recognizedDigits.Length..];

            int differences = 0;

            for (int i = 0; i < recognizedDigits.Length; i++)
            {
                if (fullEnding[i] != recognizedDigits[i])
                {
                    differences++;

                    if (differences > 1)
                        return false;
                }
            }

            return true;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            var rect = WindowFinder.GetGameWindowRect();

            if (rect == null)
            {
                SetStatus(
                    System.Windows.Media.Brushes.Red,
                    "Peliä ei löytynyt");

                PreviewImage.Source = null;
                WeightPreviewImage.Source = null;

                return;
            }

            int width =
                rect.Value.Right - rect.Value.Left;

            int height =
                rect.Value.Bottom - rect.Value.Top;

            if (!_ocrCalibrationMode)
            {
                SetStatus(
                    System.Windows.Media.Brushes.LimeGreen,
                    "Seuranta käynnissä");
            }

            Bitmap? bitmap = null;
            Bitmap? processedBitmap = null;
            Bitmap? fishLineBitmap = null;
            Bitmap? weightBitmap = null;
            Bitmap? digitsBitmap = null;

            try
            {
                if (_ocrCalibrationMode)
                {
                    bitmap =
                        ScreenCaptureService.CaptureWindowBitmap(
                            rect.Value.Left,
                            rect.Value.Top,
                            width,
                            height);
                }
                else
                {
                    var area =
                        CropService.GetFishNotificationArea(
                            width,
                            height);

                    bitmap =
                        ScreenCaptureService.CaptureWindowBitmap(
                            rect.Value.Left + area.X,
                            rect.Value.Top + area.Y,
                            area.Width,
                            area.Height);
                }

                PreviewImage.Source =
                    BitmapToImageSource(bitmap);

                if (_ocrCalibrationMode)
                    return;

                processedBitmap =
                    ImagePreprocessor.Process(bitmap);

                // Luetaan OCR-rivit sekä niiden sijainnit kuvassa.
                List<OcrTextLine> ocrLines =
                    _ocrService.ReadLines(processedBitmap);

                // Debug: tulostetaan kaikki OCR:n löytämät rivit.
                if (DebugOcr)
                {
                    if (ocrLines.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "OCR ei löytänyt yhtään tekstiriviä.");
                    }
                    else
                    {
                        foreach (OcrTextLine line in ocrLines)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"OCR-rivi: \"{line.Text}\"");
                        }
                    }
                }

                // Etsitään oikean kalailmoituksen sisältävä tekstirivi.
                ParsedFishResult? parsed =
                    FishParser.ParseFishLine(ocrLines);

                if (parsed == null)
                {
                    if (DebugOcr)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "FishParser ei löytänyt kelvollista kalaa.");
                    }

                    return;
                }

                if (DebugOcr)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Parser hyväksyi: {parsed.FishName} {parsed.OcrWeight}g");
                }

                System.Drawing.Rectangle bounds =
                    parsed.SourceLine.Bounds;

                // Lisätään tekstirivin ympärille reilusti marginaalia.
                // OCR:n antama rajaus voi leikata numeroiden ylä- tai alareunaa.
                int paddingX = Math.Max(
                    4,
                    bounds.Width / 10);

                int paddingY = Math.Max(
                    4,
                    bounds.Height / 2);

                int left = Math.Max(
                    0,
                    bounds.Left - paddingX);

                int top = Math.Max(
                    0,
                    bounds.Top - paddingY);

                int right = Math.Min(
                    processedBitmap.Width,
                    bounds.Right + paddingX);

                int bottom = Math.Min(
                    processedBitmap.Height,
                    bounds.Bottom + paddingY);

                int lineWidth =
                    right - left;

                int lineHeight =
                    bottom - top;

                if (lineWidth <= 0 ||
                    lineHeight <= 0)
                {
                    return;
                }

                // Rajataan kuvasta vain löydetty kalailmoitusrivi.
                fishLineBitmap =
                    processedBitmap.Clone(
new System.Drawing.Rectangle(
    left,
    top,
    lineWidth,
    lineHeight),
                        processedBitmap.PixelFormat);

                weightBitmap =
                    WeightCropService.CropWeight(
                        fishLineBitmap);

                // Tarkistetaan koko painokuvasta, onko viimeinen merkki g.
                char recognizedUnit =
                    _unitRecognizer.RecognizeLastCharacter(
                        weightBitmap);

                if (recognizedUnit != 'g')
                {
                    if (DebugOcr)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"HYLÄTTY YKSIKKÖTUNNISTUKSESSA: " +
                            $"kala={parsed.FishName}, " +
                            $"parserPaino={parsed.OcrWeight}, " +
                            $"tunnistettuYksikkö={recognizedUnit}, " +
                            $"rivi=\"{parsed.SourceLine.Text}\"");
                    }

                    return;
                }

                // Poistetaan painon viimeinen merkki, eli g.
                digitsBitmap =
                    WeightDigitsCropService.CropDigits(
                        weightBitmap);

                // Debug-esikatselussa näkyvät vain numerot.
                WeightPreviewImage.Source =
                    BitmapToImageSource(digitsBitmap);

                string recognizedDigits =
                    _ocrService.ReadDigits(digitsBitmap);

                if (string.IsNullOrWhiteSpace(
                        recognizedDigits))
                {
                    return;
                }

                if (recognizedDigits.Length > 5)
                    return;

                string parserDigits =
                    parsed.OcrWeight.ToString();

                string finalDigits = parserDigits;

                // Molemmat OCR:t tunnistivat saman painon.
                if (recognizedDigits == parserDigits)
                {
                    finalDigits = recognizedDigits;
                }
                // Laaja OCR on voinut lukea g-kirjaimen ylimääräiseksi
                // numeroksi 6 ennen varsinaista yksikköä.
                //
                // Esimerkiksi:
                // oikea paino:      42g
                // parserDigits:     426
                // recognizedDigits: 2
                //
                // Lopputulos: 42
                //
                // Jos pienempi OCR päättyy numeroon 6, kyseessä voi olla
                // oikeasti kuutoseen päättyvä paino, kuten 1606g.
                // Silloin viimeistä kuutosta ei poisteta.
                else if (
                    recognizedUnit == 'g' &&
                    parserDigits.EndsWith(
                        "6",
                        StringComparison.Ordinal) &&
                    IsLikelySameWeightEnding(
                        parserDigits[..^1],
                        recognizedDigits))
                {
                    // Laaja OCR luki g-kirjaimen ylimääräiseksi
                    // painon loppuun lisätyksi numeroksi 6.
                    //
                    // Esimerkiksi:
                    // parserDigits:     1566
                    // recognizedDigits: 56
                    // Lopputulos:       156
                    //
                    // Tai:
                    // parserDigits:     1696
                    // recognizedDigits: 59
                    // Lopputulos:       169
                    finalDigits = parserDigits[..^1];
                }
                // Muissa ristiriitatilanteissa käytetään laajaa OCR:ää,
                // jotta tarkasta numerorajauksesta mahdollisesti puuttuva
                // vasemman reunan numero säilyy.
                else
                {
                    if (DebugOcr)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"OCR-ristiriita: Kala={parsed.FishName}, " +
                            $"parser={parserDigits}, " +
                            $"digits={recognizedDigits}");
                    }

                    finalDigits = parserDigits;
                }

                if (DebugOcr)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Lopullinen OCR: Kala={parsed.FishName}, " +
                        $"parser={parserDigits}, " +
                        $"digits={recognizedDigits}, " +
                        $"unit={recognizedUnit}, " +
                        $"final={finalDigits}");
                }

                if (!int.TryParse(
                        finalDigits,
                        out int weight))
                {
                    if (DebugOcr)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"HYLÄTTY: int.TryParse epäonnistui ({finalDigits})");
                    }

                    return;
                }

                if (weight < 8 ||
                    weight > 100000)
                {
                    if (DebugOcr)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"HYLÄTTY: painoraja ({weight} g)");
                    }

                    return;
                }

                string fishName =
                    parsed.FishName;

                string currentRecognition =
                    $"{fishName}|{weight}";

                if (currentRecognition ==
                    _lastOcrText)
                {
                    _sameTextCount++;
                }
                else
                {
                    _sameTextCount = 0;
                    _lastOcrText =
                        currentRecognition;
                }

                if (_sameTextCount < 1)
                {
                    if (DebugOcr)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"HYLÄTTY VAKAUTUKSESSA: {fishName} {weight}g, " +
                            $"sameTextCount={_sameTextCount}");
                    }

                    return;
                }

                if (!IsFishAllowedForGameMode(fishName))
                {
                    if (DebugOcr)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"HYLÄTTY PELIMUODON VUOKSI: " +
                            $"{fishName} {weight}g, " +
                            $"tila={_selectedGameMode}");
                    }

                    return;
                }

                string normalizedFish =
                    $"{fishName} {weight}";

                TimeSpan timeSinceLastFish =
                    DateTime.Now - _lastFishAddedAt;

                if (normalizedFish == _lastFish &&
                    timeSinceLastFish.TotalSeconds < 8)
                {
                    if (DebugOcr)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"HYLÄTTY KAKSOISKIRJAUKSENA: " +
                            $"{normalizedFish}, " +
                            $"edellisestä={timeSinceLastFish.TotalSeconds:F1}s");
                    }

                    return;
                }

                if (DebugOcr)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"LISÄTÄÄN LOKIIN: {fishName} {weight}g");
                }

                if (MinimumFishWeights.TryGetValue(
        fishName,
        out int minimumWeight) &&
    weight < minimumWeight)
                {
                    if (DebugOcr)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"HYLÄTTY ALAMITAN VUOKSI: {fishName} {weight}g (< {minimumWeight}g)");
                    }

                    return;
                }

                _lastFish = normalizedFish;
                _lastFishAddedAt = DateTime.Now;

                FishNameText.Text =
                    fishName;

                FishWeightText.Text =
                    $"{weight} g";

                FishCatch catchData =
                    new FishCatch
                    {
                        Name = fishName,
                        Weight = weight,
                        Time = DateTime.Now
                    };

                _fishHistory.Insert(0, catchData);

                LogList.Items.Insert(0, catchData);

                UpdateStatistics();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Timer_Tick-virhe: {ex}");
            }
            finally
            {
                digitsBitmap?.Dispose();
                weightBitmap?.Dispose();
                fishLineBitmap?.Dispose();
                processedBitmap?.Dispose();
                bitmap?.Dispose();
            }
        }

        private void CalibrationButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            _ocrCalibrationMode = true;

            var rect =
                WindowFinder.GetGameWindowRect();

            if (rect == null)
            {
                SetStatus(
                    System.Windows.Media.Brushes.Red,
                    "Peliä ei löytynyt");

                return;
            }

            int width =
                rect.Value.Right - rect.Value.Left;

            int height =
                rect.Value.Bottom - rect.Value.Top;

            using Bitmap fullScreen =
                ScreenCaptureService.CaptureWindowBitmap(
                    rect.Value.Left,
                    rect.Value.Top,
                    width,
                    height);

            PreviewImage.Source =
                BitmapToImageSource(fullScreen);

            StatusText.Text =
                "Kalibrointi päällä - rajaa OCR-alue vetämällä";
        }

        private void CalibrationCanvas_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (!_ocrCalibrationMode)
                return;

            _cropStartPoint =
                e.GetPosition(CalibrationCanvas);

            _cropEndPoint =
                _cropStartPoint;

            CalibrationCanvas.Children.Clear();

            _calibrationRectangle =
                new System.Windows.Shapes.Rectangle
                {
                    Stroke =
                        System.Windows.Media.Brushes.Red,

                    StrokeThickness = 2
                };

            CalibrationCanvas.Children.Add(
                _calibrationRectangle);

            Canvas.SetLeft(
                _calibrationRectangle,
                _cropStartPoint.X);

            Canvas.SetTop(
                _calibrationRectangle,
                _cropStartPoint.Y);

            StatusText.Text =
                "Valitse OCR-alue vetämällä laatikko";
        }

        private void CalibrationCanvas_MouseMove(
            object sender,
            MouseEventArgs e)
        {
            if (!_ocrCalibrationMode)
                return;

            if (e.LeftButton !=
                    MouseButtonState.Pressed ||
                _calibrationRectangle == null)
            {
                return;
            }

            _cropEndPoint =
                e.GetPosition(CalibrationCanvas);

            double x =
                Math.Min(
                    _cropStartPoint.X,
                    _cropEndPoint.X);

            double y =
                Math.Min(
                    _cropStartPoint.Y,
                    _cropEndPoint.Y);

            double width =
                Math.Abs(
                    _cropEndPoint.X -
                    _cropStartPoint.X);

            double height =
                Math.Abs(
                    _cropEndPoint.Y -
                    _cropStartPoint.Y);

            Canvas.SetLeft(
                _calibrationRectangle,
                x);

            Canvas.SetTop(
                _calibrationRectangle,
                y);

            _calibrationRectangle.Width =
                width;

            _calibrationRectangle.Height =
                height;
        }

        private void CalibrationCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_ocrCalibrationMode)
                return;

            _cropEndPoint = e.GetPosition(CalibrationCanvas);

            double x = Math.Min(_cropStartPoint.X, _cropEndPoint.X);
            double y = Math.Min(_cropStartPoint.Y, _cropEndPoint.Y);

            double width = Math.Abs(_cropEndPoint.X - _cropStartPoint.X);
            double height = Math.Abs(_cropEndPoint.Y - _cropStartPoint.Y);


            if (width < 10 || height < 10)
            {
                SetStatus(System.Windows.Media.Brushes.Red, "Alue liian pieni");
                return;
            }


            // Muutetaan PreviewImagen koordinaatit suhteellisiksi arvoiksi
            double canvasWidth = CalibrationCanvas.ActualWidth;
            double canvasHeight = CalibrationCanvas.ActualHeight;


            CropService.CropX = x / canvasWidth;
            CropService.CropY = y / canvasHeight;

            CropService.CropWidth = width / canvasWidth;
            CropService.CropHeight = height / canvasHeight;

            SettingsService.SaveCropSettings();


            _ocrCalibrationMode = false;

            CalibrationCanvas.Children.Clear();
            _calibrationRectangle = null;


            StatusText.Text =
                $"OCR alue tallennettu: {CropService.CropWidth:0.000} × {CropService.CropHeight:0.000}";
        }
        private void UpdateStatistics()
        {
            List<FishCatch> countedFish;

            if (_selectedGameMode == "3 suurinta kalaa")
            {
                countedFish = _fishHistory
                    .OrderByDescending(fish => fish.Weight)
                    .Take(3)
                    .ToList();
            }
            else if (_selectedGameMode == "5 suurinta kalaa")
            {
                countedFish = _fishHistory
                    .OrderByDescending(fish => fish.Weight)
                    .Take(5)
                    .ToList();
            }
            else
            {
                countedFish =
                    _fishHistory.ToList();
            }

            int count =
                countedFish.Count;

            int totalWeight =
                countedFish.Sum(fish => fish.Weight);

            FishCatch? biggest =
                countedFish
                    .OrderByDescending(fish => fish.Weight)
                    .FirstOrDefault();

            FishCountText.Text =
                $"Kaloja: {count}";

            TotalWeightText.Text =
                $"Paino: {totalWeight} g";

            if (biggest != null)
            {
                BiggestFishText.Text =
                    $"Suurin kala: {biggest.Name} {biggest.Weight} g";
            }
            else
            {
                BiggestFishText.Text =
                    "Suurin kala: -";
            }
        }
        private BitmapSource BitmapToImageSource(Bitmap bitmap)
        {
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
        private void SetStatus(System.Windows.Media.Brush color, string text)
        {
            StatusIndicator.Fill = color;
            StatusText.Text = text;
        }

    }
}