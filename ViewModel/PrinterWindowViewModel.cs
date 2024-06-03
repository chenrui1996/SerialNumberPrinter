﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SerialNumberPrinter.Contant;
using SerialNumberPrinter.Helper;
using SerialNumberPrinter.Service;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SerialNumberPrinter.ViewModel
{
    public class PrinterWindowViewModel : ObservableRecipient
    {
        public PrinterWindowViewModel()
        {
            ProductFamilyItems = new ObservableCollection<string>();
            PrintCommand = new RelayCommand(Print);
            init();
        }

        #region content
        private ObservableCollection<string> _productFamilyItems;

        public ObservableCollection<string> ProductFamilyItems 
        {
            get { return _productFamilyItems; }
            set
            {
                if (_productFamilyItems != value)
                {
                    SetProperty(ref _productFamilyItems, value);
                }
            }
        }

        private void init()
        {
            try
            {
                UserDefinedFlag = false;
                UserDefinedVisibility = UserDefinedFlag ? "Visible" : "Collapsed";
                CodeDefinedVisibility = !UserDefinedFlag ? "Visible" : "Collapsed";
                if (ConfigurationContant.Configuration == null || ConfigurationContant.CurrentSerialNumber == -1)
                {
                    MessageBox.Show("配置文件加载失败", "初始化失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Application.Current.Shutdown();
                    });
                    return;
                }
                if (ConfigurationContant.Configuration.ProductSuffixs == null
                    || !ConfigurationContant.Configuration.ProductSuffixs.Any())
                {
                    MessageBox.Show("配置文件加载失败", "初始化失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Application.Current.Shutdown();
                    });
                    return;
                }
                var temp = ConfigurationContant.Configuration.ProductSuffixs.Select(s =>
                    string.Format("{0} {1}-{2}", s.ProductFamily, s.Naed, s.Suffix)
                );
                if (temp == null)
                {
                    MessageBox.Show("配置文件加载失败", "初始化失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Application.Current.Shutdown();
                    });
                    return;
                }
                ProductFamilyItems.Clear();
                foreach (var item in temp)
                {
                    ProductFamilyItems.Add(item);
                }
                SN = ConfigurationContant.CurrentSerialNumber.ToHexString();
                DC = DateTime.Now.ToYYWWString();
                ProductFamily = ProductFamilyItems.First();
                Copies = 1;
                Task.Run(() =>
                {
                    while (true)
                    {
                        PrintService.GetUseablePrinter();
                        if (string.IsNullOrEmpty(PrintService.PrintName))
                        {
                            DriverName = "无可用打印机";
                            DriverStatus = "";
                        }
                        else
                        {
                            DriverName = PrintService.PrintName;
                            DriverStatus = PrintService.PrintStatus;
                        }
                        Thread.Sleep(5000);
                    }
                });
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "初始化失败", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Thread.Sleep(1000);
                    Application.Current.Shutdown();
                });
            }
        }


        private string _productFamily;

        public string ProductFamily
        {
            get { return _productFamily; }
            set
            {
                if (_productFamily != value)
                {
                    SetProperty(ref _productFamily, value);
                    var arr = ProductFamily.Split("-");
                    if (arr.Length < 2)
                    {
                        return;
                    }
                    Suffix = arr[1];
                    var rev = ConfigurationContant.Configuration
                        ?.ProductSuffixs?.Where(r => r.Suffix == Suffix).FirstOrDefault()
                        ?.Revision ?? 0;
                    Revision = rev.ToString().PadLeft(2, '0');
                    Barcode = string.Format("{0}-{1}", SN, Suffix);
                    _labelbitmap = new BarCodeHelper().GenerateLabel(Barcode, DC, Revision, PrinterContant.LabelWidth, PrinterContant.LabelHight);
                    LabelImg = _labelbitmap.ConvertBitmapToBitmapImage();
                }
            }
        }

        private string _suffix;

        public string Suffix
        {
            get { return _suffix; }
            set
            {
                if (_suffix != value)
                {
                    SetProperty(ref _suffix, value);
                }
            }
        }

        private string _dc;

        public string DC
        {
            get { return _dc; }
            set
            {
                if (_dc != value)
                {
                    SetProperty(ref _dc, value);
                    _labelbitmap = new BarCodeHelper().GenerateLabel(Barcode, DC, Revision, PrinterContant.LabelWidth, PrinterContant.LabelHight);
                    LabelImg = _labelbitmap.ConvertBitmapToBitmapImage();
                }
            }
        }

        private string _sn;

        public string SN
        {
            get { return _sn; }
            set
            {
                if (_sn != value)
                {
                    SetProperty(ref _sn, value);
                    Barcode = string.Format("{0}-{1}", SN, Suffix);
                }
            }
        }

        private string _revision;

        public string Revision
        {
            get { return _revision; }
            set
            {
                if (_revision != value)
                {
                    SetProperty(ref _revision, value);
                }
            }
        }

        private string _barcode;

        public string Barcode
        {
            get { return _barcode; }
            set
            {
                if (_barcode != value)
                {
                    SetProperty(ref _barcode, value);
                    _labelbitmap = new BarCodeHelper().GenerateLabel(Barcode, DC, Revision, PrinterContant.LabelWidth, PrinterContant.LabelHight);
                    LabelImg = _labelbitmap.ConvertBitmapToBitmapImage();
                }
            }
        }

        private BitmapImage _labelImg;

        public BitmapImage LabelImg
        {
            get { return _labelImg; }
            set
            {
                if (_labelImg != value)
                {
                    SetProperty(ref _labelImg, value);
                }
            }
        }

        private Bitmap _labelbitmap { get; set; }

        private bool _userDefinedFlag;
        public bool UserDefinedFlag 
        {
            get { return _userDefinedFlag; }
            set
            {
                if (_userDefinedFlag != value)
                {
                    SetProperty(ref _userDefinedFlag, value);
                    UserDefinedVisibility = UserDefinedFlag ? "Visible" : "Collapsed";
                    CodeDefinedVisibility = !UserDefinedFlag ? "Visible" : "Collapsed";
                    if (!UserDefinedFlag)
                    {
                        SN = ConfigurationContant.CurrentSerialNumber.ToHexString();
                        DC = DateTime.Now.ToYYWWString();
                    }
                }
            }
        }

        private string _userDefinedVisibility;
        public string UserDefinedVisibility
        {
            get { return _userDefinedVisibility; }
            set
            {
                if (_userDefinedVisibility != value)
                {
                    SetProperty(ref _userDefinedVisibility, value);
                }
            }
        }

        private string _codeDefinedVisibility;
        public string CodeDefinedVisibility
        {
            get { return _codeDefinedVisibility; }
            set
            {
                if (_codeDefinedVisibility != value)
                {
                    SetProperty(ref _codeDefinedVisibility, value);
                }
            }
        }

        #endregion

        #region printer driver
        private string? _driverName;

        public string? DriverName
        {
            get { return _driverName; }
            set
            {
                if (_driverName != value)
                {
                    SetProperty(ref _driverName, value);
                }
            }
        }

        private string? _driverStatus;

        public string? DriverStatus
        {
            get { return _driverStatus; }
            set
            {
                if (_driverStatus != value)
                {
                    SetProperty(ref _driverStatus, value);
                }
            }
        }
        #endregion

        #region print
        private int _copies;

        public int Copies
        {
            get { return _copies; }
            set
            {
                if (_copies != value)
                {
                    SetProperty(ref _copies, value);
                }
            }
        }

        public ICommand PrintCommand { get; }

        private void Print()
        {
            try
            {
                var returnCode = "";
                LogHelper.Info($"start print [Barcode:{Barcode}, DC:{DC}, Revision:{Revision}, Copies:{Copies}, IsUserDefined:{UserDefinedFlag}]");
                //var re = new PrintService().PrintWithTemplet(Barcode, DC, Revision, Copies, ref returnCode);
                var re = new PrintService().PrintWithImg(_labelbitmap, Copies, ref returnCode);
                if (!re)
                {
                    LogHelper.Info($"print failed! reason [{returnCode}]");
                    MessageBox.Show(returnCode, "打印失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                LogHelper.Info($"print success!");
                if (UserDefinedFlag)
                {
                    return;
                }
                ConfigurationContant.CurrentSerialNumber = ConfigurationContant.CurrentSerialNumber + 1;
                SN = ConfigurationContant.CurrentSerialNumber.ToHexString();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

    }
}
