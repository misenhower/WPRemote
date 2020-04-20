using Komodex.Common;
using Komodex.DACP.Databases;
using Komodex.DACP.Genres;
using Komodex.DACP.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP.Containers
{
    public class DacpContainer : DacpElement
    {
        public DacpContainer(DacpDatabase database, DacpNodeDictionary nodes)
            : base(database.Client, nodes)
        {
            Database = database;
        }

        public DacpDatabase Database { get; private set; }

        public bool BasePlaylist { get; private set; }
        public ContainerType Type { get; private set; }
        public int ItemCount { get; private set; }
        public int ParentContainerID { get; private set; }

        private bool _hasChildContainers;
        public bool HasChildContainers
        {
            get { return _hasChildContainers || Database.Playlists.Any(pl => pl.ParentContainerID == this.ID); }
            private set { _hasChildContainers = value; }
        }

        protected override void ProcessNodes(DacpNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            BasePlaylist = nodes.GetBool("abpl");
            Type = (ContainerType)nodes.GetByte("aePS");
            ItemCount = nodes.GetInt("mimc");
            ParentContainerID = nodes.GetInt("mpco");
            try
            {
                HasChildContainers = (nodes.GetInt("f\uFFFDch") > 0);
            }
            catch { }
        }

        public static DacpContainer GetContainer(DacpDatabase database, DacpNodeDictionary nodes)
        {
            // Base Playlist
            if (nodes.GetBool("abpl"))
                return new DacpContainer(database, nodes);

            // Special playlist type
            ContainerType type = (ContainerType)nodes.GetByte("aePS");
            switch (type)
            {
                case ContainerType.Playlist:
                    return new Playlist(database, nodes);
                case ContainerType.Podcasts:
                    return new PodcastsContainer(database, nodes);
                case ContainerType.Movies:
                    return new MoviesContainer(database, nodes);
                case ContainerType.TVShows:
                    return new TVShowsContainer(database, nodes);
                case ContainerType.Music:
                    return new MusicContainer(database, nodes);
                case ContainerType.iTunesU:
                    return new iTunesUContainer(database, nodes);
                case ContainerType.Books:
                    return new BooksContainer(database, nodes);
                case ContainerType.GeniusMix:
                    return new GeniusMix(database, nodes);
                case ContainerType.iTunesRadio:
                    return new iTunesRadioStation(database, nodes);
            }

            return new DacpContainer(database, nodes);
        }

        protected virtual int[] MediaKinds
        {
            get { return null; }
        }

        internal virtual DacpQueryElement MediaKindQuery
        {
            get
            {
                if (Client.ServerSupportsPlayQueue)
                    return DacpQueryCollection.Or(MediaKinds.Select(i => DacpQueryPredicate.Is("com.apple.itunes.extended-media-kind", i)).ToArray());
                return DacpQueryCollection.Or(MediaKinds.Select(i => DacpQueryPredicate.Is("com.apple.itunes.mediakind", i)).ToArray());
            }
        }

        internal virtual DacpQueryElement GroupsQuery
        {
            get { return DacpQueryCollection.And(DacpQueryPredicate.IsNotEmpty("daap.songalbum"), MediaKindQuery); }
        }

        protected virtual string ItemsMeta
        {
            get { return "dmap.itemname,dmap.itemid,daap.songartist,daap.songalbumartist,daap.songalbum,com.apple.itunes.cloud-id,dmap.containeritemid,com.apple.itunes.has-video,com.apple.itunes.itms-songid,com.apple.itunes.extended-media-kind,dmap.downloadstatus,daap.songdisabled,com.apple.itunes.cloud-id,daap.songartistid,daap.songalbumid,dmap.persistentid,dmap.downloadstatus,daap.songalbum,daap.songtime,daap.songhasbeenplayed,daap.songuserplaycount"; }
        }

        protected virtual string GroupsMeta
        {
            get { return "dmap.itemname,dmap.itemid,dmap.persistentid,daap.songartist,daap.songdatereleased,dmap.itemcount,daap.songtime,dmap.persistentid,daap.songartistid"; }
        }

        #region Artwork

        public virtual string ArtworkUriFormat
        {
            get
            {
                string uri = "{0}/databases/{1}/containers/{2}/extra_data/artwork?mw={{w}}&mh={{h}}&session-id={3}";
                return string.Format(uri, Client.HttpPrefix, Database.ID, ID, Client.SessionID);
            }
        }

        #endregion

        #region Requests

        internal DacpRequest GetGroupsRequest(DacpQueryElement query, bool includeSortHeaders = false, string groupType = "albums")
        {
            DacpRequest request = new DacpRequest("/databases/{0}/groups", Database.ID);
            request.QueryParameters["meta"] = GroupsMeta;
            request.QueryParameters["type"] = "music";
            request.QueryParameters["group-type"] = groupType;
            if (includeSortHeaders)
                request.QueryParameters["include-sort-headers"] = "1";
            request.QueryParameters["sort"] = "album";
            if (query != null)
                request.QueryParameters["query"] = query.ToString();

            return request;
        }

        internal DacpRequest GetContainerItemsRequest()
        {
            DacpRequest request = new DacpRequest("/databases/{0}/containers/{1}/items", Database.ID, ID);
            request.QueryParameters["meta"] = ItemsMeta;
            request.QueryParameters["type"] = "music";

            return request;
        }

        internal DacpRequest GetItemsRequest(DacpQueryElement query, string sort = "album", bool requestSortHeaders = false)
        {
            // Apple's Remote uses the Base Playlist ID here rather than the actual container ID.
            DacpRequest request = new DacpRequest("/databases/{0}/containers/{1}/items", Database.ID, Database.BasePlaylist.ID);
            request.QueryParameters["meta"] = ItemsMeta;
            request.QueryParameters["type"] = "music";
            request.QueryParameters["sort"] = sort;
            if (requestSortHeaders)
                request.QueryParameters["include-sort-headers"] = "1";
            if (query != null)
                request.QueryParameters["query"] = query.ToString();

            return request;
        }

        internal DacpRequest GetGenresRequest(bool requestSortHeaders = false)
        {
            DacpRequest request = new DacpRequest("/databases/{0}/browse/genres", Database.ID);
            DacpQueryElement query = DacpQueryCollection.And(DacpQueryPredicate.IsNotEmpty("daap.songgenre"), MediaKindQuery);
            request.QueryParameters["filter"] = query.ToString();
            if (requestSortHeaders)
                request.QueryParameters["include-sort-headers"] = "1";
            return request;
        }

        #endregion

        #region Items/Groups

        internal Task<List<T>> GetItemsAsync<T>(DacpQueryElement query, Func<DacpNodeDictionary, T> itemGenerator)
        {
            DacpRequest request = GetItemsRequest(query);
            return Client.GetListAsync(request, itemGenerator);
        }

        internal Task<List<T>> GetGroupsAsync<T>(DacpQueryElement query, Func<DacpNodeDictionary, T> itemGenerator)
        {
            DacpRequest request = GetGroupsRequest(query);
            return Client.GetListAsync(request, itemGenerator);
        }

        #endregion

        #region Genres

        private IDacpList _groupedGenres;

        public async Task<IDacpList> GetGroupedGenresAsync()
        {
            if (_groupedGenres != null)
                return _groupedGenres;

            DacpRequest request = GetGenresRequest(true);
            _groupedGenres = await Client.GetAlphaGroupedListAsync(request, d => new DacpGenre(this, d), "abgn").ConfigureAwait(false);
            return _groupedGenres;
        }

        #endregion
    }
}
