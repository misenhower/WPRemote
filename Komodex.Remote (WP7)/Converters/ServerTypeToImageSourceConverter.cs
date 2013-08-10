using Komodex.Remote.ServerManagement;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Komodex.Remote.Converters
{
    public class ServerTypeToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ServerType type = default(ServerType);

            if (value is ServerType)
                type = (ServerType)value;

            switch (type)
            {
                case ServerType.iTunes:
                default:
                    return new Uri("/Assets/Icons/App.iTunes.png", UriKind.Relative);
                case ServerType.Foobar:
                    return new Uri("/Assets/Icons/App.Foobar.png", UriKind.Relative);
                case ServerType.MediaMonkey:
                    return new Uri("/Assets/Icons/App.MediaMonkey.png", UriKind.Relative);
                case ServerType.AlbumPlayer:
                    return new Uri("/Assets/Icons/App.AlbumPlayer.png", UriKind.Relative);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
