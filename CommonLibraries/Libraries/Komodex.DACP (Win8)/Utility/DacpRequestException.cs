using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace Komodex.DACP
{
    public class DacpRequestException : Exception
    {
        public DacpRequestException(HttpResponseMessage response)
        {
            Response = response;

            if (response.Content != null)
                response.Content.Dispose();
        }

        public HttpResponseMessage Response { get; private set; }
    }
}
