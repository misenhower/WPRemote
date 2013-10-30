using Komodex.DACP.Groups;
using Komodex.DACP.Items;
using Komodex.Remote.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Komodex.Remote.Converters
{
    public class SearchResultSectionHeaderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Type type = value as Type;
            if (type != null)
            {
                // Albums
                if (type == typeof(Album))
                    return LocalizedStrings.LibraryAlbumsPivotItem;

                // Artists
                if (type == typeof(Artist))
                    return LocalizedStrings.LibraryArtistsPivotItem;

                // Songs
                if (type == typeof(Song))
                    return LocalizedStrings.LibrarySongsPivotItem;

                // Movies
                if (type == typeof(Movie))
                    return LocalizedStrings.BrowseMovies;

                // Podcasts
                if (type == typeof(Podcast))
                    return LocalizedStrings.BrowsePodcasts;

                // TV Shows
                if (type == typeof(TVShow))
                    return LocalizedStrings.BrowseTVShows;

                // TV Show Episodes
                if (type == typeof(TVShowEpisode))
                    return LocalizedStrings.BrowseTVShowsEpisodes;

                // Audiobooks
                if (type == typeof(Audiobook))
                    return LocalizedStrings.BrowseAudiobooks;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
