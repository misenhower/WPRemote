using System;

namespace Komodex.Bonjour
{
    public class NetServiceEventArgs : EventArgs
    {
        public NetServiceEventArgs(NetService service)
        {
            Service = service;
        }

        public NetService Service { get; protected set; }
    }
}
