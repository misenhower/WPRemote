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

namespace Komodex.DACP.Library
{
    public class VideoMediaItem : MediaItem
    {
        private VideoMediaItem()
            : base()
        { }

        public VideoMediaItem(DACPServer server, byte[] data)
            : base(server, data)
        { }

        #region Properties

        private string _ShowName = null;
        public string ShowName
        {
            get { return _ShowName ?? string.Empty; }
            protected set { _ShowName = value; }
        }

        #endregion

        #region Methods

        protected override void ProcessDACPNodes(DACPNodeDictionary nodes)
        {
            base.ProcessDACPNodes(nodes);

            ShowName = nodes.GetString("aeSN");
        }

        #endregion
    }
}
