﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Komodex.Common.Converters
{
    public class BindableBooleanToStringConverter : DependencyObject, IValueConverter
    {
        #region TrueValue Property

        public static readonly DependencyProperty TrueValueProperty =
            DependencyProperty.Register("TrueValue", typeof(string), typeof(BindableBooleanToStringConverter), new PropertyMetadata(null));

        public string TrueValue
        {
            get { return (string)GetValue(TrueValueProperty); }
            set { SetValue(TrueValueProperty, value); }
        }

        #endregion

        #region FalseValue Property

        public static readonly DependencyProperty FalseValueProperty =
            DependencyProperty.Register("FalseValue", typeof(string), typeof(BindableBooleanToStringConverter), new PropertyMetadata(null));

        public string FalseValue
        {
            get { return (string)GetValue(FalseValueProperty); }
            set { SetValue(FalseValueProperty, value); }
        }

        #endregion

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
            throw new NotImplementedException();
        }
    }
}
