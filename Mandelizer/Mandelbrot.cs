using System;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Mandelizer
{
    public class Mandelbrot
    {
        public double Xmin, Xmax,
            Ymin, Ymax;

        public int iterationen;

        int MandelbrotHeight, MandelbrotWidth;

        double StepX, StepY;

        FastImage _fastImage;

        Label StatusLabel;

        Button buttonNext, buttonPrevious, buttonBerechnen;

        ProgressBar StatusBar;

        public int MandelbrotId;

        public bool CountThisMandelbrot = true;

        public Mandelbrot(double Xmin, double Xmax,
            double Ymin, double Ymax,
            int iterationen,
            System.Windows.Controls.Image MandelBrotImage, Label StatusLabel,
            Button buttonNext, Button buttonPrevious, Button buttonBerechnen,
            ProgressBar StatusBar)
        {
            this.Xmin = Xmin;
            this.Xmax = Xmax;
            this.Ymin = Ymin;
            this.Ymax = Ymax;

            this.iterationen = iterationen;

            this.MandelbrotHeight = MainWindow.MandelbrotHeight;
            this.MandelbrotWidth = MainWindow.MandelbrotWidth;

            this.StatusLabel = StatusLabel;

            this.buttonNext = buttonNext;
            this.buttonPrevious = buttonPrevious;
            this.buttonBerechnen = buttonBerechnen;

            this.MandelbrotId = MainWindow.MandelBrotId;

            this.StatusBar = StatusBar;

            // MandelBrot Bitmap erstellen
            this._fastImage = new FastImage(MandelBrotImage, MandelbrotWidth, MandelbrotHeight);
        }

        public void CalculateAndDraw()
        {
            // Start Zeit ermitteln für die Zeitmessung
            DateTime TimeStart = DateTime.Now;

            // aktuell betrachtendes MandelBrot setzen
            MainWindow.MandelBrotLookAt = MandelbrotId;

            // Status setzen und Zeit messen
            StatusLabel.Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(
                    delegate()
                    {
                        StatusLabel.Content = "Berechnung ...";
                    }));

            // Ladebalken auf Null setzen und neues Maximum definieren
            StatusBar.Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(
                    delegate()
                    {
                        StatusBar.Maximum = MandelbrotHeight / MainWindow.RefreshUIElements;
                        StatusBar.Value = 0;
                    }));

            // Berechnen-Button umfunktionieren
            buttonBerechnen.Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(
                    delegate()
                    {
                        buttonBerechnen.Content = "Abbrechen";
                    }));

            if (CountThisMandelbrot)
            {

                // Zurück Button aktivieren bei mehr als einem MandelBrot
                if (MainWindow.MandelBrotId > 0) buttonNext.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { buttonPrevious.IsEnabled = true; }));

                // Da dieses MandelBrot hinten angereiht wird, gibt es kein nächstes
                buttonNext.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { buttonNext.IsEnabled = false; }));

            }

            // Bitmap ByteArray erstellen -> Stride (Gesamte Byte Anzahl pro Zeile inklusive leere Bytes) x Höhe
            byte[] MBBitmapBytes = new byte[MandelbrotWidth * MandelbrotHeight];

            // Größe der Mandelbrotmenge auf Fenstergröße anpassen (Schrittweite für jeden Punkt)
            StepX = (Xmax - Xmin) / MandelbrotWidth;
            StepY = (Ymax - Ymin) / MandelbrotHeight;

            // Punkt C der nach der Reihe berechnet wird
            double C_real, C_imag;

            // Punkt Z der für die Iterationen benötigt wird
            double Z_real, Z_imag, Z_realTemp;

            // Zähler Variable für Anzahl der Iterationen
            int iterationsCounter = 0;

            // Temporäre Variable für die Anzahl der gebrauchten Iterationen
            int usedIterations = 0;

            C_imag = Ymin;      // Neue Reihe beginnen und imaginären Anteil von C auf Minimum setzen

            for (int y = 0; y < MandelbrotHeight; y++)
            {
                C_real = Xmin;  // Neue Spalte beginnen und realen Anteil von C auf Minimum setzen

                for (int x = 0; x < MandelbrotWidth; x++)
                {
                    Z_real = 0;                     // Ergebnis der Iteration
                    Z_imag = 0;                     // mit 0 beginnen

                    iterationsCounter = 0;          // Zähler für die Anzahl der Iterationen auf 0 setzen

                    //  Wenn die Anzahl an Iterationen noch nicht unterschritten wurde und der Betrag des Ergebnisses kleiner 2 ist wird weiter gerechnet
                    while (iterationsCounter < iterationen && ((Z_real) * (Z_real) + (Z_imag) * (Z_imag)) < 4)
                    {
                        Z_realTemp = Z_real;                                        // Vorherige Iteration quadrieren
                        Z_real = Z_real * Z_real - Z_imag * Z_imag + C_real;        // und
                        Z_imag = 2 * Z_realTemp * Z_imag + C_imag;                  // Punkt C addieren

                        iterationsCounter++;                                        // Iteration zählen
                    }

                    // Berechnen wann die Schleife stoppte
                    if (MainWindow.ColorGradientIterative)
                    {
                        usedIterations = iterationsCounter - (iterationsCounter / MainWindow.usedColors) * MainWindow.usedColors;
                        if (iterationsCounter == iterationen) usedIterations = (MainWindow.usedColors - 1);
                    }
                    else
                    {
                        usedIterations = (MainWindow.usedColors - 1) * iterationsCounter / iterationen;
                    }

                    _fastImage.SetPixel(x, y, MainWindow.MBColorMap[usedIterations].ToArgb());

                    C_real += StepX;    // Realen Anteil von C um einen Pixelschritt erhöhen
                }

                if (MainWindow.LoadPreview && (y % MainWindow.RefreshUIElements) == 1)
                {
                    _fastImage.Invalidate();
                }

                if (MainWindow.UseProgressBar && (y % MainWindow.RefreshUIElements) == 1)
                {
                    // Status in Ladebalken darstellen
                    StatusBar.Dispatcher.Invoke(DispatcherPriority.Normal,
                        new Action(
                            delegate()
                            {
                                StatusBar.Value += 1;
                            }));

                    StatusLabel.Dispatcher.Invoke(DispatcherPriority.Normal,
                        new Action(
                            delegate()
                            {
                                StatusLabel.Content = "Berechnung ... " + (int)(y * 100 / MandelbrotHeight) + "%";
                            }));
                }

                C_imag += StepY;        // Imaginären Anteil von C um einen Pixelschritt erhöhen
            }

            _fastImage.Invalidate();

            // Berechnen-Button umfunktionieren
            buttonBerechnen.Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(
                    delegate()
                    {
                        buttonBerechnen.Content = "Neu Berechnen";
                    }));

            // Status setzen und Zeit messen
            StatusLabel.Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(
                    delegate()
                    {
                        StatusLabel.Content = "Fertig - " + ((int)(DateTime.Now - TimeStart).TotalMilliseconds).ToString() + " ms";
                    }));
        }

        public void Draw()
        {
            CountThisMandelbrot = false;

            CalculateAndDraw();
        }

        public void RefreshTextBoxes(TextBox textBoxXmin, TextBox textBoxXmax, TextBox textBoxYmin, TextBox textBoxYmax, TextBox textBoxIterationen)
        {
            // Werte in TextBoxen schreiben
            textBoxXmin.Text = Xmin.ToString();
            textBoxXmax.Text = Xmax.ToString();

            textBoxYmin.Text = Ymin.ToString();
            textBoxYmax.Text = Ymax.ToString();

            textBoxIterationen.Text = iterationen.ToString();
        }
    }
}