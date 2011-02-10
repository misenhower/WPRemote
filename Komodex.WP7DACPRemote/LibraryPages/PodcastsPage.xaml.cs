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
using Clarity.Phone.Controls.Animations;

namespace Komodex.WP7DACPRemote.LibraryPages
{
    public partial class PodcastsPage : DACPServerBoundPhoneApplicationPage
    {
        public PodcastsPage()
        {
            InitializeComponent();

            AnimationContext = LayoutRoot;
        }

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            GetPodcasts();
        }

        protected override void DACPServer_ServerUpdate(object sender, DACP.ServerUpdateEventArgs e)
        {
            base.DACPServer_ServerUpdate(sender, e);

            if (e.Type == ServerUpdateType.ServerConnected)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    GetPodcasts();
                });
            }
        }

        protected override Clarity.Phone.Controls.Animations.AnimatorHelperBase GetAnimation(Clarity.Phone.Controls.Animations.AnimationType animationType, Uri toOrFrom)
        {
            string uri = toOrFrom.OriginalString;

            if (uri.Contains("PodcastPage"))
            {
                if (animationType == AnimationType.NavigateForwardOut || animationType == AnimationType.NavigateBackwardIn)
                    return this.GetListSelectorAnimation(lbPodcasts, animationType);
            }
            
            return base.GetAnimation(animationType, toOrFrom);
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

        #region Methods

        protected void GetPodcasts()
        {
            if (DACPServer == null || !DACPServer.IsConnected)
                return;

            if (DACPServer.LibraryPodcasts == null || DACPServer.LibraryPodcasts.Count == 0)
                DACPServer.GetPodcasts();
        }

        #endregion

        #region Actions

        private void PodcastButton_Click(object sender, RoutedEventArgs e)
        {
            Podcast podcast = ((Button)sender).Tag as Podcast;

            if (podcast != null)
            {
                lbPodcasts.SelectedItem = podcast;
                NavigationManager.OpenPodcastPage(podcast.ID, podcast.Name, podcast.PersistentID);
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