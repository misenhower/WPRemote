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

            if (Artist == null)
            {
                string artistName = NavigationContext.QueryString["name"] as string;
                if (string.IsNullOrEmpty(artistName))
                {
                    NavigationService.GoBack();
                    return;
                }

                Artist = new Artist(DACPServerManager.Server, artistName);
            }
        }

        #endregion

        #region Actions

        private void lbAlbums_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox listBox = (ListBox)sender;

            Album album = listBox.SelectedItem as Album;

            if (album != null)
            {
                NavigationManager.OpenAlbumPage(album.ID, album.Name, album.ArtistName, album.PersistentID);
            }

            listBox.SelectedItem = null;
        }

        private void lbSongs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox listBox = (ListBox)sender;

            Song song = listBox.SelectedItem as Song;

            if (song != null)
            {
                Artist.SendPlaySongCommand(song);
                NavigationManager.OpenNowPlayingPage();
            }

            listBox.SelectedItem = null;
        }

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetDataForPivotItem();
        }

        #endregion

        #region Methods

        private void GetDataForPivotItem()
        {
            if (Artist == null || Artist.Server == null || !Artist.Server.IsConnected)
                return;

            if (pivotControl.SelectedItem == pivotAlbums)
            {
                if (Artist.Albums == null || Artist.Albums.Count == 0)
                    Artist.GetAlbums();
            }
            else if (pivotControl.SelectedItem == pivotSongs)
            {
                if (Artist.Songs == null || Artist.Songs.Count == 0)
                    Artist.GetSongs();
            }
        }

        #endregion
    }
}