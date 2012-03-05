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

namespace Komodex.Bonjour
{
    internal class MulticastDNSChannel
    {
        // UDP Client
        protected UdpAnySourceMulticastClient _client;

        // The receive buffer size sets the maximum message size
        private readonly byte[] _receiveBuffer = new byte[2048];

        public MulticastDNSChannel()
        { }

        public MulticastDNSChannel(Message broadcastMessage)
            : this()
        {
            BroadcastMessage = broadcastMessage;
        }

        #region Properties

        public bool IsJoined { get; private set; }

        public Message BroadcastMessage { get; set; }

        #endregion

        #region Events

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        #endregion

        #region Public Methods

        public void Start()
        {
            // If the client already exists, don't attempt to replace it
            if (_client != null)
                return;

            // Create the client and attempt to join
            _client = new UdpAnySourceMulticastClient(BonjourUtility.MulticastDNSAddress, BonjourUtility.MulticastDNSPort);
            _client.BeginJoinGroup(UDPClientJoinGroupCallback, _client);
        }

        public void SendMessage()
        {
            if (BroadcastMessage == null)
                throw new InvalidOperationException("Cannot send message when BroadcastMessage is null.");

            if (_client == null)
                throw new InvalidOperationException("Call Start before attempting to send a message.");

            // If the client exists but hasn't finished joining yet, the message will be sent automatically once joining has completed
            if (!IsJoined)
                return;

            // Get the message bytes and send
            byte[] messageBytes = BroadcastMessage.GetBytes();
            _client.BeginSendToGroup(messageBytes, 0, messageBytes.Length, UDPClientSendToGroupCallback, _client);
        }

        public void Stop()
        {
            if (_client != null)
            {
                IsJoined = false;
                _client.Dispose();
                _client = null;
            }
        }

        #endregion

        #region Other Methods

        private void BeginReceiveFromGroup()
        {
            _client.BeginReceiveFromGroup(_receiveBuffer, 0, _receiveBuffer.Length, UDPClientReceiveFromGroupCallback, _client);
        }

        #endregion

        #region UDP Client Callbacks

        private void UDPClientJoinGroupCallback(IAsyncResult result)
        {
            if (result.AsyncState != _client)
                return;

            // TODO: try/catch/error handling/etc.

            _client.EndJoinGroup(result);
            IsJoined = true;

            // If a BroadcastMessage is available, send it to the network
            if (BroadcastMessage != null)
                SendMessage();
            else
                BeginReceiveFromGroup();
        }

        private void UDPClientSendToGroupCallback(IAsyncResult result)
        {
            if (result.AsyncState != _client)
                return;

            _client.EndSendToGroup(result);
            BeginReceiveFromGroup();
        }

        private void UDPClientReceiveFromGroupCallback(IAsyncResult result)
        {
            if (result.AsyncState != _client)
                return;

            IPEndPoint sourceIPEndpoint;
            int count = _client.EndReceiveFromGroup(result, out sourceIPEndpoint);

            // Parse the incoming message
            Message message = new Message(_receiveBuffer, 0, count);
            MessageReceived.Raise(this, new MessageReceivedEventArgs(message));

            BeginReceiveFromGroup();
        }

        #endregion
    }
}
