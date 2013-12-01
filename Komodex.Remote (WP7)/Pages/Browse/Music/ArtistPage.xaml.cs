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
using Komodex.Remote.Localization;
using Komodex.Remote.Data;
using System.ComponentModel;
using Clarity.Phone.Controls.Animations;

namespace Komodex.Remote.Pages.Browse.Music
{
    public partial class ArtistPage : BrowseArtistBasePage
    {
        public ArtistPage()
        {
            InitializeComponent();

            AlbumsViewSource = GetGroupViewSource(async g => await g.GetAlbumsAsync());
            var songsViewSource = GetGroupViewSource(async g => await g.GetGroupedSongsAsync());
            songsViewSource.PropertyChanged += SongsViewSource_PropertyChanged;
            SongsViewSource = songsViewSource;
        }

        public object AlbumsViewSource { get; private set; }
        public object SongsViewSource { get; private set; }

        protected override void InitializeApplicationBar()
        {
            base.InitializeApplicationBar();

            AddApplicationBarMenuItem(LocalizedStrings.BrowseArtistPlayAllSongs, () =>
            {
                if (CurrentGroup == null)
                    return;

                RemoteUtility.HandleLibraryPlayTask(CurrentGroup.Play());
            });
        }

        protected override bool ShouldShowContinuumTransition(AnimationType animationType, Uri toOrFrom)
        {
            if (animationType == AnimationType.NavigateForwardIn || animationType == AnimationType.NavigateBackwardOut)
            {
                if (toOrFrom.OriginalString.StartsWith("/Pages/Library/LibraryPage.xaml"))
                    return true;
            }
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

        #region Shuffle Button

        public static readonly DependencyProperty ShuffleButtonVisibilityProperty =
            DependencyProperty.Register("ShuffleButtonVisibility", typeof(Visibility), typeof(ArtistPage), new PropertyMetadata(Visibility.Collapsed));

        public Visibility ShuffleButtonVisibility
        {
            get { return (Visibility)GetValue(ShuffleButtonVisibilityProperty); }
            set { SetValue(ShuffleButtonVisibilityProperty, value); }
        }

        private void SongsViewSource_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            IDACPElementViewSource source = (IDACPElementViewSource)sender;

            if (e.PropertyName == "Items")
            {
                Visibility newVisibility = Visibility.Collapsed;
                var list = source.Items as IDACPList;
                if (list != null)
                {
                    if (list.IsGroupedList && list.Count > 0)
                        newVisibility = Visibility.Visible;
                    else if (!list.IsGroupedList && list.Count >= 2)
                        newVisibility = Visibility.Visible;
                }
                ShuffleButtonVisibility = newVisibility;
            }
        }

        private void ShuffleButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (CurrentGroup == null)
                return;

            RemoteUtility.HandleLibraryPlayTask(CurrentGroup.Shuffle());
            e.Handled = true;
        }

        #endregion
    }
}