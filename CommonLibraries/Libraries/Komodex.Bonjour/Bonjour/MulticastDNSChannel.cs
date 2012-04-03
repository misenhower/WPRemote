using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Net.Sockets;
using Komodex.Bonjour.DNS;
using Komodex.Common;
using System.Collections.Generic;

namespace Komodex.Bonjour
{
    internal static class MulticastDNSChannel
    {
        // UDP Client
        private static UdpAnySourceMulticastClient _client;

        // The receive buffer size sets the maximum message size
        private static readonly byte[] _receiveBuffer = new byte[2048];

        private static readonly Log.LogInstance _log = Log.GetInstance("Bonjour");

        #region Properties

        public static bool IsJoined { get; private set; }

        #endregion

        #region Listeners

        private static readonly List<IMulticastDNSListener> _listeners = new List<IMulticastDNSListener>();

        public static void AddListener(IMulticastDNSListener listener)
        {
            if (listener == null)
                throw new ArgumentNullException("listener");

            lock (_listeners)
            {
                if (_listeners.Contains(listener))
                    return;

                _listeners.Add(listener);
            }

            if (IsJoined)
                listener.MulticastDNSChannelJoined();
            else
                Start();
        }

        public static void RemoveListener(IMulticastDNSListener listener)
        {
            if (listener == null)
                throw new ArgumentNullException("listener");

            bool stop = false;

            lock (_listeners)
            {
                if (!_listeners.Contains(listener))
                    return;

                _listeners.Remove(listener);

                if (_listeners.Count == 0)
                    stop = true;
            }

            if (stop)
                Stop();
        }

        private static void SendJoinedToListeners()
        {
            IMulticastDNSListener[] listeners;
            lock (_listeners)
                listeners = _listeners.ToArray();

            for (int i = 0; i < listeners.Length; i++)
                listeners[i].MulticastDNSChannelJoined();
        }

        private static void SendMessageToListeners(Message message)
        {
            IMulticastDNSListener[] listeners;
            lock (_listeners)
                listeners = _listeners.ToArray();

            for (int i = 0; i < listeners.Length; i++)
                listeners[i].MulticastDNSMessageReceived(message);
        }

        #endregion

        #region Public Methods

        public static void SendMessage(Message message)
        {
            if (_client == null)
                throw new InvalidOperationException("Call Start before attempting to send a message.");

            if (!IsJoined)
                throw new InvalidOperationException("Client has not been joined to the network.");

            // Get the message bytes and send
            byte[] messageBytes = message.GetBytes();
            _client.BeginSendToGroup(messageBytes, 0, messageBytes.Length, UDPClientSendToGroupCallback, _client);
        }

        #endregion

        #region UDP Channel Management

        private static void Start()
        {
            // If the client already exists, don't attempt to replace it
            if (_client != null)
                return;

            // Create the client and attempt to join
            _log.Info("Joining multicast DNS channel...");
            _client = new UdpAnySourceMulticastClient(BonjourUtility.MulticastDNSAddress, BonjourUtility.MulticastDNSPort);
            _client.BeginJoinGroup(UDPClientJoinGroupCallback, _client);
        }

        private static void Stop()
        {
            if (_client != null)
            {
                _log.Info("Closing multicast DNS channel");
                IsJoined = false;
                _client.Dispose();
                _client = null;
            }
        }

        private static void BeginReceiveFromGroup()
        {
            _client.BeginReceiveFromGroup(_receiveBuffer, 0, _receiveBuffer.Length, UDPClientReceiveFromGroupCallback, _client);
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
            _log.Info("Multicast DNS channel joined");
            SendJoinedToListeners();

            BeginReceiveFromGroup();
        }

        private static void UDPClientSendToGroupCallback(IAsyncResult result)
        {
            if (result.AsyncState != _client)
                return;

            _client.EndSendToGroup(result);
            BeginReceiveFromGroup();
        }

        private static void UDPClientReceiveFromGroupCallback(IAsyncResult result)
        {
            if (result.AsyncState != _client)
                return;

            IPEndPoint sourceIPEndpoint;
            int count = _client.EndReceiveFromGroup(result, out sourceIPEndpoint);

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

            _log.Info("Received " + message.Summary);
            _log.Debug("Message details (received from {0}):\n{1}\n", sourceIPEndpoint, message.ToString());

            SendMessageToListeners(message);

            BeginReceiveFromGroup();
        }

        #endregion
    }
}
