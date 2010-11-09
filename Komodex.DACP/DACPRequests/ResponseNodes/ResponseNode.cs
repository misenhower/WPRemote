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
using System.Text;
using System.Linq;

namespace Komodex.DACP.DACPRequests.ResponseNodes
{
    public class ResponseNode
    {
        protected ResponseNode(byte[] body)
        {
            Body = body;
        }

        #region Static Methods

        public static void ProcessResponseBody(byte[] data)
        {
            ResponseNode node = new ResponseNode(data);
            node.ProcessBody();
        }

        #endregion

        #region Properties

        public byte[] Body { get; protected set; }

        #endregion

        #region Instance Methods

        protected void ProcessBody()
        {
            int dataLength = Body.Length;
            int location = 0;

            while (location + 8 < dataLength)
            {
                string code = Encoding.UTF8.GetString(Body, location, 4);
                int length = BitConverter.ToInt32(Body, location + 4).SwapBits();
                byte[] nodeBody = Body.Skip(location + 8).Take(length).ToArray();

                ProcessNode(code, nodeBody);

                location += 8 + length;
            }
        }

        protected virtual void ProcessNode(string code, byte[] body)
        {
            switch (code)
            {
                case "mlog": // Login node

                    break;
                case "cmst": // Status node

                    break;
                default:
                    // Ignore unrecognized node
                    break;
            }
        }

        #endregion

    }
}
