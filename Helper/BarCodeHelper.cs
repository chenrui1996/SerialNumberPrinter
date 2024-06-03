
using BarcodeStandard;
using SkiaSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace SerialNumberPrinter.Helper
{
    public class BarCodeHelper
    {
        public Bitmap GenerateLabel(string barCode, string dc, string revision, int width, int height)
        {
            var bitmap = new Bitmap(width, height);
            // 生成条形码图像
            var barcodeImage = new Barcode
            {
                IncludeLabel = false,
                Alignment = AlignmentPositions.Center
            }.Encode(BarcodeStandard.Type.Code128, barCode, width, height / 2);
            var barcodeBitmap = ConvertSKImageToBitmap(barcodeImage);
            //var resizedBitmap = ResizeBitmap(barcodeBitmap, width - 30, height / 2);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                g.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
                g.DrawImage(barcodeBitmap, 0, 6);
            }
            //bitmap.Save(@"C:\tempPic\temp"+DateTime.Now.Ticks+".png");
            //写入字符
            WriteText(bitmap, barCode, dc, revision);
            return bitmap;
        }

        private Bitmap ConvertSKImageToBitmap(SKImage skImage)
        {
            // 将 SKImage 编码为 PNG 格式的字节数组
            using (var skData = skImage.Encode(SKEncodedImageFormat.Png, 100))
            {
                byte[] imageBytes = skData.ToArray();

                // 使用字节数组创建 MemoryStream
                using (var ms = new MemoryStream(imageBytes))
                {
                    // 使用 MemoryStream 创建 Bitmap
                    Bitmap bitmap = new Bitmap(ms);
                    return bitmap;
                }
            }
        }

        private Bitmap ResizeBitmap(Bitmap originalBitmap, int newWidth, int newHeight)
        {
            // 创建一个新的Bitmap，其尺寸为新的宽度和高度
            Bitmap resizedBitmap = new Bitmap(newWidth, newHeight);

            // 创建Graphics对象以进行绘制
            using (Graphics graphics = Graphics.FromImage(resizedBitmap))
            {
                // 设置绘制质量
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                // 绘制原始Bitmap到新的Bitmap，进行缩放
                graphics.DrawImage(originalBitmap, 0, 0, newWidth, newHeight);
            }

            return resizedBitmap;
        }

        private void WriteText(Bitmap bitmap, string barCode, string dc, string revision)
        {
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                DrawString(graphics, barCode, 90, 48, "Arial", 12, FontStyle.Regular);
                DrawString(graphics, string.Format("Rev {0}", revision), 16, 56, "Arial", 9, FontStyle.Regular);
                DrawString(graphics, dc, 220, 56, "Arial", 9, FontStyle.Regular);
            }
        }

        private void DrawString(Graphics graphics, string data, int x, int y, string font, int fontSize, FontStyle fontStyle)
        {
            // 设置文本的绘制位置
            PointF textPosition = new PointF(x, y); // 文本在图像上的位置
            // 创建文本画刷
            using (Brush textBrush = new SolidBrush(Color.Black))
            {
                // 绘制文本
                graphics.DrawString(data, new Font(font, fontSize, fontStyle), textBrush, textPosition);
            }
        }

        private BitmapImage ConvertToBitmapImage(SKImage skImage)
        {
            SKData skData = skImage.Encode();
            byte[] imageBytes = skData.ToArray();
            using (var memory = new MemoryStream(imageBytes))
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }
    }

    public static class BarCodeHelperExtend
    {
        public static BitmapImage ConvertBitmapToBitmapImage(this Bitmap bitmap)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                // 将 Bitmap 保存到 MemoryStream
                bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                memoryStream.Position = 0;

                // 创建 BitmapImage 并加载 MemoryStream 中的数据
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // 使得 BitmapImage 可以跨线程使用

                return bitmapImage;
            }
        }

        public static byte[] ConvertBitmapTBytes(this Bitmap bitmap)
        {

            // 将 Bitmap 转换为黑白图像
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Color originalColor = bitmap.GetPixel(x, y);
                    // 转换为灰度
                    int grayScale = (int)(originalColor.R * 0.3 + originalColor.G * 0.59 + originalColor.B * 0.11);
                    Color newColor = Color.FromArgb(grayScale, grayScale, grayScale);
                    bitmap.SetPixel(x, y, newColor);
                }
            }
            using (MemoryStream output = new MemoryStream())
            {
                // Save monochrome bitmap as PNG to memory stream
                bitmap.Save(output, ImageFormat.Png);
                output.Seek(0, SeekOrigin.Begin);

                // Convert the data to hex string
                byte[] imageData = output.ToArray();
                return imageData;
            }
        }
    }

}
