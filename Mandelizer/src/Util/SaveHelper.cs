using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Mandelizer.Util
{
    public static class SaveHelper
    {
        /// <summary>
        /// saves an image to a file and returns the path
        /// </summary>
        /// <param name="image">image control to save as image</param>
        /// <returns>the selected path</returns>
        public static string SaveImage(System.Windows.Controls.Image image)
        {
            var saveImageDialog = new Microsoft.Win32.SaveFileDialog
            {
                DefaultExt = ".bmp",
                Filter = "JPG Image (.jpg)|*.jpg|Bitmap Image (.bmp)|*.bmp"
            };


            if (saveImageDialog.ShowDialog() == true)
            {
                if (saveImageDialog.FilterIndex == 0)
                {
                    var encoder = new JpegBitmapEncoder();
                    SaveUsingEncoder(image, saveImageDialog.FileName, encoder);
                }
                else
                {
                    var encoder = new BmpBitmapEncoder();
                    SaveUsingEncoder(image, saveImageDialog.FileName, encoder);
                }
            }

            return saveImageDialog.FileName;
        }

        private static void SaveUsingEncoder(FrameworkElement visual, string fileName, BitmapEncoder encoder)
        {
            var bitmap = new RenderTargetBitmap(
                (int)visual.ActualWidth,
                (int)visual.ActualHeight,
                96,
                96,
                System.Windows.Media.PixelFormats.Pbgra32);

            bitmap.Render(visual);
            BitmapFrame frame = BitmapFrame.Create(bitmap);
            encoder.Frames.Add(frame);

            using (FileStream stream = File.Create(fileName))
            {
                encoder.Save(stream);
            }
        }
    }
}
