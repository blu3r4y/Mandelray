namespace Mandelizer.Datastructures
{
    /// <summary>
    /// Defines the width and height of the rendered mandelbrot set viewport.
    /// We distinguish between the actual render size and display size.
    /// </summary>
    public class RenderSize
    {
        public const double SupersamplingFactor = 2.0;

        public int RenderWidth { get; private set; }
        public int RenderHeight { get; private set; }

        public double DisplayWidth { get; private set; }
        public double DisplayHeight { get; private set; }

        public RenderSize(double displayWidth, double displayHeight)
        {
            ChangeDisplaySize(displayWidth, displayHeight);
        }

        public RenderSize ChangeDisplaySize(double displayWidth, double displayHeight)
        {
            DisplayWidth = displayWidth;
            DisplayHeight = displayHeight;

            RenderWidth = (int)(displayWidth * SupersamplingFactor);
            RenderHeight = (int)(displayHeight * SupersamplingFactor);

            return this;
        }
    }
}
