using Komodex.Common;
using Komodex.DACP.Containers;
using Komodex.DACP.Databases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public abstract string GroupType { get; }

        #region Artwork

        public string Artwork75pxURI { get { return GetAlbumArtURI(75); } }
        public string Artwork175pxURI { get { return GetAlbumArtURI(175); } }

        private string GetAlbumArtURI(int pixels)
        {
            pixels = ResolutionUtility.GetScaledPixels(pixels);
            string uri = "{0}/databases/{1}/groups/{2}/extra_data/artwork?mw={3}&mh={3}&group-type={4}&session-id={5}";
            return string.Format(uri, Server.HTTPPrefix, Database.ID, ID, pixels, GroupType, Server.SessionID);
        }

        #endregion
    }
}
