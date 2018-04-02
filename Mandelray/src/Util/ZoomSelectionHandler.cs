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
    /// holds information about the zoomed position (left, top coordinate)
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
    /// handles the zooming and selection
    /// </summary>
    public class ZoomSelectionHandler
    {
        /// <summary>
        /// the canvas in which the zooming is possible
        /// </summary>
        public Canvas ReferenceCanvas;

        /// <summary>
        /// the displayed zooming rectangle
        /// </summary>
        public Rectangle ZoomingRectangle { get; }

        /// <summary>
        /// will be calcuted if the zooming has been selected
        /// </summary>
        public event ZoomSelectedEventHandler ZoomSelected;

        // zooming enabled
        private bool _isZooming;

        // rectangle points
        private Point _rectStart;

        private Point _rectEnd;

        /// <summary>
        /// default aspect ratio
        /// </summary>
        public static readonly double GausRatioXy;

        /// <summary>
        /// default aspect ratio
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

                // fix ratio
                if (Math.Abs(newWidth) > Math.Abs(newHeight)) newWidth = (int) (newHeight * GausRatioYx);
                if (Math.Abs(newHeight) > Math.Abs(newWidth)) newHeight = (int) (newWidth * GausRatioXy);

                // only quadratic zooming in 4th quadrant allowed
                if (newWidth > 0 && newHeight > 0)
                {
                    ZoomingRectangle.Width = newWidth;
                    ZoomingRectangle.Height = newHeight;
                }
            }
        }

        public void EndSelection()
        {
            // if zooming is still enabled
            if (_isZooming)
            {
                // remember positions
                var startX = (int) ZoomingRectangle.Margin.Left;
                var startY = (int) ZoomingRectangle.Margin.Top;
                var width = (int) ZoomingRectangle.Width;
                var height = (int) ZoomingRectangle.Height;

                // disable zooming and hide rectangle
                AbortSelection();

                // invoke event handler
                ZoomSelected?.Invoke(this, new ZoomSelectionEventArgs(startX, startY, width, height));
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
    }
}