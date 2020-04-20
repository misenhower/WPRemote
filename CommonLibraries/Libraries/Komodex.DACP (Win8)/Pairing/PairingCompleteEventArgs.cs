using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP.Pairing
{
    public class PairingCompleteEventArgs : EventArgs
    {
        public PairingCompleteEventArgs(string serviceID, string pairingCode)
        {
            ServiceID = serviceID;
            PairingCode = pairingCode;
        }

        public string ServiceID { get; private set; }
        public string PairingCode { get; private set; }
    }
}
