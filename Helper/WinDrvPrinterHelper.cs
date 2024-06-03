using SerialNumberPrinter.Contant;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace SerialNumberPrinter.Helper
{
    public static class WinDrvPrinterHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct Docinfow
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDocName;
            [MarshalAs(UnmanagedType.LPWStr)]
            private readonly string pOutputFile;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDataType;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct StructPrinterDefaults
        {
            [MarshalAs(UnmanagedType.LPTStr)]
            private readonly String pDatatype;
            private readonly IntPtr pDevMode;
            [MarshalAs(UnmanagedType.I4)]
            private readonly int DesiredAccess;
        };

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool OpenPrinter(string pPrinterName, out IntPtr hPrinter, IntPtr pDefault);

        [DllImport("winspool.Drv", EntryPoint = "OpenPrinter", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = false, CallingConvention = CallingConvention.StdCall), SuppressUnmanagedCodeSecurity()]
        private static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPTStr)] string printerName,out IntPtr phPrinter, ref StructPrinterDefaults pd);

        [DllImport("winspool.Drv", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterW", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int level, ref Docinfow pDi);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);


        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);


        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);


        [DllImport("winspool.Drv", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, ref int dwWritten);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool GetPrinter(IntPtr hPrinter,
            int dwLevel, IntPtr pPrinter, int cbBuf, out int pcbNeeded);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct PRINTER_INFO_2
        {
            public string pServerName;
            public string pPrinterName;
            public string pShareName;
            public string pPortName;
            public string pDriverName;
            public string pComment;
            public string pLocation;
            public IntPtr pDevMode;
            public string pSepFile;
            public string pPrintProcessor;
            public string pDatatype;
            public string pParameters;
            public IntPtr pSecurityDescriptor;
            public uint Attributes;
            public uint Priority;
            public uint DefaultPriority;
            public uint StartTime;
            public uint UntilTime;
            public uint Status;
            public uint cJobs;
            public uint AveragePPM;
        }

        public static bool SendBytesToPrinter(string szPrinterName, byte[] pBytes, Int32 dwCount)
        {
            var hPrinter = System.IntPtr.Zero;
            var di = new Docinfow();
            var dwWritten = 0;
            di.pDocName = "My Document";
            di.pDataType = "RAW";
            var bSuccess = false;
            var pd = new StructPrinterDefaults();
            if (OpenPrinter(szPrinterName, out hPrinter,ref pd))
            {
                if (StartDocPrinter(hPrinter, 1, ref di))
                {
                    if (StartPagePrinter(hPrinter))
                    {
                        bSuccess = WritePrinter(hPrinter, PrinterContant.BytesToIntptr(pBytes), dwCount, ref dwWritten);
                        EndPagePrinter(hPrinter);
                    }
                    EndDocPrinter(hPrinter);
                }
                ClosePrinter(hPrinter);
            }
            if (bSuccess == false)
            {
                Marshal.GetLastWin32Error();
            }
            return bSuccess;

        }

        public static bool SendFileToPrinter(string szPrinterName, string szFileName)
        {
            var stream1 = new FileStream(szFileName, FileMode.Open);
            var reader1 = new BinaryReader(stream1);
            var buffer1 = reader1.ReadBytes((int)stream1.Length);
            var ptr1 = Marshal.AllocCoTaskMem((int)stream1.Length);
            Marshal.Copy(buffer1, 0, ptr1, (int)stream1.Length);
            var flag1 = SendBytesToPrinter(szPrinterName, buffer1, (int)stream1.Length);
            Marshal.FreeCoTaskMem(ptr1);
            return flag1;

        }

        public static void SendStringToPrinter(string szPrinterName, string szString)
        {
            var dwCount = szString.Length;
            var pBytes = Encoding.Default.GetBytes(szString);
            SendBytesToPrinter(szPrinterName, pBytes, dwCount);
        }

        /// <summary>
        /// 获取打印机的状态编码(方式1)
        /// </summary>
        /// <returns></returns>
        public static int GetPrinterStatusCodeInt(string printerName)
        {
            int intRet = 0;
            IntPtr hPrinter;

            if (OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
            {
                int cbNeeded = 0;
                bool bolRet = GetPrinter(hPrinter, 2, IntPtr.Zero, 0, out cbNeeded);
                if (cbNeeded > 0)
                {
                    IntPtr pAddr = Marshal.AllocHGlobal((int)cbNeeded);
                    bolRet = GetPrinter(hPrinter, 2, pAddr, cbNeeded, out cbNeeded);
                    if (bolRet)
                    {
                        PRINTER_INFO_2 Info2 = new PRINTER_INFO_2();
                        Info2 = (PRINTER_INFO_2)Marshal.PtrToStructure(pAddr, typeof(PRINTER_INFO_2));
                        intRet = System.Convert.ToInt32(Info2.Status);
                    }
                    Marshal.FreeHGlobal(pAddr);
                }
                ClosePrinter(hPrinter);
            }


            return intRet;
        }
        /// <summary>
        /// 检查打印机是否可用(方式1)
        /// </summary>
        /// <param name="intStatusCodeValue"></param>
        /// <returns></returns>
        public static bool CheckIsEnable(int intStatusCodeValue)
        {
            if (intStatusCodeValue == 0 || intStatusCodeValue == 0x0008000 || intStatusCodeValue == 0x00000400 || intStatusCodeValue == 0x00004000)
            {
                return true;
            }
            return false;
        }


        /// <summary>
        /// 获取打印机的状态信息(方式1)
        /// </summary>
        /// <returns></returns>
        public static string GetPrinterStatusMessage(int intStatusCodeValue)
        {
            string strRet = string.Empty;
            switch (intStatusCodeValue)
            {
                case 0:
                    strRet = "准备就绪（Ready）";
                    break;
                case 0x00000200:
                    strRet = "忙(Busy）";
                    break;
                case 0x00400000:
                    strRet = "被打开（Printer Door Open）";
                    break;
                case 0x00000002:
                    strRet = "错误(Printer Error）";
                    break;
                case 0x0008000:
                    strRet = "初始化(Initializing）";
                    break;
                case 0x00000100:
                    strRet = "正在输入,输出（I/O Active）";
                    break;
                case 0x00000020:
                    strRet = "手工送纸（Manual Feed）";
                    break;
                case 0x00040000:
                    strRet = "无墨粉（No Toner）";
                    break;
                case 0x00001000:
                    strRet = "不可用（Not Available）";
                    break;
                case 0x00000080:
                    strRet = "脱机（Off Line）";
                    break;
                case 0x00200000:
                    strRet = "内存溢出（Out of Memory）";
                    break;
                case 0x00000800:
                    strRet = "输出口已满（Output Bin Full）";
                    break;
                case 0x00080000:
                    strRet = "当前页无法打印（Page Punt）";
                    break;
                case 0x00000008:
                    strRet = "塞纸（Paper Jam）";
                    break;
                case 0x00000010:
                    strRet = "打印纸用完（Paper Out）";
                    break;
                case 0x00000040:
                    strRet = "纸张问题（Page Problem）";
                    break;
                case 0x00000001:
                    strRet = "暂停（Paused）";
                    break;
                case 0x00000004:
                    strRet = "正在删除（Pending Deletion）";
                    break;
                case 0x00000400:
                    strRet = "正在打印（Printing）";
                    break;
                case 0x00004000:
                    strRet = "正在处理（Processing）";
                    break;
                case 0x00020000:
                    strRet = "墨粉不足（Toner Low）";
                    break;
                case 0x00100000:
                    strRet = "需要用户干预（User Intervention）";
                    break;
                case 0x20000000:
                    strRet = "等待（Waiting）";
                    break;
                case 0x00010000:
                    strRet = "热机中（Warming Up）";
                    break;
                default:
                    strRet = "未知状态（Unknown Status）";
                    break;
            }
            return strRet;
        }
    }
}
