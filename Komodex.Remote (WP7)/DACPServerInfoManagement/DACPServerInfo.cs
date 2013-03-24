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
using Komodex.WP7DACPRemote.Localization;
using Komodex.Common;

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
                PropertyChanged.RaiseOnUIThread(this, "ID");
            }
        }

        private string _serviceID;
        public string ServiceID
        {
            get { return _serviceID; }
            set
            {
                if (_serviceID == value)
                    return;
                _serviceID = value;
                PropertyChanged.RaiseOnUIThread(this, "ServiceID");
            }
        }

        private string _HostName = null;
        public string HostName
        {
            get { return _HostName; }
            set
            {
                if (_HostName == value)
                    return;
                _HostName = value;
                if (_HostName != null)
                    _HostName = _HostName.Trim();
                PropertyChanged.RaiseOnUIThread(this, "HostName");
            }
        }

        private string _LibraryName = null;
        public string LibraryName
        {
            get { return _LibraryName; }
            set
            {
                if (_LibraryName == value)
                    return;
                _LibraryName = value;
                PropertyChanged.RaiseOnUIThread(this, "LibraryName");
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

                PropertyChanged.RaiseOnUIThread(this, "PIN", "FormattedPIN", "PairingCode");
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

        public string SecondLine
        {
            get { return string.Format("{0}: {1}, {2}: {3:0000}", LocalizedStrings.LibraryHostname, HostName, LocalizedStrings.LibraryPIN, PIN); }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
