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

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Get the input value
            bool result = false;

            if (value is bool)
                result = (bool)value;
            else if (value is bool?)
                result = ((bool?)value).GetValueOrDefault();

            // Invert the result if necessary
            if (ShouldInvert(parameter))
                result = !result;

            // Return the true or false value
            return (result) ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = (value != null && value.Equals(TrueValue));

            if (ShouldInvert(parameter))
                result = !result;
            
            return result;
        }

        #endregion

        private static bool ShouldInvert(object parameter)
        {
            if (parameter is bool)
                return (bool)parameter;

            if (parameter is string)
            {
                string p = ((string)parameter).ToLower();
                if (p == "true" || p == "invert")
                    return true;
            }

            return false;
        }
    }
}
