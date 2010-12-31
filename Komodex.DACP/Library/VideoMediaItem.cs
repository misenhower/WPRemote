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

namespace Komodex.DACP.Library
{
    public class VideoMediaItem : MediaItem
    {
        private VideoMediaItem()
            : base()
        { }

        public VideoMediaItem(DACPServer server, byte[] data)
            : this()
        {
            Server = server;
            ParseByteData(data);
        }

        #region Properties

        public string ShowName { get; protected set; }

        #endregion

        #region Methods

        protected override bool ProcessByteKVP(System.Collections.Generic.KeyValuePair<string, byte[]> kvp)
        {
            if (base.ProcessByteKVP(kvp))
                return true;

            switch (kvp.Key)
            {
                case "aeSN": // Show name
                    ShowName = kvp.Value.GetStringValue();
                    return true;
                default:
                    return false;
            }
        }

        #endregion
    }
}
