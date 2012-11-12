using Komodex.Bonjour.DNS;

namespace Komodex.Bonjour
{
    internal interface IMulticastDNSListener
    {
        void MulticastDNSChannelJoined();
        void MulticastDNSMessageReceived(Message message);
    }
}
