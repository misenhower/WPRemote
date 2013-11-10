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
using Komodex.DACP.Genres;
using Komodex.DACP.Containers;
using Komodex.Remote.Localization;
using Komodex.Common;

namespace Komodex.Remote.Pages.Library
{
    public partial class LibraryPage : BrowseMusicContainerBasePage
    {
        public LibraryPage()
        {
            InitializeComponent();

            // Application Bar Icons
            AddAppBarNowPlayingButton();
            AddApplicationBarIconButton(LocalizedStrings.SearchAppBarButton, ResolutionUtility.GetUriWithResolutionSuffix("/Assets/Icons/Search.png"), NavigationManager.OpenSearchPage);
            AddApplicationBarIconButton(LocalizedStrings.MoreAppBarButton, ResolutionUtility.GetUriWithResolutionSuffix("/Assets/Icons/Ellipsis.png"), AppBarMoreButton_Click);

            // Shuffle All Songs
            AddApplicationBarMenuItem(LocalizedStrings.ShuffleAllSongsMenuItem, AppBarShuffleSongsButton_Click);

            // Choose Library
            AddApplicationBarMenuItem(LocalizedStrings.ChooseLibraryMenuItem, NavigationManager.OpenChooseLibraryPage);

            // View Sources
            ArtistsViewSource = GetContainerViewSource(async c => await c.GetGroupedArtistsAsync());
            AlbumsViewSource = GetContainerViewSource(async c => await c.GetGroupedAlbumsAsync());
            GenresViewSource = GetContainerViewSource(async c => await c.GetGroupedGenresAsync());
            PlaylistsViewSource = GetDatabaseViewSource(db => db.ParentPlaylists);
        }
        
        public object ArtistsViewSource { get; private set; }
        public object AlbumsViewSource { get; private set; }
        public object GenresViewSource { get; private set; }
        public object PlaylistsViewSource { get; private set; }

        protected override void OnListItemTap(DACPElement item, Common.Phone.Controls.LongListSelector list, bool isPlayButton)
        {
            if (item is Artist)
            {
                if (isPlayButton)
                    RemoteUtility.HandleLibraryPlayTask(((Artist)item).Play());
                else
                    NavigationManager.OpenArtistPage((Artist)item);
                return;
            }

            if (item is Album)
            {
                if (isPlayButton)
                    RemoteUtility.HandleLibraryPlayTask(((Album)item).Play());
                else
                    NavigationManager.OpenAlbumPage((Album)item);
                return;
            }

            if (item is DACPGenre)
            {
                NavigationManager.OpenMusicGenrePage((DACPGenre)item);
                return;
            }

            if (item is Playlist)
            {
                NavigationManager.OpenPlaylistPage((Playlist)item);
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

            if (item is Artist)
            {
                RemoteUtility.HandleLibraryPlayTask(((Artist)item).Play(mode));
                return;
            }

            if (item is Album)
            {
                RemoteUtility.HandleLibraryPlayTask(((Album)item).Play(mode));
                return;
            }

            if (item is DACPGenre)
            {
                RemoteUtility.HandleLibraryPlayTask(((DACPGenre)item).Play(mode));
                return;
            }

            if (item is Playlist)
            {
                RemoteUtility.HandleLibraryPlayTask(((Playlist)item).Play(mode));
                return;
            }
        }

        private void AppBarMoreButton_Click()
        {
            if (IsDialogOpen)
                return;

            ShowDialog(new Komodex.Remote.LibraryPages.LibraryViewDialog(CurrentDatabase));
        }

        private void AppBarShuffleSongsButton_Click()
        {
            // TODO
            throw new NotImplementedException();
        }

    }
}