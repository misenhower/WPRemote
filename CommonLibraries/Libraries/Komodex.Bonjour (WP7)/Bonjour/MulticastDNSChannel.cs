using Komodex.Bonjour.DNS;
using Komodex.Common;
using System;
using System.Collections.Generic;
using System.Net;

namespace Komodex.Bonjour
{
    internal static class MulticastDNSChannel
    {
        private static readonly Log _log = new Log("Bonjour MDNS");

        private static readonly object _sync = new object();

        private static bool _sendingMessage;
        private static bool _shutdown;

        #region Properties

        public static bool IsJoined { get; private set; }

        #endregion

        #region Listeners

        private static readonly List<IMulticastDNSListener> _listeners = new List<IMulticastDNSListener>();

        public static void AddListener(IMulticastDNSListener listener)
        {
            if (listener == null)
                throw new ArgumentNullException("listener");

            _shutdown = false;

            lock (_listeners)
                _listeners.AddOnce(listener);

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
            if (!IsJoined)
                Start();

            // Get the message bytes and send
            byte[] messageBytes = message.GetBytes();
            SendMessage(messageBytes);
        }

        #endregion

#if WP7
        #region UDP Channel Management

        // UDP Client
        private static System.Net.Sockets.UdpAnySourceMulticastClient _client;

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
                _client = new System.Net.Sockets.UdpAnySourceMulticastClient(IPAddress.Parse(BonjourUtility.MulticastDNSAddress), BonjourUtility.MulticastDNSPort);
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
#else
        #region UDP Socket Management

        private static Windows.Networking.Sockets.DatagramSocket _udpSocket;
        private static readonly Windows.Networking.HostName _mdnsHostName = new Windows.Networking.HostName(BonjourUtility.MulticastDNSAddress);

        private static async void Start()
        {
            lock (_sync)
            {
                if (_udpSocket != null)
                    return;

                _log.Info("Creating UDP socket...");
                _udpSocket = new Windows.Networking.Sockets.DatagramSocket();
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
                var outputWriter = new Windows.Storage.Streams.DataWriter(outputStream);
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

        private static void UDPSocket_MessageReceived(Windows.Networking.Sockets.DatagramSocket sender, Windows.Networking.Sockets.DatagramSocketMessageReceivedEventArgs args)
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
#endif
    }
}
