using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.HTTP
{
    public class HttpRequestEventArgs : EventArgs
    {
        public HttpRequestEventArgs(HttpRequest request)
        {
            Request = request;
        }

        public HttpRequest Request { get; protected set; }
    }
}
