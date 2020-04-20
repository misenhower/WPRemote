using Komodex.Bonjour.DNS;
using Komodex.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Komodex.Bonjour
{
    internal static partial class MulticastDNSChannel
    {
        private static DatagramSocket _udpSocket;
        private static readonly HostName _mdnsHostName = new HostName(BonjourUtility.MulticastDNSAddress);

        private static readonly SemaphoreSlim _mutex = new SemaphoreSlim(1, 1);

        private static async Task OpenSharedChannelAsync()
        {
            await _mutex.WaitAsync().ConfigureAwait(false);

            if (IsJoined || !_shouldJoinChannel)
                return;

            CloseSharedChannel();

            _log.Info("Opening shared MDNS channel...");

            while (!IsJoined && _shouldJoinChannel)
            {
                try
                {
                    // Create new socket
                    _udpSocket = new DatagramSocket();
                    _udpSocket.MessageReceived += UDPSocket_MessageReceived;

                    // Fixing an issue on Windows 8.1 by attaching the socket to the Internet connection profile's network adapter.
                    // Source: http://blogs.msdn.com/b/wsdevsol/archive/2013/12/19/datagramsocket-multicast-functionality-on-windows-8-1-throws-an-error-0x80072af9-wsahost-not-found.aspx
                    var networkAdapter = NetworkInformation.GetInternetConnectionProfile().NetworkAdapter;

                    // Bind the service name and join the multicast group
                    _log.Debug("Opening UDP socket...");
                    await _udpSocket.BindServiceNameAsync(BonjourUtility.MulticastDNSPort.ToString(), networkAdapter).AsTask().ConfigureAwait(false);
                    _log.Debug("Joining multicast group...");
                    _udpSocket.JoinMulticastGroup(_mdnsHostName);

                    // All done
                    IsJoined = true;
                }
                catch (Exception e)
                {
                    _log.Warning("Caught exception while opening UDP socket");
                    _log.Debug("Exception details: " + e.ToString());
                    CloseSharedChannel();
                }

                if (!IsJoined)
                    await Task.Delay(1000).ConfigureAwait(false);
                else
                    _log.Info("Successfully opened shared MDNS channel.");
            }


            _mutex.Release();
        }

        private static void CloseSharedChannel()
        {
            IsJoined = false;

            var socket = _udpSocket;
            if (socket != null)
            {
                // Don't remove the MessageReceived event handler here since that will throw an exception.
                try { socket.Dispose(); }
                catch { }
                _udpSocket = null;
                _log.Info("Closed shared MDNS channel.");
            }
        }

        #region Message Send/Receive

        public static async Task<bool> SendMessageAsync(byte[] buffer)
        {
            await _mutex.WaitAsync().ConfigureAwait(false);

            try
            {
                if (!IsJoined)
                    return false;

                try
                {
                    // Get the output stream
                    var outputStream = await _udpSocket.GetOutputStreamAsync(_mdnsHostName, BonjourUtility.MulticastDNSPort.ToString()).AsTask().ConfigureAwait(false);

                    // Write bytes to stream
                    var outputWriter = new DataWriter(outputStream);
                    outputWriter.WriteBytes(buffer);
                    await outputWriter.StoreAsync().AsTask().ConfigureAwait(false);

                    return true;
                }
                catch
                {
                    return false;
                }
            }
            finally
            {
                _mutex.Release();
            }
        }

        private static void UDPSocket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            if (sender != _udpSocket)
                return;

            var remoteIP = args.RemoteAddress;
            byte[] receiveBuffer;

            try
            {
                var dataReader = args.GetDataReader();
                receiveBuffer = new byte[dataReader.UnconsumedBufferLength];
                dataReader.ReadBytes(receiveBuffer);
            }
            catch
            {
                return;
            }

            // Parse the incoming message
            Message message;
            try
            {
                message = Message.FromBytes(receiveBuffer, 0, receiveBuffer.Length);
            }
            catch
            {
                _log.Info("Dropped malformed packet from " + remoteIP.DisplayName);
                return;
            }

            _log.Debug("Received " + message.Summary);
            _log.Trace("Message details (received from {0}):\n{1}", remoteIP.DisplayName, message.ToString());

            SendMessageToListeners(message);
        }

        #endregion
    }
}
