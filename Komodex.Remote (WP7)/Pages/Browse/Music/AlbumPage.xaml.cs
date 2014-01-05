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
using Komodex.DACP.Items;
using Clarity.Phone.Controls.Animations;
using Komodex.DACP.Groups;

namespace Komodex.Remote.Pages.Browse.Music
{
    public partial class AlbumPage : BrowseAlbumBasePage
    {
        private FrameworkElement _artistButton;

        public AlbumPage()
        {
            InitializeComponent();

            SongsViewSource = GetGroupViewSource(async g => await g.GetSongsAsync());
        }

        public object SongsViewSource { get; private set; }

        protected override bool ShouldShowContinuumTransition(AnimationType animationType, Uri toOrFrom)
        {
            if (animationType == AnimationType.NavigateForwardIn || animationType == AnimationType.NavigateBackwardOut)
            {
                if (toOrFrom.OriginalString.StartsWith("/Pages/Library/LibraryPage.xaml"))
                    return true;
            }
            return base.ShouldShowContinuumTransition(animationType, toOrFrom);
        }

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            if (toOrFrom != null && toOrFrom.OriginalString.StartsWith("/Pages/Browse/Music/ArtistPage.xaml"))
            {
                if (animationType == AnimationType.NavigateForwardOut || animationType == AnimationType.NavigateBackwardIn)
                {
                    if (_artistButton != null)
                        return GetContinuumAnimation(_artistButton, animationType);
                }
            }

            return base.GetAnimation(animationType, toOrFrom);
        }

        protected override void OnListItemTap(DACPElement item, Common.Phone.Controls.LongListSelector list, bool isPlayButton)
        {
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

            if (item is Song)
            {
                RemoteUtility.HandleLibraryQueueTask(CurrentGroup.PlaySong((Song)item, mode));
                return;
            }

            if (item is Album)
            {
                RemoteUtility.HandleLibraryQueueTask(((Album)item).Play(mode));
                return;
            }
        }

        private async void ArtistButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            e.Handled = true;

            if (CurrentGroup == null)
                return;

            // Set the artist button reference for the continuum transition
            _artistButton = sender as FrameworkElement;

            var artists = await CurrentContainer.GetArtistsAsync();
            if (artists != null)
            {
                var artist = artists.FirstOrDefault(a => a.Name == CurrentGroup.ArtistName);
                if (artist != null)
                    NavigationManager.OpenArtistPage(artist);
            }
        }

        private void AlbumPlayButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (CurrentGroup == null)
                return;

            RemoteUtility.HandleLibraryPlayTask(CurrentGroup.Play());
            e.Handled = true;
        }

        private void ShuffleButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (CurrentGroup == null)
                return;

            RemoteUtility.HandleLibraryPlayTask(CurrentGroup.Shuffle());
            e.Handled = true;
        }
    }
}