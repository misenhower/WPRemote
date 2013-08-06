using Clarity.Phone.Extensions;
using Komodex.Common;
using Komodex.Common.Phone;
using Komodex.DACP;
using Komodex.Remote.ServerManagement;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;

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
            SpeakerList.Link += SpeakerList_Link;
            SpeakerList.Unlink += SpeakerList_Unlink;
#else
            SpeakerList.ItemRealized += SpeakerList_ItemRealized;
            SpeakerList.ItemUnrealized += SpeakerList_ItemUnrealized;
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

        #region Speaker Control Management

#if WP7
        private void SpeakerList_Link(object sender, LinkUnlinkEventArgs e)
        {
            e.ContentPresenter.Loaded += SpeakerContentPresenter_Loaded;
        }

        private void SpeakerContentPresenter_Loaded(object sender, RoutedEventArgs e)
        {
            ContentPresenter contentPresenter = (ContentPresenter)sender;
            contentPresenter.Loaded -= SpeakerContentPresenter_Loaded;

            AirPlaySpeakerControl control = contentPresenter.FindVisualChild<AirPlaySpeakerControl>();
            if (control == null)
                return;

            _currentSpeakerControls.Add(control);
            control.SetSingleSelectionMode(_singleSelectionModeEnabled, false);
        }

        private void SpeakerList_Unlink(object sender, LinkUnlinkEventArgs e)
        {
            AirPlaySpeakerControl control = e.ContentPresenter.FindVisualChild<AirPlaySpeakerControl>();
            if (control == null)
                return;

            _currentSpeakerControls.Remove(control);
        }
#else
        private void SpeakerList_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            AirPlaySpeakerControl control = e.Container.FindVisualChild<AirPlaySpeakerControl>();
            if (control == null)
                return;

            _currentSpeakerControls.Add(control);
            control.SetSingleSelectionMode(_singleSelectionModeEnabled, false);
        }

        private void SpeakerList_ItemUnrealized(object sender, ItemRealizationEventArgs e)
        {
            AirPlaySpeakerControl control = e.Container.FindVisualChild<AirPlaySpeakerControl>();
            if (control == null)
                return;

            _currentSpeakerControls.Remove(control);
        }
#endif

        #endregion

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
