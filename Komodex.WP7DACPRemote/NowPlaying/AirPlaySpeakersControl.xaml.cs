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
using Komodex.WP7DACPRemote.Controls;
using System.Collections.Specialized;
using System.Collections.ObjectModel;

namespace Komodex.WP7DACPRemote.NowPlaying
{
    public partial class AirPlaySpeakersControl : UserControl
    {
        protected AirPlaySpeakersControl()
        {
            InitializeComponent();
        }

        public AirPlaySpeakersControl(DACPServer server)
            : this()
        {
            Speakers = server.Speakers;
            ReloadSpeakerList();
            Speakers.CollectionChanged += new NotifyCollectionChangedEventHandler(Speakers_CollectionChanged);
        }

        ObservableCollection<AirPlaySpeaker> Speakers = null;
        Dictionary<AirPlaySpeaker, AirPlaySpeakerControl> SpeakerControls = new Dictionary<AirPlaySpeaker, AirPlaySpeakerControl>();

        #region List Management

        protected void ReloadSpeakerList()
        {
            AirPlaySpeakerStackPanel.Children.Clear();
            SpeakerControls.Clear();

            foreach (AirPlaySpeaker speaker in Speakers)
                AddSpeaker(speaker);
        }

        private void AddSpeaker(AirPlaySpeaker speaker)
        {
            AirPlaySpeakerControl speakerControl = new AirPlaySpeakerControl(speaker);
            SpeakerControls.Add(speaker, speakerControl);
            AirPlaySpeakerStackPanel.Children.Add(speakerControl);
        }

        private void RemoveSpeaker(AirPlaySpeaker speaker)
        {
            if (SpeakerControls.ContainsKey(speaker))
            {
                AirPlaySpeakerControl speakerControl = SpeakerControls[speaker];
                if (AirPlaySpeakerStackPanel.Children.Contains(speakerControl))
                    AirPlaySpeakerStackPanel.Children.Remove(speakerControl);
            }
        }

        void Speakers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                ReloadSpeakerList();
                /* TODO:
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        break;
                    default:
                        break;
                }
                */
            });
        }

        #endregion

    }
}
