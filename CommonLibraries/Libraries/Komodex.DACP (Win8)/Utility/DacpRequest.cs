using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Komodex.DACP
{
    internal class DacpRequest
    {
        public DacpRequest(string uriBase)
        {
            URIBase = uriBase;
        }

        public DacpRequest(string uriBaseFormat, params object[] args)
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

        public CancellationToken CancellationToken { get; set; }

        public string GetURI()
        {
            if (QueryParameters.Count == 0)
                return URIBase;

            // Don't escape values here to avoid issues with DACP queries
            // Values need to be escaped before being placed in the QueryParameters dictionary.
            return URIBase + "?" + string.Join("&", QueryParameters.Select(kvp => kvp.Key + "=" + kvp.Value));
        }
    }
}
