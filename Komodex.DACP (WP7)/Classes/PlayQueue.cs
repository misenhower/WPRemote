using Komodex.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Komodex.DACP
{
    public sealed class PlayQueue : ObservableCollection<PlayQueueItem>
    {
        internal PlayQueue(string id)
        {
            ID = id;
        }

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

        public string ID { get; private set; }

        private string _title1;
        public string Title1
        {
            get { return _title1; }
            internal set
            {
                if (_title1 == value)
                    return;

                _title1 = value;
                ThreadUtility.RunOnUIThread(() => OnPropertyChanged(new PropertyChangedEventArgs("Title1")));
            }
        }

        private string _title2;
        public string Title2
        {
            get { return _title2; }
            internal set
            {
                if (_title2 == value)
                    return;

                _title2 = value;
                ThreadUtility.RunOnUIThread(() => OnPropertyChanged(new PropertyChangedEventArgs("Title2")));
            }
        }

        internal int StartIndex { get; set; }
        internal int ItemCount { get; set; }
    }
}
