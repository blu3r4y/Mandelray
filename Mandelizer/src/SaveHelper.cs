using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Mandelizer
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
            Microsoft.Win32.SaveFileDialog SaveImageDialog = new Microsoft.Win32.SaveFileDialog();

            SaveImageDialog.DefaultExt = ".bmp";
            SaveImageDialog.Filter = "JPG Image (.jpg)|*.jpg|Bitmap Image (.bmp)|*.bmp";

            if (SaveImageDialog.ShowDialog() == true)
            {
                if (SaveImageDialog.FilterIndex == 0)
                {
                    var encoder = new JpegBitmapEncoder();
                    SaveUsingEncoder(image, SaveImageDialog.FileName, encoder);
                }
                else
                {
                    var encoder = new BmpBitmapEncoder();
                    SaveUsingEncoder(image, SaveImageDialog.FileName, encoder);
                }
            }

            return SaveImageDialog.FileName;
        }

        private static void SaveUsingEncoder(FrameworkElement visual, string fileName, BitmapEncoder encoder)
        {
            RenderTargetBitmap bitmap = new RenderTargetBitmap(
                (int)visual.ActualWidth,
                (int)visual.ActualHeight,
                96,
                96,
                System.Windows.Media.PixelFormats.Pbgra32);

            bitmap.Render(visual);
            BitmapFrame frame = BitmapFrame.Create(bitmap);
            encoder.Frames.Add(frame);

            using (var stream = File.Create(fileName))
            {
                encoder.Save(stream);
            }
        }
    }
}
