using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Mandelizer.Datastructures;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Mandelizer.Rendering
{
    public class Renderer : IDisposable
    {
        /// <summary>
        /// After how many lines should we invalidate the graphics?
        /// </summary>
        public const int InvalidateAfterLines = 256;

        /// <summary>
        /// Contains the number of iterations
        /// </summary>
        public int[,] Buffer { get; private set; }

        private readonly long _pixelBufferPtr;

        private readonly int _maxIterations;

        private readonly MandelPos _position;

        private readonly FastImage _fastImage;
        
        private readonly ColorMappings.ColorMap _colorMap;

        private const double OneOverLogTwo = 1.4426950408889634;

        public Renderer(MandelPos position, ColorMappings.ColorMap colorMap, FastImage fastImage, int maxIterations)
        {
            _position = position;
            _colorMap = colorMap;
            _fastImage = fastImage;
            _maxIterations = maxIterations;
            _pixelBufferPtr = fastImage.GetPointer();
        }
        
        public void Render(int width, int height, CancellationTokenSource tokenSource)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // reinit bufferd values
            Buffer = new int[width, height];

            double yMin = _position.YMin;
            double xMin = _position.XMin;

            double stepX = _position.XDiff / width;
            double stepY = _position.YDiff / height;

            int maxIterations = _maxIterations;
            int indexMaxIterations = _colorMap.IndexMaxIterations;
            int colorMapLength = _colorMap.Colors.Length;

            // local copy of color map
            int[] localColorMap = new int[colorMapLength];
            Array.Copy(_colorMap.Colors, localColorMap, colorMapLength);

            var options = new ParallelOptions { CancellationToken = tokenSource.Token };

            // start rendering from the center
            var source = new[]
            {
                // middle area
                RangeMinMaxInclusive(GetIndex(height, 0.33, false), GetIndex(height, 0.66, true)),
                // top (reversed)
                RangeMinMaxInclusive(0, GetIndex(height, 0.33, true)).Reverse(),
                // bottom
                RangeMinMaxInclusive(GetIndex(height, 0.66, false), height - 1)
            }
            .SelectMany(x => x).ToArray();

            // if (source.Distinct().Count() == source.Count()) throw new Exception("implementation error");
            
            OrderablePartitioner<int> partitioner = Partitioner.Create(source, true);

            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                ParallelLoopResult result = Parallel.ForEach(partitioner, options, () => 0, (y, state, threadLocal) =>
                    {
                        double zRe, zIm, zReSq, zImSq, zReTmp;
                        int iterations;

                        // imaginary axes step
                        double cIm = yMin + y * stepY;
                        // real axes
                        double cRe = xMin;

                        for (var x = 0; x < width; x++)
                        {
                            // reset maxIterations and z
                            zRe = 0;
                            zIm = 0;
                            iterations = 0;
                            zReSq = zRe * zRe;
                            zImSq = zIm * zIm;

                            // iterate until the max number of maxIterations was not reached
                            // or the value's magnitude  falls below 2
                            while (iterations < maxIterations && zReSq + zImSq < 4)
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

                            if (iterations < maxIterations)
                            {
                                // smooth coloring algorithm
                                double zLog = Math.Log(zReSq + zImSq) / 2;
                                var nu = (int) (Math.Log(zLog * OneOverLogTwo) * OneOverLogTwo);
                                iterations = iterations + 1 - nu;

                                // directly write to pixel buffer
                                _fastImage.SetPixel(_pixelBufferPtr, x, y, localColorMap[iterations % colorMapLength]);
                            }
                            else
                            {
                                iterations = Int32.MaxValue;

                                // write a max iteration pixel
                                _fastImage.SetPixel(_pixelBufferPtr, x, y, localColorMap[indexMaxIterations]);
                            }

                            // save for further access
                            Buffer[x, y] = iterations;

                            // real axes step
                            cRe += stepX;
                        }

                        return threadLocal;
                    },
                    threadLocal =>
                    {
                        _fastImage.Dirty();

                        Trace.WriteLine($"=> Dirty requested.");
                    });
            }
            catch (OperationCanceledException)
            {
                Trace.WriteLine("=> ## Aborted calculation.");
            }

            stopwatch.Stop();

            _fastImage.Dirty();

            Trace.WriteLine($"=> Calculation took {stopwatch.ElapsedMilliseconds} milliseconds.");
            
        }

        private static int GetIndex(int value, double rate, bool lower)
        {
            double factor = 1.0 / rate;
            double part = value / factor;

            if (value % factor == 0)
            {
                return (int) (lower ? part : part + 1);
            }
            else
            {
                return (int) (lower ? Math.Floor(part) : Math.Ceiling(part));
            }
        }

        private static IEnumerable<int> RangeMinMaxInclusive(int min, int max)
        {
            return Enumerable.Range(min, max - min + 1);
        }

        public void Dispose()
        {
            Buffer = null;
        }
    }
}
