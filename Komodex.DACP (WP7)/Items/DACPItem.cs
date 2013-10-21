using Komodex.Common;
using Komodex.DACP.Containers;
using Komodex.DACP.Databases;
using Komodex.DACP.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP.Items
{
    public abstract class DACPItem : DACPElement
    {
        public DACPItem(DACPContainer container, DACPNodeDictionary nodes)
            : base(container.Server, nodes)
        {
            Database = container.Database;
            Container = container;
        }

        public DACPDatabase Database { get; private set; }
        public DACPContainer Container { get; private set; }

        public bool IsDisabled { get; private set; }
        public string ArtistName { get; private set; }
        public TimeSpan Duration { get; private set; }
        public string FormattedDuration { get { return Duration.ToShortTimeString(true); } }
        public bool HasBeenPlayed { get; private set; }
        public int PlayCount { get; private set; }

        protected override void ProcessNodes(DACPNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            IsDisabled = nodes.GetBool("asdb");
            Duration = TimeSpan.FromMilliseconds(nodes.GetInt("astm"));
            ArtistName = nodes.GetString("asar");
            HasBeenPlayed = nodes.GetBool("ashp");
            PlayCount = nodes.GetInt("aspc");
        }

        #region Artwork

        public string Artwork75pxURI { get { return GetAlbumArtURI(75, 75); } }
        public string Artwork175pxURI { get { return GetAlbumArtURI(175, 175); } }

        protected string GetAlbumArtURI(int width, int height)
        {
            width = ResolutionUtility.GetScaledPixels(width);
            height = ResolutionUtility.GetScaledPixels(height);
            string uri = "{0}/databases/{1}/items/{2}/extra_data/artwork?mw={3}&mh={4}&session-id={5}";
            return string.Format(uri, Server.HTTPPrefix, Database.ID, ID, width, height, Server.SessionID);
        }

        #endregion

        #region Play Commands

        public async Task<bool> Play()
        {
            DACPRequest request = new DACPRequest("/ctrl-int/1/playspec");
            request.QueryParameters["database-spec"] = DACPQueryPredicate.Is("dmap.persistentid", "0x" + Database.PersistentID.ToString("x16")).ToString();
            request.QueryParameters["container-spec"] = DACPQueryPredicate.Is("dmap.persistentid", "0x" + Container.PersistentID.ToString("x16")).ToString();
            request.QueryParameters["item-spec"] = DACPQueryPredicate.Is("dmap.itemid", "0x" + ID.ToString("x8")).ToString();

            try { await Server.SubmitRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        public async Task<bool> PlayQueue(PlayQueueMode mode = PlayQueueMode.Replace)
        {
            var query = DACPQueryPredicate.Is("dmap.itemid", ID);
            DACPRequest request = Database.GetPlayQueueEditRequest("add", query, mode);

            try { await Server.SubmitRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        #endregion
    }
}
