using Komodex.Bonjour.DNS;
using Komodex.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Komodex.Bonjour
{
    internal static partial class MulticastDNSChannel
    {
        #region UDP Channel Management

        // UDP Client
        private static UdpAnySourceMulticastClient _client;

        // The receive buffer size sets the maximum message size
        private static readonly byte[] _receiveBuffer = new byte[2048];

        private static void Start()
        {
            lock (_sync)
            {
                // If the client already exists, don't attempt to replace it
                if (_client != null)
                    return;

                // Create the client and attempt to join
                _log.Info("Joining multicast DNS channel...");
                _client = new UdpAnySourceMulticastClient(IPAddress.Parse(BonjourUtility.MulticastDNSAddress), BonjourUtility.MulticastDNSPort);
                _client.BeginJoinGroup(UDPClientJoinGroupCallback, _client);
            }
        }

        private static void Stop()
        {
            lock (_sync)
            {
                if (_client == null)
                    return;

                _shutdown = true;
                if (_sendingMessage)
                    return;

                _log.Info("Closing multicast DNS channel");
                IsJoined = false;
                _client.Dispose();
                _client = null;
            }
        }

        private static void SendMessage(byte[] buffer)
        {
            _sendingMessage = true;
            try
            {
                _client.BeginSendToGroup(buffer, 0, buffer.Length, UDPClientSendToGroupCallback, _client);
            }
            catch
            {
                _sendingMessage = false;
                bool restart = !_shutdown;
                Stop();
                if (restart)
                {
                    _log.Info("Restarting multicast DNS channel from SendMessage method...");
                    Start();
                }
            }
        }

        private static void BeginReceiveFromGroup()
        {
            try
            {
                _client.BeginReceiveFromGroup(_receiveBuffer, 0, _receiveBuffer.Length, UDPClientReceiveFromGroupCallback, _client);
            }
            catch
            {
                bool restart = !_shutdown;
                Stop();
                if (restart)
                {
                    _log.Info("Restarting multicast DNS channel from BeginReceiveFromGroup method...");
                    Start();
                }
            }
        }

        #endregion

        #region UDP Channel Callbacks

        private static void UDPClientJoinGroupCallback(IAsyncResult result)
        {
            if (result.AsyncState != _client)
                return;

            // TODO: try/catch/error handling/etc.

            _client.EndJoinGroup(result);
            IsJoined = true;
            _log.Info("Joined multicast DNS channel.");
            SendJoinedToListeners();

            BeginReceiveFromGroup();
        }

        private static void UDPClientSendToGroupCallback(IAsyncResult result)
        {
            if (result.AsyncState != _client)
                return;

            try
            {
                _client.EndSendToGroup(result);
            }
            catch { }

            _sendingMessage = false;
            if (!_shutdown)
                BeginReceiveFromGroup();
            else
                Stop();
        }

        private static void UDPClientReceiveFromGroupCallback(IAsyncResult result)
        {
            if (result.AsyncState != _client)
                return;

            IPEndPoint sourceIPEndpoint;
            int count;
            try
            {
                count = _client.EndReceiveFromGroup(result, out sourceIPEndpoint);
            }
            catch
            {
                return;
            }

            // Parse the incoming message
            Message message;
            try
            {
                message = Message.FromBytes(_receiveBuffer, 0, count);
            }
            catch
            {
                _log.Info("Dropped malformed packet from " + sourceIPEndpoint);
                return;
            }

            _log.Debug("Received " + message.Summary);
            _log.Trace("Message details (received from {0}):\n{1}", sourceIPEndpoint, message.ToString());

            SendMessageToListeners(message);

            if (IsJoined)
                BeginReceiveFromGroup();
        }

        #endregion
    }
}
