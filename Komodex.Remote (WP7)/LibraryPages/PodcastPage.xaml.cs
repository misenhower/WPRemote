﻿using System;
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
using Komodex.DACP.Library;
using Komodex.Remote.DACPServerManagement;

namespace Komodex.Remote.LibraryPages
{
    public partial class PodcastPage : DACPServerBoundPhoneApplicationPage
    {
        public PodcastPage()
        {
            InitializeComponent();

            AnimationContext = LayoutRoot;
        }

        #region Properties

        private Podcast Podcast
        {
            get { return LayoutRoot.DataContext as Podcast; }
            set { LayoutRoot.DataContext = value; }
        }

        #endregion

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var queryString = NavigationContext.QueryString;

            int podcastID = int.Parse(queryString["id"]);
            UInt64 podcastPersistentID = UInt64.Parse(queryString["perid"]);
            string podcastName = queryString["name"];

            if (Podcast == null)
            {
                Podcast = new Podcast(DACPServerManager.Server, podcastID, podcastName, podcastPersistentID);
                if (Podcast.Server != null && Podcast.Server.IsConnected)
                    Podcast.GetEpisodes();
            }
        }

        protected override void DACPServer_ServerUpdate(object sender, DACP.ServerUpdateEventArgs e)
        {
            base.DACPServer_ServerUpdate(sender, e);

            if (e.Type == DACP.ServerUpdateType.ServerConnected)
                Deployment.Current.Dispatcher.BeginInvoke(() => { Podcast.GetEpisodes(); });
        }

        protected override Clarity.Phone.Controls.Animations.AnimatorHelperBase GetAnimation(Clarity.Phone.Controls.Animations.AnimationType animationType, Uri toOrFrom)
        {
            string uri = toOrFrom.OriginalString;

            if (uri.Contains("PodcastsPage") || uri.Contains("SearchPage"))
                return GetContinuumAnimation(PageTitle, animationType, false);
            
            return base.GetAnimation(animationType, toOrFrom);
        }

        #endregion

        #region Actions

        private void LongListSelector_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            LongListSelector listBox = (LongListSelector)sender;
            var selectedItem = listBox.SelectedItem;

            if (selectedItem is MediaItem)
            {
                MediaItem episode = (MediaItem)selectedItem;
                episode.SendPlayMediaItemCommand();
                NavigationManager.OpenNowPlayingPage();
            }

            listBox.SelectedItem = null;
        }

        #endregion

    }

}