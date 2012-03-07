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

        private MulticastDNSChannel _channel;

        #region Public Methods

        public void SearchForServices(string serviceName)
        {
            _currentServiceName = BonjourUtility.FormatLocalHostname(serviceName);

            // Create the DNS message to send
            Message message = new Message();
            message.Questions.Add(new Question(_currentServiceName, ResourceRecordType.PTR));

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
