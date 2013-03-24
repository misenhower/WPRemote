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
using Komodex.DACP;
using Komodex.WP7DACPRemote.DACPServerManagement;
using Komodex.WP7DACPRemote.Localization;

namespace Komodex.WP7DACPRemote.Controls
{
    public partial class ConnectingStatusControl : UserControl
    {
        public ConnectingStatusControl()
        {
            InitializeComponent();

            LayoutRoot.DataContext = this;
        }

        public ConnectingStatusControl(bool useServerManager)
            : this()
        {
            if (useServerManager)
            {
                DACPServerManager.ServerChanged += new EventHandler(DACPServerManager_ServerChanged);
                DACPServer = DACPServerManager.Server;
            }
        }

        #region Properties

        private DACPServer _DACPServer = null;
        protected DACPServer DACPServer
        {
            get { return _DACPServer; }
            set
            {
                if (_DACPServer != null)
                {
                    _DACPServer.ServerUpdate -= new EventHandler<ServerUpdateEventArgs>(DACPServer_ServerUpdate);
                }

                _DACPServer = value;


                if (_DACPServer != null)
                {
                    _DACPServer.ServerUpdate += new EventHandler<ServerUpdateEventArgs>(DACPServer_ServerUpdate);
                }

                UpdateFromServer();
            }
        }

        public bool ShowProgressBar
        {
            get
            {
                return progressBar.IsIndeterminate;
            }
            set
            {
                progressBar.IsIndeterminate = value;
                progressBar.Visibility = (value) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }
        }

        public string LibraryName
        {
            get { return tbLibraryName.Text; }
            set { tbLibraryName.Text = value; }
        }

        public string LibraryConnectionText
        {
            get { return tbLibraryConnectionText.Text; }
            set { tbLibraryConnectionText.Text = value; }
        }

        #endregion

        #region Event Handlers

        void DACPServerManager_ServerChanged(object sender, EventArgs e)
        {
            DACPServer = DACPServerManager.Server;
        }

        void DACPServer_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            UpdateFromServer();
        }

        #endregion

        #region Methods

        public void UpdateFromServer()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (DACPServer == null)
                {
                    ShowProgressBar = false;
                    LibraryConnectionText = LocalizedStrings.StatusTapChooseLibrary;
                    LibraryName = string.Empty;
                }
                else if (!DACPServerManager.IsNetworkAvailable)
                {
                    ShowProgressBar = true;
                    LibraryConnectionText = LocalizedStrings.WaitingForWiFiConnection;
                    LibraryName = string.Empty;
                }
                else if (!DACPServer.IsConnected)
                {
                    ShowProgressBar = true;
                    LibraryConnectionText = LocalizedStrings.StatusConnectingToLibrary;
                    LibraryName = DACPServer.LibraryName;
                }
                else // Connected, this control shouldn't be visible at all
                {
                    ShowProgressBar = false;
                }
            });
        }

        #endregion

        #region Button

        public string ButtonText
        {
            get { return btnAction.Content as string; }
            set { btnAction.Content = value; }
        }

        public event RoutedEventHandler ButtonClick;

        private void btnAction_Click(object sender, RoutedEventArgs e)
        {
            if (ButtonClick != null)
                ButtonClick(sender, e);
        }

        #endregion
    }
}
