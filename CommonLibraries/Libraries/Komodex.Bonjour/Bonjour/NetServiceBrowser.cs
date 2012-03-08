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
using System.Diagnostics;
using System.Linq;

namespace Komodex.Bonjour
{
    public class NetServiceBrowser
    {
        // Service search parameters
        private string _currentServiceName;
        private Message _currentServiceSearchMessage;

        private MulticastDNSChannel _channel;

        #region Public Methods

        public void SearchForServices(string serviceName)
        {
            _currentServiceName = BonjourUtility.FormatLocalHostname(serviceName);

            // Create the DNS message to send
            _currentServiceSearchMessage = new Message();
            _currentServiceSearchMessage.Questions.Add(new Question(_currentServiceName, ResourceRecordType.PTR));

            // Create the channel if necessary
            if (_channel == null)
            {
                _channel = new MulticastDNSChannel();
                _channel.Joined += MulticastDNSChannel_Joined;
                _channel.MessageReceived += MulticastDNSChannel_MessageReceived;
                _channel.Start();
            }
            else
            {
                _channel.SendMessage(_currentServiceSearchMessage);
            }
        }

        public void Stop()
        {
            if (_channel != null)
            {
                _channel.Joined -= MulticastDNSChannel_Joined;
                _channel.MessageReceived -= MulticastDNSChannel_MessageReceived;
                _channel.Stop();
                _channel = null;
            }
        }

        #endregion

        private void MulticastDNSChannel_Joined(object sender, EventArgs e)
        {
            if (sender != _channel)
                return;

            if (_currentServiceSearchMessage != null)
                _channel.SendMessage(_currentServiceSearchMessage);
        }

        private void MulticastDNSChannel_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Message message = e.Message;

            // Make sure this is a response
            if (!message.QueryResponse)
                return;

            // Make sure this has an answer to what we're looking for
            if (!message.AnswerRecords.Any(rr => rr.Name == _currentServiceName))
                return;

#if DEBUG
            // Split the message into separate lines to avoid issues with exceeding the maximum length of Debug.WriteLine
            Debug.WriteLine("Message received:");
            var messageLines = message.ToString().Split('\n');
            foreach (string line in messageLines)
                Debug.WriteLine(line.Trim());
            Debug.WriteLine(string.Empty);
#endif
        }
    }
}
