using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Mandelizer.Datastructures;

namespace Mandelizer
{
    /// <summary>
    /// represents a single frame with a fixed position within the mandelbrot set.
    /// </summary>
    public sealed class MandelFrame
    {
        /// <summary>
        /// stores position information about this specific frame
        /// </summary>
        public MandelPos Position { get; }

        /// <summary>
        /// suspends all threads and stops rendering
        /// </summary>
        public static bool AbortRenderingFlag;

        // bufferd data
        private int[,] _iterationStore;

        /// <summary>
        /// maximum iterations which are used on this frame
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
        /// represents a single frame with a fixed position
        /// within the mandelbrot set. can store the information about
        /// the iterations per pixel, as long as the rendered size doesn't change.
        /// </summary>
        /// <param name="mainWindowInstance">reference to the calling main window, in order to use the proper image</param>
        /// <param name="position">position of the mandelbrot set to observe</param>
        public MandelFrame(MainWindow mainWindowInstance, MandelPos position)
        {
            Position = position;
            MaxIterations = position.RecommendedIterations;
            _mainWindow = mainWindowInstance;
        }

        public void RenderAsync(bool draw = true)
        {
            var t = new Thread(Render);
            t.Start(draw);
        }

        public void DrawAsync()
        {
            var t = new Thread(Draw);
            t.Start();
        }

        /// <summary>
        /// renders the mandelbrot and draws it on default.
        /// </summary>
        /// <param name="obj">states if the mandelbrot frame should also be drawn directly</param>
        [SuppressMessage("ReSharper", "TooWideLocalVariableScope")]
        [SuppressMessage("ReSharper", "JoinDeclarationAndInitializer")]
        private void Render(object obj = null)
        {
            // cast value
            if (!(obj is bool)) throw new ArgumentException();
            var draw = (bool)obj;

            // time measurement
            DateTime startTime = DateTime.Now;

            // reinit bufferd values
            _iterationStore = new int[_mainWindow.RenderSizeRef.RenderWidth, _mainWindow.RenderSizeRef.RenderHeight];

            // step width
            double stepX = Position.XDiff/_mainWindow.RenderSizeRef.RenderWidth;
            double stepY = Position.YDiff/_mainWindow.RenderSizeRef.RenderHeight;

            int maxIt = MaxIterations;

            // abort ongoing rendering and try to catch the lock
            AbortRenderingFlag = true;
            lock (Constants.RenderLock)
            {
                long ptr = _mainWindow.FastImageRef.Lock();

                AbortRenderingFlag = false;

                Parallel.For(0, _mainWindow.RenderSizeRef.RenderHeight, (y, state) =>
                {

                    if (AbortRenderingFlag)
                    {
                        // abort execution of wanted
                        state.Stop();
                        return;
                    }

                    // declare imaginary numbers here once
                    double cIm, cRe, zRe, zIm, zReSq, zImSq, zReTmp;
                    int iterations;

                    // imaginary axes step
                    cIm = Position.YMin + y * stepY;

                    // real axes
                    cRe = Position.XMin;
                    for (var x = 0; x < _mainWindow.RenderSizeRef.RenderWidth; x++)
                    {
                        // reset maxIterations and z
                        zRe = 0;
                        zIm = 0;
                        iterations = 0;
                        zReSq = zRe * zRe;
                        zImSq = zIm * zIm;

                        // iterate until the max number of maxIterations was not reached
                        // or the value's magnitude  falls below 2
                        while (iterations < maxIt && zReSq + zImSq < 4)
                        {
                            // z = z^2 + c
                            zReTmp = zRe;
                            zRe = zReSq - zImSq + cRe;
                            zIm = zReTmp * zIm;
                            zIm = zIm + zIm + cIm;

                            zReSq = zRe * zRe;
                            zImSq = zIm * zIm;

                            iterations++;
                        }

                        // save for further access
                        _iterationStore[x, y] = iterations;

                        // map used maxIterations to color
                        if (draw) SetPixel(ptr, x, y, iterations);

                        // real axes step
                        cRe += stepX;
                    }

                    // refresh image every x vertical lines
                    if (y % 10 == 0)
                    {
                        ptr = _mainWindow.FastImageRef.UnlockLock();
                    }
                });

                _mainWindow.FastImageRef.Unlock();

                Trace.WriteLine($"calculated mandelbrot in {(DateTime.Now - startTime).TotalMilliseconds} seconds.");
            }
        }

        /// <summary>
        /// just draws the frame, which is currently saved to the buffer.
        /// if the rendered size differs from the saved on the frame needs to be re-renderd.
        /// </summary>
        private void Draw()
        {
            // check if a bufferd value for the current dimensions exist
            if (_iterationStore != null &&
                _iterationStore.GetLength(0) == _mainWindow.RenderSizeRef.RenderWidth &&
                _iterationStore.GetLength(1) == _mainWindow.RenderSizeRef.RenderHeight)
            {
                long ptr = _mainWindow.FastImageRef.Lock();

                // abort ongoing rendering and try to catch the lock
                AbortRenderingFlag = true;
                lock (Constants.RenderLock)
                {
                    AbortRenderingFlag = false;
                    
                    Parallel.For(0, _mainWindow.RenderSizeRef.RenderHeight, (y, state) =>
                    {
                        if (AbortRenderingFlag)
                        {
                            // abort execution of wanted
                            state.Stop();
                            return;
                        }

                        for (var x = 0; x < _mainWindow.RenderSizeRef.RenderWidth; x++)
                        {
                            SetPixel(ptr, x, y, _iterationStore[x, y]);
                        }

                        // refresh image every 100 vertical lines
                        // if (y % 100 == 0) _mainWindow.FastImageRef.Invalidate();
                    });

                    _mainWindow.FastImageRef.Unlock();
                }
            }
            else
            {
                // otherwise re-rendering with new dimensions is required
                Render(true);
            }
        }
        
        /// <summary>
        /// sets the pixel by considerung the used color schema.
        /// this pseudo method should be inlined by the compiler if possible,
        /// so it can be safley used inside loops with multiple calls to it.
        /// </summary>
        /// <param name="x">horizontal position</param>
        /// <param name="y">vertical position</param>
        /// <param name="iterationsValue">the needed maxIterations for this pixel</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetPixel(long ptr, int x, int y, int iterationsValue)
        {
            int mappedIt;

            if (Constants.ColorGradientIterative)
            {
                if (iterationsValue == MaxIterations) mappedIt = (_mainWindow.ColorMapRef.Length - 1);
                else mappedIt = iterationsValue - iterationsValue / _mainWindow.ColorMapRef.Length * _mainWindow.ColorMapRef.Length;
            }
            else
            {
                mappedIt = (_mainWindow.ColorMapRef.Length - 1) * iterationsValue / MaxIterations;
            }
            
            // set pixel
            if (!_mainWindow.FastImageRef.Disposed)
            {
                _mainWindow.FastImageRef.SetPixel(ptr, x, y, _mainWindow.ColorMapRef[mappedIt].ToArgb());
            }
        }
    }
}
