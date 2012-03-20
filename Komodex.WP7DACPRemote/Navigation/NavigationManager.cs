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

namespace Komodex.WP7DACPRemote
{
    public static class NavigationManager
    {
        private static PhoneApplicationFrame RootVisual { get; set; }

        public static void DoFirstLoad(PhoneApplicationFrame frame)
        {
            RootVisual = frame;

            RootVisual.Navigated += new System.Windows.Navigation.NavigatedEventHandler(RootVisual_Navigated);
        }

        static void RootVisual_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
        }

        #region Pages

        public static void GoToFirstPage()
        {
            // Accessing RootVisual.BackStack can throw a NullReferenceException (rather than simply returning null)
            try
            {
                if (RootVisual == null || RootVisual.BackStack == null)
                    return;
            }
            catch { return; }

            Utility.BeginInvokeOnUIThread(() =>
            {
                int remaining = RootVisual.BackStack.Count();
                while (remaining > 1)
                {
                    RootVisual.RemoveBackEntry();
                    remaining--;
                }
                if (RootVisual.CanGoBack)
                    RootVisual.GoBack();
            });
        }

        public static void OpenMainPage()
        {
            RootVisual.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
        }

        public static void OpenLibraryChooserPage()
        {
            RootVisual.Navigate(new Uri("/DACPServerInfoManagement/LibraryChooserPage.xaml", UriKind.Relative));
        }

        public static void OpenAddNewServerPage()
        {
            RootVisual.Navigate(new Uri("/DACPServerInfoManagement/AddLibraryPage.xaml", UriKind.Relative));
        }

        public static void OpenArtistPage(string artistName)
        {
            artistName = Uri.EscapeDataString(artistName);
            RootVisual.Navigate(new Uri("/LibraryPages/ArtistPage.xaml?name=" + artistName, UriKind.Relative));
        }

        public static void OpenAlbumPage(int albumID, string albumName, string artistName, UInt64 albumPersistentID)
        {
            albumName = Uri.EscapeDataString(albumName);
            artistName = Uri.EscapeDataString(artistName);
            RootVisual.Navigate(new Uri("/LibraryPages/AlbumPage.xaml?id=" + albumID + "&name=" + albumName + "&artist=" + artistName + "&perid=" + albumPersistentID, UriKind.Relative));
        }

        public static void OpenGenrePage(string genreName)
        {
            genreName = Uri.EscapeDataString(genreName);
            RootVisual.Navigate(new Uri("/LibraryPages/GenrePage.xaml?name=" + genreName, UriKind.Relative));
        }

        public static void OpenPlaylistPage(int playlistID, string playlistName, UInt64 playlistPersistentID)
        {
            playlistName = Uri.EscapeDataString(playlistName);
            RootVisual.Navigate(new Uri("/LibraryPages/PlaylistPage.xaml?id=" + playlistID + "&name=" + playlistName + "&perid=" + playlistPersistentID, UriKind.Relative));
        }

        public static void OpenNowPlayingPage()
        {
            RootVisual.Navigate(new Uri("/NowPlaying/NowPlayingPage.xaml", UriKind.Relative));
        }

        public static void OpenMainLibraryPage()
        {
            RootVisual.Navigate(new Uri("/LibraryPages/MainLibraryPage.xaml", UriKind.Relative));
        }

        public static void OpenSearchPage()
        {
            RootVisual.Navigate(new Uri("/LibraryPages/SearchPage.xaml", UriKind.Relative));
        }

        public static void OpenAboutPage()
        {
            RootVisual.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }

        public static void OpenAboutPage(string iTunesVersion, int iTunesProtocolVersion, int iTunesDMAPVersion, int iTunesDAAPVersion)
        {
            iTunesVersion = Uri.EscapeDataString(iTunesVersion);
            RootVisual.Navigate(new Uri("/AboutPage.xaml?version=" + iTunesVersion + "&protocol=" + iTunesProtocolVersion + "&dmap=" + iTunesDMAPVersion + "&daap=" + iTunesDAAPVersion, UriKind.Relative));
        }

        public static void OpenVideosPage()
        {
            RootVisual.Navigate(new Uri("/LibraryPages/VideosPage.xaml", UriKind.Relative));
        }

        public static void OpenPodcastsPage()
        {
            RootVisual.Navigate(new Uri("/LibraryPages/PodcastsPage.xaml", UriKind.Relative));
        }

        public static void OpenPodcastPage(int podcastID, string podcastName, UInt64 podcastPersistentID)
        {
            podcastName = Uri.EscapeDataString(podcastName);
            RootVisual.Navigate(new Uri("/LibraryPages/PodcastPage.xaml?id=" + podcastID + "&name=" + podcastName + "&perid=" + podcastPersistentID, UriKind.Relative));
        }

        public static void OpenSettingsPage()
        {
            RootVisual.Navigate(new Uri("/Settings/SettingsPage.xaml", UriKind.Relative));
        }

        public static void OpenGeniusMixesPage()
        {
            RootVisual.Navigate(new Uri("/LibraryPages/GeniusMixesPage.xaml", UriKind.Relative));
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
