using System;
using System.Collections.Generic;
using System.Linq;
//using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace Komodex.DACP
{
    internal class DacpResponse
    {
        public DacpResponse(HttpResponseMessage httpResponse, IEnumerable<DacpNode> nodes)
        {
            HTTPResponse = httpResponse;
            Nodes = nodes;
        }

        public HttpResponseMessage HTTPResponse { get; protected set; }
        public IEnumerable<DacpNode> Nodes { get; protected set; }
    }
}
