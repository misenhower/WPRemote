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
using System.ComponentModel;
using System.Linq;
using Komodex.Common;

namespace Komodex.DACP
{
    public class AirPlaySpeaker : INotifyPropertyChanged
    {
        public AirPlaySpeaker(DACPServer server, UInt64 id)
        {
            Server = server;
            ID = id;
        }

        #region Properties

        public DACPServer Server { get; internal set; }

        private string _Name = string.Empty;
        public string Name
        {
            get { return _Name; }
            set
            {
                if (_Name == value)
                    return;
                _Name = value;
                PropertyChanged.RaiseOnUIThread(this, "Name");
            }
        }

        private UInt64 _ID = 0;
        public UInt64 ID
        {
            get { return _ID; }
            protected set
            {
                if (_ID == value)
                    return;
                _ID = value;
                PropertyChanged.RaiseOnUIThread(this, "ID");
            }
        }

        private bool _HasPassword = false;
        public bool HasPassword
        {
            get { return _HasPassword; }
            set
            {
                if (_HasPassword == value)
                    return;
                _HasPassword = value;
                PropertyChanged.RaiseOnUIThread(this, "HasPassword");
            }
        }

        private bool _HasVideo = false;
        public bool HasVideo
        {
            get { return _HasVideo; }
            set
            {
                if (_HasVideo == value)
                    return;
                _HasVideo = value;
                PropertyChanged.RaiseOnUIThread(this, "HasVideo");
            }
        }


        private bool _Active = false;
        public bool Active
        {
            get { return _Active; }
            set
            {
                if (_Active == value)
                    return;
                _Active = value;
                PropertyChanged.RaiseOnUIThread(this, "Active", "BindableActive");
            }
        }

        private bool _WaitingForActiveResponse = false;
        internal bool WaitingForActiveResponse
        {
            get { return _WaitingForActiveResponse; }
            set
            {
                if (_WaitingForActiveResponse == value)
                    return;
                _WaitingForActiveResponse = value;
                // Shouldn't need to send property changed for WaitingForActiveResponse.
                PropertyChanged.RaiseOnUIThread(this, "BindableActive");
            }
        }

        public bool? BindableActive
        {
            get
            {
                if (_WaitingForActiveResponse)
                    return null;
                return _Active;
            }
            set
            {
                if (_Active == value)
                    return;
                if (!value.HasValue)
                    return;
                Active = value.Value;
                WaitingForActiveResponse = true;
                Server.SubmitSetActiveSpeakersRequest();
            }
        }

        private int _Volume = 0;
        public int Volume
        {
            get { return _Volume; }
            set
            {
                if (_Volume == value)
                    return;
                _Volume = value;
                PropertyChanged.RaiseOnUIThread(this, "Volume");
                UpdateBindableVolume();
            }
        }

        protected int _BindableVolume = 0;
        public int BindableVolume
        {
            get { return _BindableVolume; }
            set
            {
                if (BindableVolume == value)
                    return;

                if (value <= Server.AirPlayVolumeSwitchPoint)
                {
                    double volumePercentage = (double)value / (double)Server.Volume;
                    Volume = (int)(volumePercentage * 100);

                    if (ignoringVolumeChanges)
                    {
                        sendVolumeChangeWhenFinished = Volume;
                        sendExtendedVolumeChangeWhenFinished = false;
                    }
                    else
                        SendSimpleVolumeUpdate(Volume);

                }
                else
                {
                    Server.AirPlayMasterVolumeManipulation(value);

                    if (ignoringVolumeChanges)
                    {
                        sendVolumeChangeWhenFinished = Server.Volume;
                        sendExtendedVolumeChangeWhenFinished = true;
                    }
                    else
                        SendExtendedVolumeUpdate(Server.Volume);
                }

            }
        }

        internal bool WaitingForVolumeResponse
        {
            get { return ignoringVolumeChanges; }
        }

        #endregion

        #region Methods

        private bool ignoringVolumeChanges = false;
        private int sendVolumeChangeWhenFinished = -1;
        private bool sendExtendedVolumeChangeWhenFinished = false;

        protected void SendSimpleVolumeUpdate(int newVolume)
        {
            ignoringVolumeChanges = true;
            string url = "/ctrl-int/1/setproperty"
                + "?speaker-id=" + ID
                + "&dmcp.volume=" + newVolume
                + "&session-id=" + Server.SessionID;
            Server.SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessVolumeUpdateResponse));
        }

        protected void SendExtendedVolumeUpdate(int newVolume)
        {
            ignoringVolumeChanges = true;
            string url = "/ctrl-int/1/setproperty"
                + "?dmcp.volume=" + newVolume
                + "&include-speaker-id=" + ID
                + "&session-id=" + Server.SessionID;
            Server.SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessVolumeUpdateResponse));
        }

        protected void ProcessVolumeUpdateResponse(HTTPRequestInfo requestInfo)
        {
            if (sendVolumeChangeWhenFinished >= 0)
            {
                int newVolume = sendVolumeChangeWhenFinished;
                sendVolumeChangeWhenFinished = -1;

                if (sendExtendedVolumeChangeWhenFinished)
                    SendExtendedVolumeUpdate(newVolume);
                else
                    SendSimpleVolumeUpdate(newVolume);
            }
            else
            {
                ignoringVolumeChanges = false;
                Server.GetSpeakersIfNeeded();
            }
        }

        internal void UpdateBindableVolume()
        {
            lock (Server.Speakers)
            {
                // If there are no other AirPlay speakers enabled, just return the server volume
                if (!Server.Speakers.Any(s => s != this && s.Active))
                    _BindableVolume = Server.Volume;

                else if (Server.Volume == 100)
                    _BindableVolume = Volume;

                else
                {
                    double volumePercentage = (double)Volume / 100;
                    _BindableVolume = (int)((double)Server.Volume * volumePercentage);
                }
            }

            PropertyChanged.RaiseOnUIThread(this, "BindableVolume");
        }

        public void SetSingleActiveSpeaker()
        {
            Server.SetSingleActiveSpeaker(this);
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
