using Komodex.DACP.Groups;
using Komodex.DACP.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Komodex.Remote.Converters
{
    public class SearchResultsSecondLineConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;

            Type type = value.GetType();

            // Albums
            if (type == typeof(Album))
                return ((Album)value).ArtistName;

            // Artists
            if (type == typeof(Artist))
                return null;

            // Songs
            if (type == typeof(Song))
                return ((Song)value).ArtistAndAlbumName;

            // Movies
            if (type == typeof(Movie))
                return ((Movie)value).ArtistName;

            // Podcasts
            if (type == typeof(Podcast))
                return ((Podcast)value).ArtistName;

            // TV Shows
            if (type == typeof(TVShow))
                return TVShowSeasonEpisodeTextConverter.FormattedSeason(((TVShow)value).SeasonNumber);

            // TV Show Episodes
            if (type == typeof(TVShowEpisode))
            {
                var episode = (TVShowEpisode)value;
                return TVShowSeasonEpisodeTextConverter.FormattedTitleSeason(episode.SeriesName, episode.SeasonNumber);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
