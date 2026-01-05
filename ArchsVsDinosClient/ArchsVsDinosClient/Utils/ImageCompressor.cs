using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ArchsVsDinosClient.Utils
{
    public static class ImageCompressor
    {
        public static byte[] CompressImage(byte[] imageBytes, int maxSizeKB = 50, int maxWidth = 300)
        {
            try
            {
                using (var ms = new MemoryStream(imageBytes))
                {
                    var decoder = BitmapDecoder.Create(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    var originalFrame = decoder.Frames[0];

                    int newWidth = maxWidth;
                    int newHeight = (int)(originalFrame.PixelHeight * ((double)newWidth / originalFrame.PixelWidth));

                    if (originalFrame.PixelWidth < maxWidth)
                    {
                        newWidth = originalFrame.PixelWidth;
                        newHeight = originalFrame.PixelHeight;
                    }

                    var transformedBitmap = new TransformedBitmap(originalFrame, new ScaleTransform(
                        (double)newWidth / originalFrame.PixelWidth,
                        (double)newHeight / originalFrame.PixelHeight
                    ));

                    int quality = 90;
                    byte[] compressedBytes;

                    do
                    {
                        using (var outputStream = new MemoryStream())
                        {
                            var encoder = new JpegBitmapEncoder
                            {
                                QualityLevel = quality
                            };
                            encoder.Frames.Add(BitmapFrame.Create(transformedBitmap));
                            encoder.Save(outputStream);
                            compressedBytes = outputStream.ToArray();
                        }

                        if (compressedBytes.Length <= maxSizeKB * 1024)
                            break;

                        quality -= 10;

                    } while (quality > 10);

                    return compressedBytes;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with the image: {ex.Message}");
                throw;
            }
        }

        public static byte[] CompressForAvatar(byte[] imageBytes)
        {
            return CompressImage(imageBytes, maxSizeKB: 40, maxWidth: 200);
        }

        public static bool IsImageSizeValid(byte[] imageBytes, int maxSizeKB = 50)
        {
            return imageBytes != null && imageBytes.Length <= maxSizeKB * 1024;
        }

        public static double GetImageSizeKB(byte[] imageBytes)
        {
            return imageBytes != null ? Math.Round(imageBytes.Length / 1024.0, 2) : 0;
        }

        public static byte[] CompressFromFile(string filePath, int maxSizeKB = 50, int maxWidth = 300)
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            return CompressImage(fileBytes, maxSizeKB, maxWidth);
        }
    }
}
