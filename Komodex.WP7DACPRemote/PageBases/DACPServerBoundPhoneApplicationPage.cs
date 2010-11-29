using System;
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

namespace Komodex.WP7DACPRemote
{
    public class DACPServerBoundPhoneApplicationPage : PhoneApplicationPage
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

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            DACPServerManager.ServerChanged += new EventHandler(DACPServerManager_ServerChanged);

            if (DACPServer != DACPServerManager.Server)
                DACPServer = DACPServerManager.Server;
            else
                AttachServerEvents();

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
            // Do nothing
        }


        #endregion

        #region Methods

        private void UpdateApplicationBarVisibility()
        {
            if (ApplicationBar == null)
                return;

            ApplicationBar.IsVisible = (DACPServer != null && DACPServer.IsConnected);
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
