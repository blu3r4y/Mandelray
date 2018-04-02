using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using Colorspace;
using MathNet.Numerics;
using MathNet.Numerics.Interpolation;
using Mandelray.Util;

namespace Mandelray.Datastructures
{
    /// <summary>
    /// Supplies some color mapping arrays
    /// </summary>
    public class ColorMappings
    {
        /// <summary>
        /// Gets fired if the selected color mapping changes
        /// </summary>
        /// <param name="map">the new color map</param>
        public delegate void SelectedItemChangedEventHandler(ColorMap map);

        /// <summary>
        /// Represents a color mapping by a name and the corresponding color array
        /// </summary>
        public class ColorMap
        {
            private string Name { get; }
            public int[] Colors { get; }

            /// <summary>
            /// The index within the palette, which should be used for coloring areas,
            /// where we reached the maximum number of iterations.
            /// </summary>
            public readonly int IndexMaxIterations;

            public ColorMap(string name, IEnumerable<Color> colors, int indexMaxIterations = 0)
                : this(name, colors.Select(e => e.ToArgb()).ToArray(), indexMaxIterations)
            {
            }

            public ColorMap(string name, int[] colors, int indexMaxIterations = 0)
            {
                Name = name;
                Colors = colors;
                IndexMaxIterations = indexMaxIterations;
            }

            public override string ToString()
            {
                return Name;
            }
        }

        /// <summary>
        /// Holds the currently selected color mapping
        /// </summary>
        public static ColorMap SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                ItemChanged?.Invoke(value);
            }
        }

        private static ColorMap _selectedItem;

        /// <summary>
        /// Holds all the available color mappings
        /// </summary>
        public static ObservableCollection<ColorMap> Items { get; }

        /// <summary>
        /// Will be fired if the selcted item changed
        /// </summary>
        public static event SelectedItemChangedEventHandler ItemChanged;

        public static readonly Color[] GrayScale;
        public static readonly Color[] MultiColor;
        public static readonly Color[] UltraFractal;

        static ColorMappings()
        {
            /* ultra fractal (https://stackoverflow.com/a/25816111/927377) */

            const int ultraFractalSize = 128;
            UltraFractal = new Color[ultraFractalSize];

            var points = new[]
            {
                0.0,
                0.16 * ultraFractalSize,
                0.42 * ultraFractalSize,
                0.6425 * ultraFractalSize,
                0.8575 * ultraFractalSize
            };

            var rgb = new[]
            {
                Color.FromArgb(0, 7, 100),
                Color.FromArgb(32, 107, 203),
                Color.FromArgb(237, 255, 255),
                Color.FromArgb(255, 170, 0),
                Color.FromArgb(0, 2, 0),
            };

            double[][] hsv = rgb.Select(ColorToHsv).ToArray();

            IEnumerable<double> hue = hsv.Select(e => e[0]);
            IEnumerable<double> sat = hsv.Select(e => e[1]);
            IEnumerable<double> val = hsv.Select(e => e[2]);

            IInterpolation iHue = Interpolate.CubicSpline(points, hue);
            IInterpolation iSat = Interpolate.CubicSpline(points, sat);
            IInterpolation iVal = Interpolate.CubicSpline(points, val);

            for (var i = 0; i < ultraFractalSize; i++)
            {
                double h = iHue.Interpolate(i).Clamp(0, 255);
                double s = iSat.Interpolate(i).Clamp(0, 1);
                double v = iVal.Interpolate(i).Clamp(0, 1);

                UltraFractal[i] = ColorFromHsv(h, s, v);
            }

            /* gray scale */

            GrayScale = new Color[256];

            for (var i = 0; i < GrayScale.Length; i++)
            {
                GrayScale[i] = Color.FromArgb(255 - i, 255 - i, 255 - i);
            }

            /* multi color */

            MultiColor = new Color[256];
            MultiColor[0] = Color.FromArgb(255, 255, 255);  // white
            MultiColor[31] = Color.FromArgb(255, 255, 0);   // yellow
            MultiColor[63] = Color.FromArgb(0, 255, 0);     // green
            MultiColor[95] = Color.FromArgb(0, 255, 255);   // light blue
            MultiColor[127] = Color.FromArgb(0, 0, 255);    // blue
            MultiColor[159] = Color.FromArgb(255, 0, 255);  // violet
            MultiColor[191] = Color.FromArgb(255, 0, 0);    // red
            MultiColor[225] = Color.FromArgb(0, 0, 0);      // black

            for (var i = 1; i <= 30; i++)
            {
                MultiColor[i] = Color.FromArgb(255, 255, 255 - (8 * i));
                MultiColor[i + 31] = Color.FromArgb(255 - (8 * i), 255, 0);
                MultiColor[i + 63] = Color.FromArgb(0, 255, 8 * i);
                MultiColor[i + 95] = Color.FromArgb(0, 255 - (8 * i), 255);
                MultiColor[i + 127] = Color.FromArgb(8 * i, 0, 255);
                MultiColor[i + 159] = Color.FromArgb(255, 0, 255 - (8 * i));
                MultiColor[i + 191] = Color.FromArgb(255 - (8 * i), 0, 0);

                MultiColor[222] = Color.FromArgb(0, 0, 0);
                MultiColor[223] = Color.FromArgb(0, 0, 0);
                MultiColor[224] = Color.FromArgb(0, 0, 0);
            }

            /* list which holds all the mappings */

            Items = new ObservableCollection<ColorMap>
            {
                new ColorMap("Ultra Fractal", UltraFractal, (int) (0.8575 * ultraFractalSize)),
                new ColorMap("Multi Color", MultiColor),
                new ColorMap("Grayscale", GrayScale)
            };

            SelectedItem = Items[0];
        }

        private static double[] ColorToHsv(Color color)
        {
            var rgb = new ColorRGB(new ColorRGB32Bit(color.R, color.G, color.B));
            var hsv = new ColorHSV(rgb);

            return new [] {hsv.H, hsv.S, hsv.V};
        }

        private static Color ColorFromHsv(double hue, double saturation, double value)
        {
            var hsv = new ColorHSV(hue, saturation, value);
            var rgb = new ColorRGB32Bit(new ColorRGB(hsv));

            return Color.FromArgb(rgb.R, rgb.G, rgb.B);
        }
    }
}