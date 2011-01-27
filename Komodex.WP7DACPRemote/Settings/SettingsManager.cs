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
using System.Collections.ObjectModel;
using System.Linq;

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
            ArtistClickAction = Enum<ArtistClickActions>.ParseOrDefault(GetValue<string>(kArtistClickActionKey), ArtistClickActions.OpenArtistPage);
        }

        #region Methods

        protected T GetValue<T>(string keyName, T defaultValue = default(T))
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
                SendPropertyChanged("RunUnderLockTakesEffectNextRun");
            }
        }

        public bool RunUnderLockTakesEffectNextRun
        {
            get
            {
                return (PhoneApplicationService.Current.ApplicationIdleDetectionMode == IdleDetectionMode.Disabled && !RunUnderLock);
            }
        }

        #endregion

        #region Artist Click Action

        private static readonly string kArtistClickActionKey = "SettingsArtistClickAction";

        private ObservableCollection<ArtistClickActionStruct> _ArtistClickActionStructs = null;
        public ObservableCollection<ArtistClickActionStruct> ArtistClickActionStructs
        {
            get
            {
                if (_ArtistClickActionStructs == null)
                {
                    _ArtistClickActionStructs = new ObservableCollection<ArtistClickActionStruct>();
                    _ArtistClickActionStructs.Add(new ArtistClickActionStruct(ArtistClickActions.OpenArtistPage, "open the artist page"));
                    _ArtistClickActionStructs.Add(new ArtistClickActionStruct(ArtistClickActions.OpenAlbumPage, "open the album page"));
                }
                return _ArtistClickActionStructs;
            }
        }

        private ArtistClickActions _ArtistClickAction = 0;
        public ArtistClickActions ArtistClickAction
        {
            get { return _ArtistClickAction; }
            set
            {
                if (_ArtistClickAction == value)
                    return;
                _ArtistClickAction = value;
                SetValue(kArtistClickActionKey, _ArtistClickAction.ToString());
                SendPropertyChanged("ArtistClickAction");
                SendPropertyChanged("BindableClickAction");
            }
        }

        public ArtistClickActionStruct BindableArtistClickAction
        {
            get { return ArtistClickActionStructs.First(a => a.ArtistClickAction == ArtistClickAction); }
            set { ArtistClickAction = value.ArtistClickAction; }
        }

        #region Enum and Struct

        public enum ArtistClickActions
        {
            OpenArtistPage,
            OpenAlbumPage,
        }

        public struct ArtistClickActionStruct
        {
            public ArtistClickActionStruct(ArtistClickActions clickAction, string name)
                : this()
            {
                ArtistClickAction = clickAction;
                Name = name;
            }

            public ArtistClickActions ArtistClickAction { get; private set; }
            public string Name { get; private set; }

            public override string ToString()
            {
                return Name;
            }
        }

        #endregion

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
