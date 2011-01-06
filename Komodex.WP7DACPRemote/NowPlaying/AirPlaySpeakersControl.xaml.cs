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

namespace Komodex.WP7DACPRemote.NowPlaying
{
    public partial class AirPlaySpeakersControl : UserControl
    {
        public AirPlaySpeakersControl()
        {
            InitializeComponent();
        }

        private void Slider_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            AirPlaySpeaker speaker = ((Slider)sender).Tag as AirPlaySpeaker;

            if (speaker != null)
                ((DACPServer)DataContext).AirPlaySpeakerManipulationStarted(speaker);
        }

        private void Slider_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            ((DACPServer)DataContext).AirPlaySpeakerManipulationStopped();
        }
    }
}
