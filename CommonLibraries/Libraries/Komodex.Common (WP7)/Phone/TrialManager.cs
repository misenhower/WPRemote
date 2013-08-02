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
using Microsoft.Phone.Marketplace;
using Komodex.Analytics;

namespace Komodex.Common.Phone
{
    public class TrialManager
    {
        private static readonly Log _log = new Log("TrialManager");
        private static bool _initialized;

        #region Trial Parameters

#if DEBUG
        private static bool _simulateTrialMode;
        public static bool SimulateTrialMode
        {
            get { return _simulateTrialMode; }
            set
            {
                ThrowIfInitialized();
                _simulateTrialMode = value;
            }
        }

        private static bool _simulateTrialExpired;
        public static bool SimulateTrialExpired
        {
            get { return _simulateTrialExpired; }
            set
            {
                ThrowIfInitialized();
                _simulateTrialExpired = value;
                if (_simulateTrialExpired)
                    _simulateTrialMode = true;
            }
        }
#endif

        private static int _trialDays;
        /// <summary>
        /// Gets or sets the number of days for which the user is permitted to use a trial version of the application.
        /// </summary>
        public static int TrialDays
        {
            get { return _trialDays; }
            set
            {
                ThrowIfInitialized();
                _trialDays = value;
            }
        }

        private static bool _resetTrialExpirationDateOnNewVersion = true;
        /// <summary>
        /// Determines whether the trial expiration date should be reset when the user upgrades to a new version.
        /// </summary>
        public static bool ResetTrialExpirationDateOnNewVersion
        {
            get { return _resetTrialExpirationDateOnNewVersion; }
            set
            {
                ThrowIfInitialized();
                _resetTrialExpirationDateOnNewVersion = value;
            }
        }

        private static bool _autoStartTrial = true;
        /// <summary>
        /// Determines whether the trial period should be started automatically. If false, call StartTrial to begin the trial period.
        /// </summary>
        public static bool AutoStartTrial
        {
            get { return _autoStartTrial; }
            set
            {
                ThrowIfInitialized();
                _autoStartTrial = value;
            }
        }

        #endregion

        #region Properties

        private static bool _isTrial;
        public static bool IsTrial
        {
            get
            {
                ThrowIfNotInitialized();
                return _isTrial;
            }
            protected set { _isTrial = value; }
        }

        private static TrialState _trialState;
        public static TrialState TrialState
        {
            get
            {
                ThrowIfNotInitialized();
                return _trialState;
            }
            protected set { _trialState = value; }
        }

        private static readonly Setting<DateTime?> _trialExpirationDate = new Setting<DateTime?>("TrialManagerTrialExpirationDate");
        private static readonly Setting<string> _trialExpirationVersion = new Setting<string>("TrialManagerTrialExpirationVersion");
        public static DateTime? TrialExpirationDate
        {
            get
            {
                ThrowIfNotInitialized();
                return _trialExpirationDate.Value;
            }
            protected set { _trialExpirationDate.Value = value; }
        }

        private static int? _trialDaysRemaining;
        public static int? TrialDaysRemaining
        {
            get
            {
                ThrowIfNotInitialized();
                return _trialDaysRemaining;
            }
            protected set { _trialDaysRemaining = value; }
        }

        #endregion

        #region Trial Methods

        public static void Initialize()
        {
            if (_initialized)
                return;
            _initialized = true;

            // Determine whether we are running in trial mode
            LicenseInformation license = new LicenseInformation();
            IsTrial = license.IsTrial();

#if DEBUG
            if (SimulateTrialMode)
                IsTrial = true;
#endif

            // Start the trial if we need to
            if (IsTrial && TrialExpirationDate == null && AutoStartTrial)
                StartTrial();

            // Reset the trial expiration date upon upgrading to a new version
            if (ResetTrialExpirationDateOnNewVersion)
            {
                if (TrialExpirationDate != null && _trialExpirationVersion.Value != Utility.ApplicationVersion)
                    ResetTrialExpirationDate();
            }

            // Update trial state and days remaining
            UpdateTrialState();

            _log.Info("Current state: {0}", TrialState);
            if (IsTrial)
            {
                if (TrialExpirationDate != null)
                    _log.Info("Trial expiration date: {0:d} ({1} day(s) remaining)", TrialExpirationDate, TrialDaysRemaining);
                else
                    _log.Info("Trial expiration date is null.");
            }
        }

        /// <summary>
        /// Begins the trial period if an expiration date is not already set.
        /// </summary>
        public static void StartTrial()
        {
            if (TrialExpirationDate != null)
                return;

            ResetTrialExpirationDate();
        }

        /// <summary>
        /// Sets the trial expiration date based on the specified number of trial days.
        /// </summary>
        protected static void ResetTrialExpirationDate()
        {
            _log.Info("Resetting trial expiration date...");

            if (TrialDays <= 0)
                TrialExpirationDate = null;
            else
                TrialExpirationDate = DateTime.Now.AddDays(TrialDays).Date;

            _trialExpirationVersion.Value = Utility.ApplicationVersion;

            UpdateTrialState();
        }

        protected static void UpdateTrialState()
        {
            // Full version
            if (!IsTrial)
            {
                TrialState = TrialState.FullVersion;
                TrialDaysRemaining = null;
                return;
            }

            // Trial without an expiration date
            if (TrialExpirationDate == null)
            {
                TrialState = TrialState.Trial;
                TrialDaysRemaining = null;
                return;
            }

            // Determine whether the trial period has expired
            var days = (TrialExpirationDate.Value - DateTime.Now).TotalDays;
#if DEBUG
            if (SimulateTrialExpired)
                days = 0;
#endif
            if (days <= 0)
            {
                TrialState = TrialState.Expired;
                TrialDaysRemaining = 0;
            }
            else
            {
                TrialState = TrialState.Trial;
                TrialDaysRemaining = (int)Math.Ceiling(days);
            }
        }

        #endregion

        private static void ThrowIfInitialized()
        {
            if (_initialized)
                throw new InvalidOperationException("Operation must be completed before TrialManager is initialized.");
        }

        private static void ThrowIfNotInitialized()
        {
            if (!_initialized)
                throw new InvalidOperationException("Operation must be completed after TrialManager is initialized.");
        }
    }

    public enum TrialState
    {
        FullVersion,
        Trial,
        Expired,
    }
}
