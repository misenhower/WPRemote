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
using Komodex.DACP.Genres;
using Komodex.DACP.Containers;

namespace Komodex.Remote.Pages.Browse.Music
{
    public partial class MusicGenrePage : BrowseMusicContainerBasePage
    {
        public MusicGenrePage()
        {
            InitializeComponent();

            ArtistsViewSource = GetContainerViewSource(async c => await c.GetGenreArtistsAsync(CurrentGenreName));
            AlbumsViewSource = GetContainerViewSource(async c => await c.GetGenreAlbumsAsync(CurrentGenreName));
            SongsViewSource = GetGenreViewSource(async g => await g.GetGroupedItemsAsync());
        }

        public string CurrentGenreName { get; private set; }
        public object ArtistsViewSource { get; private set; }
        public object AlbumsViewSource { get; private set; }
        public object SongsViewSource { get; private set; }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var queryString = NavigationContext.QueryString;
            CurrentGenreName = queryString["genre"];

            base.OnNavigatedTo(e);
        }

        protected override DACPGenre GetGenre(MusicContainer container)
        {
            return new DACPGenre(CurrentContainer, CurrentGenreName);
        }

        protected override bool ShouldShowContinuumTransition(Clarity.Phone.Controls.Animations.AnimationType animationType, Uri toOrFrom)
        {
            if (toOrFrom.OriginalString.StartsWith("/Pages/Library/LibraryPage.xaml"))
                return true;
            return base.ShouldShowContinuumTransition(animationType, toOrFrom);
        }

        protected override async void OnListItemTap(DACPElement item, Common.Phone.Controls.LongListSelector list)
        {
            if (item is Artist)
            {
                NavigationManager.OpenArtistPage((Artist)item);
                return;
            }

            if (item is Album)
            {
                NavigationManager.OpenAlbumPage((Album)item);
                return;
            }

            if (item is DACPItem)
            {
                if (await CurrentGenre.PlayItem((DACPItem)item))
                    NavigationManager.OpenNowPlayingPage();
                else
                    RemoteUtility.ShowLibraryError();
                return;
            }

            base.OnListItemTap(item, list);
        }

        private void PlayQueueButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}