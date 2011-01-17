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
                SendPropertyChanged("Name");
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
                SendPropertyChanged("ID");
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
                SendPropertyChanged("HasPassword");
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
                SendPropertyChanged("HasVideo");
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
                SendPropertyChanged("Active");
                SendPropertyChanged("BindableActive");
            }
        }

        private bool _WaitingForResponse = false;
        internal bool WaitingForResponse
        {
            get { return _WaitingForResponse; }
            set
            {
                if (_WaitingForResponse == value)
                    return;
                _WaitingForResponse = value;
                // Shouldn't need to send property changed for WaitingForResponse.
                SendPropertyChanged("BindableActive");
            }
        }

        public bool? BindableActive
        {
            get
            {
                if (_WaitingForResponse)
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
                WaitingForResponse = true;
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
                SendPropertyChanged("Volume");
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
                    SendSimpleVolumeUpdate();

                }
                else
                {
                    Server.AirPlayMasterVolumeManipulation(value);
                    SendExtendedVolumeUpdate();
                }

            }
        }

        #endregion

        #region Methods

        protected void SendSimpleVolumeUpdate()
        {
            string url = "/ctrl-int/1/setproperty"
                + "?speaker-id=" + ID
                + "&dmcp.volume=" + Volume
                + "&session-id=" + Server.SessionID;
            Server.SubmitHTTPRequest(url);
        }

        protected void SendExtendedVolumeUpdate()
        {
            string url = "/ctrl-int/1/setproperty"
                + "?dmcp.volume=" + Server.Volume
                + "&include-speaker-id=" + ID
                + "&session-id=" + Server.SessionID;
            Server.SubmitHTTPRequest(url);
        }

        internal void UpdateBindableVolume()
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

            SendPropertyChanged("BindableVolume");
        }

        #endregion

        #region Notify Property Changed

        protected void SendPropertyChanged(string propertyName)
        {
            // TODO: Is this the best way to execute this on the UI thread?
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #endregion
    }
}
