using Komodex.Bonjour.DNS;
using Komodex.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Komodex.Bonjour
{
    internal partial class MulticastDNSChannel
    {
        private static readonly Log _log = new Log("Bonjour MDNS") { Level = LogLevel.Debug };

        private static bool _shouldJoinChannel;

        #region Properties

        public static bool IsJoined { get; private set; }

        #endregion

        #region Listeners

        private static readonly List<IMulticastDNSListener> _listeners = new List<IMulticastDNSListener>();

        public static async Task AddListenerAsync(IMulticastDNSListener listener)
        {
            if (listener == null)
                throw new ArgumentNullException("listener");

            lock (_listeners)
            {
                _listeners.AddOnce(listener);
                _shouldJoinChannel = true;
            }

            if (!IsJoined)
                await OpenSharedChannelAsync().ConfigureAwait(false);
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
                {
                    stop = true;
                    _shouldJoinChannel = false;
                }
            }

            if (stop)
                CloseSharedChannel();
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

        public static Task<bool> SendMessageAsync(Message message)
        {
            return SendMessageAsync(message.GetBytes());
        }

        #endregion
    }
}
