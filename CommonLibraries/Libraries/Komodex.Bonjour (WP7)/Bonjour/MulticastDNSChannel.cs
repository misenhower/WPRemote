using Komodex.Bonjour.DNS;
using Komodex.Common;
using System;
using System.Collections.Generic;
using System.Net;

namespace Komodex.Bonjour
{
    internal static partial class MulticastDNSChannel
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
    }
}
