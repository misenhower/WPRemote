using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Komodex.Common.Converters
{
    public abstract class BooleanConverterBase<T> : IValueConverter
    {
        public T TrueValue { get; set; }
        public T FalseValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool result = System.Convert.ToBoolean(value);
            bool shouldInvert = System.Convert.ToBoolean(parameter);
            if (shouldInvert)
                result = !result;
            return (result) ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            bool result = (value != null && value.Equals(TrueValue));

            bool shouldInvert = System.Convert.ToBoolean(parameter);
            if (shouldInvert)
                result = !result;

            return result;
        }
    }
}
