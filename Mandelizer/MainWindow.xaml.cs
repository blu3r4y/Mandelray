using System;
using System.Windows;
using System.Windows.Controls;
using System.Drawing;
using System.Windows.Input;
using Media = System.Windows.Media;
using System.Windows.Media.Imaging;
using Shapes = System.Windows.Shapes;
using System.IO;
using System.Windows.Threading;
using System.Threading;

namespace Mandelizer
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Variablen

        // Benötigt um einige Funktion erst nach vollständigem Laden des Fensters auszuführen
        bool WindowReady = false;

        // Anzahl der Farben die benutzt werden (zu Beginn Maximum setzen)
        public static int usedColors = 768;

        // Color Map
        public static Color[] MBColorMap = new Color[usedColors];

        // Mandelbrote
        Mandelbrot[] MandelbrotTemp = new Mandelbrot[1000];

        // Nummer des aktuellen Mandelbrots
        public static int MandelBrotId = -1;

        // Gerade betrachtetes Mandelbrot (-1 keines)
        public static int MandelBrotLookAt = -1;

        // Breite und Höhe des Bereichs der Mandelbrot Menge in Pixel
        public static int MandelbrotWidth;
        public static int MandelbrotHeight;

        // Größen-Verhältniswerte der Mandelbrotmenge
        double GausRatioXY, GausRatioYX;

        // Auswahlrechteck automatisch auf das Größenverhältnis anpassen?
        bool RatioZoom = true;

        // Während des Ladevorgangs die Mandelbrot Menge darstellen?
        public static bool LoadPreview = false;

        // Ladebalken benutzen?
        public static bool UseProgressBar = true;

        // Farbverlauf
        public static bool ColorGradientPercent = false;
        public static bool ColorGradientIterative = true;

        // Aktualisierung der Steuerelemnte beim Berechnen alle X Zeilen
        public static int RefreshUIElements = 20;

        // Wird ein Auswahlrechteck gezeichnet?
        bool isZooming = false;

        // Auswahlrechteck erstellen
        Shapes.Rectangle ZoomingRectangle = new Shapes.Rectangle();

        // Auswahlrechteck Start Punkt
        System.Windows.Point RectStartPoint = new System.Windows.Point();

        // Auswahlrechteck End Punkt
        System.Windows.Point RectEndPoint = new System.Windows.Point();

        // Standard Werte für erste Mandelbrot Menge
        double StdXmin = -2.5f, StdXmax = 1.5f;
        double StdYmin = -1.5f, StdYmax = 1.5f;

        // Berechnungs Thread
        Thread MandelBrotThread;

        #endregion

        #region Start Intialisierung

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Farben setzen
            SetColors.MultiColor1(ref MBColorMap);

            // Auswahlrechteck intialisieren
            ZoomingRectangle.Stroke = Media.Brushes.Black;
            ZoomingRectangle.StrokeThickness = 1;
            ZoomingRectangle.Fill = new Media.SolidColorBrush(Media.Color.FromArgb(150, 255, 255, 255));     // hellblau durchsichtig .FromArgb(45, 0, 0, 255)
            ZoomingRectangle.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            ZoomingRectangle.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            ZoomingRectangle.Height = 0;
            ZoomingRectangle.Width = 0;
            // Zum Fenster hinzufügen
            MandelBrotCanvas.Children.Add(ZoomingRectangle);

            // Funktionen freigeben, Fenster wurde vollständig geladen
            WindowReady = true;

            // MandelBrot zum Start zeichnen
            MandelBrotId++;
            MandelbrotTemp[MandelBrotId] = new Mandelbrot(StdXmin, StdXmax, StdYmin, StdYmax, 100, MandelBrotImage, StatusLabel, buttonNext, buttonPrevious, buttonBerechnen, ProgressBarMandelBrot);
            MandelbrotTemp[MandelBrotId].RefreshTextBoxes(textBoxXmin, textBoxXmax, textBoxYmin, textBoxYmax, textBoxIterationen);

            // Größen aktualisieren
            RefreshSize();

            MandelBrotThread = new Thread(new ThreadStart(MandelbrotTemp[MandelBrotId].CalculateAndDraw));
            MandelBrotThread.Start();
        }

        #endregion

        #region Buttons - Farben

        private void buttonReverseColors_Click(object sender, RoutedEventArgs e)
        {
            // Farben umdrehen
            SetColors.Reverse(ref MBColorMap);

            // Mandelbrot aktualisieren
            RefreshMandelBrot();
        }

        private void comboBoxColors_DropDownClosed(object sender, EventArgs e)
        {
            // Ausgewählte Farben setzen
            switch (comboBoxColors.SelectedIndex)
            {
                case 0: SetColors.GrayScale(ref MBColorMap); break;
                case 1: SetColors.MonoRed(ref MBColorMap); break;
                case 2: SetColors.MonoGreen(ref MBColorMap); break;
                case 3: SetColors.MonoBlue(ref MBColorMap); break;
                case 4: SetColors.MultiColor1(ref MBColorMap); break;
                case 5: SetColors.MultiColor2(ref MBColorMap); break;
            }

            // Mandelbrot aktualisieren
            RefreshMandelBrot();
        }

        #endregion

        #region Buttons - Navigation

        private void buttonStartPosition_Click(object sender, RoutedEventArgs e)
        {
            // Mandelbrot berechnen
            MandelBrotId++;
            MandelbrotTemp[MandelBrotId] = new Mandelbrot(StdXmin,
                StdXmax,
                StdYmin,
                StdYmax,
                Convert.ToInt32(textBoxIterationen.Text),
                MandelBrotImage, StatusLabel,
                buttonNext, buttonPrevious, buttonBerechnen,
                ProgressBarMandelBrot);

            MandelbrotTemp[MandelBrotId].RefreshTextBoxes(textBoxXmin, textBoxXmax, textBoxYmin, textBoxYmax, textBoxIterationen);

            MandelBrotThread.Abort();
            MandelBrotThread = new Thread(new ThreadStart(MandelbrotTemp[MandelBrotId].CalculateAndDraw));
            MandelBrotThread.Start();
        }

        private void buttonPrevious_Click(object sender, RoutedEventArgs e)
        {
            // Wenn das betrachtete MandelBrot keinen ungültigen Wert erreicht
            if ((MandelBrotLookAt - 1) >= 0)
            {
                // Aktuell betrachtendes MandelBrot um einen Wert zurücksetzen
                MandelBrotLookAt--;

                // Ein Schritt zurück ermöglicht wieder einen Schritt vor
                buttonNext.IsEnabled = true;

                // Mandelbrot berechnen
                MandelbrotTemp[MandelBrotLookAt] = new Mandelbrot(MandelbrotTemp[MandelBrotLookAt].Xmin,
                    MandelbrotTemp[MandelBrotLookAt].Xmax,
                    MandelbrotTemp[MandelBrotLookAt].Ymin,
                    MandelbrotTemp[MandelBrotLookAt].Ymax,
                    MandelbrotTemp[MandelBrotLookAt].iterationen,
                    MandelBrotImage, StatusLabel,
                    buttonNext, buttonPrevious, buttonBerechnen,
                    ProgressBarMandelBrot);

                MandelbrotTemp[MandelBrotLookAt].CountThisMandelbrot = false;
                MandelbrotTemp[MandelBrotLookAt].MandelbrotId = MandelBrotLookAt;

                MandelbrotTemp[MandelBrotLookAt].RefreshTextBoxes(textBoxXmin, textBoxXmax, textBoxYmin, textBoxYmax, textBoxIterationen);

                MandelBrotThread.Abort();
                MandelBrotThread = new Thread(new ThreadStart(MandelbrotTemp[MandelBrotLookAt].CalculateAndDraw));
                MandelBrotThread.Start();

            }

            if (MandelBrotLookAt == 0)
            {
                // Wenn dieses MandelBrot das älteste war kann nicht weiter zurückgeklickt werden
                buttonPrevious.IsEnabled = false;
            }
        }

        private void buttonNext_Click(object sender, RoutedEventArgs e)
        {
            // Wenn das betrachtete MandelBrot keinen ungültigen Wert erreicht
            if ((MandelBrotLookAt + 1) <= MandelBrotId)
            {
                // Aktuell betrachtendes MandelBrot um einen Wert erhöhen
                MandelBrotLookAt++;

                // Ein Schritt vor ermöglicht wieder einen Schritt zurück
                buttonPrevious.IsEnabled = true;

                // Mandelbrot berechnen
                MandelbrotTemp[MandelBrotLookAt] = new Mandelbrot(MandelbrotTemp[MandelBrotLookAt].Xmin,
                    MandelbrotTemp[MandelBrotLookAt].Xmax,
                    MandelbrotTemp[MandelBrotLookAt].Ymin,
                    MandelbrotTemp[MandelBrotLookAt].Ymax,
                    MandelbrotTemp[MandelBrotLookAt].iterationen,
                    MandelBrotImage, StatusLabel,
                    buttonNext, buttonPrevious, buttonBerechnen,
                    ProgressBarMandelBrot);

                MandelbrotTemp[MandelBrotLookAt].CountThisMandelbrot = false;
                MandelbrotTemp[MandelBrotLookAt].MandelbrotId = MandelBrotLookAt;

                MandelbrotTemp[MandelBrotLookAt].RefreshTextBoxes(textBoxXmin, textBoxXmax, textBoxYmin, textBoxYmax, textBoxIterationen);

                MandelBrotThread.Abort();
                MandelBrotThread = new Thread(new ThreadStart(MandelbrotTemp[MandelBrotLookAt].CalculateAndDraw));
                MandelBrotThread.Start();
            }

            if (MandelBrotLookAt == MandelBrotId)
            {
                // Wenn dieses MandelBrot das neuste war kann nicht weiter vorgeklickt werden
                buttonNext.IsEnabled = false;
            }
        }

        #endregion

        #region Buttons - Berechnung

        private void buttonBerechnen_Click(object sender, RoutedEventArgs e)
        {
            if (buttonBerechnen.Content.ToString() == "Neu Berechnen")
            {
                // Werte erkennen und zeichnen
                RefreshMandelBrot();
            }
            else
            {
                buttonBerechnen.IsEnabled = false;

                MandelBrotThread.Abort();

                ProgressBarMandelBrot.Value = 0;
                StatusLabel.Content = "Berechnung abgebrochen";

                buttonBerechnen.Content = "Neu Berechnen";

                buttonBerechnen.IsEnabled = true;

            }
        }

        private void checkBoxIterationen_Checked(object sender, RoutedEventArgs e)
        {
            /* in Entwicklung ... */
        }

        private void sliderIterationen_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // wenn das Fenster vollständig geladen wurde
            if (WindowReady)
            {
                // Wert des Sliders in TextBox schreiben
                textBoxIterationen.Text = ((int)(sliderIterationen.Value)).ToString();
            }
        }

        private void textBoxIterationen_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Versuchen ...
            try
            {
                // Wenn sich der Wert zwischen 0 und Maximum befindet
                if (Convert.ToDouble(textBoxIterationen.Text) >= 0 && Convert.ToDouble(textBoxIterationen.Text) <= sliderIterationen.Maximum)
                {
                    // Wert der TextBox auf den Slider auftragen
                    sliderIterationen.Value = Convert.ToDouble(textBoxIterationen.Text);
                }
            }
            catch
            {
                // bei ungültigen Werten nichts machen
            }
        }

        #endregion

        #region MandelBrot zeichnen

        public void RefreshMandelBrot()
        {
            // Mandelbrot zeichnen
            Mandelbrot MyMandelbrot = new Mandelbrot(Convert.ToDouble(textBoxXmin.Text),
                Convert.ToDouble(textBoxXmax.Text),
                Convert.ToDouble(textBoxYmin.Text),
                Convert.ToDouble(textBoxYmax.Text), Convert.ToInt32(textBoxIterationen.Text),
                MandelBrotImage, StatusLabel,
                buttonNext, buttonPrevious, buttonBerechnen,
                ProgressBarMandelBrot);

            MyMandelbrot.RefreshTextBoxes(textBoxXmin, textBoxXmax, textBoxYmin, textBoxYmax, textBoxIterationen);

            MandelBrotThread.Abort();
            MandelBrotThread = new Thread(new ThreadStart(MyMandelbrot.CalculateAndDraw));
            MandelBrotThread.Start();
        }

        #endregion

        #region Zoom

        public void CalculateZoom(int StartX, int StartY, int PixelWidth, int PixelHeight)
        {
            // Betrag der Breite und Höhe in der komplexen Ebene
            double ComplexPlaneWidth = Math.Abs(MandelbrotTemp[MandelBrotLookAt].Xmax - MandelbrotTemp[MandelBrotLookAt].Xmin);
            double ComplexPlaneHeight = Math.Abs(MandelbrotTemp[MandelBrotLookAt].Ymax - MandelbrotTemp[MandelBrotLookAt].Ymin);

            // Betrag der Startpunkte in der komplexen Ebene
            double ComplexRectX = (ComplexPlaneWidth * StartX) / MandelbrotWidth;
            double ComplexRectY = (ComplexPlaneHeight * StartY) / MandelbrotHeight;

            // Länge des Auswahlrechtecks in der komplexen Ebene
            double ComplexRectWidth = (ComplexPlaneWidth * PixelWidth) / MandelbrotWidth;
            double ComplexRectHeight = (ComplexPlaneHeight * PixelHeight) / MandelbrotHeight;

            // MandelBrot berechnen
            MandelBrotId++;
            MandelbrotTemp[MandelBrotId] = new Mandelbrot(MandelbrotTemp[MandelBrotLookAt].Xmin + ComplexRectX,
                MandelbrotTemp[MandelBrotLookAt].Xmin + ComplexRectX + ComplexRectWidth,
                MandelbrotTemp[MandelBrotLookAt].Ymin + ComplexRectY,
                MandelbrotTemp[MandelBrotLookAt].Ymin + ComplexRectY + ComplexRectHeight,
                Convert.ToInt32(textBoxIterationen.Text),
                MandelBrotImage, StatusLabel,
                buttonNext, buttonPrevious, buttonBerechnen,
                ProgressBarMandelBrot);

            MandelbrotTemp[MandelBrotId].RefreshTextBoxes(textBoxXmin, textBoxXmax, textBoxYmin, textBoxYmax, textBoxIterationen);

            MandelBrotThread.Abort();
            MandelBrotThread = new Thread(new ThreadStart(MandelbrotTemp[MandelBrotId].CalculateAndDraw));
            MandelBrotThread.Start();
        }

        #endregion

        #region Auswahlrechteck

        private void MandelBrotImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Auswahlrechteck starten
            isZooming = true;

            // Startpunkt setzen
            RectStartPoint.X = e.GetPosition(MandelBrotCanvas).X;
            RectStartPoint.Y = e.GetPosition(MandelBrotCanvas).Y;

            // Endpunkt zurücksetzen
            RectEndPoint.X = RectStartPoint.X;
            RectStartPoint.Y = RectStartPoint.Y;

            // Auswahlrechteck zurücksetzen
            ZoomingRectangle.Width = 0;
            ZoomingRectangle.Height = 0;

            // Startposition setzen
            ZoomingRectangle.Margin = new Thickness(RectStartPoint.X, RectStartPoint.Y, 0, 0);
        }

        private void MandelBrotImage_MouseMove(object sender, MouseEventArgs e)
        {
            // Wenn das Auswahlrechteck aktiviert ist (Taste gedrückt)
            if (isZooming)
            {
                // Endpunkt setzen
                RectEndPoint.X = e.GetPosition(MandelBrotCanvas).X;
                RectEndPoint.Y = e.GetPosition(MandelBrotCanvas).Y;

                // Breite und Höhe ermitteln
                int newWidth = (int)(RectEndPoint.X - RectStartPoint.X);
                int newHeight = (int)(RectEndPoint.Y - RectStartPoint.Y);

                if (RatioZoom)
                {
                    if (Math.Abs(newWidth) > Math.Abs(newHeight)) newWidth = (int)(newHeight * GausRatioYX);
                    if (Math.Abs(newHeight) > Math.Abs(newWidth)) newHeight = (int)(newWidth * GausRatioXY);

                    if (newWidth > 0 && newHeight > 0)              // 4. Quadrant (nur quadratische Zoomen)
                    {
                        ZoomingRectangle.Width = newWidth;
                        ZoomingRectangle.Height = newHeight;
                    }
                }
                else
                {
                    if (newWidth > 0 && newHeight > 0)              // 4. Quadrant
                    {
                        ZoomingRectangle.Width = newWidth;
                        ZoomingRectangle.Height = newHeight;
                    }
                    else if (newWidth < 0 && newHeight > 0)         // 3. Quadrant
                    {
                        ZoomingRectangle.Margin = new Thickness(RectStartPoint.X + newWidth, RectStartPoint.Y, 0, 0);

                        ZoomingRectangle.Width = Math.Abs(newWidth);
                        ZoomingRectangle.Height = newHeight;
                    }
                    else if (newWidth < 0 && newHeight < 0)         // 2. Quadrant
                    {
                        ZoomingRectangle.Margin = new Thickness(RectEndPoint.X, RectEndPoint.Y, 0, 0);

                        ZoomingRectangle.Width = Math.Abs(newWidth);
                        ZoomingRectangle.Height = Math.Abs(newHeight);
                    }
                    else if (newWidth > 0 && newHeight < 0)         // 1. Quadrant
                    {
                        ZoomingRectangle.Margin = new Thickness(RectStartPoint.X, RectStartPoint.Y + newHeight, 0, 0);

                        ZoomingRectangle.Width = newWidth;
                        ZoomingRectangle.Height = Math.Abs(newHeight);
                    }
                }
            }
        }

        private void MandelBrotImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Wenn das Auswahlrechteck noch aktiviert ist (Taste gedrückt)
            if (isZooming)
            {
                // Auswahlrechteck deaktivieren
                isZooming = false;

                // StartPosition des Rechtecks speichern
                int StartX = (int)ZoomingRectangle.Margin.Left;
                int StartY = (int)ZoomingRectangle.Margin.Top;

                // Breite und Höhe des Rechtecks speichern
                int PixelWidth = (int)ZoomingRectangle.Width;
                int PixelHeight = (int)ZoomingRectangle.Height;

                // Auswahlrechteck ausblenden
                ZoomingRectangle.Width = 0;
                ZoomingRectangle.Height = 0;

                // Werte an CalculateZoom übergeben
                CalculateZoom(StartX, StartY, PixelWidth, PixelHeight);
            }
        }

        private void MandelBrotImage_MouseLeave(object sender, MouseEventArgs e)
        {
            // Auswahlrechteck deaktivieren
            isZooming = false;

            // Auswahlrechteck ausblenden
            ZoomingRectangle.Width = 0;
            ZoomingRectangle.Height = 0;
        }

        #endregion

        #region Bildeinrichtung

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Bei Fenstergrößenänderung die Größen aktualisieren
            RefreshSize();
        }

        public void RefreshSize()
        {
            GausRatioXY = Math.Abs(StdYmax - StdYmin) / Math.Abs(StdXmax - StdXmin);       // Ratio X : Y
            GausRatioYX = Math.Abs(StdXmax - StdXmin) / Math.Abs(StdYmax - StdYmin);       // Ratio Y : X

            // Beste Darstellungsmöglichkeit prüfen
            if ((int)MandelBrotGrid.ActualWidth <= (int)MandelBrotGrid.ActualHeight)
            {
                // Breite ist die kleinere Größe, deshalb wird die Höhe auch nach der Breite orientiert
                MandelbrotWidth = (int)MandelBrotGrid.ActualWidth;
                MandelbrotHeight = (int)((double)MandelbrotWidth * GausRatioXY);
            }
            else
            {
                // Höhe ist die kleinere Größe, deshalb wird die Breite auch nach der Höhe orientiert
                MandelbrotHeight = (int)MandelBrotGrid.ActualHeight;
                MandelbrotWidth = (int)((double)MandelbrotHeight * GausRatioYX);
            }

            // Steuerlement Image auf Größe setzen
            MandelBrotImage.Width = MandelbrotWidth;
            MandelBrotImage.Height = MandelbrotHeight;

            // Darüberliegendes Canvas Element auf die Bildgröße setzen
            MandelBrotCanvas.Width = MandelbrotWidth;
            MandelBrotCanvas.Height = MandelbrotHeight;
        }

        private void checkBoxFullscreen_Checked(object sender, RoutedEventArgs e)
        {
            // Vollbild setzen
            this.WindowStyle = System.Windows.WindowStyle.None;
            this.WindowState = System.Windows.WindowState.Maximized;

            // Benutzer informieren
            MessageBox.Show("Escape drücken um Vollbildmodus wieder zu verlassen.");
        }

        private void checkBoxFullscreen_Unchecked(object sender, RoutedEventArgs e)
        {
            // Normales Fenster wiederherstellen
            this.WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
            this.WindowState = System.Windows.WindowState.Normal;
        }

        #endregion

        #region Sonstige Funktionen

        public static void DoEvents()
        {
            // Methode die alle Steuerelemente sofort aktualisiert
            if (Application.Current != null)
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate { }));
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Escape Taste abfangen
            if (e.Key == Key.Escape)
            {
                // Wenn gerade die Steuerung ausgeblendet ist ...
                if (checkBoxControlVisibility.IsChecked == true)
                {
                    // Steuerung wieder einblenden
                    ControlGrid.Width = new GridLength(240);

                    // CheckBox neu setzen
                    checkBoxControlVisibility.IsChecked = false;
                }
                // Wenn ansonsten der Vollbildmodus aktiviert ist ...
                else if (checkBoxFullscreen.IsChecked == true)
                {
                    // Normales Fenster wiederherstellen
                    this.WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
                    this.WindowState = System.Windows.WindowState.Normal;

                    // CheckBox neu setzen
                    checkBoxFullscreen.IsChecked = false;
                }
            }
        }

        #endregion

        #region Speichern

        private void buttonSavePosition_Click(object sender, RoutedEventArgs e)
        {
            /* in Entwicklung ... */
        }

        private void buttonSaveBild_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog SaveImageDialog = new Microsoft.Win32.SaveFileDialog();

            SaveImageDialog.DefaultExt = ".bmp";
            SaveImageDialog.Filter = "Bitmap Image (.bmp)|*.bmp|Jpeg Image (.jpeg)|*.jpeg";

            if (SaveImageDialog.ShowDialog() == true)
            {
                if (SaveImageDialog.FilterIndex == 1)
                {
                    var encoder = new JpegBitmapEncoder();
                    SaveUsingEncoder(MandelBrotImage, SaveImageDialog.FileName, encoder);
                }
                else
                {
                    var encoder = new BmpBitmapEncoder();
                    SaveUsingEncoder(MandelBrotImage, SaveImageDialog.FileName, encoder);
                }
            }
        }

        void SaveUsingEncoder(FrameworkElement visual, string fileName, BitmapEncoder encoder)
        {
            RenderTargetBitmap bitmap = new RenderTargetBitmap(
                (int)visual.ActualWidth,
                (int)visual.ActualHeight,
                96,
                96,
                Media.PixelFormats.Pbgra32);
            bitmap.Render(visual);
            BitmapFrame frame = BitmapFrame.Create(bitmap);
            encoder.Frames.Add(frame);

            using (var stream = File.Create(fileName))
            {
                encoder.Save(stream);
            }
        }

        #endregion

        #region CheckBoxen - Erweiterte Optionen

        /* Ratio Zoom */

        private void checkBoxRatioZoom_Checked(object sender, RoutedEventArgs e)
        {
            RatioZoom = true;
        }
        private void checkBoxRatioZoom_Unchecked(object sender, RoutedEventArgs e)
        {
            RatioZoom = false;
        }

        /* Preview */

        private void checkBoxPreview_Checked(object sender, RoutedEventArgs e)
        {
            LoadPreview = true;
        }
        private void checkBoxPreview_Unchecked(object sender, RoutedEventArgs e)
        {
            LoadPreview = false;
        }

        /* Progress Bar */

        private void checkBoxProgressBar_Checked(object sender, RoutedEventArgs e)
        {
            UseProgressBar = true;
        }
        private void checkBoxProgressBar_Unchecked(object sender, RoutedEventArgs e)
        {
            UseProgressBar = false;
        }

        /* Steuerung verstecken */

        private void checkBoxControlVisibility_Checked(object sender, RoutedEventArgs e)
        {
            // Steuerung verstecken
            ControlGrid.Width = new GridLength(0);

            // Benutzer informieren
            MessageBox.Show("Escape drücken um Steuerung wieder einzublenden.");
        }

        /* Iterativer Farbverlauf */

        private void radioButtonColorsIterationen_Checked(object sender, RoutedEventArgs e)
        {
            ColorGradientIterative = true;
            ColorGradientPercent = false;
        }

        private void radioButtonColorsIterationen_Unchecked(object sender, RoutedEventArgs e)
        {
            ColorGradientPercent = true;
            ColorGradientIterative = false;
        }

        /* Prozentueller Farbverlauf */

        private void radioButtonColorsProzent_Checked(object sender, RoutedEventArgs e)
        {
            ColorGradientPercent = true;
            ColorGradientIterative = false;
        }

        private void radioButtonColorsProzent_Unchecked(object sender, RoutedEventArgs e)
        {
            ColorGradientIterative = true;
            ColorGradientPercent = false;
        }

        #endregion
    }
}
