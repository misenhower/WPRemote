using Komodex.DACP.Library;
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
            MediaItem item = value as MediaItem;
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
