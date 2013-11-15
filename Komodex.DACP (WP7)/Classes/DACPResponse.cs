using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Komodex.DACP
{
    internal class DACPResponse
    {
        public DACPResponse(HttpResponseMessage httpResponse, IEnumerable<DACPNode> nodes)
        {
            HTTPResponse = httpResponse;
            Nodes = nodes;
        }

        public HttpResponseMessage HTTPResponse { get; protected set; }
        public IEnumerable<DACPNode> Nodes { get; protected set; }
    }
}
