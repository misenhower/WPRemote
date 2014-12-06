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

namespace Komodex.Remote.Controls
{
    public partial class AppleTVControlDialog : DialogUserControlBase
    {
        public AppleTVControlDialog()
        {
            InitializeComponent();

            DataContext = ServerManager.CurrentServer;
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
    }
}
