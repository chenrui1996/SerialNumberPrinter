using System;
using System.Runtime.InteropServices;

namespace SerialNumberPrinter.Helper
{
    internal static class LptPrintHelper
    {
        //C#LPT端口打印类的操作  
        private static int _iHandle;

        [DllImport("kernel32.dll")]
        private static extern int CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            int dwShareMode,
            int lpSecurityAttributes,
            int dwCreationDisposition,
            int dwFlagsAndAttributes,
            int hTemplateFile
            );

        [DllImport("kernel32.dll")]
        private static extern bool WriteFile(
            int hFile,
            byte[] lpBuffer,
            int nNumberOfBytesToWrite,
            ref int lpNumberOfBytesWritten,
            ref Overlapped lpOverlapped
            );

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(
            int hObject
            );

        public static bool Open(string lptStr)
        {
            _iHandle = CreateFile(lptStr, 0x40000000, 0, 0, 3, 0, 0);
            if (_iHandle != -1)
            {
                return true;
            }
            return false;
        }

        public static bool Write(string lptStr, byte[] mybyte)
        {
            if (!Open(lptStr))
            {
                return false;
            }
            ;
            if (!Write(mybyte))
            {
                return false;
            }
            ;
            if (!Close())
            {
                return false;
            }
            return true;
        }

        public static bool Write(byte[] mybyte)
        {
            if (_iHandle != -1)
            {
                var i = 0;
                var x = new Overlapped();
                return WriteFile(
                    _iHandle, mybyte, mybyte.Length, ref i, ref x);
            }
            throw new Exception("端口未打开!");
        }

        public static bool Close()
        {
            return CloseHandle(_iHandle);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Overlapped
        {
            private readonly int Internal;
            private readonly int InternalHigh;
            private readonly int Offset;
            private readonly int OffSetHigh;
            private readonly int hEvent;
        }
    }
}