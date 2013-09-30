using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.DACP
{
    internal class DACPRequest
    {
        public DACPRequest(string uriBase)
        {
            URIBase = uriBase;
        }

        public DACPRequest(string uriBaseFormat, params object[] args)
            : this(string.Format(uriBaseFormat, args))
        { }

        public string URIBase { get; protected set; }

        private Dictionary<string, string> _queryParameters = new Dictionary<string, string>();
        public Dictionary<string, string> QueryParameters { get { return _queryParameters; } }

        private bool _includeSessionID = true;
        public bool IncludeSessionID
        {
            get { return _includeSessionID; }
            set { _includeSessionID = value; }
        }

        public string GetURI()
        {
            if (QueryParameters.Count == 0)
                return URIBase;
            return URIBase + "?" + string.Join("&", QueryParameters.Select(kvp => Uri.EscapeDataString(kvp.Key) + "=" + Uri.EscapeUriString(kvp.Value)));
        }
    }
}
