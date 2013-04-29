using Komodex.Common.Phone;
using Komodex.Remote.ServerManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.Remote
{
    public class RemoteBasePage : PhoneApplicationBasePage
    {
        #region Connection Status Popup

        private bool _disableConnectionStatusPopup;
        public bool DisableConnectionStatusPopup
        {
            get { return _disableConnectionStatusPopup; }
            set
            {
                if (_disableConnectionStatusPopup == value)
                    return;

                _disableConnectionStatusPopup = value;
                ConnectionStatusPopupManager.UpdatePopupVisibility();
            }
        }

        #endregion
    }
}
