using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.DACP
{
    public class PlayQueue : List<PlayQueueItem>
    {
        internal PlayQueue(DACPServer server, byte[] data)
        {
            var nodes = DACPUtility.GetResponseNodes(data);
            foreach (var itemNode in nodes)
            {
                switch (itemNode.Key)
                {
                    case "ceQk":
                        ID = itemNode.Value.GetStringValue();
                        break;

                    case "ceQi":
                        StartIndex = itemNode.Value.GetInt32Value();
                        break;

                    case "ceQm":
                        ItemCount = itemNode.Value.GetInt32Value();
                        break;

                    case "ceQl":
                        Title1 = itemNode.Value.GetStringValue();
                        break;

                    case "ceQh":
                        Title2 = itemNode.Value.GetStringValue();
                        break;
                }
            }
        }

        public string ID { get; protected set; }
        public string Title1 { get; protected set; }
        public string Title2 { get; protected set; }

        internal int StartIndex { get; private set; }
        internal int ItemCount { get; private set; }
    }
}
