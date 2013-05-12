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

namespace Komodex.Remote.LibraryPages
{
    public partial class PodcastsPage : RemoteBasePage
    {
        public PodcastsPage()
        {
            InitializeComponent();
        }

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            GetPodcasts();
        }

        protected override void CurrentServer_ServerUpdate(object sender, DACP.ServerUpdateEventArgs e)
        {
            base.CurrentServer_ServerUpdate(sender, e);

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

        #endregion

        #region Methods

        protected void GetPodcasts()
        {
            if (CurrentServer == null || !CurrentServer.IsConnected)
                return;

            if (CurrentServer.LibraryPodcasts == null || CurrentServer.LibraryPodcasts.Count == 0)
                CurrentServer.GetPodcasts();
        }

        #endregion

        #region Actions

        private void LongListSelector_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            LongListSelector listBox = (LongListSelector)sender;
            var selectedItem = listBox.SelectedItem;

            if (selectedItem is Podcast)
            {
                Podcast podcast = (Podcast)selectedItem;
                NavigationManager.OpenPodcastPage(podcast.ID, podcast.Name, podcast.PersistentID);
            }
        }

        #endregion

    }
}