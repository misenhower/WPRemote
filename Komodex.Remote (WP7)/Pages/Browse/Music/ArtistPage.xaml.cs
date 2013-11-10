using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Komodex.DACP;
using Komodex.DACP.Groups;
using Komodex.DACP.Items;

namespace Komodex.Remote.Pages.Browse.Music
{
    public partial class ArtistPage : BrowseArtistBasePage
    {
        public ArtistPage()
        {
            InitializeComponent();

            AlbumsViewSource = GetGroupViewSource(async g => await g.GetAlbumsAsync());
            SongsViewSource = GetGroupViewSource(async g => await g.GetGroupedSongsAsync());
        }

        public object AlbumsViewSource { get; private set; }
        public object SongsViewSource { get; private set; }

        protected override bool ShouldShowContinuumTransition(Clarity.Phone.Controls.Animations.AnimationType animationType, Uri toOrFrom)
        {
            if (toOrFrom.OriginalString.StartsWith("/Pages/Library/LibraryPage.xaml"))
                return true;
            return base.ShouldShowContinuumTransition(animationType, toOrFrom);
        }

        protected override async void OnListItemTap(DACPElement item, Common.Phone.Controls.LongListSelector list)
        {
            if (item is Album)
            {
                NavigationManager.OpenAlbumPage((Album)item);
                return;
            }

            if (item is Song)
            {
                RemoteUtility.HandleLibraryPlayTask(((Song)item).Play());
                return;
            }

            base.OnListItemTap(item, list);
        }

        private void PlayQueueButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}