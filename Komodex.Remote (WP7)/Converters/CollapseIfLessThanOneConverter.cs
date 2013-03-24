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
using Komodex.DACP;
using Komodex.DACP.Library;

namespace Komodex.Remote.Converters
{
    public class CollapseIfLessThanOneConverter : IValueConverter
    {

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool visible = false;

            if (value is GroupedItems<MediaItem>)
            {
                var groupedItems = (GroupedItems<MediaItem>)value;
                int count = 0;
                foreach (var item in groupedItems)
                {
                    count += item.Count;
                    if (count >= 1)
                    {
                        visible = true;
                        break;
                    }
                }
            }
            else if (value is ICollection)
            {
                visible = ((ICollection)value).Count >= 1;
            }

            return (visible) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
