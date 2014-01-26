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
    public class DacpItem : DacpElement
    {
        public DacpItem(DacpContainer container, DacpNodeDictionary nodes)
            : base(container.Client, nodes)
        {
            Database = container.Database;
            Container = container;
        }

        public DacpDatabase Database { get; private set; }
        public DacpContainer Container { get; private set; }

        public int ContainerItemID { get; private set; }
        public bool IsDisabled { get; private set; }
        public string ArtistName { get; private set; }
        public string AlbumName { get; private set; }
        public TimeSpan Duration { get; private set; }
        public string FormattedDuration { get { return Duration.ToShortTimeString(true); } }
        public bool HasBeenPlayed { get; private set; }
        public int PlayCount { get; private set; }
        public string CodecType { get; private set; }
        public int Bitrate { get; private set; }

        protected override void ProcessNodes(DacpNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            ContainerItemID = nodes.GetInt("mcti");
            IsDisabled = nodes.GetBool("asdb");
            Duration = TimeSpan.FromMilliseconds(nodes.GetInt("astm"));
            ArtistName = nodes.GetString("asar");
            AlbumName = nodes.GetString("asal");
            HasBeenPlayed = nodes.GetBool("ashp");
            PlayCount = nodes.GetInt("aspc");
            CodecType = nodes.GetString("ascd");
            Bitrate = nodes.GetShort("asbr");
        }

        public string ArtistAndAlbumName
        {
            get { return Utility.JoinNonEmptyStrings(" – ", ArtistName, AlbumName); }
        }

        public ItemPlayedState PlayedState
        {
            get
            {
                if (HasBeenPlayed)
                {
                    if (PlayCount > 0)
                        return ItemPlayedState.HasBeenPlayed;
                    return ItemPlayedState.PartiallyPlayed;
                }
                return ItemPlayedState.Unplayed;
            }
        }

        #region Artwork

        public virtual string ArtworkUriFormat
        {
            get
            {
                string uri = "{0}/databases/{1}/items/{2}/extra_data/artwork?mw={{w}}&mh={{h}}&session-id={3}";
                return string.Format(uri, Client.HttpPrefix, Database.ID, ID, Client.SessionID);
            }
        }

        #endregion

        #region Play Commands

        public async Task<bool> Play()
        {
            DacpRequest request = new DacpRequest("/ctrl-int/1/playspec");
            if (Database.PersistentID != 0 && Container.PersistentID != 0)
            {
                request.QueryParameters["database-spec"] = DacpQueryPredicate.Is("dmap.persistentid", "0x" + Database.PersistentID.ToString("x16")).ToString();
                request.QueryParameters["container-spec"] = DacpQueryPredicate.Is("dmap.persistentid", "0x" + Container.PersistentID.ToString("x16")).ToString();
            }
            else
            {
                request.QueryParameters["database-spec"] = DacpQueryPredicate.Is("dmap.itemid", "0x" + Database.ID.ToString("x")).ToString();
                request.QueryParameters["container-spec"] = DacpQueryPredicate.Is("dmap.itemid", "0x" + Container.ID.ToString("x")).ToString();
            }
            request.QueryParameters["item-spec"] = DacpQueryPredicate.Is("dmap.itemid", "0x" + ID.ToString("x8")).ToString();

            try { await Client.SendRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        public async Task<bool> PlayQueue(PlayQueueMode mode = PlayQueueMode.Replace)
        {
            var query = DacpQueryPredicate.Is("dmap.itemid", ID);
            DacpRequest request = Database.GetPlayQueueEditRequest("add", query, mode);

            try { await Client.SendRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        #endregion
    }
}
