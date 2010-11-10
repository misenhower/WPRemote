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

namespace Komodex.DACP
{
    internal class HTTPRequestState
    {
        private HTTPRequestState() { }

        public HTTPRequestState(HttpWebRequest webRequest)
        {
            WebRequest = webRequest;
        }

        public HttpWebRequest WebRequest { get; protected set; }
    }
}
