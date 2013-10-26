using Komodex.DACP;
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
            var state = ItemPlayedState.Unplayed;
            if (value is ItemPlayedState)
                state = (ItemPlayedState)value;

            switch (state)
            {
                case ItemPlayedState.PartiallyPlayed:
                    return PartiallyWatchedStyle;
                case ItemPlayedState.HasBeenPlayed:
                    return WatchedStyle;
                case ItemPlayedState.Unplayed:
                default:
                    return UnwatchedStyle;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
