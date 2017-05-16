﻿using System;

namespace Mandelizer.Datastructures
{
    /// <summary>
    /// defines a spot within the mandelbrot set
    /// </summary>
    public class MandelPos
    {
        public double XMin { get; set; }
        public double XMax { get; set; }

        public double YMin { get; set; }
        public double YMax { get; set; }

        public double XDiff => Math.Abs(XMax - XMin);
        public double YDiff => Math.Abs(YMax - YMin);

        public MandelPos(double xmin, double xmax, double ymin, double ymax)
        {
            XMin = xmin;
            XMax = xmax;
            YMin = ymin;
            YMax = ymax;
        }

        /// <summary>
        /// recommended number of iterations for this area
        /// of the mandelbrot set. the deeper you are zooming into
        /// the mandelbrot set, the more iterations are needed.
        /// </summary>
        public int RecommendedIterations
        {
            get
            {
                double min = Math.Min(XDiff, YDiff);
                return (int) (Math.Log(1/min)*40 + 100);
            }
        }

    }
}