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
using System.Windows.Data;

namespace Komodex.WP7DACPRemote.ThirdParty
{
    public class BooleanToStringConverter : DependencyObject, IValueConverter
    {
        public static readonly DependencyProperty ValueIfTrueProperty =
            DependencyProperty.Register("ValueIfTrue", typeof(string), typeof(BooleanToStringConverter),
            new PropertyMetadata((string)"true"));

        public string ValueIfTrue
        {
            get { return (string)GetValue(ValueIfTrueProperty); }
            set { SetValue(ValueIfTrueProperty, value); }
        }

        public static readonly DependencyProperty ValueIfFalseProperty =
            DependencyProperty.Register("ValueIfFalse", typeof(string), typeof(BooleanToStringConverter),
            new PropertyMetadata((string)"false"));

        public string ValueIfFalse
        {
            get { return (string)GetValue(ValueIfFalseProperty); }
            set { SetValue(ValueIfFalseProperty, value); }
        }

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var flag = false;
            if (value is bool)
            {
                flag = (bool)value;
            }
            else if (value is bool?)
            {
                var nullable = (bool?)value;
                flag = nullable.GetValueOrDefault();
            }
            if (parameter != null)
            {
                if (bool.Parse((string)parameter))
                {
                    flag = !flag;
                }
            }
            if (flag)
            {
                //return new Uri(ValueIfTrue, UriKind.Relative);
                return ValueIfTrue;
            }
            else
            {
                //return new Uri(ValueIfFalse, UriKind.Relative);
                return ValueIfFalse;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
