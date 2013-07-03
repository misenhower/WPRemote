using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Komodex.Remote.Converters
{
    public class AppendFilenameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            BitmapImage image = value as BitmapImage;
            if (image == null)
                return value;

            Uri source = image.UriSource;
            if (source == null || source.IsAbsoluteUri)
                return source;

            string uriString = source.ToString();
            int separatorIndex = uriString.LastIndexOf('.');
            string filename = uriString.Substring(0, separatorIndex);
            string extension = uriString.Substring(separatorIndex, uriString.Length - separatorIndex);

            return new Uri(filename + parameter as string + extension, UriKind.Relative);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
