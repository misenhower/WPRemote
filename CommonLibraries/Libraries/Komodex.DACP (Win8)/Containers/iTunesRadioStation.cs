using Komodex.DACP.Databases;
using Komodex.DACP.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP.Containers
{
    public class iTunesRadioStation : DacpContainer
    {
        public iTunesRadioStation(DacpDatabase database, DacpNodeDictionary nodes)
            : base(database, nodes)
        { }

        public bool IsFeaturedStation { get; private set; }

        protected override void ProcessNodes(DacpNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            IsFeaturedStation = nodes.GetBool("aeRf");
        }

        #region Commands

        public async Task<bool> SendPlayCommandAsync()
        {
            DacpRequest request = new DacpRequest("/ctrl-int/1/playspec");
            request.QueryParameters["database-spec"] = DacpQueryPredicate.Is("dmap.itemid", "0x" + Database.ID.ToString("x")).ToString();
            request.QueryParameters["container-spec"] = DacpQueryPredicate.Is("dmap.itemid", "0x" + ID.ToString("x")).ToString();

            try { await Client.SendRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        #endregion
    }
}
