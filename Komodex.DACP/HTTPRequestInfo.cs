using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;

namespace Komodex.DACP
{
    public class HTTPRequestInfo
    {
        private HTTPRequestInfo() { }

        public HTTPRequestInfo(HttpWebRequest webRequest)
        {
            WebRequest = webRequest;
        }

        public HttpWebRequest WebRequest { get; protected set; }

        public byte[] ResponseBody { get; set; }

        private List<KeyValuePair<string, byte[]>> _ResponseNodes = null;
        public List<KeyValuePair<string, byte[]>> ResponseNodes
        {
            get
            {
                if (_ResponseNodes == null)
                    _ResponseNodes = Utility.GetResponseNodes(ResponseBody);
                return _ResponseNodes;
            }
        }
    }
}
