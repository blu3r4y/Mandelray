using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Mandelizer.Datastructures;
using Mandelizer.Util;
using Mandelizer.Rendering;

namespace Mandelizer
{
    public sealed partial class MainWindow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// stores the object, which holds a handle to render the wpf image faster
        /// </summary>
        public FastImage FastImageRef { get; private set; }

        /// <summary>
        /// stores the object, which holds a handle to render the wpf image faster
        /// </summary>
        public FastImage FastImagePreviewRef { get; private set; }

        /// <summary>
        /// the used coler map for all frames in argb32 encoding
        /// </summary>
        public ColorMappings.ColorMap ColorMapRef => ColorMappings.SelectedItem;

        /// <summary>
        /// keeps the current render size information
        /// </summary>
        public readonly RenderSize RenderSizeRef;

        /// <summary>
        /// holds all the frames which got calculated already
        /// </summary>
        public List<MandelFrame> FrameBuffer = new List<MandelFrame>();
        
        /// <summary>
        /// holds the current index within the frame buffer
        /// </summary>
        public int FrameBufferIndex { get; private set; }

        /// <summary>
        /// can take a step back in the queue of mandelbrot sets
        /// </summary>
        public bool CanStepBack => HasFrames && FrameBufferIndex > 0;

        /// <summary>
        /// can take a step forward in the queue of mandelbrot sets
        /// </summary>
        public bool CanStepForward => HasFrames && FrameBufferIndex < FrameBuffer.Count - 1;

        /// <summary>
        /// current active frame
        /// </summary>
        public MandelFrame CurrentFrame => HasFrames ? FrameBuffer[FrameBufferIndex] : null;

        /// <summary>
        /// current displayed position
        /// </summary>
        public MandelPos CurrentPos => CurrentFrame != null ? CurrentFrame.Position : MandelPos.DefaultPos;

        // handles zooming
        private readonly ZoomSelectionHandler _zoomSelectionHandler;

        // has the framebuffer already some elements?
        private bool HasFrames => FrameBuffer.Count > 0;

        public MainWindow()
        {
            InitializeComponent();

            // default render size
            RenderSizeRef = new RenderSize(0, 0);

            // initiate zooming handler
            _zoomSelectionHandler = new ZoomSelectionHandler(MandelBrotCanvas);
            _zoomSelectionHandler.ZoomSelected += ZoomSelectionHandler_ZoomSelected;

            // subscribe to color change
            ColorMappings.ItemChanged += (map) => RedrawCurrentFrame();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            // update size
            RefreshRenderSize(true);

            // add zooming rectangle
            MandelBrotCanvas.Children.Add(_zoomSelectionHandler.ZoomingRectangle);
            
            // setup data binding
            DataContext = this;
            PositionStackPanel.DataContext = this;

            // enque and render initial mandelbrot set
            SubmitFrame(new MandelFrame(this, MandelPos.DefaultPos));
        }

        /// <summary>
        /// refreshes the final pixel render size which will be used
        /// by all the rendering methods
        /// </summary>
        /// <param name="invalidateFastImage">creates a new FastImage and disposes the old one</param>
        private void RefreshRenderSize(bool invalidateFastImage = false)
        {
            if (!IsLoaded) return;

            Thread refreshThread = new Thread(() =>
            {
                // suppress rendering actions while the windows is changed
                MandelFrame.CancelToken.Cancel();
                lock (MandelFrame.RenderLock)
                {
                    MandelFrame.ResetToken();

                    // check best aspect ratio
                    double newWidth, newHeight;
                    if ((int)MandelBrotGrid.ActualWidth <= (int)MandelBrotGrid.ActualHeight)
                    {
                        // width is smaller
                        newWidth = MandelBrotGrid.ActualWidth;
                        newHeight = newWidth * ZoomSelectionHandler.GausRatioXy;
                    }
                    else
                    {
                        // height is smaller
                        newHeight = MandelBrotGrid.ActualHeight;
                        newWidth = newHeight * ZoomSelectionHandler.GausRatioYx;
                    }

                    RenderSizeRef.ChangeDisplaySize(newWidth, newHeight);

                    InvokeOnMainWindow(() =>
                    {
                        // set wpf control size
                        MandelBrotImage.Width = RenderSizeRef.DisplayWidth;
                        MandelBrotImage.Height = RenderSizeRef.DisplayHeight;
                        MandelBrotImagePreview.Width = RenderSizeRef.DisplayWidth;
                        MandelBrotImagePreview.Height = RenderSizeRef.DisplayHeight;
                        MandelBrotCanvas.Width = RenderSizeRef.DisplayWidth;
                        MandelBrotCanvas.Height = RenderSizeRef.DisplayHeight;

                        UpdateLayout();
                    });

                    if (invalidateFastImage)
                    {
                        InvokeOnMainWindow(() =>
                        {
                            // grab a new fast image
                            FastImageRef?.Dispose();
                            FastImageRef = new FastImage(MandelBrotImage, RenderSizeRef.RenderWidth, RenderSizeRef.RenderHeight);
                            
                            FastImagePreviewRef?.Dispose();
                            FastImagePreviewRef = new FastImage(MandelBrotImagePreview, RenderSizeRef.PreviewWidth, RenderSizeRef.PreviewHeight);
                        });

                        // re-render
                        CurrentFrame?.RenderAsync();
                    }
                }
            });

            refreshThread.Name = "RefreshThread";
            refreshThread.Start();
        }

