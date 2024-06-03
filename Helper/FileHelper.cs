using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialNumberPrinter.Helper
{
    /// <summary>
    /// 文件操作
    /// </summary>
    public class FileHelper
    {
        public string ReadAllText(string path)
        {
            if (System.IO.File.Exists(path))
            {
                return System.IO.File.ReadAllText(@path);
            }
            return "";
        }

        public void WriteAllText(string path, string data)
        {
            if (System.IO.File.Exists(path))
            {
                System.IO.File.WriteAllText(@path, data);
            }
        }
    }
}
