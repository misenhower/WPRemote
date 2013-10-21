using Komodex.DACP.Items;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Komodex.Remote.Converters
{
    public class EpisodeWatchedStyleConverter : IValueConverter
    {
        public Style UnwatchedStyle { get; set; }
        public Style PartiallyWatchedStyle { get; set; }
        public Style WatchedStyle { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
#if DEBUG
            if (value is SampleData.SampleDataDACPItem)
            {
                var sdItem = (SampleData.SampleDataDACPItem)value;
                if (sdItem.HasBeenPlayed)
                {
                    if (sdItem.PlayCount > 0)
                        return WatchedStyle;
                    return PartiallyWatchedStyle;
                }
                return UnwatchedStyle;
            }
#endif

            var item = value as DACPItem;
            if (item == null)
                return WatchedStyle;

            if (item.HasBeenPlayed)
            {
                if (item.PlayCount > 0)
                    return WatchedStyle;
                return PartiallyWatchedStyle;
            }
            return UnwatchedStyle;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
