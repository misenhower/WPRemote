using Komodex.DACP.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Komodex.Remote.Converters
{
    public class PlaylistTypeToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Playlist playlist = value as Playlist;
            if (playlist != null)
            {
                if (playlist.IsSmartPlaylist)
                    return new Uri("/Assets/Icons/Playlist.Smart.png", UriKind.Relative);
                if (playlist.IsSavedGenius)
                    return new Uri("/Assets/Icons/Playlist.SavedGenius.png", UriKind.Relative);
                if (playlist.HasChildContainers)
                    return new Uri("/Assets/Icons/Playlist.Folder.png", UriKind.Relative);
            }

            return new Uri("/Assets/Icons/Playlist.Normal.png", UriKind.Relative);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
