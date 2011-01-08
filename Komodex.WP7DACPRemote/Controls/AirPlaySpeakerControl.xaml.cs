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

namespace Komodex.WP7DACPRemote.Controls
{
    public partial class AirPlaySpeakerControl : UserControl
    {
        protected AirPlaySpeakerControl()
        {
            InitializeComponent();
        }

        public AirPlaySpeakerControl(AirPlaySpeaker speaker)
            : this()
        {
            AirPlaySpeaker = speaker;
            UpdateVisualState(false);
            AirPlaySpeaker.PropertyChanged += new PropertyChangedEventHandler(AirPlaySpeaker_PropertyChanged);
        }

        void AirPlaySpeaker_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Active")
                UpdateVisualState();
        }

        #region Properties

        protected AirPlaySpeaker AirPlaySpeaker
        {
            get { return DataContext as AirPlaySpeaker; }
            set { DataContext = value; }
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
            AirPlaySpeaker.Server.AirPlaySpeakerManipulationStopped();
        }

        #endregion

        #region Methods

        protected void UpdateVisualState(bool useTransitions = true)
        {
            if (AirPlaySpeaker.Active == null)
                return;

            if (AirPlaySpeaker.Active == true)
                VisualStateManager.GoToState(this, "SpeakerActiveState", useTransitions);
            else
                VisualStateManager.GoToState(this, "SpeakerInactiveState", useTransitions);
        }

        #endregion
    }
}
