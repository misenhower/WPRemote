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

namespace Komodex.DACP
{
    public class AirPlaySpeaker : INotifyPropertyChanged
    {
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
            set
            {
                if (_ID == value)
                    return;
                _ID = value;
                SendPropertyChanged("ID");
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

        private bool _WaitingForResponse =false;
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
                SendAdjustedVolumePropertyChanged();
            }
        }

        public int BindableVolume
        {
            get
            {
                if (Server.Volume == 100)
                    return Volume;

                double volumePercentage = (double)Volume / 100;
                return (int)((double)Server.Volume * volumePercentage);
            }
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

        internal void SendAdjustedVolumePropertyChanged()
        {
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
