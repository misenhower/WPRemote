using Komodex.Bonjour.DNS;

namespace Komodex.Bonjour
{
    internal interface IMulticastDNSListener
    {
        void MulticastDNSMessageReceived(Message message);
    }
}
