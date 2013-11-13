using Komodex.Remote.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Komodex.Remote.Converters
{
    public class GeniusMixDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string description = value as string;
            if (string.IsNullOrEmpty(description))
                return null;

            description = description.Replace(",,,", LocalizedStrings.BrowseGeniusMixesArtistSeparator);
            return string.Format(LocalizedStrings.BrowseGeniusMixesArtistsAndOthers, description);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
