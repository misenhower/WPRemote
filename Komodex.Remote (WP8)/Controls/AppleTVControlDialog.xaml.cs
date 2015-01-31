using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Komodex.Common.Phone;
using Komodex.Remote.ServerManagement;
using System.Windows.Input;
using Komodex.DACP;
using System.ComponentModel;

namespace Komodex.Remote.Controls
{
    public partial class AppleTVControlDialog : DialogUserControlBase
    {
        private bool _ignorePasswordChanges = true;

        public AppleTVControlDialog()
        {
            InitializeComponent();

            DataContext = ServerManager.CurrentServer;
        }

        protected override void DialogService_Opened(object sender, EventArgs e)
        {
            base.DialogService_Opened(sender, e);

            DACPServer server = DataContext as DACPServer;
            if (server != null)
                server.PropertyChanged += DACPServer_PropertyChanged;

            UpdateKeyboardVisibility();
        }

        protected override void DialogService_Closed(object sender, EventArgs e)
        {
            base.DialogService_Closed(sender, e);

            DACPServer server = DataContext as DACPServer;
            if (server != null)
                server.PropertyChanged -= DACPServer_PropertyChanged;
        }

        private void DACPServer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "IsAppleTVKeyboardVisible":
                case "CurrentAppleTVKeyboardType":
                    UpdateKeyboardVisibility();
                    break;
            }
        }

        private void UpdateKeyboardVisibility()
        {
            DACPServer server = DataContext as DACPServer;
            if (server == null)
                return;

            if (server.IsAppleTVKeyboardVisible)
            {
                Trackpad.Visibility = Visibility.Collapsed;
                Keyboard.Visibility = Visibility.Visible;

                var inputScope = new InputScope();

                var keyboardType = server.CurrentAppleTVKeyboardType;

                switch (keyboardType)
                {
                    case AppleTVKeyboardType.Email:
                        inputScope.Names.Add(new InputScopeName() { NameValue = InputScopeNameValue.EmailNameOrAddress });
                        break;
                    case AppleTVKeyboardType.Standard:
                    default:
                        inputScope.Names.Add(new InputScopeName() { NameValue = InputScopeNameValue.Default });
                        break;
                }

                KeyboardTextBox.InputScope = inputScope;

                if (keyboardType == AppleTVKeyboardType.Password)
                {
                    _ignorePasswordChanges = true;
                    KeyboardPasswordBox.Password = string.Empty;
                    KeyboardTextBox.Visibility = Visibility.Collapsed;
                    KeyboardPasswordBox.Visibility = Visibility.Visible;
                    KeyboardPasswordBox.Focus();
                    _ignorePasswordChanges = false;
                }
                else
                {
                    _ignorePasswordChanges = true;
                    KeyboardPasswordBox.Visibility = Visibility.Collapsed;
                    KeyboardTextBox.Visibility = Visibility.Visible;
                    KeyboardTextBox.Focus();
                }
            }
            else
            {
                Keyboard.Visibility = Visibility.Collapsed;
                Trackpad.Visibility = Visibility.Visible;
            }
        }

        private bool _isDragging;

        private void Trackpad_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            // This event is fired even when the user taps the trackpad so we don't actually want to do anything here.
        }

        private void Trackpad_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            var origin = e.ManipulationOrigin;
            if (!_isDragging)
                ServerManager.CurrentServer.AppleTVTrackpadTouchStart((short)origin.X, (short)origin.Y);
            else
                ServerManager.CurrentServer.AppleTVTrackpadTouchMove((short)origin.X, (short)origin.Y);
            _isDragging = true;
        }

        private void Trackpad_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            var origin = e.ManipulationOrigin;
            ServerManager.CurrentServer.AppleTVTrackpadTouchRelease((short)origin.X, (short)origin.Y);
            _isDragging = false;
        }

        private void Trackpad_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ServerManager.CurrentServer.SendAppleTVSelectCommand();
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            ServerManager.CurrentServer.SendAppleTVMenuCommandAsync();
        }

        private void MenuButton_Hold(object sender, EventArgs e)
        {
            ServerManager.CurrentServer.SendAppleTVTopMenuCommandAsync();
        }

        private void ContextMenuButton_Click(object sender, RoutedEventArgs e)
        {
            ServerManager.CurrentServer.SendAppleTVContextMenuCommandAsync();
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            ServerManager.CurrentServer.SendPlayPauseCommand();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ((TextBox)sender).GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            var server = DataContext as DACPServer;
            if (server == null || !server.IsConnected)
                return;

            switch (e.Key)
            {
                case Key.Enter:
                    var task = server.SendAppleTVKeyboardDoneCommandAsync();
                    break;
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_ignorePasswordChanges)
                return;

            DACPServer server = DataContext as DACPServer;
            if (server == null || !server.IsConnected)
                return;

            string obscured = new string('*', ((PasswordBox)sender).Password.Length);
            server.BindableAppleTVKeyboardString = obscured;
        }

        private void PasswordBox_KeyUp(object sender, KeyEventArgs e)
        {
            var server = DataContext as DACPServer;
            if (server == null || !server.IsConnected)
                return;

            switch (e.Key)
            {
                case Key.Enter:
                    var task = server.SendAppleTVKeyboardSecureTextAsync(KeyboardPasswordBox.Password);
                    break;
            }
        }
    }
}
