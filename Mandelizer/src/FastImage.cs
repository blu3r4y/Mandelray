using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

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

        // constants for pinvoke methods
        private const uint FileMapAllAccess = 0xF001F;
        private const uint PageReadwrite = 0x04;

        // linked image object
        private readonly Image _wrappedImage = new Image();
        
        // some image characteristics
        private readonly int _width;
        private readonly int _height;
        private readonly int _bitsPerPixel = PixelFormats.Bgr32.BitsPerPixel / 8;

        // managed bitmap source
        private readonly InteropBitmap _interopBitmap;
        
        // handle to the mapped file
        private readonly IntPtr _fileHandle;

        // pointer into the memory mapped file view
        private readonly unsafe int* _viewPtr;

        /// <summary>
        /// creates or opens a named or unnamed file mapping object for a specified file.
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
        /// maps a view of a file mapping into the address space of a calling process.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr MapViewOfFile(
          IntPtr hFileMappingObject,
          uint dwDesiredAccess,
          uint dwFileOffsetHigh,
          uint dwFileOffsetLow,
          uint dwNumberOfBytesToMap);

        /// <summary>
        /// unmaps a mapped view of a file from the calling process's address space.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        /// <summary>
        /// closes an open object handle.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        public FastImage(Image image, int width, int height)
        {
            _width = width;
            _height = height;

            _wrappedImage.Width = width;
            _wrappedImage.Height = height;

            // create a file mapping with the size of the image
            _fileHandle = CreateFileMapping(new IntPtr(-1),
                IntPtr.Zero,
                PageReadwrite,
                0,
                (uint)(_width * _height * _bitsPerPixel),
                null);

            // get managed bitmap source
            int stride = _width * _bitsPerPixel;
            _interopBitmap = Imaging.CreateBitmapSourceFromMemorySection(_fileHandle,
                _width, _height,
                PixelFormats.Bgr32,
                stride,
                0) as InteropBitmap;

            _wrappedImage.Source = _interopBitmap;

            unsafe
            {
                // map the file mapping into the memory space
                _viewPtr = (int*)MapViewOfFile(_fileHandle,
                    FileMapAllAccess,
                    0, 0,
                    (uint)(_width * _height * 4)).ToPointer();
            }

            // bind the source of the image to this map
            image.Source = _wrappedImage.Source;
        }

        /// <summary>
        /// sets the pixel in the image directly and fast.
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
        /// refreshes the coordinate mapping instantly.
        /// </summary>
        public void Invalidate()
        {
            _interopBitmap.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                _interopBitmap.Invalidate();
            }));
        }

        /// <summary>
        /// disposes all file and memory mappings.
        /// makes the whole object unusable afterwards.
        /// </summary>
        public void Dispose()
        {
            Disposed = true;

            CloseHandle(_fileHandle);

            unsafe
            {
                UnmapViewOfFile((IntPtr) _viewPtr);
            }
        }
    }
}

