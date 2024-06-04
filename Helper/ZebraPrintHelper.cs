using SerialNumberPrinter.Contant;
using SerialNumberPrinter.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace SerialNumberPrinter.Helper
{
    /// <summary>
    /// 斑马打印助手,支持LPT/COM/USB/TCP四种模式
    /// </summary>
    public static class ZebraPrintHelper
    {
        #region 私有字段
        /// <summary>
        /// 线程锁，防止多线程调用。
        /// </summary>
        private static readonly object SyncRoot = new object();
        /// <summary>
        /// ZPL压缩字典
        /// </summary>
        private static readonly List<KeyValue> CompressDictionary = new List<KeyValue>();

        /// <summary>
        /// 图像宽度
        /// </summary>
        private static int _graphWidth;

        /// <summary>
        /// 图像高度
        /// </summary>
        private static int _graphHeight;
        /// <summary>
        /// 行
        /// </summary>
        private static int RowSize => (((_graphWidth) + 31) >> 5) << 2;
        /// <summary>
        /// 行实际字节数
        /// </summary>
        private static int RowRealBytesCount
        {
            get
            {
                if ((_graphWidth % 8) > 0)
                {
                    return _graphWidth / 8 + 1;
                }
                else
                {
                    return _graphWidth / 8;
                }
            }
        }
        /// <summary>
        /// 图像buffer
        /// </summary>
        private static byte[] GraphBuffer { get; set; }
        #endregion

        #region 定义属性
        public static float TcpLabelMaxHeightCm { get; set; } = 1000;
        public static int TcpPrinterDpi { get; set; } = 300;
        public static string TcpIpAddress { get; set; } = "127.0.0.1";
        public static int TcpPort { get; set; } = 2001;

        private static int _copies;
        private static int _port;
        private static string? _printerName;
        private static DeviceType _printerType;
        private static ProgrammingLanguage _printerProgrammingLanguage;
        #endregion

        #region 静态构造方法
        static ZebraPrintHelper()
        {
            InitCompressCode();
            _port = 1;
            GraphBuffer = new byte[0];
            _printerProgrammingLanguage = ProgrammingLanguage.Zpl;
        }

        /// <summary>
        /// 初始化ZPL压缩字典
        /// </summary>
        private static void InitCompressCode()
        {
            //G H I J K L M N O P Q R S T U V W X Y        对应1,2,3,4……18,19。
            //g h i j k l m n o p q r s t u v w x y z      对应20,40,60,80……340,360,380,400。            
            for (var i = 0; i < 19; i++)
            {
                CompressDictionary.Add(new KeyValue() {Key = Convert.ToChar(71 + i), Value = i + 1});
            }
            for (var i = 0; i < 20; i++)
            {
                CompressDictionary.Add(new KeyValue() {Key = Convert.ToChar(103 + i), Value = (i + 1)*20});
            }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 串口打印[cmd]
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="port"></param>
        /// <param name="copies"></param>
        /// <param name="printerProgrammingLanguage"></param>
        /// <returns></returns>
        public static bool PrintWithCom(string cmd, int port, int copies = 1, ProgrammingLanguage printerProgrammingLanguage = ProgrammingLanguage.Zpl)
        {
            _printerType = DeviceType.Com;
            _port = port;
            _copies = copies;
            _printerProgrammingLanguage = printerProgrammingLanguage;
            return PrintCommand(cmd);
        }

        /// <summary>
        /// 串口打印[cmd]
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="port"></param>
        /// <param name="copies"></param>
        /// <param name="printerProgrammingLanguage"></param>
        /// <returns></returns>
        public static bool PrintWithCom(byte[] bytes, int port, int copies = 1, ProgrammingLanguage printerProgrammingLanguage = ProgrammingLanguage.Zpl)
        {
            _printerType = DeviceType.Com;
            _port = port;
            _copies = copies;
            _printerProgrammingLanguage = printerProgrammingLanguage;
            return PrintGraphics(bytes);
        }

        /// <summary>
        /// lpt打印[cmd]
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="port"></param>
        /// <param name="copies"></param>
        /// <param name="printerProgrammingLanguage"></param>
        /// <returns></returns>
        public static bool PrintWithLpt(string cmd, int port, int copies = 1, ProgrammingLanguage printerProgrammingLanguage = ProgrammingLanguage.Zpl)
        {
            _printerType = DeviceType.Lpt;
            _port = port;
            _copies = copies;
            var result = "";
            for (var i = 0; i < copies; i++)
            {
                result += cmd;
            }
            return PrintCommand(result);
        }

        /// <summary>
        /// lpt打印[图像]
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="port"></param>
        /// <param name="copies"></param>
        /// <param name="printerProgrammingLanguage"></param>
        /// <returns></returns>
        public static bool PrintWithLpt(byte[] bytes, int port, int copies = 1, ProgrammingLanguage printerProgrammingLanguage = ProgrammingLanguage.Zpl)
        {
            _printerType = DeviceType.Lpt;
            _port = port;
            _copies = copies;
            _printerProgrammingLanguage = printerProgrammingLanguage;
            return PrintGraphics(bytes);
        }

        /// <summary>
        /// TCP打印[cmd]
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="copies"></param>
        /// <param name="printerProgrammingLanguage"></param>
        /// <returns></returns>
        public static bool PrintWithTcp(string cmd, int copies = 1, ProgrammingLanguage printerProgrammingLanguage = ProgrammingLanguage.Zpl)
        {
            _printerType = DeviceType.Tcp;
            _copies = copies;
            _printerProgrammingLanguage = printerProgrammingLanguage;
            return PrintCommand(cmd);
        }

        /// <summary>
        /// TCP打印[图像]
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="copies"></param>
        /// <param name="printerProgrammingLanguage"></param>
        /// <returns></returns>
        public static bool PrintWithTcp(byte[] bytes, int copies = 1, ProgrammingLanguage printerProgrammingLanguage = ProgrammingLanguage.Zpl)
        {
            _printerType = DeviceType.Tcp;
            _copies = copies;
            _printerProgrammingLanguage = printerProgrammingLanguage;
            return PrintGraphics(bytes);
        }

        /// <summary>
        /// 驱动打印[cmd]
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="printerName"></param>
        /// <param name="copies"></param>
        /// <param name="printerProgrammingLanguage"></param>
        /// <returns></returns>
        public static bool PrintWithDrv(string cmd, string printerName, int copies = 1, ProgrammingLanguage printerProgrammingLanguage = ProgrammingLanguage.Zpl)
        {
            _printerType = DeviceType.Drv;
            _printerName = printerName;
            _copies = copies;
            _printerProgrammingLanguage = printerProgrammingLanguage;
            return PrintCommand(cmd);
        }

        /// <summary>
        /// 驱动打印[图像]
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="printerName"></param>
        /// <param name="copies"></param>
        /// <param name="printerProgrammingLanguage"></param>
        /// <returns></returns>
        public static bool PrintWithDrv(byte[] bytes, string printerName, int copies = 1, ProgrammingLanguage printerProgrammingLanguage = ProgrammingLanguage.Zpl)
        {
            _printerType = DeviceType.Drv;
            _printerName = printerName;
            _copies = copies;
            _printerProgrammingLanguage = printerProgrammingLanguage;
            return PrintGraphics(bytes);
        }
        #endregion

        #region 打印ZPL、EPL、CPCL、TCP指令
        /// <summary>
        /// 打印指令
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        private static bool PrintCommand(string cmd)
        {
            lock (SyncRoot)
            {
                var result = false;
                try
                {
                    switch (_printerType)
                    {
                        case DeviceType.Com:
                            result = ComPrint(Encoding.Default.GetBytes(cmd));
                            break;
                        case DeviceType.Lpt:
                            result = LptPrint(Encoding.Default.GetBytes(cmd));
                            break;
                        case DeviceType.Drv:
                            result = DrvPrint(Encoding.Default.GetBytes(cmd));
                            break;
                        case DeviceType.Tcp:
                            result = TcpPrint(Encoding.Default.GetBytes(cmd));
                            break;
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message, ex);
                }
                finally
                {
                    GraphBuffer = new byte[0];
                }
                return result;
            }
        }
        #endregion

        #region 打印图像
        private static bool PrintGraphics(byte[] graph)
        {
            lock (SyncRoot)
            {
                var result = false;
                try
                {
                    GraphBuffer = graph;
                    var cmdBytes = new byte[0];
                    switch (_printerProgrammingLanguage)
                    {
                        case ProgrammingLanguage.Zpl:
                            cmdBytes = GetZplBytes();
                            break;
                        case ProgrammingLanguage.Epl:
                            cmdBytes = GetEplBytes();
                            break;
                        case ProgrammingLanguage.Cpcl:
                            cmdBytes = GetCpclBytes();
                            break;
                    }
                    switch (_printerType)
                    {
                        case DeviceType.Com:
                            result = ComPrint(cmdBytes);
                            break;
                        case DeviceType.Lpt:
                            result = LptPrint(cmdBytes);
                            break;
                        case DeviceType.Drv:
                            result = DrvPrint(cmdBytes);
                            break;
                        case DeviceType.Tcp:
                            result = TcpPrint(cmdBytes);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message, ex);
                }
                finally
                {
                    GraphBuffer = new byte[0];
                }
                return result;
            }
        }
        #endregion

        #region 打印指令
        private static bool DrvPrint(byte[] cmdBytes)
        {
            try
            {
                if (!string.IsNullOrEmpty(_printerName))
                {
                    return WinDrvPrinterHelper.SendBytesToPrinter(_printerName, cmdBytes, cmdBytes.Count());
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception("打印失败!", ex);
            }
        }

        private static bool ComPrint(byte[] cmdBytes)
        {
            var com = new SerialPort(string.Format("{0}{1}", _printerType, _port), 9600, Parity.None, 8,
                StopBits.One);
            try
            {
                com.Open();
                com.Write(cmdBytes, 0, cmdBytes.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("打印失败!", ex);
            }
            finally
            {
                if (com.IsOpen)
                {
                    com.Close();
                }
            }
            return true;
        }

        private static bool LptPrint(byte[] cmdBytes)
        {
            return LptPrintHelper.Write(string.Format("{0}{1}", _printerType, _port), cmdBytes);
        }

        private static bool TcpPrint(byte[] cmdBytes)
        {
            var tcp = new TcpClient();
            try
            {
                tcp.Connect(TcpIpAddress, TcpPort);
                tcp.SendTimeout = 1000;
                tcp.ReceiveTimeout = 1000;
                if (tcp.Connected)
                {
                    tcp.Client.Send(cmdBytes);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("打印失败，请检查打印机或网络设置。", ex);
            }
            finally
            {
                if (tcp.Client != null)
                {
                    tcp.Client.Close();
                    tcp.Client.Dispose();
                }
                tcp.Close();
            }
            return true;
        }
        #endregion

        #region 生成ZPL图像打印指令
        private static byte[] GetZplBytes()
        {
            var bmpData = GetBitmapData();
            var bmpDataLength = bmpData.Length;
            for (var i = 0; i < bmpDataLength; i++)
            {
                bmpData[i] ^= 0xFF;
            }
            var copiesString = string.Empty;
            var textHex = BitConverter.ToString(bmpData).Replace("-", string.Empty);
            var textBitmap = CompressLz77(textHex);
            for (var i = 0; i < _copies; i++)
            {
                copiesString += $"^XA^MD{PrinterContant.PrintDarkness}^FO0,0^XGR:IMAGE.GRF,1,1^FS^XZ";
            }
            var text = string.Format("^XA^IDR:*.GRF^XZ~DGR:IMAGE.GRF,{0},{1},{2}{3}",
                _graphHeight*RowRealBytesCount,
                RowRealBytesCount,
                textBitmap,
                copiesString);
            //LogHelper.Debug($"text\r\n{text}");
            return Encoding.UTF8.GetBytes(text);
        }
        #endregion

        #region 生成EPL图像打印指令
        private static byte[] GetEplBytes()
        {
            var buffer = GetBitmapData();
            var text = string.Format("N\r\nGW{0},{1},{2},{3},{4}\r\nP{5},1\r\n", 0, 0,
                RowRealBytesCount,
                _graphHeight,
                Encoding.GetEncoding("iso-8859-1").GetString(buffer),
                _copies
            );
            return Encoding.GetEncoding("iso-8859-1").GetBytes(text);
        }
        #endregion

        #region 生成CPCL图像打印指令
        private static byte[] GetCpclBytes()
        {
            var bmpData = GetBitmapData();
            var bmpDataLength = bmpData.Length;
            for (var i = 0; i < bmpDataLength; i++)
            {
                bmpData[i] ^= 0xFF;
            }
            var textHex = BitConverter.ToString(bmpData).Replace("-", string.Empty);
            var text = string.Format("! {0} {1} {2} {3} {4}\r\nEG {5} {6} {7} {8} {9}\r\nFORM\r\nPRINT\r\n",
                0, //水平偏移量
                TcpPrinterDpi, //横向DPI
                TcpPrinterDpi, //纵向DPI
                (int) (TcpLabelMaxHeightCm / 2.54f * TcpPrinterDpi), //标签最大像素高度=DPI*标签纸高度(英寸)
                _copies, //份数
                RowRealBytesCount, //图像的字节宽度
                _graphHeight, //图像的像素高度
                0, //横向的开始位置
                0, //纵向的开始位置
                textHex
                );
            return Encoding.UTF8.GetBytes(text);
        }
        #endregion

        #region 获取单色位图数据

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pimage"></param>
        /// <returns></returns>
        private static Bitmap ConvertToGrayscale(Bitmap pimage)
        {
            Bitmap source;
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
            var sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var imageSize = sourceData.Stride*sourceData.Height;
            var sourceBuffer = new byte[imageSize];
            Marshal.Copy(sourceData.Scan0, sourceBuffer, 0, imageSize);
            source.UnlockBits(sourceData);
            var destination = new Bitmap(source.Width, source.Height, PixelFormat.Format1bppIndexed);
            var destinationData = destination.LockBits(
                new Rectangle(0, 0, destination.Width, destination.Height), ImageLockMode.WriteOnly,
                PixelFormat.Format1bppIndexed);
            imageSize = destinationData.Stride*destinationData.Height;
            var destinationBuffer = new byte[imageSize];

            var height = source.Height;
            var width = source.Width;
            const int threshold = 500;

            // Iterate lines
            for (var y = 0; y < height; y++)
            {
                var sourceIndex = y*sourceData.Stride;
                var destinationIndex = y*destinationData.Stride;
                byte destinationValue = 0;
                var pixelValue = 128;

                // Iterate pixels
                for (int x = 0; x < width; x++)
                {
                    // Compute pixel brightness (i.e. total of Red, Green, and Blue values)
                    var pixelTotal = sourceBuffer[sourceIndex + 1] + sourceBuffer[sourceIndex + 2] +
                                     sourceBuffer[sourceIndex + 3];
                    if (pixelTotal > threshold)
                    {
                        destinationValue += (byte) pixelValue;
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
        private static byte[] GetBitmapData()
        {
            var srcStream = new MemoryStream();
            var dstStream = new MemoryStream();
            Bitmap? srcBmp = null;
            Bitmap? dstBmp = null;
            byte[] result;
            try
            {
                srcStream = new MemoryStream(GraphBuffer);
                srcBmp = Bitmap.FromStream(srcStream) as Bitmap;
                srcStream.ToArray();
                if (srcBmp != null)
                {
                    _graphWidth = srcBmp.Width;
                    _graphHeight = srcBmp.Height;
                    //dstBmp = srcBmp.Clone(new Rectangle(0, 0, srcBmp.Width, srcBmp.Height), PixelFormat.Format1bppIndexed);
                    dstBmp = ConvertToGrayscale(srcBmp);
                }
                dstBmp?.Save(dstStream, ImageFormat.Bmp);
                var dstBuffer = dstStream.ToArray();

                var bfOffBits = BitConverter.ToInt32(dstBuffer, 10);
                result = new byte[_graphHeight*RowRealBytesCount];

                //读取时需要反向读取每行字节实现上下翻转的效果，打印机打印顺序需要这样读取。
                for (var i = 0; i < _graphHeight; i++)
                {
                    Array.Copy(dstBuffer, bfOffBits + (_graphHeight - 1 - i) * RowSize, result, i*RowRealBytesCount,
                        RowRealBytesCount);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
            finally
            {
                srcStream.Dispose();
                dstStream.Dispose();
                srcBmp?.Dispose();
                dstBmp?.Dispose();
            }
            return result;
        }

        #endregion

        #region LZ77图像字节流压缩方法

        private static string CompressLz77(string text)
        {
            //将转成16进制的文本进行压缩
            var result = string.Empty;
            var arrChar = text.ToCharArray();
            var count = 1;
            for (var i = 1; i < text.Length; i++)
            {
                if (arrChar[i - 1] == arrChar[i])
                {
                    count++;
                }
                else
                {
                    result += ConvertNumber(count) + arrChar[i - 1];
                    count = 1;
                }
                if (i == text.Length - 1)
                {
                    result += ConvertNumber(count) + arrChar[i];
                }
            }
            return result;
        }

        public static string DecompressLz77(string text)
        {
            var result = string.Empty;
            var arrChar = text.ToCharArray();
            var count = 0;
            foreach (var t in arrChar)
            {
                if (IsHexChar(t))
                {
                    //十六进制值
                    result += new string(t, count == 0 ? 1 : count);
                    count = 0;
                }
                else
                {
                    //压缩码
                    var value = GetCompressValue(t);
                    count += value;
                }
            }
            return result;
        }

        private static int GetCompressValue(char c)
        {
            var result = 0;
            foreach (var t in CompressDictionary)
            {
                if (c == t.Key)
                {
                    result = t.Value;
                }
            }
            return result;
        }

        private static bool IsHexChar(char c)
        {
            return c > 47 && c < 58 || c > 64 && c < 71 || c > 96 && c < 103;
        }

        private static string ConvertNumber(int count)
        {
            //将连续的数字转换成LZ77压缩代码，如000可用I0表示。
            var result = string.Empty;
            if (count <= 1)
                return result;
            while (count > 0)
            {
                for (var i = CompressDictionary.Count - 1; i >= 0; i--)
                {
                    if (count < CompressDictionary[i].Value)
                        continue;
                    result += CompressDictionary[i].Key;
                    count -= CompressDictionary[i].Value;
                    break;
                }
            }
            return result;
        }

        #endregion
    }
}
