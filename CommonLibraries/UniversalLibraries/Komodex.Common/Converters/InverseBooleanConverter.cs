using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Komodex.Common.Converters
{
    public class InverseBooleanConverter : BooleanConverterBase<bool>
    {
        public InverseBooleanConverter()
        {
            TrueValue = false;
            FalseValue = true;
        }
    }
}
