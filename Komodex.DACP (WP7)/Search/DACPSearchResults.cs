using Komodex.DACP.Databases;
using Komodex.DACP.Groups;
using Komodex.DACP.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Komodex.DACP.Search
{
    public class DACPSearchResults : DACPList<ISearchResultSection>
    {
        public DACPSearchResults(DACPDatabase database, string searchString)
            : base(true)
        {
            Database = database;
            SearchString = searchString;

            // Add search result sections
            string wildcardSearch = "*" + searchString + "*";
            Add(new SearchResultSection<Album>(database, async (db, token) => await db.MusicContainer.SearchAlbumsAsync(wildcardSearch, token)));
            Add(new SearchResultSection<Artist>(database, async (db, token) => await db.MusicContainer.SearchArtistsAsync(wildcardSearch, token)));
            Add(new SongsSearchResultSection(database, wildcardSearch, async (db, token) => await db.MusicContainer.SearchSongsAsync(wildcardSearch, token)));
            if (database.MoviesContainer != null)
                Add(new SearchResultSection<Movie>(database, async (db, token) => await db.MoviesContainer.SearchMoviesAsync(wildcardSearch, token)));
            if (database.PodcastsContainer != null)
                Add(new SearchResultSection<Podcast>(database, async (db, token) => await db.PodcastsContainer.SearchShowsAsync(wildcardSearch, token)));
            if (database.TVShowsContainer != null)
            {
                Add(new SearchResultSection<TVShow>(database, async (db, token) => await db.TVShowsContainer.SearchShowsAsync(wildcardSearch, token)));
                Add(new SearchResultSection<TVShowEpisode>(database, async (db, token) => await db.TVShowsContainer.SearchEpisodesAsync(wildcardSearch, token)));
            }
            if (database.iTunesUContainer != null)
                Add(new SearchResultSection<iTunesUCourse>(database, async (db, token) => await (db.iTunesUContainer.SearchCoursesAsync(wildcardSearch, token))));
            if (database.BooksContainer != null)
                Add(new SearchResultSection<Audiobook>(database, async (db, token) => await db.BooksContainer.SearchAudiobooksAsync(wildcardSearch, token)));
        }

        public string SearchString { get; private set; }
        public DACPDatabase Database { get; private set; }

        public async Task SearchAsync(CancellationToken cancellationToken)
        {
            foreach (var item in this)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                try
                {
                    await item.SearchAsync(cancellationToken).ConfigureAwait(false);
                }
                catch { }
            }

            // NOTE: All of the returned Tasks could be sent to Task.WhenAll to perform searches simultaneously.
            // TODO: Determine whether search method should be changed.
        }
    }
}
