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

namespace Komodex.WP7DACPRemote.DACPServerInfoManagement
{
    public class DACPServerInfo : INotifyPropertyChanged
    {
        private Guid _ID = Guid.Empty;
        public Guid ID
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

        private string _HostName = null;
        public string HostName{
            get { return _HostName; }
            set
            {
                if (_HostName == value)
                    return;
                _HostName = value;
                SendPropertyChanged("HostName");
            }
        }

        private string _LibraryName = null;
        public string LibraryName {
            get { return _LibraryName; }
            set
            {
                if (_LibraryName == value)
                    return;
                _LibraryName = value;
                SendPropertyChanged("LibraryName");
            }
        }

        private int? _PIN = null;
        public int? PIN
        {
            get { return _PIN; }
            set
            {
                if (_PIN == value)
                    return;

                if (value == null || value <= 0 || value > 9999)
                    _PIN = null;
                else
                    _PIN = value;

                SendPropertyChanged("PIN");
                SendPropertyChanged("FormattedPIN");
                SendPropertyChanged("PairingCode");
            }
        }

        public string FormattedPIN
        {
            get
            {
                if (PIN.HasValue)
                    return PIN.Value.ToString("0000");
                return string.Empty;
            }
            set
            {
                int parsed;
                if (int.TryParse(value, out parsed))
                    PIN = parsed;
                else
                    PIN = null;
            }
        }

        public string PairingCode
        {
            get { return string.Format("{0:0000}{0:0000}{0:0000}{0:0000}", PIN); }
        }

        #region Notify Property Changed

        protected void SendPropertyChanged(string propertyName)
        {
            // TODO: Is this the best way to execute this on the UI thread?
            if (PropertyChanged != null)
                Deployment.Current.Dispatcher.BeginInvoke(() => { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); });

        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #endregion
    }
}
