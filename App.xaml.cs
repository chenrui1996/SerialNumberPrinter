using log4net.Config;
using Newtonsoft.Json;
using SerialNumberPrinter.Contant;
using SerialNumberPrinter.Helper;
using SerialNumberPrinter.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SerialNumberPrinter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);
                ConfigurationContant.GetConfiguration();
                ConfigurationContant.GetCurrentSerialNumber();
                PrinterContant.InitTemplateUrl();

                string path = AppDomain.CurrentDomain.BaseDirectory + "\\log4net.config";
                XmlConfigurator.Configure(new FileInfo(path));
            }
            catch 
            {
                return;
            }
            
        }
    }
}
