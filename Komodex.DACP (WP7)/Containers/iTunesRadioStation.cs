using Komodex.DACP.Databases;
using Komodex.DACP.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP.Containers
{
    public class iTunesRadioStation : DACPContainer
    {
        public iTunesRadioStation(DACPDatabase database, DACPNodeDictionary nodes)
            : base(database, nodes)
        { }

        public bool IsFeaturedStation { get; private set; }

        protected override void ProcessNodes(DACPNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            IsFeaturedStation = nodes.GetBool("aeRf");
        }

        #region Artwork

        public string Artwork173pxURI { get { return GetAlbumArtURI(173, 173); } }

        #endregion

        #region Commands

        public async Task<bool> Play()
        {
            DACPRequest request = new DACPRequest("/ctrl-int/1/playspec");
            request.QueryParameters["database-spec"] = DACPQueryPredicate.Is("dmap.itemid", "0x" + Database.ID.ToString("x")).ToString();
            request.QueryParameters["container-spec"] = DACPQueryPredicate.Is("dmap.itemid", "0x" + ID.ToString("x")).ToString();

            try { await Server.SubmitRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        #endregion
    }
}
