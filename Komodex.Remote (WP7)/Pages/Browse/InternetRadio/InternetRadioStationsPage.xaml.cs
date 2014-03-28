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

namespace Komodex.Remote.Pages.Browse.InternetRadio
{
    public partial class InternetRadioStationsPage : BrowsePlaylistBasePage
    {
        private int _playlistID;

        public InternetRadioStationsPage()
        {
            InitializeComponent();

            StationsViewSource = GetContainerViewSource(async c => await c.GetItemsAsync());
        }

        public object StationsViewSource { get; private set; }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var queryString = NavigationContext.QueryString;
            _playlistID = int.Parse(queryString["playlistID"]);

            base.OnNavigatedTo(e);
        }

        protected async override void OnDatabaseChanged()
        {
            var db = CurrentDatabase;
            if (db.Playlists == null)
            {
                SetProgressIndicator(null, true);
                await db.RequestContainersAsync();
                ClearProgressIndicator();
            }

            base.OnDatabaseChanged();
        }

        protected override Playlist GetContainer(DACPDatabase database)
        {
            var playlists = database.Playlists;
            if (playlists == null)
                return null;

            return database.Playlists.First(pl => pl.ID == _playlistID);
        }

        protected override async void OnListItemTap(DACPElement item, Common.Phone.Controls.LongListSelector list, bool isPlayButton)
        {
            if (item is DACPItem)
            {
                SetProgressIndicator(null, true);
                await RemoteUtility.HandleLibraryPlayTaskAsync(((DACPItem)item).Play());
                ClearProgressIndicator();
            }
        }
    }
}
