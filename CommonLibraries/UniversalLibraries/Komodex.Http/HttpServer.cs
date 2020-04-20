using Komodex.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;

namespace Komodex.HTTP
{
    public class HttpServer
    {
        private static readonly Log _log = new Log("HTTP Server");

        protected StreamSocketListener _socketListener;

        public HttpServer()
        {

        }

        public HttpServer(string serviceName)
            : this()
        {
            ServiceName = serviceName;
        }

        public event EventHandler<HttpRequestEventArgs> RequestReceived;

        #region Properties

        private string _serviceName = "80";
        /// <summary>
        /// Gets or sets the local service name or TCP port on which to bind the server.
        /// </summary>
        public string ServiceName
        {
            get { return _serviceName; }
            set { _serviceName = value; }
        }

        #endregion

        public async Task Start()
        {
            if (_socketListener != null)
                return;

            _log.Info("Starting HTTP server on port {0}...", ServiceName);

            _socketListener = new StreamSocketListener();
            _socketListener.ConnectionReceived += SocketListener_ConnectionReceived;

            try
            {
                await _socketListener.BindServiceNameAsync(ServiceName);
            }
            catch (Exception ex)
            {
                _log.Error("Could not start HTTP socket listener: " + ex.ToString());
                Stop();
                throw;
            }
        }

        public void Stop()
        {
            if (_socketListener == null)
                return;

            _log.Info("Stopping HTTP server on port {0}...", ServiceName);

            _socketListener.Dispose();
            _socketListener = null;
        }

        private async void SocketListener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            if (sender != _socketListener)
                return;

            StreamSocket socket = args.Socket;

            _log.Debug("Incoming connection to {0}:{1} from {2}...", socket.Information.LocalAddress.DisplayName, socket.Information.LocalPort, socket.Information.RemoteAddress.DisplayName);

            HttpRequest request = await HttpRequest.GetHttpRequest(args.Socket);
            if (request == null)
                return;

            _log.Info("Received request from {0}: {1}", socket.Information.RemoteAddress.DisplayName, request.Uri.PathAndQuery);

            RequestReceived.Raise(this, new HttpRequestEventArgs(request));
        }

    }
}
