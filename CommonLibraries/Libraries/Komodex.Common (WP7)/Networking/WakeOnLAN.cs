using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
#if WP7
using System.Net.Sockets;
#else
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
#endif

namespace Komodex.Networking
{
    public static class WakeOnLAN
    {
        private static readonly string BroadcastAddress = "255.255.255.255";
        private const int BroadcastPort = 9;

        public static Task<bool> SendWOLPacket(byte[] macAddress)
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
            return SendMessage(message);
        }

#if WP7
        private static Task<bool> SendMessage(byte[] message)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            SocketAsyncEventArgs socketArgs = new SocketAsyncEventArgs();
            socketArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(BroadcastAddress), BroadcastPort);
            socketArgs.SetBuffer(message, 0, message.Length);
            socketArgs.Completed += (sender, e) =>
            {
                bool success = (e.SocketError == SocketError.Success);
                try { socket.Shutdown(SocketShutdown.Both); }
                catch { success = false; }
                try { socket.Close(); }
                catch { success = false; }
                tcs.TrySetResult(success);
            };

            try
            {
                socket.SendToAsync(socketArgs);
            }
            catch
            {
                tcs.TrySetResult(false);
            }

            return tcs.Task;
        }
#else
        private static async Task<bool> SendMessage(byte[] message)
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
#endif
    }
}
