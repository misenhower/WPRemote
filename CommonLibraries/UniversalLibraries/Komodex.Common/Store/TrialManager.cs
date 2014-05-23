//using Komodex.Analytics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Store;

namespace Komodex.Common.Store
{
    public class TrialManager
    {
        protected TrialManager()
        {
            LicenseInformation = CurrentApp.LicenseInformation;
            LicenseInformation.LicenseChanged += LicenseInformation_LicenseChanged;

            UpdateLicenseInfo();
        }

        private static readonly TrialManager _current;
        public static TrialManager Current { get { return _current; } }

        static TrialManager()
        {
            _current = new TrialManager();
        }

        public LicenseInformation LicenseInformation { get; protected set; }
        public bool IsTrial { get; protected set; }
        public bool IsActive { get; protected set; }

        public bool WasTrial
        {
            get
            {
                throw new NotImplementedException();
                //return !IsTrial && FirstRunNotifier.WasTrial;
            }
        }

        private void LicenseInformation_LicenseChanged()
        {
            UpdateLicenseInfo();
        }

        protected void UpdateLicenseInfo()
        {
            IsTrial = LicenseInformation.IsTrial;
            IsActive = LicenseInformation.IsActive;
        }

    }
}
