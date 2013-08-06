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
using System.Windows.Media;
using Clarity.Phone.Extensions;
using Komodex.DACP;
using System.ComponentModel;

namespace Komodex.Remote.Controls
{
    public partial class AirPlaySpeakersDialog : DialogUserControlBase
    {
        private bool _singleSelectionModeEnabled;
        private List<AirPlaySpeakerControl> _currentSpeakerControls = new List<AirPlaySpeakerControl>();

        public AirPlaySpeakersDialog()
        {
            InitializeComponent();

#if WP7
            SpeakerList.Link += (sender, e) => ItemRealized(e.ContentPresenter);
            SpeakerList.Unlink += (sender, e) => ItemUnrealized(e.ContentPresenter);
#else
            SpeakerList.ItemRealized += (sender, e) => ItemRealized(e.Container);
            SpeakerList.ItemUnrealized += (sender, e) => ItemUnrealized(e.Container);
#endif
        }

        protected override void Show(ContentPresenter container)
        {
            DataContext = ServerManager.CurrentServer;
            AttachServerEvents(ServerManager.CurrentServer);
            UpdateSingleSelectionMode(false);

            base.Show(container);
        }

        public override void Hide(MessageBoxResult result = MessageBoxResult.None)
        {
            base.Hide(result);

            DetachServerEvents(ServerManager.CurrentServer);
        }

        #region Server Events

        protected void AttachServerEvents(DACPServer server)
        {
            if (server == null)
                return;

            server.PropertyChanged += DACPServer_PropertyChanged;
        }

        protected void DetachServerEvents(DACPServer server)
        {
            if (server == null)
                return;

            server.PropertyChanged -= DACPServer_PropertyChanged;
        }

        private void DACPServer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsCurrentlyPlayingVideo")
                UpdateSingleSelectionMode(true);
        }

        #endregion

        #region Speaker Control Management

        private void ItemRealized(ContentPresenter contentPresenter)
        {
            AirPlaySpeakerControl control = contentPresenter.FindVisualChild<AirPlaySpeakerControl>();
            if (control == null)
                return;

            _currentSpeakerControls.Add(control);
            control.SetSingleSelectionMode(_singleSelectionModeEnabled, false);
        }

        private void ItemUnrealized(ContentPresenter contentPresenter)
        {
            AirPlaySpeakerControl control = contentPresenter.FindVisualChild<AirPlaySpeakerControl>();
            if (control == null)
                return;

            _currentSpeakerControls.Remove(control);
        }

        #endregion

        #region Single Selection Mode

        public void UpdateSingleSelectionMode(bool useTransitions)
        {
            DACPServer server = ServerManager.CurrentServer;
            if (server == null)
                return;

            if (server.IsCurrentlyPlayingVideo)
                SetSingleSelectionMode(true, useTransitions);
            else
                SetSingleSelectionMode(false, useTransitions);
        }

        protected void SetSingleSelectionMode(bool value, bool useTransitions)
        {
            _singleSelectionModeEnabled = value;

            foreach (var control in _currentSpeakerControls)
                control.SetSingleSelectionMode(value, useTransitions);
        }

        private void AirPlaySpeakerControl_SpeakerClicked(object sender, EventArgs e)
        {
            Hide();
        }

        #endregion

    }
}
