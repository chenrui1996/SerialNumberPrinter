using HandyControl.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SerialNumberPrinter.Helper;
using SerialNumberPrinter.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialNumberPrinter.Contant
{
    public static class ConfigurationContant
    {
        public static string SerialNumberUrl = "\\serialnumber.json";

        public static string ConfigurationUrl = "\\config.json";

        private static int _currentSerialNumber;

        public static int CurrentSerialNumber 
        { 
            set
            {
                if (_currentSerialNumber != value)
                {
                    _currentSerialNumber = value;
                    UpdateCurrentSerialNumber();
                }
            }
            get
            {
                GetCurrentSerialNumber();
                return _currentSerialNumber;
            }
        }

        public static ConfigurationModel? Configuration { set; get; }

        public static void GetConfiguration()
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            if (string.IsNullOrEmpty(exeDir))
            {
                return;
            }
            var url = exeDir + ConfigurationUrl;
            var result = new FileHelper().ReadAllText(url);
            Configuration = JsonConvert.DeserializeObject<ConfigurationModel>(result);
        }

        public static void UpdateConfiguration()
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            if (string.IsNullOrEmpty(exeDir))
            {
                return;
            }
            var url = exeDir + ConfigurationUrl;
            var data = JsonConvert.SerializeObject(Configuration);
            new FileHelper().WriteAllText(url, data);
        }

        public static void GetCurrentSerialNumber()
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            if (string.IsNullOrEmpty(exeDir))
            {
                return;
            }
            var url = exeDir + SerialNumberUrl;
            var result = new FileHelper().ReadAllText(url);
            var data = JsonConvert.DeserializeObject<JObject>(result);
            if (data == null || !data.ContainsKey("CurrentSerialNumber"))
            {
                _currentSerialNumber = -1;
                return;
            }
            _currentSerialNumber = int.Parse(data["CurrentSerialNumber"]?.ToString() ?? "-1");
        }

        public static void UpdateCurrentSerialNumber()
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            if (string.IsNullOrEmpty(exeDir))
            {
                return;
            }
            var url = exeDir + SerialNumberUrl;
            var data = JsonConvert.SerializeObject(new
            {
                CurrentSerialNumber = _currentSerialNumber
            });
            new FileHelper().WriteAllText(url, data);
        }
    }

    public static class ConfigurationContantExtend
    {
        public static string ToHexString(this int src)
        {
            return src.ToString("X6");
        }

        public static string ToYYWWString(this DateTime dateTime)
        {
            string year = dateTime.ToString("yy");

            // 获取当前周数
            CultureInfo ci = CultureInfo.CurrentCulture;
            int weekNum = ci.Calendar.GetWeekOfYear(dateTime, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            // 格式化为 YYWW
            return $"{year}{weekNum:D2}";
        }
    }
}
