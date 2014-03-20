using Komodex.DACP.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;

namespace Komodex.Remote.Converters
{
    public class PlaylistTypeToControlTemplateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Playlist playlist = value as Playlist;
            if (playlist != null)
            {
                if (playlist.IsSmartPlaylist)
                    return SmartPlaylistTemplate;
                if (playlist.IsSavedGenius)
                    return SavedGeniusPlaylistTemplate;
                if (playlist.HasChildContainers)
                    return PlaylistFolderTemplate;
            }

            return NormalPlaylistTemplate;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public ControlTemplate NormalPlaylistTemplate { get; set; }
        public ControlTemplate SmartPlaylistTemplate { get; set; }
        public ControlTemplate SavedGeniusPlaylistTemplate { get; set; }
        public ControlTemplate PlaylistFolderTemplate { get; set; }
    }
}
