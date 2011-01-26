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
using System.IO.IsolatedStorage;
using Microsoft.Phone.Shell;

namespace Komodex.WP7DACPRemote.Settings
{
    public class SettingsManager : INotifyPropertyChanged
    {
        private SettingsManager()
        { }

        private static IsolatedStorageSettings IsolatedSettings = IsolatedStorageSettings.ApplicationSettings;

        private static SettingsManager _Current = new SettingsManager();
        public static SettingsManager Current
        {
            get { return _Current; }
        }

        private bool _Initialized = false;
        public void Initialize()
        {
            if (_Initialized)
                return;
            _Initialized = true;

            RunUnderLock = GetValue<bool>(kRunUnderLockKey, true);
        }

        #region Methods

        protected T GetValue<T>(string keyName, T defaultValue)
        {
            T returnValue;
            if (IsolatedSettings.TryGetValue<T>(keyName, out returnValue))
                return returnValue;
            return defaultValue;
        }

        protected void SetValue(string keyName, object value)
        {
            IsolatedSettings[keyName] = value;
            IsolatedSettings.Save();
        }

        #endregion

        #region Run Under Lock

        private static readonly string kRunUnderLockKey = "SettingsRunUnderLock";

        private bool _RunUnderLock = false;
        public bool RunUnderLock
        {
            get { return _RunUnderLock; }
            set
            {
                if (value)
                {
                    try
                    {
                        PhoneApplicationService.Current.ApplicationIdleDetectionMode = IdleDetectionMode.Disabled;
                    }
                    catch { }
                }

                _RunUnderLock = value;
                SetValue(kRunUnderLockKey, _RunUnderLock);
                SendPropertyChanged("RunUnderLock");
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
