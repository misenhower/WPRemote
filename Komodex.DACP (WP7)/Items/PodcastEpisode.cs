using Komodex.Common;
using Komodex.DACP.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.DACP.Items
{
    public class PodcastEpisode : DACPItem
    {
        public PodcastEpisode(DACPContainer container, DACPNodeDictionary nodes)
            : base(container, nodes)
        { }

        public DateTime? DateReleased { get; private set; }

        protected override void ProcessNodes(DACPNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            if (nodes.ContainsKey("asdr"))
                DateReleased = nodes.GetDateTime("asdr");
        }

        #region Display

        public string SecondLine
        {
            get
            {
                string dateString = null;
                if (DateReleased != null)
                    dateString = DateReleased.Value.ToShortDateString();

                return Utility.JoinNonEmptyStrings(" – ", dateString, FormattedDuration);
            }
        }

        #endregion
    }
}
