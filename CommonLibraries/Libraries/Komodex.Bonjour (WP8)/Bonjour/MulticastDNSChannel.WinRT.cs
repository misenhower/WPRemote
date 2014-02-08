using Komodex.Bonjour.DNS;
using Komodex.Common;
using System;
using System.Collections.Generic;
using System.Net;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Komodex.Bonjour
{
    internal static partial class MulticastDNSChannel
    {
        #region UDP Socket Management

        private static DatagramSocket _udpSocket;
        private static readonly HostName _mdnsHostName = new HostName(BonjourUtility.MulticastDNSAddress);

        private static async void Start()
        {
            lock (_sync)
            {
                if (_udpSocket != null)
                    return;

                _log.Info("Creating UDP socket...");
                _udpSocket = new DatagramSocket();
            }

            bool restartAfterDelay = false;

            try
            {
                _udpSocket.MessageReceived += UDPSocket_MessageReceived;
                await _udpSocket.BindServiceNameAsync(BonjourUtility.MulticastDNSPort.ToString());

                if (_shutdown)
                    return;

                _udpSocket.JoinMulticastGroup(_mdnsHostName);
            }
            catch (ObjectDisposedException)
            {
                Restart();
                return;
            }
            catch (Exception)
            {
                // This is probably an "only one usage of each socket address is normally permitted" exception.
                _udpSocket = null;
                restartAfterDelay = true;
            }

            if (restartAfterDelay)
            {
                await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(5));
                Restart();
                return;
            }

            IsJoined = true;
            _log.Info("Joined multicast DNS group.");
            SendJoinedToListeners();
        }

        private static void Stop()
        {
            lock (_sync)
            {
                if (_udpSocket == null)
                    return;

                _shutdown = true;
                if (_sendingMessage)
                    return;

                _log.Info("Closing UDP socket...");
                IsJoined = false;
                _udpSocket.Dispose();
                _udpSocket = null;
            }
        }

        private static void Restart()
        {
            lock (_sync)
            {
                try
                {
                    Stop();
                }
                catch (ObjectDisposedException) { }

                _udpSocket = null;
                _shutdown = false;

                lock (_listeners)
                {
                    if (_listeners.Count == 0)
                        return;
                }
            }

            Start();
        }

        private static async void SendMessage(byte[] buffer)
        {
            _sendingMessage = true;

            try
            {
                // Get the output stream
                var outputStream = await _udpSocket.GetOutputStreamAsync(_mdnsHostName, BonjourUtility.MulticastDNSPort.ToString());

                // Write bytes to stream
                var outputWriter = new DataWriter(outputStream);
                outputWriter.WriteBytes(buffer);
                await outputWriter.StoreAsync();
            }
            catch (ObjectDisposedException)
            {
                _sendingMessage = false;
                Restart();
                return;
            }

            _sendingMessage = false;

            // Shutdown if necessary
            if (_shutdown)
                Stop();
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
