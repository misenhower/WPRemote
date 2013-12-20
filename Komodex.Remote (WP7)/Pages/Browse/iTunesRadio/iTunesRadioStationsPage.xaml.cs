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

namespace Komodex.Remote.Pages.Browse.iTunesRadio
{
    public partial class iTunesRadioStationsPage : BrowseDatabaseBasePage
    {
        public iTunesRadioStationsPage()
        {
            InitializeComponent();
        }

        protected async override void OnDatabaseChanged()
        {
            base.OnDatabaseChanged();

            if (CurrentDatabase != null)
            {
                var radioDB = (iTunesRadioDatabase)CurrentDatabase;
                if (radioDB.Stations == null)
                {
                    SetProgressIndicator(null, true);
                    await radioDB.RequestStationsAsync();
                    ClearProgressIndicator();
                }
            }
        }
    }
}