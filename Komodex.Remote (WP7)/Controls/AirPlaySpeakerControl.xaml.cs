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
using System.ComponentModel;
using Microsoft.Phone.Controls;
using Komodex.Common;

namespace Komodex.Remote.Controls
{
    public partial class AirPlaySpeakerControl : UserControl
    {
        private bool _singleSelectionModeEnabled;

        public AirPlaySpeakerControl()
        {
            InitializeComponent();
        }

        #region Speaker Property

        // If we're in design mode, set the property type to "object" to allow the SampleDataAirPlaySpeaker class as well
        public static readonly DependencyProperty SpeakerProperty =
            DependencyProperty.Register("Speaker", (DesignerProperties.IsInDesignTool) ? typeof(object) : typeof(AirPlaySpeaker), typeof(AirPlaySpeakerControl), new PropertyMetadata(SpeakerPropertyChanged));

        private static void SpeakerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AirPlaySpeakerControl control = (AirPlaySpeakerControl)d;

            control.DetachSpeakerEvents(e.OldValue as AirPlaySpeaker);
            control.AttachSpeakerEvents(e.NewValue as AirPlaySpeaker);

            control.UpdateVisualState(false);
        }

        public AirPlaySpeaker Speaker
        {
            get { return (AirPlaySpeaker)GetValue(SpeakerProperty); }
            set { SetValue(SpeakerProperty, value); }
        }

        #endregion

        #region Speaker Event Handlers

        protected void AttachSpeakerEvents(AirPlaySpeaker speaker)
        {
            if (speaker == null)
                return;

            speaker.PropertyChanged += AirPlaySpeaker_PropertyChanged;
        }

        protected void DetachSpeakerEvents(AirPlaySpeaker speaker)
        {
            if (speaker == null)
                return;

            speaker.PropertyChanged -= AirPlaySpeaker_PropertyChanged;
        }

        private void AirPlaySpeaker_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "BindableActive")
                UpdateVisualState(true);
        }

        #endregion

        #region Slider

        private void Slider_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            Speaker.Server.AirPlaySpeakerManipulationStarted(Speaker);
        }

        private void Slider_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            Speaker.Server.AirPlaySpeakerManipulationStopped(Speaker);
        }

        #endregion

        #region Actions

        private void SpeakerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_singleSelectionModeEnabled)
                return;

            Speaker.SetSingleActiveSpeaker();
            SpeakerClicked.Raise(this, new EventArgs());
        }

        #endregion

        #region Methods

        protected void UpdateVisualState(bool useTransitions)
        {
            if (DesignerProperties.IsInDesignTool)
                return;

            if (Speaker.BindableActive == null)
                return;

            if (Speaker.BindableActive == true)
                VisualStateManager.GoToState(this, "SpeakerActiveState", useTransitions);
            else
                VisualStateManager.GoToState(this, "SpeakerInactiveState", useTransitions);
        }

        public void SetSingleSelectionMode(bool value, bool useTransitions)
        {
            _singleSelectionModeEnabled = value;

            if (_singleSelectionModeEnabled)
                VisualStateManager.GoToState(this, "SingleSelectMode", useTransitions);
            else
                VisualStateManager.GoToState(this, "MultiSelectMode", useTransitions);

            TiltEffect.SetSuppressTilt(SpeakerButton, !_singleSelectionModeEnabled);
        }

        #endregion

        #region Events

        public event EventHandler<EventArgs> SpeakerClicked;

        #endregion

    }
}
