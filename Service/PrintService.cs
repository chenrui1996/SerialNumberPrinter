using SerialNumberPrinter.Contant;
using SerialNumberPrinter.Helper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Printing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SerialNumberPrinter.Service
{
    /// <summary>
    /// 打印
    /// </summary>
    public class PrintService
    {
        public static string PrintName  { set; get; } = "";
        public static string PrintStatus { set; get; } = "";
        public bool PrintWithTemplet(string barCode, string dc, string revision, int copies, ref string returnCode)
        {
            try
            {
                if (string.IsNullOrEmpty(PrintName))
                {
                    returnCode = "未安装打印机！";
                    return false;
                }
                var cmd = PrinterContant.GetZplStrFromFile(PrinterContant.TemplateUrl, barCode, dc, revision, copies);
                if (string.IsNullOrWhiteSpace(cmd))
                {
                    returnCode = "模板读取失败！";
                    return false;
                }
                LogHelper.Info($"GetZplStrFromFile[{cmd}]");
                return ZebraPrintHelper.PrintWithDrv(cmd, PrintName, copies);
            }
            catch (Exception e)
            {
                returnCode = e.Message;
                return false;
            }
        }

        public bool PrintWithImg(Bitmap _labelbitmap, int copies, ref string returnCode)
        {
            try
            {
                if (string.IsNullOrEmpty(PrintName))
                {
                    returnCode = "未安装打印机！";
                    return false;
                }
                //_labelbitmap.Save(@$"C:\tempPic\{DateTime.Now.Ticks}.png");
                var bytes = _labelbitmap.ConvertBitmapTBytes();
                if (bytes.Length == 0)
                {
                    returnCode = "图片加载失败！";
                    return false;
                }
                //StringBuilder hexString = new StringBuilder(bytes.Length * 2);
                //foreach (byte b in bytes)
                //{
                //    hexString.AppendFormat("{0:x2}", b);
                //}
                //LogHelper.Debug($"bytes:\r\n{hexString}");
                return ZebraPrintHelper.PrintWithDrv(bytes, PrintName, copies);
            }
            catch (Exception e)
            {
                returnCode = e.Message;
                return false;
            }
        }

        /// <summary>
        /// 获取第一个可用斑马打印机
        /// </summary>
        public static void GetUseablePrinter()
        {
            try
            {
                // 获取本地打印服务器上的所有打印队列
                var printers = PrinterSettings.InstalledPrinters;
                foreach (var printer in printers)
                {
                    if (printer?.ToString()?.StartsWith("ZDesigner") ?? false)
                    {
                        var printName = printer?.ToString() ?? "";
                        var stCode = WinDrvPrinterHelper.GetPrinterStatusCodeInt(printName);
                        if (WinDrvPrinterHelper.CheckIsEnable(stCode))
                        {
                            PrintName = printName;
                            PrintStatus = WinDrvPrinterHelper.GetPrinterStatusMessage(stCode);
                            return;
                        }
                    }
                }
                PrintName = "";
            }
            catch
            {
                PrintName = "";
            }

            
        }

        
    }
}
