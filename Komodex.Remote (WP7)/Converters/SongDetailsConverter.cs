using Komodex.Common;
using Komodex.DACP.Items;
using Komodex.Remote.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Komodex.Remote.Converters
{
    public class SongDetailsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
#if DEBUG
            if (value is SampleData.SampleDataSong)
            {
                var sampleSong = (SampleData.SampleDataSong)value;
                return string.Join(" – ", sampleSong.ArtistName, sampleSong.FormattedDuration);
            }
#endif

            Song song = value as Song;
            if (song == null)
                return null;

            return FormattedSongDetails(song);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static string FormattedSongDetails(Song song)
        {
            if (song == null)
                return null;

            // Get the artist name (if necessary)
            string artistName = null;
            if (!string.IsNullOrEmpty(song.AlbumArtistName) && song.ArtistName != song.AlbumArtistName)
                artistName = song.ArtistName;

            // Get the codec and bitrate info (if necessary)
            string codecBitrate = null;
            if (SettingsManager.Current.ShowCodecAndBitrate)
            {
                string bitrate = null;
                if (song.Bitrate > 0)
                    bitrate = string.Format("{0} kbps", song.Bitrate);

                codecBitrate = Utility.JoinNonEmptyStrings(", ", song.CodecType, bitrate);
            }

            // Format the output
            return Utility.JoinNonEmptyStrings(" – ", artistName, song.FormattedDuration, codecBitrate);
        }
    }
}
