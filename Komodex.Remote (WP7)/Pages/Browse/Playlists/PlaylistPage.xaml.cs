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
using System.ComponentModel;
using Komodex.Remote.Data;
using Clarity.Phone.Controls.Animations;

namespace Komodex.Remote.Pages.Browse.Playlists
{
    public partial class PlaylistPage : BrowsePlaylistBasePage
    {
        private int _playlistID;

        public PlaylistPage()
        {
            InitializeComponent();

            var playlistViewSource = GetContainerViewSource(c => c.GetItemsOrSubListsAsync());
            playlistViewSource.PropertyChanged += PlaylistViewSource_PropertyChanged;
            PlaylistViewSource = playlistViewSource;
        }

        public object PlaylistViewSource { get; private set; }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var queryString = NavigationContext.QueryString;
            _playlistID = int.Parse(queryString["playlistID"]);

            base.OnNavigatedTo(e);
        }

        protected override Playlist GetContainer(DACPDatabase database)
        {
            return database.Playlists.First(pl => pl.ID == _playlistID);
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
            if (item is Playlist)
            {
                NavigationManager.OpenPlaylistPage((Playlist)item);
                return;
            }

            if (item is DACPItem)
            {
                RemoteUtility.HandleLibraryPlayTask(CurrentContainer.PlayItem((DACPItem)item));
            }
        }

        private void PlayQueueButton_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;

            PlayQueueMode mode;
            switch (menuItem.Name)
            {
                case "PlayNextButton": mode = PlayQueueMode.PlayNext; break;
                case "AddToUpNextButton": mode = PlayQueueMode.AddToQueue; break;
                default: return;
            }

            if (menuItem.DataContext is DACPItem)
            {
                RemoteUtility.HandleLibraryPlayTask(CurrentContainer.PlayItem((DACPItem)menuItem.DataContext, mode));
            }
        }

        #region Shuffle Button

        public static readonly DependencyProperty ShuffleButtonVisibilityProperty =
            DependencyProperty.Register("ShuffleButtonVisibility", typeof(Visibility), typeof(PlaylistPage), new PropertyMetadata(Visibility.Collapsed));

        public Visibility ShuffleButtonVisibility
        {
            get { return (Visibility)GetValue(ShuffleButtonVisibilityProperty); }
            set { SetValue(ShuffleButtonVisibilityProperty, value); }
        }

        private void PlaylistViewSource_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            IDACPElementViewSource source = (IDACPElementViewSource)sender;

            if (e.PropertyName == "Items")
            {
                Visibility newVisibility = Visibility.Collapsed;
                var list = source.Items as List<DACPItem>;
                if (list != null && list.Count > 2)
                    newVisibility = Visibility.Visible;
                ShuffleButtonVisibility = newVisibility;
            }
        }

        private void ShuffleButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            RemoteUtility.HandleLibraryPlayTask(CurrentContainer.Shuffle());
            e.Handled = true;
        }

        #endregion
    }
}