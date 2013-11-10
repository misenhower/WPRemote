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

        protected override void OnListItemTap(DACPElement item, Common.Phone.Controls.LongListSelector list, bool isPlayButton)
        {
            if (item is Album)
            {
                if (isPlayButton)
                    RemoteUtility.HandleLibraryPlayTask(((Album)item).Play());
                else
                    NavigationManager.OpenAlbumPage((Album)item);
                return;
            }

            if (item is Song)
            {
                RemoteUtility.HandleLibraryPlayTask(CurrentGroup.PlaySong((Song)item));
                return;
            }
        }

        private void PlayQueueButton_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            DACPElement item = menuItem.DataContext as DACPElement;
            if (item == null)
                return;

            PlayQueueMode mode;
            switch (menuItem.Name)
            {
                case "PlayNextButton": mode = PlayQueueMode.PlayNext; break;
                case "AddToUpNextButton": mode = PlayQueueMode.AddToQueue; break;
                default: return;
            }

            if (item is Album)
            {
                RemoteUtility.HandleLibraryPlayTask(((Album)item).Play(mode));
                return;
            }

            if (item is Song)
            {
                RemoteUtility.HandleLibraryPlayTask(CurrentGroup.PlaySong((Song)item, mode));
                return;
            }
        }
    }
}