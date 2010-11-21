using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Komodex.DACP.Library;
using Komodex.WP7DACPRemote.DACPServerManagement;

namespace Komodex.WP7DACPRemote.LibraryPages
{
    public partial class AlbumPage : PhoneApplicationPage
    {
        public AlbumPage()
        {
            InitializeComponent();
        }

        #region Properties

        private Album Album
        {
            get { return DataContext as Album; }
            set
            {
                if (Album != null)
                {

                }

                DataContext = value;

                if (Album != null)
                {

                }
            }
        }

        #endregion

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var queryString = NavigationContext.QueryString;

            string albumName = queryString["name"];
            string artistName = queryString["artist"];
            string albumIDString = queryString["id"];

            if (string.IsNullOrEmpty(albumName) || string.IsNullOrEmpty(artistName) || string.IsNullOrEmpty(albumIDString))
            {
                NavigationService.GoBack();
                return;
            }

            UInt64 albumID;

            if (!UInt64.TryParse(albumIDString, out albumID))
            {
                NavigationService.GoBack();
                return;
            }

            Album = new Album(DACPServerManager.Server, albumName, artistName, albumID);
            Album.GetSongs();
        }

        #endregion

        #region Actions

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox listBox = (ListBox)sender;

            Song song = listBox.SelectedItem as Song;

            if (song != null)
            {
                Album.SendPlaySongCommand(song);
                NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
            }
        }

        #endregion
    }
}