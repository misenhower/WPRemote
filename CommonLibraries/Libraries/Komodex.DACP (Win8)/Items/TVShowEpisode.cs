using Komodex.Common;
using Komodex.DACP.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.DACP.Items
{
    public class TVShowEpisode : DacpItem
    {
        public TVShowEpisode(DacpContainer container, DacpNodeDictionary nodes)
            : base(container, nodes)
        { }

        public DateTime? DateReleased { get; private set; }
        public int SeasonNumber { get; private set; }
        public int EpisodeNumber { get; private set; }
        public string SeriesName { get; private set; }
        public bool IsHD { get; private set; }

        protected override void ProcessNodes(DacpNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            if (nodes.ContainsKey("asdr"))
                DateReleased = nodes.GetDateTime("asdr");
            SeasonNumber = nodes.GetInt("aeSU");
            EpisodeNumber = nodes.GetInt("aeES");
            SeriesName = nodes.GetString("aeSN");
            IsHD = nodes.GetBool("aeHD");
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