        private void ZoomSelectionHandler_ZoomSelected(object sender, ZoomSelectionEventArgs e)
        {
            // magnitude of height and width in complex plane
            double complexPlaneWidth = CurrentFrame.Position.XDiff;
            double complexPlaneHeight = CurrentFrame.Position.YDiff;

            // magnitude of starting points in complex plane
            double complexRectX = (complexPlaneWidth * e.StartX) / RenderSizeRef.DisplayWidth;
            double complexRectY = (complexPlaneHeight * e.StartY) / RenderSizeRef.DisplayHeight; 

            // length of the zooming rectangle in the complex plane
            double complexRectWidth = (complexPlaneWidth * e.Width) / RenderSizeRef.DisplayWidth;
            double complexRectHeight = (complexPlaneHeight * e.Height) / RenderSizeRef.DisplayHeight;
            
            // calculate new positions in mandelbrot set
            MandelPos pos = new MandelPos(CurrentFrame.Position.XMin + complexRectX,
                CurrentFrame.Position.XMin + complexRectX + complexRectWidth,
                CurrentFrame.Position.YMin + complexRectY,
                CurrentFrame.Position.YMin + complexRectY + complexRectHeight);
            
            // enque and render
            SubmitFrame(new MandelFrame(this, pos));
        }


        /// <summary>
        /// submits a frame to the list, renders and displays it.
        /// takes care of correct databinding updates.
        /// </summary>
        /// <param name="frame">the frame to display</param>
        private void SubmitFrame(MandelFrame frame)
        {
            // calculate new index
            FrameBufferIndex = HasFrames ? FrameBufferIndex + 1 : 0;

            // submit to list
            FrameBuffer.Insert(FrameBufferIndex, frame);

            RedrawCurrentFrame();
        }


        /// <summary>
        /// rerenders the current frame.
        /// takes care of correct databinding updates.
        /// </summary>
        /// <param name="rerender">renders the current frame again</param>
        [SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
        private void RedrawCurrentFrame(bool rerender = false)
        {
            // render
            CurrentFrame?.DrawAsync();

            // update properties
            OnPropertyChanged(nameof(CurrentFrame));
            OnPropertyChanged(nameof(CurrentPos));
            OnPropertyChanged(nameof(CanStepBack));
            OnPropertyChanged(nameof(CanStepForward));
        }

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void InvokeOnMainWindow(Action action)
        {
            // dispatch on ui thread
            Application.Current?.Dispatcher.Invoke(DispatcherPriority.Background, action);
        }

        #region Handler Wrapper

        private
        void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RefreshRenderSize(true);
        }

        private void MandelBrotCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _zoomSelectionHandler.StartSelection(e);
        }

        private void MandelBrotCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            _zoomSelectionHandler.MoveSelection(e);
        }

        private void MandelBrotCanvas_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _zoomSelectionHandler.EndSelection();
        }

        private void MandelBrotCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            _zoomSelectionHandler.AbortSelection();
        }

        private void ButtonBerechnen_Click(object sender, RoutedEventArgs e)
        {
            CurrentFrame?.RenderAsync();
        }
        private void ButtonPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (CanStepBack)
            {
                FrameBufferIndex--;
                RedrawCurrentFrame();
            }
        }

        private void ButtonNext_Click(object sender, RoutedEventArgs e)
        {
            if (CanStepForward)
            {
                FrameBufferIndex++;
                RedrawCurrentFrame();
            }
        }
        private void ButtonStartPositon_Click(object sender, RoutedEventArgs e)
        {
            FrameBufferIndex = 0;
            RedrawCurrentFrame();
        }

        private void ButtonSaveBild_Click(object sender, RoutedEventArgs e)
        {
            string fileName = SaveHelper.SaveImage(MandelBrotImage);
            Process.Start(fileName);
        }

        #endregion
    }
}
