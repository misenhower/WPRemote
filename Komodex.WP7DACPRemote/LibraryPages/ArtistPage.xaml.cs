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
    public partial class ArtistPage : PhoneApplicationPage
    {
        public ArtistPage()
        {
            InitializeComponent();
        }

        #region Properties

        private Artist Artist
        {
            get { return DataContext as Artist; }
            set
            {
                if (Artist != null)
                {

                }

                DataContext = value;

                if (Artist != null)
                {

                }
            }
        }

        #endregion

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string artistName = NavigationContext.QueryString["name"] as string;
            if (string.IsNullOrEmpty(artistName))
            {
                NavigationService.GoBack();
                return;
            }

            Artist = new Artist(DACPServerManager.Server, artistName);
            Artist.GetAlbums();
        }

        #endregion

        #region Actions

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox listBox = (ListBox)sender;

            Album album = listBox.SelectedItem as Album;

            if (album != null)
            {
                GoToAlbumPage(album.Name, "TODO", album.PersistentID);
            }
        }

        #endregion

        #region Methods

        private void GoToAlbumPage(string albumName, string artistName, UInt64 albumID)
        {
            NavigationService.Navigate(new Uri("/LibraryPages/AlbumPage.xaml?name=" + albumName + "&artist=" + artistName + "&id=" + albumID, UriKind.Relative));
        }

        #endregion
    }
}