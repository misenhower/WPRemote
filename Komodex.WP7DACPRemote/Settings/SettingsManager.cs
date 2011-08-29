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
using System.Collections.Generic;
using Komodex.WP7DACPRemote.Localization;

namespace Komodex.WP7DACPRemote.Settings
{
    public class SettingsManager : INotifyPropertyChanged
    {
        protected SettingsManager()
        { }

        public static SettingsManager Current { get; protected set; }

        public static void Initialize()
        {
            Current = new SettingsManager();
        }

        #region Run Under Lock

        private Setting<bool> _runUnderLock = new Setting<bool>("SettingsRunUnderLock", true, UpdateRunUnderLock);
        public bool RunUnderLock
        {
            get { return _runUnderLock.Value; }
            set
            {
                if (_runUnderLock.Value == value)
                    return;

                _runUnderLock.Value = value;
                SendPropertyChanged("RunUnderLock");
                SendPropertyChanged("RunUnderLockTakesEffectNextRun");
            }
        }

        private static void UpdateRunUnderLock(bool value)
        {
            if (value)
            {
                try
                {
                    PhoneApplicationService.Current.ApplicationIdleDetectionMode = IdleDetectionMode.Disabled;
                }
                catch { }
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

        private ObservableCollection<ArtistClickActionStruct> _ArtistClickActionStructs = null;
        public ObservableCollection<ArtistClickActionStruct> ArtistClickActionStructs
        {
            get
            {
                if (_ArtistClickActionStructs == null)
                {
                    _ArtistClickActionStructs = new ObservableCollection<ArtistClickActionStruct>();
                    _ArtistClickActionStructs.Add(new ArtistClickActionStruct(ArtistClickActions.OpenArtistPage, LocalizedStrings.SettingsArtistTapOpenArtistPage));
                    _ArtistClickActionStructs.Add(new ArtistClickActionStruct(ArtistClickActions.OpenAlbumPage, LocalizedStrings.SettingsArtistTapOpenAlbumPage));
                }
                return _ArtistClickActionStructs;
            }
        }

        private Setting<ArtistClickActions> _artistClickAction = new Setting<ArtistClickActions>("SettingsArtistClickAction", 0);
        public ArtistClickActions ArtistClickAction
        {
            get { return _artistClickAction.Value; }
            set
            {
                if (_artistClickAction.Value == value)
                    return;

                _artistClickAction.Value = value;
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

        #region Extended Error Reporting

        private Setting<bool> _extendedErrorReporting = new Setting<bool>("SettingsExtendedErrorReporting", false);
        public bool ExtendedErrorReporting
        {
            get { return _extendedErrorReporting.Value; }
            set
            {
                if (_extendedErrorReporting.Value == value)
                    return;

                _extendedErrorReporting.Value = value;
                SendPropertyChanged("ExtendedErrorReporting");
            }
        }

        #endregion

        #region Setting class

        protected class Setting<T>
        {
            private static IsolatedStorageSettings _isolatedSettings = IsolatedStorageSettings.ApplicationSettings;

            public Setting(string keyName, T defaultValue = default(T), Action<T> changeAction = null)
            {
                _keyName = keyName;
                _changeAction = changeAction;

                // Try to load the value from isolated storage, or use the default value
                if (!_isolatedSettings.TryGetValue<T>(_keyName, out _value))
                    Value = defaultValue; // This will save the default value to isolated storage as well

                // If an action was specified, run it on the initial value
                if (_changeAction != null)
                    _changeAction(_value);
            }

            protected string _keyName;
            protected T _value;
            protected Action<T> _changeAction;

            public T Value
            {
                get { return _value; }
                set
                {
                    _value = value;

                    // Save the new value to isolated storage
                    _isolatedSettings[_keyName] = _value;
                    _isolatedSettings.Save();

                    // Run modified action
                    if (_changeAction != null)
                        _changeAction(_value);
                }
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
