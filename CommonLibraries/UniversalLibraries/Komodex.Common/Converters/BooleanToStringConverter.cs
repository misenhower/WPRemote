using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Komodex.Common.Converters
{
    public class BooleanToStringConverter : BooleanConverterBase<string>
    {
        public BooleanToStringConverter()
        {
            TrueValue = "True";
            FalseValue = "False";
        }
    }
}
