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
using Komodex.Common;

namespace Komodex.Remote.LibraryPages
{
    public partial class VideosPage : RemoteBasePage
    {
        public VideosPage()
        {
            InitializeComponent();
        }

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            try
            {
                if (State.ContainsKey(StateUtils.SavedStateKey))
                {
                    this.RestoreState(pivotControl, 0);
                }
            }
            catch (InvalidOperationException) { }

            GetDataForPivotItem();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            try
            {
                this.PreserveState(pivotControl);
                State[StateUtils.SavedStateKey] = true;
            }
            catch (InvalidOperationException) { }
        }

        #endregion

        #region Event Handlers

        protected override void CurrentServer_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            base.CurrentServer_ServerUpdate(sender, e);

            if (e.Type == ServerUpdateType.ServerConnected)
                Utility.BeginInvokeOnUIThread(GetDataForPivotItem);
        }

        #endregion

        #region Actions

        private void pivotControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetDataForPivotItem();
        }

        private void LongListSelector_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            LongListSelector listBox = (LongListSelector)sender;
            var selectedItem = listBox.SelectedItem;

            if (selectedItem is MediaItem)
            {
                MediaItem movie = (MediaItem)selectedItem;
                movie.SendPlayMediaItemCommand();
                NavigationManager.OpenNowPlayingPage();
            }
        }

        #endregion

        #region Methods

        private void GetDataForPivotItem()
        {
            if (CurrentServer == null || !CurrentServer.IsConnected)
                return;

            if (pivotControl.SelectedItem == pivotMovies)
            {
                if (CurrentServer.LibraryMovies == null || CurrentServer.LibraryMovies.Count == 0)
                    CurrentServer.GetMovies();
            }
            else if (pivotControl.SelectedItem == pivotTVShows)
            {
                if (CurrentServer.LibraryTVShows == null || CurrentServer.LibraryTVShows.Count == 0)
                    CurrentServer.GetTVShows();
            }
        }

        #endregion

    }
}