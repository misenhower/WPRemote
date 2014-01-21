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
    public abstract class DacpGroup : DacpElement
    {
        public DacpGroup(DacpContainer container, DacpNodeDictionary nodes)
            : base(container.Client, nodes)
        {
            Database = container.Database;
            Container = container;
        }

        public DacpDatabase Database { get; private set; }
        public DacpContainer Container { get; private set; }

        public virtual string GroupType
        {
            get { return "albums"; }
        }

        internal virtual DacpQueryElement GroupQuery
        {
            get { return DacpQueryPredicate.Is("daap.songalbumid", PersistentID); }
        }

        internal virtual DacpQueryElement ItemQuery
        {
            get { return DacpQueryCollection.And(GroupQuery, Container.MediaKindQuery); }
        }

        internal virtual DacpQueryElement UnplayedItemQuery
        {
            get { return DacpQueryCollection.And(DacpQueryPredicate.Is("daap.songuserplaycount", 0), GroupQuery, Container.MediaKindQuery); }
        }

        #region Artwork

        public string Artwork75pxURI { get { return GetAlbumArtURI(75, 75); } }
        public string Artwork175pxURI { get { return GetAlbumArtURI(175, 175); } }

        protected internal virtual string GetAlbumArtURI(int width, int height)
        {
            //width = ResolutionUtility.GetScaledPixels(width);
            //height = ResolutionUtility.GetScaledPixels(height);
            string uri = "{0}/databases/{1}/groups/{2}/extra_data/artwork?mw={3}&mh={4}&group-type={5}&session-id={6}";
            return string.Format(uri, Client.HttpPrefix, Database.ID, ID, width, height, GroupType, Client.SessionID);
        }

        #endregion

        #region Items

        internal Task<List<T>> GetItemsAsync<T>(Func<DacpNodeDictionary, T> itemGenerator)
        {
            return Container.GetItemsAsync(ItemQuery, itemGenerator);
        }

        #endregion
    }
}
