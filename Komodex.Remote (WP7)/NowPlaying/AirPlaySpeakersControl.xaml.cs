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
using Komodex.Remote.Controls;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using Komodex.Common;

namespace Komodex.Remote.NowPlaying
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
            Server = server;
            ReloadSpeakerList();
            UpdateIsPlayingVideo();
            Server.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Server_PropertyChanged);
            Server.Speakers.CollectionChanged += new NotifyCollectionChangedEventHandler(Speakers_CollectionChanged);
            Server.AirPlaySpeakerUpdate += new EventHandler(Server_AirPlaySpeakerUpdate);
        }

        DACPServer Server = null;
        Dictionary<AirPlaySpeaker, AirPlaySpeakerControl> SpeakerControls = new Dictionary<AirPlaySpeaker, AirPlaySpeakerControl>();

        #region Properties

        private bool _SingleSelectMode = false;
        public bool SingleSelectMode
        {
            get { return _SingleSelectMode; }
            set
            {
                _SingleSelectMode = value;
                foreach (var speaker in SpeakerControls)
                    speaker.Value.SingleSelectionMode = value;
            }
        }

        #endregion

        #region Event Handlers

        void Server_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "IsCurrentlyPlayingVideo":
                    UpdateIsPlayingVideo();
                    break;
                default:
                    break;
            }
        }

        void Server_AirPlaySpeakerUpdate(object sender, EventArgs e)
        {
            Utility.BeginInvokeOnUIThread(UpdateMasterVolumeSlider);
        }

        void Speakers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Utility.BeginInvokeOnUIThread(() =>
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

        void SpeakerControl_SingleSpeakerClicked(object sender, EventArgs e)
        {
            if (SingleSpeakerClicked != null)
                SingleSpeakerClicked(sender, new EventArgs());
        }

        #endregion

        #region List Management

        protected void ReloadSpeakerList()
        {
            var tempSpeakerControls = SpeakerControls.Keys.ToList();
            foreach (AirPlaySpeaker speaker in tempSpeakerControls)
                RemoveSpeaker(speaker);

            foreach (AirPlaySpeaker speaker in Server.Speakers)
                AddSpeaker(speaker);
        }

        private void AddSpeaker(AirPlaySpeaker speaker)
        {
            AirPlaySpeakerControl speakerControl = new AirPlaySpeakerControl(speaker, SingleSelectMode);
            if (Server.IsCurrentlyPlayingVideo && !speaker.HasVideo)
                speakerControl.Visibility = System.Windows.Visibility.Collapsed;
            speakerControl.SingleSpeakerClicked += new EventHandler(SpeakerControl_SingleSpeakerClicked);
            SpeakerControls.Add(speaker, speakerControl);
            AirPlaySpeakerStackPanel.Children.Add(speakerControl);
        }

        private void RemoveSpeaker(AirPlaySpeaker speaker)
        {
            if (SpeakerControls.ContainsKey(speaker))
            {
                AirPlaySpeakerControl speakerControl = SpeakerControls[speaker];
                speakerControl.SingleSpeakerClicked -= new EventHandler(SpeakerControl_SingleSpeakerClicked);
                if (AirPlaySpeakerStackPanel.Children.Contains(speakerControl))
                    AirPlaySpeakerStackPanel.Children.Remove(speakerControl);
                SpeakerControls.Remove(speaker);
            }
        }

        private void UpdateIsPlayingVideo()
        {
            bool isPlayingVideo = Server.IsCurrentlyPlayingVideo;

            // For now, just enable SingleSelectMode when we are playing video
            // TODO: Handle separately
            SingleSelectMode = isPlayingVideo;

            foreach (var speaker in SpeakerControls)
            {
                if (isPlayingVideo && !speaker.Key.HasVideo)
                    speaker.Value.Visibility = System.Windows.Visibility.Collapsed;
                else
                    speaker.Value.Visibility = System.Windows.Visibility.Visible;
            }

            UpdateMasterVolumeSlider();
        }

        private void UpdateMasterVolumeSlider()
        {
            bool enableMasterVolume = true;

            if (Server.IsCurrentlyPlayingVideo)
            {
                var mainSpeaker = Server.Speakers.FirstOrDefault(s => s.ID == 0);
                if (mainSpeaker != null)
                    enableMasterVolume = mainSpeaker.Active;
            }

            MasterVolumeSlider.IsEnabled = enableMasterVolume;
        }

        #endregion

        #region Events

        public event EventHandler SingleSpeakerClicked;

        #endregion

    }
}
