using Mandelizer.Datastructures;

namespace Mandelizer
{
    /// <summary>
    /// holds constants which are used all over the program
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// color gradient schema
        /// </summary>
        public const bool ColorGradientIterative = true;

        /// <summary>
        /// initial start position of mandel
        /// </summary>
        public static readonly MandelPos DefaultPos;

        /// <summary>
        /// default aspect ratio
        /// </summary>
        public static readonly double GausRatioXY, GausRatioYX;

        /// <summary>
        /// used for synchronization. only one object can have the rendering permission
        /// at any time.
        /// </summary>
        public static readonly object RenderLock = new object();

        static Constants()
        {
            // starting position
            DefaultPos = new MandelPos(-2.5, 1.5, -1.5, 1.5);

            // calculate gaussian ratios
            GausRatioXY = DefaultPos.YDiff / DefaultPos.XDiff;
            GausRatioYX = DefaultPos.XDiff / DefaultPos.YDiff;
        }
    }
}
