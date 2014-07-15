using Komodex.Common;
using Komodex.DACP.Containers;
using Komodex.DACP.Databases;
using Komodex.DACP.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP.Groups
{
    public abstract class DACPGroup : DACPElement
    {
        public DACPGroup(DACPContainer container, DACPNodeDictionary nodes)
            : base(container.Server, nodes)
        {
            Database = container.Database;
            Container = container;
        }

        public DACPDatabase Database { get; private set; }
        public DACPContainer Container { get; private set; }

        public virtual string GroupType
        {
            get { return "albums"; }
        }

        internal virtual DACPQueryElement GroupQuery
        {
            get { return DACPQueryPredicate.Is("daap.songalbumid", PersistentID); }
        }

        internal virtual DACPQueryElement ItemQuery
        {
            get { return DACPQueryCollection.And(GroupQuery, Container.MediaKindQuery); }
        }

        internal virtual DACPQueryElement UnplayedItemQuery
        {
            get { return DACPQueryCollection.And(DACPQueryPredicate.Is("daap.songuserplaycount", 0), GroupQuery, Container.MediaKindQuery); }
        }

        #region Artwork

        public string Artwork75pxURI { get { return GetAlbumArtURI(75, 75); } }
        public string Artwork175pxURI { get { return GetAlbumArtURI(175, 175); } }

        protected internal virtual string GetAlbumArtURI(int width, int height)
        {
            width = ResolutionUtility.GetScaledPixels(width);
            height = ResolutionUtility.GetScaledPixels(height);
            string uri = "{0}/databases/{1}/groups/{2}/extra_data/artwork?mw={3}&mh={4}&group-type={5}&session-id={6}";

            object groupID;
            if (Server.IsAppleTV)
                groupID = PersistentID;
            else
                groupID = ID;

            return string.Format(uri, Server.HTTPPrefix, Database.ID, groupID, width, height, GroupType, Server.SessionID);
        }

        #endregion

        #region Items

        internal Task<List<T>> GetItemsAsync<T>(Func<DACPNodeDictionary, T> itemGenerator)
        {
            return Container.GetItemsAsync(ItemQuery, itemGenerator);
        }

        #endregion
    }
}
