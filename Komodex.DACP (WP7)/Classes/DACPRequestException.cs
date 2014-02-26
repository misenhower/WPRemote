using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Komodex.DACP
{
    public class DACPRequestException : HttpRequestException
    {
        public DACPRequestException(HttpResponseMessage response)
        {
            Response = response;

            if (response.Content != null)
                response.Content.Dispose();
        }

        public HttpResponseMessage Response { get; private set; }
    }
}
