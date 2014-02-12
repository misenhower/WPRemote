using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.Common.Networking
{
    // This class should only be needed on WP7. While UdpAnySourceMulticastClient is available on WP8,
    // it does not work correctly and a DatagramSocket should be used instead.

    public static class UdpAnySourceMulticastClientExtensions
    {
        public static Task JoinGroupAsync(this UdpAnySourceMulticastClient client)
        {
            return Task.Factory.FromAsync(client.BeginJoinGroup, client.EndJoinGroup, null);
        }

        public static Task<UdpAnySourceMulticastClientReceieveFromGroupResult> ReceiveFromGroupAsync(this UdpAnySourceMulticastClient client, byte[] buffer, int offset, int count)
        {
            var taskCompletionSource = new TaskCompletionSource<UdpAnySourceMulticastClientReceieveFromGroupResult>();

            client.BeginReceiveFromGroup(buffer, offset, count, asyncResult =>
            {
                try
                {
                    IPEndPoint source;
                    int length = client.EndReceiveFromGroup(asyncResult, out source);
                    taskCompletionSource.TrySetResult(new UdpAnySourceMulticastClientReceieveFromGroupResult(length, source));
                }
                catch (Exception ex)
                {
                    taskCompletionSource.TrySetException(ex);
                }
            }, null);

            return taskCompletionSource.Task;
        }

        public static Task SendToGroupAsync(this UdpAnySourceMulticastClient client, byte[] buffer, int offset, int count)
        {
            return Task.Factory.FromAsync(client.BeginSendToGroup, client.EndSendToGroup, buffer, offset, count, null);
        }
    }

    public class UdpAnySourceMulticastClientReceieveFromGroupResult
    {
        public UdpAnySourceMulticastClientReceieveFromGroupResult(int length, IPEndPoint source)
        {
            Length = length;
            Source = source;
        }

        public int Length { get; private set; }
        public IPEndPoint Source { get; private set; }
    }
}
