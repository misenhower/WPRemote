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
using Komodex.Common;

namespace Komodex.DACP
{
    public partial class DACPServer
    {
        internal int AirPlayVolumeSwitchPoint = 0;
        private bool needToGetSpeakers;

        #region Properties

        private readonly ObservableCollection<AirPlaySpeaker> _speakers = new ObservableCollection<AirPlaySpeaker>();
        public ObservableCollection<AirPlaySpeaker> Speakers
        {
            get { return _speakers; }
        }

        #endregion

        #region HTTP Requests and Responses

        #region Get Speakers

        private bool gettingSpeakers = false;

        public void GetSpeakers()
        {
            if (!gettingSpeakers)
                SubmitGetSpeakersRequest();
        }

        protected void SubmitGetSpeakersRequest()
        {
            gettingSpeakers = true;
            string url = "/ctrl-int/1/getspeakers?session-id=" + SessionID;
            SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessGetSpeakersResponse));
        }

        protected void ProcessGetSpeakersResponse(HTTPRequestInfo requestInfo)
        {
            List<UInt64> foundSpeakerIDs = new List<UInt64>();

            string name;
            UInt64 id;
            bool hasPassword;
            bool hasVideo;
            bool active;
            int volume;

            foreach (var kvp in requestInfo.ResponseNodes)
            {
                if (kvp.Key == "mdcl")
                {
                    name = string.Empty;
                    id = 0;
                    hasPassword = false;
                    hasVideo = false;
                    active = false;
                    volume = 0;

                    var speakerNodes = DACPUtility.GetResponseNodes(kvp.Value);

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
                            case "caiv": // Has Video
                                hasVideo = true;
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

                    AirPlaySpeaker speaker;

                    lock (Speakers)
                    {
                        speaker = Speakers.FirstOrDefault(s => s.ID == id);
                        if (speaker == null)
                        {
                            speaker = new AirPlaySpeaker(this, id);
                            speaker.HasVideo = (hasVideo || id == 0);
                            ThreadUtility.RunOnUIThread(() => Speakers.Add(speaker));
                        }
                    }

                    speaker.HasPassword = hasPassword;
                    speaker.Name = name;
                    speaker.Active = active;
                    speaker.Volume = volume;
                    speaker.WaitingForActiveResponse = false;
                }
            }

            lock (Speakers)
            {
                // Handle speakers that are no longer available
                // Need to call ToList() so the source collection doesn't change during enumeration
                var removedSpeakers = Speakers.Where(s => !foundSpeakerIDs.Contains(s.ID)).ToList();
                foreach (AirPlaySpeaker removedSpeaker in removedSpeakers)
                    ThreadUtility.RunOnUIThread(() => Speakers.Remove(removedSpeaker));
            }

            SendAirPlaySpeakerUpdate();
            gettingSpeakers = false;
        }

        #endregion

        #region Set Active Speakers

        internal void SetSingleActiveSpeaker(AirPlaySpeaker speaker)
        {
            string url = "/ctrl-int/1/setspeakers"
                + "?speaker-id=0x" + speaker.ID.ToString("x")
                + "&session-id=" + SessionID;
            SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessSetActiveSpeakersResponse), true, r => r.ExceptionHandlerDelegate = new HTTPExceptionHandler(HandleSetActiveSpeakersException));
        }

        internal void SubmitSetActiveSpeakersRequest()
        {
            string speakers;
            lock (Speakers)
                speakers = string.Join(",", Speakers.Where(s => s.Active).Select(s => "0x" + s.ID.ToString("x")).ToArray());

            string url = "/ctrl-int/1/setspeakers"
                + "?speaker-id=" + speakers
                + "&session-id=" + SessionID;
            SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessSetActiveSpeakersResponse), true, r => r.ExceptionHandlerDelegate = new HTTPExceptionHandler(HandleSetActiveSpeakersException));
        }

        protected void HandleSetActiveSpeakersException(HTTPRequestInfo requestInfo, WebException e)
        {
            // TODO
            //SendServerUpdate(ServerUpdateType.AirPlaySpeakerPassword);
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
            needToGetSpeakers = false;
            AirPlaySpeaker otherSpeaker;
            lock (Speakers)
            {
                otherSpeaker = (from s in Speakers
                                where s != speaker && s.Active
                                orderby s.BindableVolume descending
                                select s).FirstOrDefault();
            }

            if (otherSpeaker != null)
                AirPlayVolumeSwitchPoint = otherSpeaker.BindableVolume;
            else
                AirPlayVolumeSwitchPoint = 0;
        }

        public void AirPlaySpeakerManipulationStopped(AirPlaySpeaker speaker)
        {
            if (!speaker.WaitingForVolumeResponse)
                SubmitGetSpeakersRequest();
            else
                needToGetSpeakers = true;
        }

        internal void GetSpeakersIfNeeded()
        {
            if (needToGetSpeakers)
                SubmitGetSpeakersRequest();
            needToGetSpeakers = false;
        }

        internal void AirPlayMasterVolumeManipulation(int newVolume)
        {
            _currentVolume = newVolume;
            PropertyChanged.RaiseOnUIThread(this, "CurrentVolume", "BindableVolume");
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
