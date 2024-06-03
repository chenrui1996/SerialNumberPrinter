using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SerialNumberPrinter.Helper
{
    
        /// <summary>
        /// 斑马工具类,把图像转换成斑马打印机的命令
        /// </summary>
        public class ZebraUnity
        {
            #region 定义私有字段
            /// <summary>
            /// 线程锁，防止多线程调用。
            /// </summary>
            private static object SyncRoot = new object();
            /// <summary>
            /// ZPL压缩字典
            /// </summary>
            private static List<KeyValuePair<char, int>> compressDictionary = new List<KeyValuePair<char, int>>();
            #endregion

            #region 构造方法

            static ZebraUnity()
            {
                InitCompressCode();
            }

            #endregion

            #region 定义属性
            /// <summary>
            /// 图像的二进制数据
            /// </summary>
            public static byte[] GraphBuffer { get; set; }
            /// <summary>
            /// 图像的宽度
            /// </summary>
            private static int GraphWidth { get; set; }
            /// <summary>
            /// 图像的高度
            /// </summary>
            private static int GraphHeight { get; set; }
            private static int RowSize
            {
                get
                {
                    return (((GraphWidth) + 31) >> 5) << 2;
                }
            }
            /// <summary>
            /// 每行的字节数
            /// </summary>
            private static int RowRealBytesCount
            {
                get
                {
                    if ((GraphWidth % 8) > 0)
                    {
                        return GraphWidth / 8 + 1;
                    }
                    else
                    {
                        return GraphWidth / 8;
                    }
                }
            }
            #endregion

            #region 位图转斑马指令字符串
            /// <summary>
            /// 位图转斑马指令字符串
            /// </summary>
            /// <param name="bitmap">位图数据</param>
            /// <param name="totalBytes">总共的字节数</param>
            /// <param name="rowBytes">每行的字节数</param>
            /// <returns>斑马ZPL 2命令</returns>
            public static string BmpToZpl(byte[] bitmap, out int totalBytes, out int rowBytes)
            {
                try
                {
                    GraphBuffer = bitmap;
                    byte[] bmpData = getBitmapData();
                    string textHex = BitConverter.ToString(bmpData).Replace("-", string.Empty);
                    string textBitmap = CompressLZ77(textHex);
                    totalBytes = GraphHeight * RowRealBytesCount;
                    rowBytes = RowRealBytesCount;
                    return textBitmap;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            /// <summary>
            /// 位图转ZPL指令
            /// </summary>
            /// <param name="bitmap">位图</param>
            /// <param name="totalBytes">返回参数总共字节数</param>
            /// <param name="rowBytes">返回参数每行的字节数</param>
            /// <returns>ZPL命令</returns>
            public static string BmpToZpl(Image bitmap, out int totalBytes, out int rowBytes)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Bmp);
                    return BmpToZpl(stream.ToArray(), out totalBytes, out rowBytes);
                }
            }

            /// <summary>
            /// 根据图片生成图片的ASCII 十六进制
            /// </summary>
            /// <param name="sourceBmp">原始图片</param>
            /// <param name="totalBytes">总共字节数</param>
            /// <param name="rowBytes">每行的字节数</param>
            /// <returns>ASCII 十六进制</returns>
            public static string BitmapToHex(Image sourceBmp, out int totalBytes, out int rowBytes)
            {
                // 转成单色图
                Bitmap grayBmp = ConvertToGrayscale(sourceBmp as Bitmap);
                // 锁定位图数据    
                Rectangle rect = new Rectangle(0, 0, grayBmp.Width, grayBmp.Height);
                System.Drawing.Imaging.BitmapData bmpData = grayBmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, grayBmp.PixelFormat);
                // 获取位图数据第一行的起始地址     
                IntPtr ptr = bmpData.Scan0;
                // 定义数组以存放位图的字节流数据      
                // 处理像素宽对应的字节数，如不为8的倍数，则对最后一个字节补0    
                int width = (int)Math.Ceiling(grayBmp.Width / 8.0);
                // 获取位图实际的字节宽，这个值因为要考虑4的倍数关系，可能大于width  
                int stride = Math.Abs(bmpData.Stride);
                // 计算位图数据实际所占的字节数，并定义数组      
                int bitmapDataLength = stride * grayBmp.Height;
                byte[] ImgData = new byte[bitmapDataLength];
                // 从位图文件复制图像数据到数组，从实际图像数据的第一行开始；因ptr指针而无需再考虑行倒序存储的处理          
                System.Runtime.InteropServices.Marshal.Copy(ptr, ImgData, 0, bitmapDataLength);
                // 计算异或操作数，以处理包含图像数据但又有补0操作的那个字节         
                byte mask = 0xFF;
                // 计算这个字节补0的个数       
                //int offset = 8 * width - grayBmp.Width;
                int offset = 8 - (grayBmp.Width % 8);
                //offset %= 8;
                offset = offset % 8;
                // 按补0个数对0xFF做相应位数的左移位操作           
                mask <<= (byte)offset;
                // 图像反色处理        
                for (int j = 0; j < grayBmp.Height; j++)
                {
                    for (int i = 0; i < stride; i++)
                    {
                        if (i < width - 1) //无补0的图像数据
                        {
                            ImgData[j * stride + i] ^= 0xFF;
                        }
                        else if (i == width - 1) //有像素的最后一个字节，可能有补0   
                        {
                            ImgData[j * stride + i] ^= mask;
                        }
                        else  //为满足行字节宽为4的倍数而最后补的字节        
                        {
                            //ImgData[j * stride + i] = 0x00;
                            ImgData[j * stride + i] ^= 0x00;
                        }
                    }
                }
                // 将位图数据转换为16进制的ASCII字符          
                string zplString = BitConverter.ToString(ImgData);
                zplString = CompressLZ77(zplString.Replace("-", string.Empty));
                totalBytes = bitmapDataLength;
                rowBytes = stride;
                return zplString;
            }
            #endregion

            #region 获取单色位图数据
            /// <summary>
            /// 获取单色位图数据
            /// </summary>
            /// <param name="pimage"></param>
            /// <returns></returns>
            private static Bitmap ConvertToGrayscale(Bitmap pimage)
            {
                Bitmap source = null;

                // If original bitmap is not already in 32 BPP, ARGB format, then convert
                if (pimage.PixelFormat != PixelFormat.Format32bppArgb)
                {
                    source = new Bitmap(pimage.Width, pimage.Height, PixelFormat.Format32bppArgb);
                    source.SetResolution(pimage.HorizontalResolution, pimage.VerticalResolution);
                    using (Graphics g = Graphics.FromImage(source))
                    {
                        g.DrawImageUnscaled(pimage, 0, 0);
                    }
                }
                else
                {
                    source = pimage;
                }

                // Lock source bitmap in memory
                BitmapData sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                // Copy image data to binary array
                int imageSize = sourceData.Stride * sourceData.Height;
                byte[] sourceBuffer = new byte[imageSize];
                Marshal.Copy(sourceData.Scan0, sourceBuffer, 0, imageSize);

                // Unlock source bitmap
                source.UnlockBits(sourceData);

                // Create destination bitmap
                Bitmap destination = new Bitmap(source.Width, source.Height, PixelFormat.Format1bppIndexed);

                // Lock destination bitmap in memory
                BitmapData destinationData = destination.LockBits(new Rectangle(0, 0, destination.Width, destination.Height), ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);

                // Create destination buffer
                imageSize = destinationData.Stride * destinationData.Height;
                byte[] destinationBuffer = new byte[imageSize];

                int sourceIndex = 0;
                int destinationIndex = 0;
                int pixelTotal = 0;
                byte destinationValue = 0;
                int pixelValue = 128;
                int height = source.Height;
                int width = source.Width;
                int threshold = 500;

                // Iterate lines
                for (int y = 0; y < height; y++)
                {
                    sourceIndex = y * sourceData.Stride;
                    destinationIndex = y * destinationData.Stride;
                    destinationValue = 0;
                    pixelValue = 128;

                    // Iterate pixels
                    for (int x = 0; x < width; x++)
                    {
                        // Compute pixel brightness (i.e. total of Red, Green, and Blue values)
                        pixelTotal = sourceBuffer[sourceIndex + 1] + sourceBuffer[sourceIndex + 2] + sourceBuffer[sourceIndex + 3];
                        if (pixelTotal > threshold)
                        {
                            destinationValue += (byte)pixelValue;
                        }
                        if (pixelValue == 1)
                        {
                            destinationBuffer[destinationIndex] = destinationValue;
                            destinationIndex++;
                            destinationValue = 0;
                            pixelValue = 128;
                        }
                        else
                        {
                            pixelValue >>= 1;
                        }
                        sourceIndex += 4;
                    }
                    if (pixelValue != 128)
                    {
                        destinationBuffer[destinationIndex] = destinationValue;
                    }
                }

                // Copy binary image data to destination bitmap
                Marshal.Copy(destinationBuffer, 0, destinationData.Scan0, imageSize);

                // Unlock destination bitmap
                destination.UnlockBits(destinationData);

                // Dispose of source if not originally supplied bitmap
                if (source != pimage)
                {
                    source.Dispose();
                }

                // Return
                return destination;
            }
            /// <summary>
            /// 获取单色位图数据(1bpp)，不含文件头、信息头、调色板三类数据。
            /// </summary>
            /// <returns></returns>
            private static byte[] getBitmapData()
            {
                MemoryStream srcStream = new MemoryStream();
                MemoryStream dstStream = new MemoryStream();
                Bitmap srcBmp = null;
                Bitmap dstBmp = null;
                byte[] srcBuffer = null;
                byte[] dstBuffer = null;
                byte[] result = null;
                try
                {
                    srcStream = new MemoryStream(GraphBuffer);
                    srcBmp = Bitmap.FromStream(srcStream) as Bitmap;
                    srcBuffer = srcStream.ToArray();
                    GraphWidth = srcBmp.Width;
                    GraphHeight = srcBmp.Height;
                    //dstBmp = srcBmp.Clone(new Rectangle(0, 0, srcBmp.Width, srcBmp.Height), PixelFormat.Format1bppIndexed);
                    dstBmp = ConvertToGrayscale(srcBmp);
                    dstBmp.Save(dstStream, ImageFormat.Bmp);
                    dstBuffer = dstStream.ToArray();

                    result = dstBuffer;

                    int bfOffBits = BitConverter.ToInt32(dstBuffer, 10);
                    result = new byte[GraphHeight * RowRealBytesCount];

                    //读取时需要反向读取每行字节实现上下翻转的效果，打印机打印顺序需要这样读取。
                    for (int i = 0; i < GraphHeight; i++)
                    {
                        int sindex = bfOffBits + (GraphHeight - 1 - i) * RowSize;
                        int dindex = i * RowRealBytesCount;
                        Array.Copy(dstBuffer, sindex, result, dindex, RowRealBytesCount);
                    }

                    for (int i = 0; i < result.Length; i++)
                    {
                        result[i] ^= 0xFF;
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message, ex);
                }
                finally
                {
                    if (srcStream != null)
                    {
                        srcStream.Dispose();
                        srcStream = null;
                    }
                    if (dstStream != null)
                    {
                        dstStream.Dispose();
                        dstStream = null;
                    }
                    if (srcBmp != null)
                    {
                        srcBmp.Dispose();
                        srcBmp = null;
                    }
                    if (dstBmp != null)
                    {
                        dstBmp.Dispose();
                        dstBmp = null;
                    }
                }
                return result;
            }
            #endregion

            #region LZ77图像字节流压缩方法
            private static string CompressLZ77(string text)
            {
                //将转成16进制的文本进行压缩
                string result = string.Empty;
                char[] arrChar = text.ToCharArray();
                int count = 1;
                for (int i = 1; i < text.Length; i++)
                {
                    if (arrChar[i - 1] == arrChar[i])
                    {
                        count++;
                    }
                    else
                    {
                        result += convertNumber(count) + arrChar[i - 1];
                        count = 1;
                    }
                    if (i == text.Length - 1)
                    {
                        result += convertNumber(count) + arrChar[i];
                    }
                }
                return result;
            }

            private static string DecompressLZ77(string text)
            {
                string result = string.Empty;
                char[] arrChar = text.ToCharArray();
                int count = 0;
                for (int i = 0; i < arrChar.Length; i++)
                {
                    if (isHexChar(arrChar[i]))
                    {
                        //十六进制值
                        result += new string(arrChar[i], count == 0 ? 1 : count);
                        count = 0;
                    }
                    else
                    {
                        //压缩码
                        int value = GetCompressValue(arrChar[i]);
                        count += value;
                    }
                }
                return result;
            }

            private static int GetCompressValue(char c)
            {
                int result = 0;
                for (int i = 0; i < compressDictionary.Count; i++)
                {
                    if (c == compressDictionary[i].Key)
                    {
                        result = compressDictionary[i].Value;
                    }
                }
                return result;
            }

            private static bool isHexChar(char c)
            {
                return c > 47 && c < 58 || c > 64 && c < 71 || c > 96 && c < 103;
            }

            private static string convertNumber(int count)
            {
                //将连续的数字转换成LZ77压缩代码，如000可用I0表示。
                string result = string.Empty;
                if (count > 1)
                {
                    while (count > 0)
                    {
                        for (int i = compressDictionary.Count - 1; i >= 0; i--)
                        {
                            if (count >= compressDictionary[i].Value)
                            {
                                result += compressDictionary[i].Key;
                                count -= compressDictionary[i].Value;
                                break;
                            }
                        }
                    }
                }
                return result;
            }

            private static void InitCompressCode()
            {
                //G H I J K L M N O P Q R S T U V W X Y        对应1,2,3,4……18,19。
                //g h i j k l m n o p q r s t u v w x y z      对应20,40,60,80……340,360,380,400。            
                for (int i = 0; i < 19; i++)
                {
                    compressDictionary.Add(new KeyValuePair<char, int>(Convert.ToChar(71 + i), i + 1));
                }
                for (int i = 0; i < 20; i++)
                {
                    compressDictionary.Add(new KeyValuePair<char, int>(Convert.ToChar(103 + i), (i + 1) * 20));
                }
            }
            #endregion
        }
}
