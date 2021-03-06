﻿using System;
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
using Komodex.Common;
using System.Text;

namespace Komodex.DACP
{
    public class HTTPRequestInfo
    {
        private static readonly Log _log = new Log("DACP HTTPRequestInfo");

        private HTTPRequestInfo() { }

        internal HTTPRequestInfo(HttpWebRequest webRequest)
        {
            WebRequest = webRequest;
        }

        public HttpWebRequest WebRequest { get; protected set; }
        public HttpWebResponse WebResponse { get; set; }

        public bool IsDataRetrieval { get; set; }

        public HTTPResponseHandler ResponseHandlerDelegate { get; set; }
        public HTTPExceptionHandler ExceptionHandlerDelegate { get; set; }

        public object ActionObject { get; set; }

        public string ResponseCode { get; set; }

        private byte[] _responseBody = null;
        public byte[] ResponseBody
        {
            get { return _responseBody; }
            set
            {
                _responseBody = value;
#if DEBUG
                if (_log.EffectiveLevel <= LogLevel.Trace)
                    PrintDebugBytes();
#endif
            }
        }

        private IEnumerable<DACPNode> _responseNodes = null;
        internal IEnumerable<DACPNode> ResponseNodes
        {
            get
            {
                if (_responseNodes == null)
                    _responseNodes = DACPUtility.GetResponseNodes(ResponseBody);
                return _responseNodes;
            }
        }

#if DEBUG

        private void PrintDebugBytes()
        {
            lock(_log)
                _log.Trace(GetDebugBytes(ResponseCode, ResponseBody, 1));
        }

        private string GetDebugBytes(string code, byte[] body, int tabLevel, StringBuilder sb=null)
        {
            string debugText;
            string tab1 = new string('\t', tabLevel - 1);
            string tab2 = new string('\t', tabLevel);

            if (sb == null)
                sb = new StringBuilder("Response content:\n");
            sb.AppendFormat(tab1 + "{0}[{1,3}] +++", code, body.Length).AppendLine();

            var nodes = DACPUtility.GetResponseNodes(body);
            foreach (var kvp in nodes)
            {
                if (containerNodes.Contains(kvp.Key))
                {
                    GetDebugBytes(kvp.Key, kvp.Value, tabLevel + 1, sb);
                }
                else
                {
                    debugText = string.Format(tab2 + "{0}[{1,3}] ", kvp.Key, kvp.Value.Length);

                    switch (kvp.Value.Length)
                    {
                        case 1:
                            debugText += string.Format(" 0x{0:x2} = {0}", kvp.Value[0]);
                            break;
                        case 2:
                            Int16 value = kvp.Value.GetInt16Value();
                            debugText += string.Format(" 0x{0:x4} = {0}", value);
                            if (value >= 32 && value <= 126)
                                debugText += string.Format(" ({0})", (char)value);
                            break;
                        case 4:
                            debugText += string.Format(" 0x{0:x8} = {0}", kvp.Value.GetInt32Value());
                            break;
                        case 8:
                            debugText += string.Format(" 0x{0:x16} = {0}", kvp.Value.GetInt64Value());
                            break;
                        default:
                            debugText += " => " + kvp.Value.GetStringValue();
                            break;
                    }

                    sb.AppendLine(debugText);
                }
            }

            return sb.ToString();
        }

        private List<string> containerNodes = new List<string>(new string[]{
            "msrv", "msml", "mlog", "cmst",
            "mlcl", "abar", "mshl", "mlit",
            "mdcl",
            "ceQS",
        });
#endif

    }
}
