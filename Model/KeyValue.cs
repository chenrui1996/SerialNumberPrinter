using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SerialNumberPrinter.Model
{
    /// <summary>
    /// ZPL压缩字典
    /// </summary>
    public class KeyValue
    {
        public char Key { set; get; }
        public int Value { set; get; }
    }
}
