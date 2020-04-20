using Komodex.Common;
using Komodex.DACP.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.DACP.Items
{
    public class PodcastEpisode : DacpItem
    {
        public PodcastEpisode(DacpContainer container, DacpNodeDictionary nodes)
            : base(container, nodes)
        { }

        public DateTime? DateReleased { get; private set; }

        protected override void ProcessNodes(DacpNodeDictionary nodes)
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
                    //dateString = DateReleased.Value.ToShortDateString();
                    dateString = DateReleased.Value.ToString();

                return Utility.JoinNonEmptyStrings(" – ", dateString, FormattedDuration);
            }
        }

        #endregion
    }
}
