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
using Komodex.Common;
using Komodex.Common.Phone;

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

        private readonly Setting<ArtistClickActions> _artistClickAction = new Setting<ArtistClickActions>("SettingsArtistClickAction", 0);
        public ArtistClickActions ArtistClickAction
        {
            get { return _artistClickAction.Value; }
            set
            {
                if (_artistClickAction.Value == value)
                    return;

                _artistClickAction.Value = value;
                PropertyChanged.RaiseOnUIThread(this, "ArtistClickAction", "BindableClickAction");
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
