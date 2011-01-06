using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;

namespace Komodex.DACP
{
    public partial class DACPServer
    {
        internal int AirPlayVolumeSwitchPoint = 0;

        #region Properties

        private ObservableCollection<AirPlaySpeaker> _Speakers = null;
        public ObservableCollection<AirPlaySpeaker> Speakers
        {
            get { return _Speakers; }
            protected set
            {
                if (_Speakers == value)
                    return;
                _Speakers = value;
                SendPropertyChanged("Speakers");
            }
        }

        #endregion

        #region HTTP Requests and Responses

        #region Get Speakers

        protected void SubmitGetSpeakersRequest()
        {
            string url = "/ctrl-int/1/getspeakers?session-id=" + SessionID;
            SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessGetSpeakersResponse));
        }

        protected void ProcessGetSpeakersResponse(HTTPRequestInfo requestInfo)
        {
            if (Speakers == null)
                Speakers = new ObservableCollection<AirPlaySpeaker>();

            List<UInt64> foundSpeakerIDs = new List<UInt64>();

            string name;
            UInt64 id;
            bool primary;
            bool active;
            int volume;

            foreach (var kvp in requestInfo.ResponseNodes)
            {
                if (kvp.Key == "mdcl")
                {
                    name = string.Empty;
                    id = 0;
                    primary = false;
                    active = false;
                    volume = 0;

                    var speakerNodes = Utility.GetResponseNodes(kvp.Value);

                    foreach (var speakerKvp in speakerNodes)
                    {
                        switch (speakerKvp.Key)
                        {
                            case "minm": // Speaker name
                                name = speakerKvp.Value.GetStringValue();
                                break;
                            case "msma": // Speaker ID
                                id = (UInt64)speakerKvp.Value.GetInt64Value();
                                break;
                            case "cavd": // Primary (non-remote) speaker
                                primary = (speakerKvp.Value[0] > 0);
                                break;
                            case "caia": // Speaker active
                                active = (speakerKvp.Value[0] > 0);
                                break;
                            case "cmvo": // Speaker volume
                                volume = speakerKvp.Value.GetInt32Value();
                                break;
                            default:
                                break;
                        }
                    }

                    if (foundSpeakerIDs.Contains(id))
                        continue;

                    foundSpeakerIDs.Add(id);

                    AirPlaySpeaker speaker = Speakers.FirstOrDefault(s => s.ID == id);
                    if (speaker == null)
                    {
                        speaker = new AirPlaySpeaker();
                        speaker.Server = this;
                        speaker.ID = id;
                        Speakers.Add(speaker);
                    }

                    speaker.Name = name;
                    speaker.Active = active;
                    speaker.Volume = volume;
                }
            }

            // Handle speakers that are no longer available
            var removedSpeakers = Speakers.Where(s => !foundSpeakerIDs.Contains(s.ID));
            foreach (AirPlaySpeaker removedSpeaker in removedSpeakers)
                Speakers.Remove(removedSpeaker);
        }

        #endregion

        #endregion

        #region Methods

        public void AirPlaySpeakerManipulationStarted(AirPlaySpeaker speaker)
        {
            AirPlaySpeaker otherSpeaker = (from s in Speakers
                                           where s != speaker
                                           orderby s.BindableVolume descending
                                           select s).FirstOrDefault();

            if (otherSpeaker != null)
                AirPlayVolumeSwitchPoint = otherSpeaker.BindableVolume;
            else
                AirPlayVolumeSwitchPoint = 0;
        }

        public void AirPlaySpeakerManipulationStopped()
        {
            SubmitGetSpeakersRequest();
        }

        internal void AirPlayMasterVolumeManipulation(int newVolume)
        {
            _Volume = newVolume;
            SendPropertyChanged("Volume");
        }

        #endregion

    }
}
