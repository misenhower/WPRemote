using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.Remote.ServerManagement
{
    public class ServerConnectionInfo
    {
        public string ServiceID { get; set; }
        public ServerType ServerType { get; set; }
        public string Name { get; set; }
        public string LastHostname { get; set; }
        public string LastIPAddress { get; set; }
        public bool IsAvailable { get; set; }
    }
}
