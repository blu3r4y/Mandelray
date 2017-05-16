using System;
using System.Collections.ObjectModel;
using System.Drawing;

namespace Mandelizer.Datastructures
{
    /// <summary>
    /// supplies some color mapping arrays
    /// </summary>
    public class ColorMappings
    {
        /// <summary>
        /// gets fired if the selected color mapping changes
        /// </summary>
        /// <param name="map">the new color map</param>
        public delegate void SelectedItemChangedEventHandler(ColorMap map);

        /// <summary>
        /// represents a color mapping by a name and the corresponding color array
        /// </summary>
        public class ColorMap
        {
            private string Name { get; }
            public Color[] Colors { get; }

            public ColorMap(string name, Color[] colors)
            {
                Name = name;
                Colors = colors;
            }

            public override string ToString()
            {
                return Name;
            }
        }

        /// <summary>
        /// holds the currently selected color mapping
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
        /// holds all the available color mappings
        /// </summary>
        public static ObservableCollection<ColorMap> Items { get; }

        /// <summary>
        /// will be fired if the selcted item changed
        /// </summary>
        public static event SelectedItemChangedEventHandler ItemChanged;

        public static readonly Color[] GrayScale;
        public static readonly Color[] MonoBlue;
        public static readonly Color[] MonoGreen;
        public static readonly Color[] MonoRed;
        public static readonly Color[] MultiColor1;
        public static readonly Color[] MultiColor2;

        static ColorMappings()
        {
            /* mono color */

            GrayScale = new Color[256];
            MonoBlue = new Color[256];
            MonoGreen = new Color[256];
            MonoRed = new Color[256];

            for (var i = 0; i < 256; i++)
            {
                GrayScale[i] = Color.FromArgb(255 - i, 255 - i, 255 - i);
                MonoBlue[i] = Color.FromArgb(i, i, 255);
                MonoGreen[i] = Color.FromArgb(i, 255, i);
                MonoRed[i] = Color.FromArgb(255, i, i);
            }

            /* multi color 1 */

            MultiColor1 = new Color[256];
            MultiColor1[0] = Color.FromArgb(255, 255, 255);    // white
            MultiColor1[31] = Color.FromArgb(255, 255, 0);     // yellow
            MultiColor1[63] = Color.FromArgb(0, 255, 0);       // green
            MultiColor1[95] = Color.FromArgb(0, 255, 255);     // light blue
            MultiColor1[127] = Color.FromArgb(0, 0, 255);      // blue
            MultiColor1[159] = Color.FromArgb(255, 0, 255);    // violet
            MultiColor1[191] = Color.FromArgb(255, 0, 0);      // red
            MultiColor1[225] = Color.FromArgb(0, 0, 0);        // black

            for (var i = 1; i <= 30; i++)
            {
                MultiColor1[i] = Color.FromArgb(255, 255, 255 - (8 * i));
                MultiColor1[i + 31] = Color.FromArgb(255 - (8 * i), 255, 0);
                MultiColor1[i + 63] = Color.FromArgb(0, 255, 8 * i);
                MultiColor1[i + 95] = Color.FromArgb(0, 255 - (8 * i), 255);
                MultiColor1[i + 127] = Color.FromArgb(8 * i, 0, 255);
                MultiColor1[i + 159] = Color.FromArgb(255, 0, 255 - (8 * i));
                MultiColor1[i + 191] = Color.FromArgb(255 - (8 * i), 0, 0);

                MultiColor1[222] = Color.FromArgb(0, 0, 0);
                MultiColor1[223] = Color.FromArgb(0, 0, 0);
                MultiColor1[224] = Color.FromArgb(0, 0, 0);
            }

            /* multi color 2 */
            
            MultiColor2 = new Color[768];

            // white - red
            for (var i = 0; i < 256; i++)
            {
                MultiColor2[i] = Color.FromArgb(255, 255 - i, 255 - i);
            }
            // red - blue
            for (var i = 0; i < 256; i++)
            {
                MultiColor2[256 + i] = ColorFromHsv(i, 1, 1);
            }
            // blue - black
            for (var i = 0; i < 256; i++)
            {
                MultiColor2[512 + i] = Color.FromArgb(0, 0, 255 - i);
            }

            /* list which holds all the mappings */

            Items = new ObservableCollection<ColorMap>
            {
                new ColorMap("Grayscale", GrayScale),
                new ColorMap("Mono Blue", MonoBlue),
                new ColorMap("Mono Red", MonoRed),
                new ColorMap("Mono Green", MonoGreen),
                new ColorMap("Multi Color 1", MultiColor1),
                new ColorMap("Multi Color 2", MultiColor2)
            };
            
            SelectedItem = Items[4];
        }

        private static Color ColorFromHsv(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            switch (hi)
            {
                case 0:
                    return Color.FromArgb(255, v, t, p);

                case 1:
                    return Color.FromArgb(255, q, v, p);

                case 2:
                    return Color.FromArgb(255, p, v, t);

                case 3:
                    return Color.FromArgb(255, p, q, v);

                case 4:
                    return Color.FromArgb(255, t, p, v);
            }

            return Color.FromArgb(255, v, p, q);
        }
    }
}
