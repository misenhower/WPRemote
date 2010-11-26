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
using Komodex.DACP;
using Komodex.WP7DACPRemote.DACPServerManagement;
using System.Text.RegularExpressions;

namespace Komodex.WP7DACPRemote.DACPServerInfoManagement
{
    public partial class AddLibraryPage : PhoneApplicationPage
    {
        DACPServerInfo serverInfo = null;
        DACPServer server = null;

        public AddLibraryPage()
        {
            InitializeComponent();

            serverInfo = new DACPServerInfo();
            serverInfo.ID = Guid.NewGuid();
            DataContext = serverInfo;
        }

        #region AppBar Button Handlers

        private void UpdateBoundData()
        {
            try
            {
                TextBox tb = (TextBox)FocusManager.GetFocusedElement();
                var binding = tb.GetBindingExpression(TextBox.TextProperty);
                binding.UpdateSource();
            }
            catch { }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // Make sure the newly entered data has been bound to the DACPServerInfo object
            UpdateBoundData();

            // Validate the server info
            SetVisibility(true);
            server = new DACPServer(serverInfo.HostName, serverInfo.PairingCode);
            server.ServerUpdate += new EventHandler<ServerUpdateEventArgs>(server_ServerUpdate);
            server.Start(false);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }

        #endregion

        #region Control Event Handlers

        private void tbPIN_KeyDown(object sender, KeyEventArgs e)
        {
            // Only allow numeric characters
            bool validCharacter = (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || (e.Key >= Key.D0 && e.Key <= Key.D9);

            if (!validCharacter)
                e.Handled = true;
        }

        private void tbPIN_KeyUp(object sender, KeyEventArgs e)
        {
            TextBox textBox = (TextBox)sender;

            int selectionStart = textBox.SelectionStart;
            int textLen = textBox.Text.Length;

            textBox.Text = Regex.Replace(textBox.Text, "\\D", string.Empty);

            int newTextLen = textBox.Text.Length;
            if (newTextLen < textLen && selectionStart > 0)
                selectionStart--;

            textBox.SelectionStart = selectionStart;
        }

        private void connectingStatusControl_ButtonClick(object sender, RoutedEventArgs e)
        {
            if (server != null)
            {
                server.Stop();
                server = null;
            }
            SetVisibility(false);
        }

        #endregion

        #region Server Validation

        void SetVisibility(bool isConnecting)
        {
            ApplicationBar.IsVisible = !isConnecting;
            connectingStatusControl.ShowProgressBar = isConnecting;
            connectingStatusControl.Visibility = (isConnecting) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        void server_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                switch (e.Type)
                {
                    case ServerUpdateType.ServerConnected:
                        // PIN was correct
                        serverInfo.LibraryName = server.LibraryName;
                        server.Stop();
                        server = null;
                        DACPServerViewModel.Instance.Items.Add(serverInfo);
                        DACPServerManager.ConnectToServer(serverInfo.ID);
                        NavigationService.GoBack();
                        break;
                    case ServerUpdateType.Error:
                        MessageBox.Show("Could not connect to library. Please check your settings and try again.");
                        SetVisibility(false);
                        break;
                    default:
                        break;
                }
            });
        }

        #endregion
    }
}