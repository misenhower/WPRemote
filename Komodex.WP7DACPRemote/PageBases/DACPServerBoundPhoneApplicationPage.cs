﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Komodex.DACP;
using Komodex.WP7DACPRemote.DACPServerManagement;
using Microsoft.Phone.Shell;
using Clarity.Phone.Controls;

namespace Komodex.WP7DACPRemote
{
    public class DACPServerBoundPhoneApplicationPage : AnimatedBasePage
    {
        #region Properties

        protected DACPServer DACPServer
        {
            get { return DataContext as DACPServer; }
            set
            {
                DetachServerEvents();

                DataContext = value;

                AttachServerEvents();
            }
        }

        #endregion

        #region Standard Play Transport Application Bar

        private ApplicationBarIconButton btnAppBarPreviousTrack = null;
        private ApplicationBarIconButton btnAppBarPlayPause = null;
        private ApplicationBarIconButton btnAppBarNextTrack = null;
        private readonly Uri iconPlay = new Uri("/icons/appbar.transport.play.rest.png", UriKind.Relative);
        private readonly Uri iconPause = new Uri("/icons/appbar.transport.pause.rest.png", UriKind.Relative);

        protected bool UsesStandardPlayTransportApplicationBar { get; private set; }

        protected void InitializeStandardPlayTransportApplicationBar()
        {
            // Previous track
            btnAppBarPreviousTrack = new ApplicationBarIconButton(new Uri("/icons/appbar.transport.rew.rest.png", UriKind.Relative));
            btnAppBarPreviousTrack.Text = "previous";
            btnAppBarPreviousTrack.Click += new EventHandler(btnAppBarPreviousTrack_Click);
            ApplicationBar.Buttons.Add(btnAppBarPreviousTrack);

            // Play/pause
            btnAppBarPlayPause = new ApplicationBarIconButton(iconPlay);
            btnAppBarPlayPause.Text = "play/pause";
            btnAppBarPlayPause.Click += new EventHandler(btnAppBarPlayPause_Click);
            ApplicationBar.Buttons.Add(btnAppBarPlayPause);

            // Next track
            btnAppBarNextTrack = new ApplicationBarIconButton(new Uri("/icons/appbar.transport.ff.rest.png", UriKind.Relative));
            btnAppBarNextTrack.Text = "next";
            btnAppBarNextTrack.Click += new EventHandler(btnAppBarNextTrack_Click);
            ApplicationBar.Buttons.Add(btnAppBarNextTrack);

            // Set the flag
            UsesStandardPlayTransportApplicationBar = true;
        }

        private void UpdateTransportButtons()
        {
            if (!UsesStandardPlayTransportApplicationBar)
                return;

            bool isStopped = true;
            bool isPlaying = false;

            if (DACPServer != null)
            {
                isStopped = (DACPServer.PlayState == PlayStates.Stopped && DACPServer.CurrentSongName == null);
                isPlaying = (DACPServer.PlayState == PlayStates.Playing);
            }

            btnAppBarPreviousTrack.IsEnabled = btnAppBarPlayPause.IsEnabled = btnAppBarNextTrack.IsEnabled = !isStopped;

            if (isPlaying)
                btnAppBarPlayPause.IconUri = iconPause;
            else
                btnAppBarPlayPause.IconUri = iconPlay;
        }

        private void btnAppBarPreviousTrack_Click(object sender, EventArgs e)
        {
            DACPServer.SendPrevItemCommand();
        }

        private void btnAppBarNextTrack_Click(object sender, EventArgs e)
        {
            DACPServer.SendNextItemCommand();
        }

        private void btnAppBarPlayPause_Click(object sender, EventArgs e)
        {
            DACPServer.SendPlayPauseCommand();
        }

        #endregion

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            DACPServerManager.ServerChanged += new EventHandler(DACPServerManager_ServerChanged);

            if (DACPServer != DACPServerManager.Server)
                DACPServer = DACPServerManager.Server;
            else
                AttachServerEvents();

            UpdateTransportButtons();
            UpdateApplicationBarVisibility();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            DACPServerManager.ServerChanged -= new EventHandler(DACPServerManager_ServerChanged);
            DetachServerEvents();
        }

        #endregion

        #region Event Handlers

        protected virtual void DACPServerManager_ServerChanged(object sender, EventArgs e)
        {
            DACPServer = DACPServerManager.Server;
            UpdateApplicationBarVisibility();
        }

        protected virtual void DACPServer_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                UpdateApplicationBarVisibility();
            });
        }

        protected virtual void DACPServer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "PlayState")
                UpdateTransportButtons();
        }

        #endregion

        #region Methods

        private void UpdateApplicationBarVisibility()
        {
            if (ApplicationBar == null)
                return;

            ApplicationBar.IsVisible = (!NavigationManager.NavigatingToFirstPage && DACPServer != null && DACPServer.IsConnected);
        }

        private void AttachServerEvents()
        {
            if (DACPServer != null)
            {
                DACPServer.ServerUpdate += new EventHandler<ServerUpdateEventArgs>(DACPServer_ServerUpdate);
                DACPServer.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(DACPServer_PropertyChanged);
            }
        }

        private void DetachServerEvents()
        {
            if (DACPServer != null)
            {
                DACPServer.ServerUpdate -= new EventHandler<ServerUpdateEventArgs>(DACPServer_ServerUpdate);
                DACPServer.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(DACPServer_PropertyChanged);
            }
        }

        #endregion
    }
}
