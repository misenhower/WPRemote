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
            }
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
