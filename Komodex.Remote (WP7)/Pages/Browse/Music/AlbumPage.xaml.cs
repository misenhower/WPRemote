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

        protected override bool ShouldShowContinuumTransition(Clarity.Phone.Controls.Animations.AnimationType animationType, Uri toOrFrom)
        {
            if (toOrFrom.OriginalString.StartsWith("/Pages/Library/LibraryPage.xaml"))
                return true;
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

        protected override void OnListItemTap(DACPElement item, Common.Phone.Controls.LongListSelector list)
        {
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

        private async void ArtistButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            e.Handled = true;

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
            RemoteUtility.HandleLibraryPlayTask(CurrentGroup.Play());
            e.Handled = true;
        }

        private void ShuffleButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            RemoteUtility.HandleLibraryPlayTask(CurrentGroup.Shuffle());
            e.Handled = true;
        }
    }
}