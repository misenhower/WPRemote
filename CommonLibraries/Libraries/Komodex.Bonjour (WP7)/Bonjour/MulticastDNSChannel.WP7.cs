using Komodex.Bonjour.DNS;
using Komodex.Common;
using Komodex.Common.Networking;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Komodex.Bonjour
{
    internal static partial class MulticastDNSChannel
    {
        // UDP Client
        private static UdpAnySourceMulticastClient _client;

        // The receive buffer size sets the maximum message size
        private static readonly byte[] _receiveBuffer = new byte[2048];

        private static readonly AsyncSemaphore _mutex = new AsyncSemaphore(1);

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
                    _client = new UdpAnySourceMulticastClient(IPAddress.Parse(BonjourUtility.MulticastDNSAddress), BonjourUtility.MulticastDNSPort);
                    _log.Debug("Joining multicast group...");
                    await _client.JoinGroupAsync().ConfigureAwait(false);
                    IsJoined = true;

                    BeginReceiveMessageLoop();
                }
                catch (Exception e)
                {
                    _log.Warning("Caught exception while opening UDP socket");
                    _log.Debug("Exception details: " + e.ToString());
                    CloseSharedChannel();
                }

                if (!IsJoined)
                    await TaskEx.Delay(1000).ConfigureAwait(false);
                else
                    _log.Info("Successfully opened shared MDNS channel.");
            }

            _mutex.Release();
        }

        private static void CloseSharedChannel()
        {
            IsJoined = false;

            var client = _client;
            if (client != null)
            {
                try { client.Dispose(); }
                catch { }
                _client = null;
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
                    await _client.SendToGroupAsync(buffer, 0, buffer.Length);
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

        private static async void BeginReceiveMessageLoop()
        {
            while (IsJoined)
            {
                UdpAnySourceMulticastClientReceieveFromGroupResult result;
                try
                {
                    result = await _client.ReceiveFromGroupAsync(_receiveBuffer, 0, _receiveBuffer.Length);
                }
                catch
                {
                    return;
                }

                // Parse the incoming message
                Message message;
                try
                {
                    message = Message.FromBytes(_receiveBuffer, 0, result.Length);
                }
                catch
                {
                    _log.Info("Dropped malformed packet from " + result.Source);
                    return;
                }

                _log.Debug("Received " + message.Summary);
                _log.Trace("Message details (received from {0}):\n{1}", result.Source, message.ToString());

                SendMessageToListeners(message);
            }
        }

        #endregion
    }
}
