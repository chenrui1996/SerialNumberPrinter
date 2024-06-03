using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using SerialNumberPrinter.Helper;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace SerialNumberPrinter.Contant
{
    /// <summary>
    /// 常量及常用方法
    /// </summary>
    public static class PrinterContant
    {
        public static int LabelHight = 80;
        public static int LabelWidth = 275;

        /// <summary>
        /// 
        /// </summary>
        public static string TemplateUrl = "";


        public static void InitTemplateUrl()
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            if (string.IsNullOrEmpty(exeDir))
            {
                return;
            }
            TemplateUrl = exeDir + "\\Template\\Template.txt";
        }
        /// <summary>
        /// byte转IntPtr
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static IntPtr BytesToIntptr(byte[] bytes)
        {
            var size = bytes.Length;
            var buffer = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(bytes, 0, buffer, size);
                return buffer;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        /// <summary>
        /// 获取Zpl指令
        /// </summary>
        /// <param name="path"></param>
        /// <param name="barcode"></param>
        /// <param name="copies"></param>
        /// <returns></returns>
        public static string GetZplStrFromFile(string path, string barCode, string dc, string revision, int copies)
        {
            var result = new FileHelper().ReadAllText(path);
            result = result.Replace("$BarCode$", barCode);
            result = result.Replace("$DC$", dc);
            result = result.Replace("$Revision$", revision);
            result = result.Replace("$Copies$", copies.ToString());
            return result;
        }

        //public static string GetZplStrFromImg(Bitmap _labelbitmap, int copies)
        //{
        //    var result = new FileHelper().ReadAllText(path);
        //    result = result.Replace("$BarCode$", barCode);
        //    result = result.Replace("$DC$", dc);
        //    result = result.Replace("$revision$", revision);
        //    result = result.Replace("$Copies$", copies.ToString());
        //    return result;
        //}
    }
}
