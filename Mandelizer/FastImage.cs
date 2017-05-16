using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace Mandelizer
{
    public class FastImage
    {
        // linked image object
        private readonly Image _wrappedImage = new Image();

        // constants for pinvoke methods
        private const uint FileMapAllAccess = 0xF001F;
        private const uint PageReadwrite = 0x04;

        // some image characteristics
        private readonly int _width;
        private readonly int _height;
        private readonly int _bitsPerPixel = PixelFormats.Bgr32.BitsPerPixel / 8;
        private readonly InteropBitmap _interopBitmap;

        // pointer into the memory mapped file view
        private readonly unsafe int* _viewPtr;

        /// <summary>
        /// creates or opens a named or unnamed file mapping object for a specified file
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateFileMapping(
            IntPtr hFile,
            IntPtr lpFileMappingAttributes,
            uint flProtect,
            uint dwMaximumSizeHigh,
            uint dwMaximumSizeLow,
            string lpName);

        /// <summary>
        /// maps a view of a file mapping into the address space of a calling process
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr MapViewOfFile(
          IntPtr hFileMappingObject,
          uint dwDesiredAccess,
          uint dwFileOffsetHigh,
          uint dwFileOffsetLow,
          uint dwNumberOfBytesToMap);
        
        public FastImage(Image image, int width, int height)
        {
            _width = width;
            _height = height;

            _wrappedImage.Width = width;
            _wrappedImage.Height = height;

            // create a file mapping with the size of the image
            IntPtr fileMappingPtr = CreateFileMapping(new IntPtr(-1),
                IntPtr.Zero,
                PageReadwrite,
                0,
                (uint) (_width * _height * _bitsPerPixel),
                null);

            // get managed bitmap source
            int stride = _width * _bitsPerPixel;
            _interopBitmap = Imaging.CreateBitmapSourceFromMemorySection(fileMappingPtr,
                _width, _height,
                PixelFormats.Bgr32,
                stride,
                0) as InteropBitmap;

            _wrappedImage.Source = _interopBitmap;

            unsafe
            {
                // map the file mapping into the memory space
                _viewPtr = (int*)MapViewOfFile(fileMappingPtr,
                    FileMapAllAccess,
                    0, 0,
                    (uint) (_width * _height * 4)).ToPointer();
            }

            // bind the source of the image to this map
            image.Source = _wrappedImage.Source;
        }

        /// <summary>
        /// sets the pixel in the image directly and fast
        /// </summary>
        /// <param name="x">horizontal coordinate</param>
        /// <param name="y">vertical coordinate</param>
        /// <param name="color">32bit argb value of the pixel</param>
        public void SetPixel(int x, int y, int color)
        {
            // check boundaries
            x = x < _width ? x : _width - 1;
            y = y < _height ? y : _height - 1;

            unsafe
            {
                // set pixel directly
                _viewPtr[y * _width + x] = color;
            }
        }

        /// <summary>
        /// refreshes the coordinate mapping instantly (slow)
        /// </summary>
        public void Invalidate()
        {
            _interopBitmap.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    _interopBitmap.Invalidate();
                }));
        }
    }
}

