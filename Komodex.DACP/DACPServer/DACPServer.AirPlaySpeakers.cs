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
            bool hasPassword;
            bool primary;
            bool active;
            int volume;

            foreach (var kvp in requestInfo.ResponseNodes)
            {
                if (kvp.Key == "mdcl")
                {
                    name = string.Empty;
                    id = 0;
                    hasPassword = false;
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
                            case "cahp": // Has Password
                                hasPassword = (speakerKvp.Value[0] > 0);
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
                        speaker = new AirPlaySpeaker(this, id);
                        Speakers.Add(speaker);
                    }

                    speaker.HasPassword = hasPassword;
                    speaker.Name = name;
                    speaker.Active = active;
                    speaker.Volume = volume;
                    speaker.WaitingForResponse = false;
                }
            }

            // Handle speakers that are no longer available
            // Need to call ToList() so the source collection doesn't change during enumeration
            var removedSpeakers = Speakers.Where(s => !foundSpeakerIDs.Contains(s.ID)).ToList();
            foreach (AirPlaySpeaker removedSpeaker in removedSpeakers)
                Speakers.Remove(removedSpeaker);

            SendAirPlaySpeakerUpdate();
        }

        #endregion

        #region Set Active Speakers

        internal void SubmitSetActiveSpeakersRequest()
        {
            string speakers = string.Join(",", Speakers.Where(s => s.Active).Select(s => "0x" + s.ID.ToString("x")).ToArray());
            string url = "/ctrl-int/1/setspeakers"
                + "?speaker-id=" + speakers
                + "&session-id=" + SessionID;
            SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessSetActiveSpeakersResponse), true, r => r.ExceptionHandlerDelegate = new HTTPExceptionHandler(HandleSetActiveSpeakersException));
        }

        protected void HandleSetActiveSpeakersException(HTTPRequestInfo requestInfo, WebException e)
        {
            SendServerUpdate(ServerUpdateType.AirPlaySpeakerPassword);
            SubmitGetSpeakersRequest();
        }

        protected void ProcessSetActiveSpeakersResponse(HTTPRequestInfo requestInfo)
        {
            SubmitGetSpeakersRequest();
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

        #region Events

        public event EventHandler AirPlaySpeakerUpdate;

        protected void SendAirPlaySpeakerUpdate()
        {
            if (AirPlaySpeakerUpdate != null)
                AirPlaySpeakerUpdate(this, new EventArgs());
        }

        #endregion

    }
}
