﻿using System;

namespace Mandelray.Datastructures
{
    /// <summary>
    /// Defines a spot within the mandelbrot set
    /// </summary>
    public class MandelPos
    {
        /// <summary>
        /// Initial start position of mandel
        /// </summary>
        public static readonly MandelPos DefaultPos;

        public double XMin { get; set; }
        public double XMax { get; set; }

        public double YMin { get; set; }
        public double YMax { get; set; }

        public double XDiff => Math.Abs(XMax - XMin);
        public double YDiff => Math.Abs(YMax - YMin);

        static MandelPos()
        {
            // starting position
            DefaultPos = new MandelPos(-2.5, 1.5, -1.5, 1.5);
        }

        public MandelPos(double xmin, double xmax, double ymin, double ymax)
        {
            XMin = xmin;
            XMax = xmax;
            YMin = ymin;
            YMax = ymax;
        }

        /// <summary>
        /// Recommended number of iterations for this area
        /// of the Mandelbrot set. The deeper you are zooming into
        /// the Mandelbrot set, the more iterations are needed.
        /// </summary>
        public int RecommendedIterations
        {
            get
            {
                double min = Math.Min(XDiff, YDiff);
                return (int) (Math.Log(1 / min) * 40 + 100);
            }
        }
    }
}