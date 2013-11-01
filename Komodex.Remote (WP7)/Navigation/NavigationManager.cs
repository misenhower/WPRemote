using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Linq;
using Komodex.Common;
using Microsoft.Phone.Tasks;
using Komodex.DACP.Groups;
using Komodex.DACP;
using Komodex.DACP.Databases;
using Komodex.DACP.Containers;
using Komodex.DACP.Genres;

namespace Komodex.Remote
{
    public static class NavigationManager
    {
        private static void Navigate(string uri)
        {
            Utility.BeginInvokeOnUIThread(() =>
            {
                App.RootFrame.Navigate(new Uri(uri, UriKind.Relative));
            });
        }

        private static void Navigate(string uriFormat, params object[] args)
        {
            Navigate(string.Format(uriFormat, args));
        }

        #region Pages

        public static void ClearPageHistory()
        {
            Utility.BeginInvokeOnUIThread(() =>
            {
                // Accessing RootVisual.BackStack can throw a NullReferenceException (rather than simply returning null)
                try
                {
                    if (App.RootFrame == null || App.RootFrame.BackStack == null)
                        return;
                }
                catch { return; }

                int remaining = App.RootFrame.BackStack.Count();
                while (remaining > 1)
                {
                    App.RootFrame.RemoveBackEntry();
                    remaining--;
                }
            });
        }

        public static void GoToFirstPage()
        {
            Utility.BeginInvokeOnUIThread(() =>
            {
                ClearPageHistory();
                if (App.RootFrame.CanGoBack)
                    App.RootFrame.GoBack();
            });
        }

        public static void OpenMainPage()
        {
            Navigate("/MainPage.xaml");
        }

        public static void OpenChooseLibraryPage()
        {
            Navigate("/Pages/ChooseLibraryPage.xaml");
        }

        #region Music

        public static void OpenLibraryPage(DACPDatabase database)
        {
            Navigate("/Pages/Library/LibraryPage.xaml?databaseID={0}", database.ID);
        }

        public static void OpenArtistPage(Artist artist)
        {
            Navigate("/LibraryPages/ArtistPage.xaml?database={0}&container={1}&artist={2}", artist.Database.ID, artist.Container.ID, artist.ID);
        }

        [Obsolete]
        public static void OpenArtistPage(string artistName)
        {
        }

        public static void OpenAlbumPage(Album album)
        {
            Navigate("/LibraryPages/AlbumPage.xaml?database={0}&container={1}&album={2}", album.Database.ID, album.Container.ID, album.ID);
        }

        [Obsolete]
        public static void OpenAlbumPage(int albumID, string albumName, string artistName, UInt64 albumPersistentID)
        {
        }

        public static void OpenMusicGenrePage(DACPGenre genre)
        {
            Navigate("/Pages/Browse/Music/MusicGenrePage.xaml?databaseID={0}&genre={1}", genre.Database.ID, Uri.EscapeDataString(genre.Name));
        }

        #endregion

        #region Playlists

        public static void OpenPlaylistPage(Playlist playlist)
        {
            Navigate("/Pages/Browse/Playlists/PlaylistPage.xaml?databaseID={0}&playlistID={1}", playlist.Database.ID, playlist.ID);
        }

        #endregion

        #region Movies

        public static void OpenMoviesPage(DACPDatabase database)
        {
            Navigate("/Pages/Browse/Movies/MoviesPage.xaml?databaseID={0}", database.ID);
        }

        public static void OpenMovieGenrePage(DACPGenre genre)
        {
            Navigate("/Pages/Browse/Movies/MovieGenrePage.xaml?databaseID={0}&genre={1}", genre.Database.ID, Uri.EscapeDataString(genre.Name));
        }

        #endregion

        #region TV Shows

        public static void OpenTVShowsPage(DACPDatabase database)
        {
            Navigate("/Pages/Browse/TVShows/TVShowsPage.xaml?databaseID={0}", database.ID);
        }

        public static void OpenTVShowEpisodesPage(TVShow tvShow)
        {
            Navigate("/Pages/Browse/TVShows/TVShowEpisodesPage.xaml?databaseID={0}&groupID={1}", tvShow.Database.ID, tvShow.Index);
        }

        #endregion

        #region Podcasts

        public static void OpenPodcastsPage(DACPDatabase database)
        {
            Navigate("/Pages/Browse/Podcasts/PodcastsPage.xaml?databaseID={0}", database.ID);
        }

        public static void OpenPodcastEpisodesPage(Podcast podcast)
        {
            Navigate("/Pages/Browse/Podcasts/PodcastEpisodesPage.xaml?databaseID={0}&groupID={1}", podcast.Database.ID, podcast.ID);
        }

        #endregion

        #region Audiobooks

        public static void OpenAudiobooksPage(DACPDatabase database)
        {
            Navigate("/Pages/Browse/Audiobooks/AudiobooksPage.xaml?databaseID={0}", database.ID);
        }

        public static void OpenAudiobookEpisodesPage(Audiobook audiobook)
        {
            Navigate("/Pages/Browse/Audiobooks/AudiobookEpisodesPage.xaml?databaseID={0}&groupID={1}", audiobook.Database.ID, audiobook.ID);
        }

        #endregion

        public static void OpenGenrePage(string genreName)
        {
            genreName = Uri.EscapeDataString(genreName);
            Navigate("/LibraryPages/GenrePage.xaml?name=" + genreName);
        }

        [Obsolete]
        public static void OpenPlaylistPage(int playlistID, string playlistName, UInt64 playlistPersistentID)
        {
        }

        public static void OpenNowPlayingPage()
        {
            Navigate("/Pages/NowPlayingPage.xaml");
        }

        [Obsolete]
        public static void OpenMainLibraryPage()
        {
        }

        public static void OpenSearchPage()
        {
            Navigate("/Pages/Search/SearchPage.xaml");
        }

        public static void OpenAboutPage()
        {
            Navigate("/AboutPage.xaml");
        }

        public static void OpenAboutPage(string iTunesVersion, int iTunesProtocolVersion, int iTunesDMAPVersion, int iTunesDAAPVersion)
        {
            iTunesVersion = Uri.EscapeDataString(iTunesVersion);
            Navigate("/AboutPage.xaml?version=" + iTunesVersion + "&protocol=" + iTunesProtocolVersion + "&dmap=" + iTunesDMAPVersion + "&daap=" + iTunesDAAPVersion);
        }

        public static void OpenVideosPage()
        {
            Navigate("/LibraryPages/VideosPage.xaml");
        }

        [Obsolete]
        public static void OpenPodcastsPage()
        {
        }

        [Obsolete]
        public static void OpenPodcastPage(int podcastID, string podcastName, UInt64 podcastPersistentID)
        {
        }

        public static void OpenSettingsPage()
        {
            Navigate("/Settings/SettingsPage.xaml");
        }

        public static void OpenGeniusMixesPage()
        {
            Navigate("/LibraryPages/GeniusMixesPage.xaml");
        }

        #endregion

        #region Marketplace

        public static void OpenMarketplaceDetailPage()
        {
            // Open the current app in the marketplace
            MarketplaceDetailTask marketplace = new MarketplaceDetailTask();
            marketplace.Show();
        }

        #endregion
    }
}
