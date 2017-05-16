namespace Mandelizer.Datastructures
{
    /// <summary>
    /// defines the width and height of the rendered mandelbrot set viewport
    /// </summary>
    public class RenderSize
    {
        public int Width;
        public int Height;

        public RenderSize(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }
}
