using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Komodex.Networking
{
    public static class WakeOnLAN
    {
        private static readonly string BroadcastAddress = "255.255.255.255";
        private const int BroadcastPort = 9;

        public static Task<bool> SendWOLPacketAsync(byte[] macAddress)
        {
            if (macAddress.Length != 6)
                throw new ArgumentException("macAddress");

            // Generate WOL message
            // Message is 6 0xFF bytes followed by the MAC address repeated 16 times.
            byte[] message = new byte[6 + 16 * 6];
            for (int i = 0; i < 6; i++)
                message[i] = 0xFF;
            for (int i = 6; i < message.Length; i++)
                message[i] = macAddress[i % 6];

            // Send the message
            return SendMessageAsync(message);
        }

        private static async Task<bool> SendMessageAsync(byte[] message)
        {
            try
            {
                using (DatagramSocket socket = new DatagramSocket())
                {
                    var stream = await socket.GetOutputStreamAsync(new HostName(BroadcastAddress), BroadcastPort.ToString());
                    var writer = new DataWriter(stream);
                    writer.WriteBytes(message);
                    await writer.StoreAsync();
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
