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
using System.Collections;

namespace Komodex.Remote.Converters
{
    public class NoListItemsVisibilityConverter : IValueConverter
    {

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is IList)
            {
                IList listValues = (IList)value;
                if (listValues.Count == 0)
                    return Visibility.Visible;

                bool foundSubObjects = false;
                foreach (object listObject in listValues)
                {
                    if (!(listObject is IList) || ((IList)listObject).Count > 0)
                    {
                        foundSubObjects = true;
                        break;
                    }
                }
                if (!foundSubObjects)
                    return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
