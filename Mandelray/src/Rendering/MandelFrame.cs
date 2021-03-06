﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Mandelray.Datastructures;

namespace Mandelray.Rendering
{
    /// <summary>
    /// Represents a single frame with a fixed position within the mandelbrot set.
    /// </summary>
    public sealed class MandelFrame
    {
        public static CancellationTokenSource CancelToken = new CancellationTokenSource();

        /// <summary>
        /// Used for synchronization.
        /// Only one object can have the rendering permission at any time.
        /// </summary>
        public static readonly object RenderLock = new object();

        /// <summary>
        /// Stores position information about this specific frame
        /// </summary>
        public MandelPos Position { get; }

        /// <summary>
        /// Buffered data
        /// </summary>
        private int[,] _iterationStore;

        /// <summary>
        /// Maximum iterations which are used on this frame
        /// </summary>
        public int MaxIterations
        {
            get => _maxIterations;
            set
            {
                _maxIterations = value;

                // trigger an update to the view
                if (_mainWindow?.CurrentFrame != null)
                {
                    // ReSharper disable once ExplicitCallerInfoArgument
                    _mainWindow.OnPropertyChanged(nameof(_mainWindow.CurrentFrame));
                }
            }
        }

        private int _maxIterations;

        private readonly MainWindow _mainWindow;

        /// <summary>
        /// Represents a single frame with a fixed position within the Mandelbrot set.
        /// Can store the information about the iterations per pixel, as long as the rendered size doesn't change.
        /// </summary>
        /// <param name="mainWindowInstance">Reference to the calling main window, in order to use the proper image</param>
        /// <param name="position">Position of the Mandelbrot set to observe</param>
        public MandelFrame(MainWindow mainWindowInstance, MandelPos position)
        {
            Position = position;
            MaxIterations = position.RecommendedIterations;
            _mainWindow = mainWindowInstance;
        }

        public static void ResetToken()
        {
            CancelToken?.Dispose();
            CancelToken = new CancellationTokenSource();
        }

        public void RenderAsync()
        {
            var t = new Thread(Render);
            t.Start();
        }

        public void DrawAsync()
        {
            var t = new Thread(Draw);
            t.Start();
        }

        /// <summary>
        /// Renders the mandelbrot and draws it on default.
        /// </summary>
        private void Render()
        {
            // abort ongoing rendering and try to catch the lock
            CancelToken.Cancel();
            lock (RenderLock)
            {
                ResetToken();

                using (var renderer = new Renderer(Position, _mainWindow.ColorMapRef, _mainWindow.FastImagePreviewRef,
                    MaxIterations))
                {
                    //_mainWindow.FastImagePreviewRef.Clear();
                    renderer.Render(_mainWindow.RenderSizeRef.PreviewWidth, _mainWindow.RenderSizeRef.PreviewHeight,
                        CancelToken);
                }

                using (var renderer = new Renderer(Position, _mainWindow.ColorMapRef, _mainWindow.FastImageRef,
                    MaxIterations))
                {
                    _mainWindow.FastImageRef.Clear();
                    renderer.Render(_mainWindow.RenderSizeRef.RenderWidth, _mainWindow.RenderSizeRef.RenderHeight,
                        CancelToken);

                    // store reference to the buffer
                    _iterationStore = renderer.Buffer;
                }
            }
        }

        /// <summary>
        /// Just draws the frame, which is currently saved to the buffer.
        /// If the rendered size differs from the saved on the frame needs to be re-renderd.
        /// </summary>
        private void Draw()
        {
            // check if a bufferd value for the current dimensions exist
            if (_iterationStore != null &&
                _iterationStore.GetLength(0) == _mainWindow.RenderSizeRef.RenderWidth &&
                _iterationStore.GetLength(1) == _mainWindow.RenderSizeRef.RenderHeight)
            {
                // abort ongoing rendering and try to catch the lock
                CancelToken.Cancel();
                lock (RenderLock)
                {
                    ResetToken();

                    long ptr = _mainWindow.FastImageRef.Lock();
                    _mainWindow.FastImageRef.Clear();

                    var options = new ParallelOptions {CancellationToken = CancelToken.Token};

                    try
                    {
                        Parallel.For(0, _mainWindow.RenderSizeRef.RenderHeight, options, (y, state) =>
                        {
                            for (var x = 0; x < _mainWindow.RenderSizeRef.RenderWidth; x++)
                            {
                                int value = _iterationStore[x, y];
                                if (value != int.MaxValue)
                                {
                                    _mainWindow.FastImageRef.SetPixel(ptr, x, y,
                                        _mainWindow.ColorMapRef.Colors[value % _mainWindow.ColorMapRef.Colors.Length]);
                                }
                                else
                                {
                                    _mainWindow.FastImageRef.SetPixel(ptr, x, y,
                                        _mainWindow.ColorMapRef.Colors[_mainWindow.ColorMapRef.IndexMaxIterations]);
                                }
                            }
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        Trace.WriteLine("=> ## Aborted rendering from buffer.");
                    }

                    _mainWindow.FastImageRef.Unlock();
                }
            }
            else
            {
                // otherwise re-rendering with new dimensions is required
                Render();
            }
        }
    }
}