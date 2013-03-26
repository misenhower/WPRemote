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
using Komodex.Remote.Localization;
using Komodex.Common;
using Komodex.Common.Phone;

namespace Komodex.Remote.Settings
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

        private readonly Setting<bool> _runUnderLock = new Setting<bool>("SettingsRunUnderLock", true, UpdateRunUnderLock);
        public bool RunUnderLock
        {
            get { return _runUnderLock.Value; }
            set
            {
                if (_runUnderLock.Value == value)
                    return;

                _runUnderLock.Value = value;
                PropertyChanged.RaiseOnUIThread(this, "RunUnderLock", "RunUnderLockTakesEffectNextRun");
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

        private readonly List<KeyValuePair<ArtistClickAction, string>> _artistClickActions = new List<KeyValuePair<ArtistClickAction, string>>()
        {
            new KeyValuePair<ArtistClickAction, string>(ArtistClickAction.OpenArtistPage, LocalizedStrings.SettingsArtistTapOpenArtistPage),
            new KeyValuePair<ArtistClickAction, string>(ArtistClickAction.OpenAlbumPage, LocalizedStrings.SettingsArtistTapOpenAlbumPage),
        };

        public List<KeyValuePair<ArtistClickAction, string>> ArtistClickActions
        {
            get { return _artistClickActions; }
        }

        private readonly Setting<ArtistClickAction> _artistClickAction = new Setting<ArtistClickAction>("SettingsArtistClickAction");
        public ArtistClickAction ArtistClickAction
        {
            get { return _artistClickAction.Value; }
            set
            {
                if (_artistClickAction.Value == value)
                    return;

                _artistClickAction.Value = value;
                PropertyChanged.RaiseOnUIThread(this, "ArtistClickAction", "BindableArtistClickAction");
            }
        }

        public KeyValuePair<ArtistClickAction, string> BindableArtistClickAction
        {
            get { return _artistClickActions.FirstOrDefault(a => a.Key == ArtistClickAction); }
            set { ArtistClickAction = value.Key; }
        }

        #endregion

        #region Extended Error Reporting

        private readonly Setting<bool> _extendedErrorReporting = new Setting<bool>("SettingsExtendedErrorReporting", false);
        public bool ExtendedErrorReporting
        {
            get { return _extendedErrorReporting.Value; }
            set
            {
                if (_extendedErrorReporting.Value == value)
                    return;

                _extendedErrorReporting.Value = value;
                PropertyChanged.RaiseOnUIThread(this, "ExtendedErrorReporting");
            }
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

    }
}
