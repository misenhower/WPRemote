using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Komodex.DACP
{
    internal class DACPResponse
    {
        public DACPResponse(HttpResponseMessage response, IEnumerable<DACPNode> nodes)
        {
            Response = response;
            Nodes = nodes;
        }

        public HttpResponseMessage Response { get; protected set; }
        public IEnumerable<DACPNode> Nodes { get; protected set; }
    }
}
