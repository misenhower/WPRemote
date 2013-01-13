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
using Komodex.Bonjour;
using System.ComponentModel;
using Komodex.Common;
using System.Linq;

namespace Komodex.CommonLibrariesTestApp.Bonjour
{
    public class NetServiceViewModel:INotifyPropertyChanged
    {
        public NetServiceViewModel(NetService service)
        {
            Service = service;
            Refresh();
            Resolved = "Resolved: No";
            Service.ServiceResolved += new EventHandler<NetServiceEventArgs>(Service_ServiceResolved);
        }

        void Service_ServiceResolved(object sender, NetServiceEventArgs e)
        {
            Resolved = "Resolved: Yes";
            Refresh();
        }

        #region Properties

        public NetService Service { get; protected set; }


        private string _name;
        public string Name
        {
            get { return _name; }
            protected set
            {
                if (_name == value)
                    return;
                _name = value;
                PropertyChanged.RaiseOnUIThread(this, "Name");
            }
        }

        private string _address;
        public string Address
        {
            get { return _address; }
            protected set
            {
                if (_address == value)
                    return;
                _address = value;
                PropertyChanged.RaiseOnUIThread(this, "Address");
            }
        }

        private string _ipAddresses;
        public string IPAddresses
        {
            get { return _ipAddresses; }
            protected set
            {
                if (_ipAddresses == value)
                    return;
                _ipAddresses = value;
                PropertyChanged.RaiseOnUIThread(this, "IPAddresses");
            }
        }

        private string _txtData;
        public string TXTData
        {
            get { return _txtData; }
            protected set
            {
                if (_txtData == value)
                    return;
                _txtData = value;
                PropertyChanged.RaiseOnUIThread(this, "TXTData");
            }
        }

        private string _resolved;
        public string Resolved
        {
            get { return _resolved; }
            protected set
            {
                if (_resolved == value)
                    return;
                _resolved = value;
                PropertyChanged.RaiseOnUIThread(this, "Resolved");
            }
        }

        #endregion

        #region Methods

        public void Refresh()
        {
            Name = Service.FullServiceInstanceName;
            Address = string.Format("{0}:{1}", Service.Hostname, Service.Port);
            if (Service.IPAddresses != null && Service.IPAddresses.Count > 0)
                IPAddresses = "IPs: " + string.Join(", ", Service.IPAddresses);
            else
                IPAddresses = "No IPs";

            string txt = string.Empty;
            if (Service.TXTRecordData != null)
                txt = string.Join("\n", Service.TXTRecordData.Select(kvp => kvp.Key + "=" + kvp.Value));
            TXTData = txt;
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
