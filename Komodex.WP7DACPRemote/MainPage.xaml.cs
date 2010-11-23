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
using Komodex.DACP;
using Komodex.WP7DACPRemote.DACPServerInfoManagement;
using Microsoft.Phone.Shell;
using Komodex.WP7DACPRemote.DACPServerManagement;
using Komodex.DACP.Library;

namespace Komodex.WP7DACPRemote
{
    public partial class MainPage : PhoneApplicationPage
    {
        public MainPage()
        {
            InitializeComponent();

            // ApplicationBar button and menu item references must be referenced at run time
            btnPlayPause = (ApplicationBarIconButton)ApplicationBar.Buttons[1];
        }

        DACPServerViewModel viewModel = DACPServerViewModel.Instance;
        private ApplicationBarIconButton btnPlayPause = null;
        private readonly Uri iconPlay = new Uri("/icons/appbar.transport.play.rest.png", UriKind.Relative);
        private readonly Uri iconPause = new Uri("/icons/appbar.transport.pause.rest.png", UriKind.Relative);

        #region Static Properties

        private static bool _SuppressAutoOpenServerListPage = false;
        public static bool SuppressAutoOpenServerListPage
        {
            get { return _SuppressAutoOpenServerListPage; }
            set { _SuppressAutoOpenServerListPage = value; }
        }

        #endregion

        #region Properties

        private DACPServer DACPServer
        {
            get { return DataContext as DACPServer; }
            set
            {
                if (DACPServer != null)
                {
                    DACPServer.ServerUpdate -= new EventHandler<ServerUpdateEventArgs>(DACPServer_ServerUpdate);
                    DACPServer.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(DACPServer_PropertyChanged);
                }

                DataContext = value;

                if (DACPServer != null)
                {
                    DACPServer.ServerUpdate += new EventHandler<ServerUpdateEventArgs>(DACPServer_ServerUpdate);
                    DACPServer.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(DACPServer_PropertyChanged);
                }
            }
        }

        #endregion

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            DACPServerManager.DoFirstLoad();

            DACPServerManager.ServerChanged += new EventHandler(DACPServerManager_ServerChanged);

            DACPServer = DACPServerManager.Server;

            GetDataForPivotItem();

            if (DACPServer == null)
            {
                if (!SuppressAutoOpenServerListPage)
                {
                    SuppressAutoOpenServerListPage = true;
                    GoToSettingsPage();
                }
            }
        }


        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            DACPServerManager.ServerChanged -= new EventHandler(DACPServerManager_ServerChanged);
            DACPServer = null;

        }

        #endregion

        #region Event Handlers

        void DACPServerManager_ServerChanged(object sender, EventArgs e)
        {
            DACPServer = DACPServerManager.Server;
        }

        void DACPServer_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                switch (e.Type)
                {
                    case ServerUpdateType.ServerInfoResponse:
                        break;
                    case ServerUpdateType.ServerConnected:
                        GetDataForPivotItem();
                        break;
                    case ServerUpdateType.Error:
                        //GoToSettingsPage();
                        break;
                    default:
                        break;
                }
            });
        }

        void DACPServer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "PlayState":
                    UpdatePlayPauseButton();
                    break;
                default:
                    break;
            }
        }

        private void UpdatePlayPauseButton()
        {
            if (DACPServer.PlayState == PlayStates.Playing)
                btnPlayPause.IconUri = iconPause;
            else
                btnPlayPause.IconUri = iconPlay;
        }

        #endregion

        #region Actions

        private void connectingStatusControl_ButtonClick(object sender, RoutedEventArgs e)
        {
            GoToSettingsPage();
        }

        private void mnuSettings_Click(object sender, EventArgs e)
        {
            GoToSettingsPage();
        }

        private void btnPrevious_Click(object sender, EventArgs e)
        {
            DACPServer.SendPrevItemCommand();
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            DACPServer.SendNextItemCommand();
        }

        private void btnPlayPause_Click(object sender, EventArgs e)
        {
            DACPServer.SendPlayPauseCommand();
        }

        private void pivotControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetDataForPivotItem();
        }

        private void lbArtists_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LongListSelector listBox = (LongListSelector)sender;

            Artist artist = listBox.SelectedItem as Artist;

            if (artist != null)
            {
                GoToArtistPage(artist.Name);
            }
        }

        private void lbAlbums_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LongListSelector listBox = (LongListSelector)sender;

            Album album = listBox.SelectedItem as Album;

            if (album != null)
            {
                GoToAlbumPage(album.Name, album.ArtistName, album.PersistentID);
            }
        }


        #endregion

        #region Methods

        private void GoToSettingsPage()
        {
            NavigationService.Navigate(new Uri("/DACPServerInfoManagement/LibraryChooserPage.xaml", UriKind.Relative));
        }

        private void GetDataForPivotItem()
        {
            if (DACPServer == null || !DACPServer.IsConnected)
                return;

            if (pivotControl.SelectedItem == pivotArtists)
            {
                if (DACPServer.LibraryArtists == null || DACPServer.LibraryArtists.Count == 0)
                    DACPServer.GetArtists();
            }
            else if (pivotControl.SelectedItem == pivotAlbums)
            {
                if (DACPServer.LibraryAlbums == null || DACPServer.LibraryAlbums.Count == 0)
                    DACPServer.GetAlbums();
            }
        }

        private void GoToArtistPage(string artistName)
        {
            artistName = Uri.EscapeDataString(artistName);
            NavigationService.Navigate(new Uri("/LibraryPages/ArtistPage.xaml?name=" + artistName, UriKind.Relative));
        }

        private void GoToAlbumPage(string albumName, string artistName, UInt64 albumID)
        {
            albumName = Uri.EscapeDataString(albumName);
            artistName = Uri.EscapeDataString(artistName);
            NavigationService.Navigate(new Uri("/LibraryPages/AlbumPage.xaml?name=" + albumName + "&artist=" + artistName + "&id=" + albumID, UriKind.Relative));
        }

        #endregion

    }
}