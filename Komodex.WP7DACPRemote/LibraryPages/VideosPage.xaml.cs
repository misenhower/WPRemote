using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Komodex.DACP;
using Komodex.DACP.Library;

namespace Komodex.WP7DACPRemote.LibraryPages
{
    public partial class VideosPage : DACPServerBoundPhoneApplicationPage
    {
        public VideosPage()
        {
            InitializeComponent();

            AnimationContext = LayoutRoot;
        }

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (State.ContainsKey(StateUtils.SavedStateKey))
            {
                this.RestoreState(pivotControl, 0);
            }

            GetDataForPivotItem();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            this.PreserveState(pivotControl);
            State[StateUtils.SavedStateKey] = true;
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (openedGroupViewSelector != null)
            {
                openedGroupViewSelector.CloseGroupView();
                openedGroupViewSelector = null;
                e.Cancel = true;
                return;
            }

            base.OnBackKeyPress(e);
        }

        #endregion

        #region Event Handlers

        protected override void DACPServer_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            base.DACPServer_ServerUpdate(sender, e);

            if (e.Type == ServerUpdateType.ServerConnected)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    GetDataForPivotItem();
                });
            }
        }

        #endregion

        #region Actions

        private void pivotControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetDataForPivotItem();
        }

        #endregion

        #region Movies

        private void MoviePlayButton_Click(object sender, RoutedEventArgs e)
        {
            MediaItem movie = ((Button)sender).Tag as MediaItem;

            if (movie != null)
            {
                movie.SendPlayMediaItemCommand();
                NavigationManager.OpenNowPlayingPage();
            }
        }

        #endregion

        #region Methods

        private void GetDataForPivotItem()
        {
            if (DACPServer == null || !DACPServer.IsConnected)
                return;

            if (pivotControl.SelectedItem == pivotMovies)
            {
                if (DACPServer.LibraryMovies == null || DACPServer.LibraryMovies.Count == 0)
                    DACPServer.GetMovies();
            }
            else if (pivotControl.SelectedItem == pivotTVShows)
            {
                if (DACPServer.LibraryTVShows == null || DACPServer.LibraryTVShows.Count == 0)
                    DACPServer.GetTVShows();
            }
        }

        #endregion

        #region Group View Management

        private LongListSelector openedGroupViewSelector = null;

        private void LongListSelector_GroupViewOpened(object sender, GroupViewOpenedEventArgs e)
        {
            openedGroupViewSelector = (LongListSelector)sender;
        }

        private void LongListSelector_GroupViewClosing(object sender, GroupViewClosingEventArgs e)
        {
            openedGroupViewSelector = null;
        }

        #endregion
    }
}