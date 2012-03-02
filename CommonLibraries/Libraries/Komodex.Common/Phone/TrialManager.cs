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
        protected TrialManager()
        {
            LicenseInformation license = new LicenseInformation();
            IsTrial = license.IsTrial();

#if DEBUG
            if (SimulateTrial)
                IsTrial = true;
#endif
        }

        #region Static

        public static TrialManager Current { get; protected set; }

        public static void Initialize()
        {
            Current = new TrialManager();
        }

        #endregion

        public bool IsTrial { get; protected set; }

        /// <summary>
        /// Returns true if this is the first time running after upgrading from a trial.
        /// </summary>
        public bool WasTrial
        {
            get { return !IsTrial && FirstRunNotifier.PreviousTrialMode; }
        }

#if DEBUG

        private Setting<bool> _simulateTrial = new Setting<bool>("TrialManagerSimulateTrial", false);
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

    }
}
