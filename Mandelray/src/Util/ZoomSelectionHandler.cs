using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Mandelray.Datastructures;

namespace Mandelray.Util
{
    /// <summary>
    /// Holds information about the zoomed position (left, top coordinate)
    /// and its height and width
    /// </summary>
    public class ZoomSelectionEventArgs : EventArgs
    {
        public readonly int StartX;
        public readonly int StartY;

        public readonly int Width;
        public readonly int Height;

        public ZoomSelectionEventArgs(int startX, int startY, int width, int height)
        {
            StartX = startX;
            StartY = startY;
            Width = width;
            Height = height;
        }
    }

    public delegate void ZoomSelectedEventHandler(object sender, ZoomSelectionEventArgs e);

    /// <summary>
    /// Handles the zooming and selection
    /// </summary>
    public class ZoomSelectionHandler
    {
        /// <summary>
        /// The canvas in which the zooming is possible
        /// </summary>
        public Canvas ReferenceCanvas;

        /// <summary>
        /// The displayed zooming rectangle
        /// </summary>
        public Rectangle ZoomingRectangle { get; }

        /// <summary>
        /// Will be calcuted if the zooming has been selected
        /// </summary>
        public event ZoomSelectedEventHandler ZoomSelected;

        private bool _isZooming;

        private Point _rectStart;

        private Point _rectEnd;

        /// <summary>
        /// Default aspect ratio
        /// </summary>
        public static readonly double GausRatioXy;

        /// <summary>
        /// Default aspect ratio
        /// </summary>
        public static readonly double GausRatioYx;

        static ZoomSelectionHandler()
        {
            // calculate gaussian ratios
            GausRatioXy = MandelPos.DefaultPos.YDiff / MandelPos.DefaultPos.XDiff;
            GausRatioYx = MandelPos.DefaultPos.XDiff / MandelPos.DefaultPos.YDiff;
        }

        public ZoomSelectionHandler(Canvas referenceCanvas)
        {
            ReferenceCanvas = referenceCanvas;

            // light grey rectangle
            ZoomingRectangle = new Rectangle
            {
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Fill = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Height = 0,
                Width = 0
            };
        }

        public void StartSelection(MouseEventArgs e)
        {
            _isZooming = true;

            // set start point
            _rectStart.X = e.GetPosition(ReferenceCanvas).X;
            _rectStart.Y = e.GetPosition(ReferenceCanvas).Y;

            // set end point
            _rectEnd.X = _rectStart.X;
            _rectStart.Y = _rectStart.Y;

            // reset rectangle
            ZoomingRectangle.Width = 0;
            ZoomingRectangle.Height = 0;
            ZoomingRectangle.Margin = new Thickness(_rectStart.X, _rectStart.Y, 0, 0);
        }

        public void MoveSelection(MouseEventArgs e)
        {
            // if zooming is enabled
            if (_isZooming)
            {
                // set end point
                _rectEnd.X = e.GetPosition(ReferenceCanvas).X;
                _rectEnd.Y = e.GetPosition(ReferenceCanvas).Y;

                // grab width and height
                var newWidth = (int) (_rectEnd.X - _rectStart.X);
                var newHeight = (int) (_rectEnd.Y - _rectStart.Y);

                // show rectangle only in 4th quadrant
                if (newWidth > 0 && newHeight > 0)
                {
                    (ZoomingRectangle.Width, ZoomingRectangle.Height) = FixRatio(newWidth, newHeight);
                }
                else
                {
                    (ZoomingRectangle.Width, ZoomingRectangle.Height) = (0, 0);
                }
            }
        }

        public void EndSelection()
        {
            // if zooming is still enabled
            if (_isZooming)
            {
                // remember positions
                var startX = (int) _rectStart.X;
                var startY = (int) _rectStart.Y;
                var width = (int) (_rectEnd.X - _rectStart.X);
                var height = (int) (_rectEnd.Y - _rectStart.Y);

                // only quadratic zooming in 4th quadrant allowed
                if (width > 0 && height > 0)
                {
                    // fix ratio
                    (width, height) = FixRatio(width, height);

                    // disable zooming and hide rectangle
                    AbortSelection();

                    // invoke event handler
                    ZoomSelected?.Invoke(this, new ZoomSelectionEventArgs(startX, startY, width, height));
                }
            }
        }

        public void AbortSelection()
        {
            // disable zooming
            _isZooming = false;

            // hide rectangle
            ZoomingRectangle.Width = 0;
            ZoomingRectangle.Height = 0;
        }

        private static (int width, int height) FixRatio(int width, int height)
        {
            if (Math.Abs(width) > Math.Abs(height)) width = (int)(height * GausRatioYx);
            if (Math.Abs(height) > Math.Abs(width)) height = (int)(width * GausRatioXy);

            return (width, height);
        }
    }
}