using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Komodex.Common.Converters
{
    public class MultiResolutionImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Get the BitmapImage if possible
            var image = value as BitmapImage;
            if (image == null)
                return value;

            // Get the image's source URI if possible
            Uri uri = image.UriSource;
            if (uri == null)
                return value;

            return ResolutionUtility.GetUriWithResolutionSuffix(uri);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
