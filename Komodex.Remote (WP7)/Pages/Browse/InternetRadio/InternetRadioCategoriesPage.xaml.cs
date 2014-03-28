using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Komodex.DACP.Databases;
using Komodex.DACP;
using Komodex.DACP.Containers;

namespace Komodex.Remote.Pages.Browse.InternetRadio
{
    public partial class InternetRadioCategoriesPage : BrowseDatabaseBasePage
    {
        public InternetRadioCategoriesPage()
        {
            InitializeComponent();
        }

        protected async override void OnDatabaseChanged()
        {
            base.OnDatabaseChanged();

            var db = CurrentDatabase;
            if (db != null && db.Playlists == null)
            {
                SetProgressIndicator(null, true);
                await CurrentDatabase.RequestContainersAsync();
                ClearProgressIndicator();
            }
        }

        protected override void OnListItemTap(DACPElement item, Common.Phone.Controls.LongListSelector list, bool isPlayButton)
        {
            if (item is Playlist)
            {
                NavigationManager.OpenInternetRadioStationsPage((Playlist)item);
                return;
            }

            base.OnListItemTap(item, list, isPlayButton);
        }
    }
}