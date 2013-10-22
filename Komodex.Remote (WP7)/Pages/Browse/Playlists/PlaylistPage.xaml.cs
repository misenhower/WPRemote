using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Komodex.DACP.Containers;
using Komodex.DACP.Databases;
using Komodex.DACP;
using Komodex.DACP.Items;

namespace Komodex.Remote.Pages.Browse.Playlists
{
    public partial class PlaylistPage : BrowsePlaylistBasePage
    {
        private int _playlistID;

        public PlaylistPage()
        {
            InitializeComponent();

            PlaylistViewSource = GetContainerViewSource(c => c.GetItemsOrSubListsAsync());
        }

        public object PlaylistViewSource { get; private set; }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var queryString = NavigationContext.QueryString;
            _playlistID = int.Parse(queryString["playlistID"]);

            base.OnNavigatedTo(e);
        }

        protected override Playlist GetContainer(DACPDatabase database)
        {
            return database.Playlists.First(pl => pl.ID == _playlistID);
        }

        protected override bool ShouldShowContinuumTransition(Clarity.Phone.Controls.Animations.AnimationType animationType, Uri toOrFrom)
        {
            if (toOrFrom.OriginalString.StartsWith("/LibraryPages/MainLibraryPage.xaml"))
                return true;
            return base.ShouldShowContinuumTransition(animationType, toOrFrom);
        }

        protected override async void OnListItemTap(DACPElement item, Common.Phone.Controls.LongListSelector list)
        {
            if (item is Playlist)
            {
                NavigationManager.OpenPlaylistPage((Playlist)item);
                return;
            }

            if (item is DACPItem)
            {
                if (await CurrentContainer.PlayItem((DACPItem)item))
                    NavigationManager.OpenNowPlayingPage();
                else
                    RemoteUtility.ShowLibraryError();
            }
        }
    }
}