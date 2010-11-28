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

        private static bool NavigateToFirstPage = false;

        static void RootVisual_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            if (NavigateToFirstPage)
            {
                if (RootVisual.CanGoBack)
                    RootVisual.GoBack();
                else
                    NavigateToFirstPage = false;
            }
        }

        #region Public Methods

        public static void GoToFirstPage()
        {
            if (RootVisual == null)
                return;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (RootVisual.CanGoBack)
                {
                    NavigateToFirstPage = true;
                    try
                    {
                        RootVisual.GoBack();
                    }
                    catch { }
                }
                else
                {
                    NavigateToFirstPage = false;
                }
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

        public static void OpenNowPlayingPage()
        {
            RootVisual.Navigate(new Uri("/NowPlayingPage.xaml", UriKind.Relative));
        }

        #endregion
    }
}
