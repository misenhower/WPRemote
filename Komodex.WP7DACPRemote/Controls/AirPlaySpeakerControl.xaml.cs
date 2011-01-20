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
using Microsoft.Unsupported;

namespace Komodex.WP7DACPRemote.Controls
{
    public partial class AirPlaySpeakerControl : UserControl
    {
        protected AirPlaySpeakerControl()
        {
            InitializeComponent();
        }

        public AirPlaySpeakerControl(AirPlaySpeaker speaker, bool singleSelect)
            : this()
        {
            AirPlaySpeaker = speaker;
            UpdateVisualState(false);
            SetSingleSelectMode(singleSelect, false);
            AirPlaySpeaker.PropertyChanged += new PropertyChangedEventHandler(AirPlaySpeaker_PropertyChanged);
        }

        void AirPlaySpeaker_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "BindableActive")
                UpdateVisualState();
        }

        #region Properties

        protected AirPlaySpeaker AirPlaySpeaker
        {
            get { return DataContext as AirPlaySpeaker; }
            set { DataContext = value; }
        }

        private bool _SingleSelectMode = false;
        public bool SingleSelectionMode
        {
            get { return _SingleSelectMode; }
            set { SetSingleSelectMode(value, true); }
        }

        #endregion

        #region Checkbox

        private void AirPlaySpeakerCheckBox_Click(object sender, RoutedEventArgs e)
        {
        }

        #endregion

        #region Slider

        private void Slider_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            AirPlaySpeaker.Server.AirPlaySpeakerManipulationStarted(AirPlaySpeaker);
        }

        private void Slider_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            AirPlaySpeaker.Server.AirPlaySpeakerManipulationStopped(AirPlaySpeaker);
        }

        #endregion

        #region Actions

        private void SpeakerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SingleSelectionMode)
                return;

            AirPlaySpeaker.SetSingleActiveSpeaker();
            if (SingleSpeakerClicked != null)
                SingleSpeakerClicked(this, new EventArgs());
        }

        #endregion

        #region Methods

        protected void UpdateVisualState(bool useTransitions = true)
        {
            if (AirPlaySpeaker.BindableActive == null)
                return;

            if (AirPlaySpeaker.BindableActive == true)
                VisualStateManager.GoToState(this, "SpeakerActiveState", useTransitions);
            else
                VisualStateManager.GoToState(this, "SpeakerInactiveState", useTransitions);
        }

        protected void SetSingleSelectMode(bool singleSelect, bool useTransitions = true)
        {
            if (singleSelect)
                VisualStateManager.GoToState(this, "SingleSelectMode", useTransitions);
            else
                VisualStateManager.GoToState(this, "MultiSelectMode", useTransitions);

            TiltEffect.SetSuppressTilt(SpeakerButton, !singleSelect);

            _SingleSelectMode = singleSelect;
        }

        #endregion

        #region Events

        public event EventHandler SingleSpeakerClicked;

        #endregion

    }
}
