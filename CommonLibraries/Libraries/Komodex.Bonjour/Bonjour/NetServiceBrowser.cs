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
using Komodex.Bonjour.DNS;

namespace Komodex.Bonjour
{
    public class NetServiceBrowser
    {
        // Service search parameters
        private string _currentServiceName;

        private MulticastDNSChannel _channel;

        #region Public Methods

        public void SearchForServices(string serviceName)
        {
            _currentServiceName = serviceName;

            // Create the DNS message to send
            Message message = new Message();
            message.Questions.Add(new Question(_currentServiceName, RRType.SRV));

            // Create the channel if necessary
            if (_channel == null)
            {
                _channel = new MulticastDNSChannel(message);
                _channel.MessageReceived += new EventHandler<MessageReceivedEventArgs>(MulticastDNSChannel_MessageReceived);
                _channel.Start();
            }
            else
            {
                _channel.BroadcastMessage = message;
                _channel.SendMessage();
            }
        }

        public void Stop()
        {
            if (_channel != null)
            {
                _channel.Stop();
                _channel = null;
            }
        }

        #endregion

        private void MulticastDNSChannel_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
        }

    }
}
