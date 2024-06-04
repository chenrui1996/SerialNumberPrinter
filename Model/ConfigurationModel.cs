using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialNumberPrinter.Model
{
    public class ConfigurationModel
    {
        public int HorizontalFeed { set; get; }
        public int DefultCopies { set; get; }
        public int DefultColums { set; get; }
        public int PrintDarkness { set; get; }
        public List<ProductSuffix>? ProductSuffixs { set; get; }
    }

    public class ProductSuffix
    {
        public string? ProductFamily { set; get; }

        public string? Naed { set; get; }

        public string? Suffix { set; get; }

        public int Revision { set; get; }
    }
}
