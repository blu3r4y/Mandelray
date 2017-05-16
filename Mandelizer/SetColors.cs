using System;
using System.Drawing;

namespace Mandelizer
{
    public static class SetColors
    {
        public static void GrayScale(ref Color[] ColorMap)
        {
            Mandelizer.MainWindow.usedColors = 512;

            for (int i = 0; i < 256; i++)
            {
                ColorMap[i] = Color.FromArgb(255 - i, 255 - i, 255 - i);
            }

            /*
            for (int i = 0; i < 256; i++)
            {
                ColorMap[256 + i] = Color.FromArgb(255 - i, 255 - i, 255 - i);
            }
            */
        }

        public static void MonoRed(ref Color[] MBColorMap)
        {
            Mandelizer.MainWindow.usedColors = 256;

            for (int i = 0; i < 256; i++)
            {
                MBColorMap[i] = Color.FromArgb(i, i, 255);
            }
        }

        public static void MonoGreen(ref Color[] MBColorMap)
        {
            Mandelizer.MainWindow.usedColors = 256;

            for (int i = 0; i < 256; i++)
            {
                MBColorMap[i] = Color.FromArgb(i, 255, i);
            }
        }

        public static void MonoBlue(ref Color[] MBColorMap)
        {
            Mandelizer.MainWindow.usedColors = 256;

            for (int i = 0; i < 256; i++)
            {
                MBColorMap[i] = Color.FromArgb(255, i, i);
            }
        }

        public static void MultiColor1(ref Color[] MBColorMap)
        {
            Mandelizer.MainWindow.usedColors = 256;

            MBColorMap[0] = Color.FromArgb(255, 255, 255);    // weiß
            MBColorMap[31] = Color.FromArgb(255, 255, 0);     // gelb
            MBColorMap[63] = Color.FromArgb(0, 255, 0);       // grün
            MBColorMap[95] = Color.FromArgb(0, 255, 255);     // hellblau
            MBColorMap[127] = Color.FromArgb(0, 0, 255);      // blau
            MBColorMap[159] = Color.FromArgb(255, 0, 255);    // violett
            MBColorMap[191] = Color.FromArgb(255, 0, 0);      // rot
            MBColorMap[225] = Color.FromArgb(0, 0, 0);        // schwarz

            for (int i = 1; i <= 30; i++)
            {
                MBColorMap[i] = Color.FromArgb(255, 255, 255 - (8 * i));
                MBColorMap[i + 31] = Color.FromArgb(255 - (8 * i), 255, 0);
                MBColorMap[i + 63] = Color.FromArgb(0, 255, 8 * i);
                MBColorMap[i + 95] = Color.FromArgb(0, 255 - (8 * i), 255);
                MBColorMap[i + 127] = Color.FromArgb(8 * i, 0, 255);
                MBColorMap[i + 159] = Color.FromArgb(255, 0, 255 - (8 * i));
                MBColorMap[i + 191] = Color.FromArgb(255 - (8 * i), 0, 0);

                MBColorMap[222] = Color.FromArgb(0, 0, 0);
                MBColorMap[223] = Color.FromArgb(0, 0, 0);
                MBColorMap[224] = Color.FromArgb(0, 0, 0);
            }
        }

        public static void MultiColor2(ref Color[] MBColorMap)
        {
            Mandelizer.MainWindow.usedColors = 768;

            // weiß - rot
            for (int i = 0; i < 256; i++)
            {
                MBColorMap[i] = Color.FromArgb(255, 255 - i, 255 - i);
            }
            // rot - blau
            for (int i = 0; i < 256; i++)
            {
                MBColorMap[256 + i] = ColorFromHSV(i, 1, 1);
            }
            // blau - schwarz
            for (int i = 0; i < 256; i++)
            {
                MBColorMap[512 + i] = Color.FromArgb(0, 0, 255 - i);
            }
        }

        public static void MultiColor3(ref Color[] MBColorMap)
        {
            // ...
        }

        public static void Reverse(ref Color[] MBColorMap)
        {
            Array.Reverse(MBColorMap);
        }

        #region Farbkonvertierung

        public static Color InterpolColor(Color color1, Color color2, double relation)
        {
            int AbstandVektorR = color2.R - color1.R;
            int AbstandVektorG = color2.G - color1.G;
            int AbstandVektorB = color2.B - color1.B;

            int ColorR = color1.R + (int)(relation * AbstandVektorR);
            int ColorG = color1.G + (int)(relation * AbstandVektorG);
            int ColorB = color1.B + (int)(relation * AbstandVektorB);

            return Color.FromArgb(ColorR, ColorB, ColorG);
        }

        public static void ColorToHSV(Color color, out double hue, out double saturation, out double value)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            hue = color.GetHue();
            saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            value = max / 255d;
        }

        public static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(255, t, p, v);
            else
                return Color.FromArgb(255, v, p, q);
        }

        #endregion
    }
}