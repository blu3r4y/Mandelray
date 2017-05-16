using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Image = System.Windows.Controls.Image;

namespace Mandelizer
{
    /// <summary>
    /// faster image implementation for low level set pixel performance
    /// </summary>
    public class FastImage : IDisposable
    {
        /// <summary>
        /// tells if this object is still usable and not disposed yet
        /// </summary>
        public bool Disposed { get; private set; }

        // some image characteristics
        private readonly int _width;
        private readonly int _height;
        private readonly int _bitsPerPixel = PixelFormats.Bgr32.BitsPerPixel / 8;

        private readonly WriteableBitmap _writeableBitmap;

        private BitmapContext _context;

        public FastImage(Image image, int width, int height)
        {
            _width = width;
            _height = height;

            // bind the source of the image to this map
            _writeableBitmap = BitmapFactory.New(width, height);
            _writeableBitmap.Clear(Colors.White);
            
            image.Source = _writeableBitmap;
        }

        /// <summary>
        /// sets the pixel in the image directly and fast.
        /// </summary>
        /// <param name="x">horizontal coordinate</param>
        /// <param name="y">vertical coordinate</param>
        /// <param name="color">32bit argb value of the pixel</param>
        public void SetPixel(long ptr, int x, int y, int color)
        {
            unsafe
            {
                ((int*)ptr)[y * _width + x] = color;
            }
        }

        public long Lock()
        {
            long ptr = 0;

            Application.Current.Dispatcher.Invoke(() =>
            {
                _writeableBitmap.Lock();
                ptr = (long)_writeableBitmap.BackBuffer;
            }, DispatcherPriority.Render);

            return ptr;
        }

        public void Unlock()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, _width, _height));
                _writeableBitmap.Unlock();
            }, DispatcherPriority.Render);
        }

        public long UnlockLock()
        {
            long ptr = 0;

            Application.Current.Dispatcher.Invoke(() =>
            {
                _writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, _width, _height));
                _writeableBitmap.Unlock();
                _writeableBitmap.Lock();
                ptr = (long)_writeableBitmap.BackBuffer;
            }, DispatcherPriority.Render);

            return ptr;
        }

        /// <summary>
        /// disposes all file and memory mappings.
        /// makes the whole object unusable afterwards.
        /// </summary>
        public void Dispose()
        {
            Disposed = true;
        }
    }
}

