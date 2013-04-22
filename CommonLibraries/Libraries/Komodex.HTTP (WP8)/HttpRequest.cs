using Komodex.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;

namespace Komodex.HTTP
{
    public class HttpRequest
    {
        private static readonly Log _log = new Log("HTTP Request");

        private HttpRequest(StreamSocket socket)
        {

        }

        internal static HttpRequest GetHttpRequest(StreamSocket socket)
        {
            return null;
        }

        #region Properties

        public HttpMethod Method { get; protected set; }

        public string QueryString { get; protected set; }

        #endregion
    }
}
