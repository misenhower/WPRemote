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

namespace Komodex.Common.Phone
{
    public class TrialManager
    {
        protected TrialManager(int trialDays, bool autoSetFirstExpirationDate, bool resetTrialExpirationOnNewVersion)
        {
            LicenseInformation license = new LicenseInformation();
            IsTrial = license.IsTrial();

#if DEBUG
            if (SimulateTrial)
                IsTrial = true;
#endif

            TrialDays = trialDays;
            ResetTrialExpirationOnNewVersion = resetTrialExpirationOnNewVersion;

            if (TrialDays > 0 && autoSetFirstExpirationDate && TrialExpirationDate == DateTime.MinValue)
                ResetTrialExpiration();
        }

        #region Static

        public static TrialManager Current { get; protected set; }

        public static void Initialize(int trialDays = 0, bool autoSetFirstExpirationDate = true, bool resetTrialExpirationOnNewVersion = true)
        {
            Current = new TrialManager(trialDays, autoSetFirstExpirationDate, resetTrialExpirationOnNewVersion);
        }

        #endregion

        public bool IsTrial { get; protected set; }

        /// <summary>
        /// Returns true if this is the first time running after upgrading from a trial.
        /// </summary>
        public bool WasTrial
        {
            get { return !IsTrial && FirstRunNotifier.WasTrial; }
        }

#if DEBUG

        private readonly Setting<bool> _simulateTrial = new Setting<bool>("TrialManagerSimulateTrial", false);
        public bool SimulateTrial
        {
            get { return _simulateTrial.Value; }
            set
            {
                if (_simulateTrial.Value == value)
                    return;

                _simulateTrial.Value = value;
            }
        }

#endif
        
        #region Trial Expiration

        private Setting<DateTime> _trialExpirationDate = new Setting<DateTime>("TrialManagerTrialExpirationDate");
        public DateTime TrialExpirationDate
        {
            get { return _trialExpirationDate.Value; }
            private set
            {
                if (_trialExpirationDate.Value == value)
                    return;
                _trialExpirationDate.Value = value;
            }
        }

        public int TrialDays { get; set; }
        public bool ResetTrialExpirationOnNewVersion { get; set; }

        public bool TrialExpired
        {
            get { return IsTrial && DateTime.Now >= TrialExpirationDate; }
        }

        public int TrialDaysLeft
        {
            get
            {
                if (!IsTrial || TrialExpirationDate == DateTime.MinValue)
                    return -1;

                var timeLeft = (TrialExpirationDate - DateTime.Now);
                var daysLeft = Math.Ceiling(timeLeft.TotalDays);
                if (daysLeft < 0)
                    daysLeft = 0;
                return (int)daysLeft;
            }
        }

        public void ResetTrialExpiration()
        {
            if (TrialDays <= 0)
                TrialExpirationDate = DateTime.MinValue;
            else
                TrialExpirationDate = DateTime.Now.AddDays(TrialDays);
        }

        #endregion

    }
}
