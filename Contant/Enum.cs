using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SerialNumberPrinter.Contant
{
    public enum PrintType
    {
        General = 10,
        Continuously = 20
    }

    public enum ProgrammingLanguage
    {
        Zpl = 10,
        Epl = 20,
        Cpcl = 30
    }

    public enum DeviceType
    {
        Com = 10,
        Lpt = 20,
        Tcp = 30,
        Drv = 40
    }

    public enum LogType
    {
        Print = 10,
        Error = 99,
    }
}
