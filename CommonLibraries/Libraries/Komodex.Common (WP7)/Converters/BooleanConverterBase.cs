using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Globalization;
using System.Windows.Data;

namespace Komodex.Common.Converters
{
    public abstract class BooleanConverterBase<T> : IValueConverter
    {
        public T TrueValue { get; set; }
        public T FalseValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = System.Convert.ToBoolean(value);
            bool shouldInvert = System.Convert.ToBoolean(parameter);
            if (shouldInvert)
                result = !result;
            return (result) ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = (value != null && value.Equals(TrueValue));

            bool shouldInvert = System.Convert.ToBoolean(parameter);
            if (shouldInvert)
                result = !result;

            return result;
        }
    }
}
