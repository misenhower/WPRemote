﻿using System;
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
            Server = server;
            ReloadSpeakerList();
            UpdateIsPlayingVideo();
            Server.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Server_PropertyChanged);
            Server.Speakers.CollectionChanged += new NotifyCollectionChangedEventHandler(Speakers_CollectionChanged);
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

        #region List Management

        protected void ReloadSpeakerList()
        {
            AirPlaySpeakerStackPanel.Children.Clear();
            SpeakerControls.Clear();

            foreach (AirPlaySpeaker speaker in Server.Speakers)
                AddSpeaker(speaker);
        }

        private void AddSpeaker(AirPlaySpeaker speaker)
        {
            AirPlaySpeakerControl speakerControl = new AirPlaySpeakerControl(speaker, SingleSelectMode);
            if (Server.IsCurrentlyPlayingVideo && !speaker.HasVideo)
                speakerControl.Visibility = System.Windows.Visibility.Collapsed;
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
        }

        #endregion

    }
}
