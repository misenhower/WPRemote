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

        public static void OpenManualPairingPage()
        {
            Navigate("/Pages/Pairing/ManualPairingPage.xaml");
        }

        public static void OpenArtistPage(string artistName)
        {
            artistName = Uri.EscapeDataString(artistName);
            Navigate("/LibraryPages/ArtistPage.xaml?name=" + artistName);
        }

        public static void OpenAlbumPage(int albumID, string albumName, string artistName, UInt64 albumPersistentID)
        {
            albumName = Uri.EscapeDataString(albumName);
            artistName = Uri.EscapeDataString(artistName);
            Navigate("/LibraryPages/AlbumPage.xaml?id=" + albumID + "&name=" + albumName + "&artist=" + artistName + "&perid=" + albumPersistentID);
        }

        public static void OpenGenrePage(string genreName)
        {
            genreName = Uri.EscapeDataString(genreName);
            Navigate("/LibraryPages/GenrePage.xaml?name=" + genreName);
        }

        public static void OpenPlaylistPage(int playlistID, string playlistName, UInt64 playlistPersistentID)
        {
            playlistName = Uri.EscapeDataString(playlistName);
            Navigate("/LibraryPages/PlaylistPage.xaml?id=" + playlistID + "&name=" + playlistName + "&perid=" + playlistPersistentID);
        }

        public static void OpenNowPlayingPage()
        {
            Navigate("/NowPlaying/NowPlayingPage.xaml");
        }

        public static void OpenMainLibraryPage()
        {
            Navigate("/LibraryPages/MainLibraryPage.xaml");
        }

        public static void OpenSearchPage()
        {
            Navigate("/LibraryPages/SearchPage.xaml");
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

        public static void OpenPodcastsPage()
        {
            Navigate("/LibraryPages/PodcastsPage.xaml");
        }

        public static void OpenPodcastPage(int podcastID, string podcastName, UInt64 podcastPersistentID)
        {
            podcastName = Uri.EscapeDataString(podcastName);
            Navigate("/LibraryPages/PodcastPage.xaml?id=" + podcastID + "&name=" + podcastName + "&perid=" + podcastPersistentID);
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
