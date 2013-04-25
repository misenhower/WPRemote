using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Komodex.HTTP
{
    public class HttpResponse
    {
        public HttpResponse()
        {
            Headers["Content-Type"] = "text/html";
        }

        private HttpStatusCode _statusCode = HttpStatusCode.OK;
        public HttpStatusCode StatusCode
        {
            get { return _statusCode; }
            set { _statusCode = value; }
        }

        private Dictionary<string, string> _headers = new Dictionary<string, string>();
        public Dictionary<string,string> Headers
        {
            get { return _headers; }
        }

        private MemoryStream _body = new MemoryStream();
        public MemoryStream Body
        {
            get { return _body; }
        }
    }
}
