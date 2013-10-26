using Komodex.Common;
using Komodex.DACP.Groups;
using Komodex.DACP.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Komodex.Remote.Converters
{
    public class TVShowSeasonEpisodeTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string title = null;
            int season = 0;
            int episode = 0;

            // Get the data
            if (value is TVShow)
            {
                var tvShow = (TVShow)value;
                title = tvShow.Name;
                season = tvShow.SeasonNumber;
            }
            else if (value is TVShowEpisode)
            {
                var tvShowEpisode = (TVShowEpisode)value;
                title = tvShowEpisode.SeriesName;
                season = tvShowEpisode.SeasonNumber;
                episode = tvShowEpisode.EpisodeNumber;
            }
#if DEBUG
            else if (value is SampleData.SampleDataTVShow)
            {
                var tvShow = (SampleData.SampleDataTVShow)value;
                title = tvShow.Name;
                season = tvShow.SeasonNumber;
            }
            else if (value is SampleData.SampleDataTVShowEpisode)
            {
                var tvShowEpisode = (SampleData.SampleDataTVShowEpisode)value;
                title = tvShowEpisode.SeriesName;
                season = tvShowEpisode.SeasonNumber;
                episode = tvShowEpisode.EpisodeNumber;
            }
#endif
            else
            {
                return null;
            }

            // Format the data
            switch (parameter as string)
            {
                case "Season":
                    return FormattedSeason(season);
                case "Episode":
                    return FormattedEpisode(episode);
                case "SeasonEpisode":
                    return FormattedSeasonEpisode(season, episode);
                case "TitleSeason":
                    return FormattedTitleSeason(title, season);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        protected string FormattedSeason(int season)
        {
            if (season <= 0)
                return null;
            return string.Format("Season {0}", season);
        }

        protected string FormattedEpisode(int episode)
        {
            if (episode <= 0)
                return null;
            return string.Format("Episode {0}", episode);
        }

        protected string FormattedSeasonEpisode(int season, int episode)
        {
            if (season <= 0)
                return FormattedEpisode(episode);
            if (episode <= 0)
                return FormattedSeason(season);
            return string.Format("Season {0}, Episode {1}", season, episode);
        }

        protected string FormattedTitleSeason(string title, int season)
        {
            if (string.IsNullOrEmpty(title) || season <= 0)
                return title;
            return string.Format("{0}, Season {1}", title, season);
        }
    }
}
