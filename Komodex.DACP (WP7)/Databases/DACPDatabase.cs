using Komodex.DACP.Containers;
using Komodex.DACP.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP.Databases
{
    public class DACPDatabase : DACPElement
    {
        public DACPDatabase(DACPServer server, DACPNodeDictionary nodes)
            : base(server, nodes)
        { }

        public int DBKind { get; private set; }
        public UInt64 ServiceID { get; private set; }

        protected override void ProcessNodes(DACPNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            DBKind = nodes.GetInt("mdbk");
            ServiceID = (UInt64)nodes.GetLong("aeIM");
        }

        #region Containers

        public DACPContainer BasePlaylist { get; private set; }
        public MusicContainer MusicContainer { get; private set; }
        public PodcastsContainer PodcastsContainer { get; private set; }
        public MoviesContainer MoviesContainer { get; private set; }
        public TVShowsContainer TVShowsContainer { get; private set; }

        private List<Playlist> _playlists;
        public List<Playlist> Playlists
        {
            get { return _playlists; }
            private set
            {
                if (_playlists == value)
                    return;
                _playlists = value;
                SendPropertyChanged();
            }
        }

        internal async Task<bool> RequestContainersAsync()
        {
            DACPRequest request = new DACPRequest("/databases/{0}/containers", ID);
            request.QueryParameters["meta"] = "dmap.itemname,dmap.itemid,com.apple.itunes.cloud-id,dmap.downloadstatus,dmap.persistentid,daap.baseplaylist,com.apple.itunes.special-playlist,com.apple.itunes.smart-playlist,com.apple.itunes.saved-genius,dmap.parentcontainerid,dmap.editcommandssupported,com.apple.itunes.jukebox-current,daap.songcontentdescription";

            try
            {
                var response = await Server.SubmitRequestAsync(request).ConfigureAwait(false);
                var containers = DACPUtility.GetItemsFromNodes(response.Nodes, n => DACPContainer.GetContainer(this, n));

                Playlists = new List<Playlist>();

                foreach (DACPContainer container in containers)
                {
                    if (container.BasePlaylist)
                    {
                        BasePlaylist = container;
                        continue;
                    }

                    switch (container.Type)
                    {
                        case ContainerType.Playlist:
                            Playlists.Add((Playlist)container);
                            break;
                        case ContainerType.Music:
                            MusicContainer = (MusicContainer)container;
                            break;
                        case ContainerType.Movies:
                            MoviesContainer = (MoviesContainer)container;
                            break;
                        case ContainerType.TVShows:
                            TVShowsContainer = (TVShowsContainer)container;
                            break;
                        case ContainerType.Podcasts:
                            break;
                        case ContainerType.iTunesU:
                            break;
                        case ContainerType.Books:
                            break;
                        case ContainerType.Purchased:
                            break;
                        case ContainerType.Rentals:
                            break;
                        case ContainerType.GeniusMixes:
                            break;
                        case ContainerType.GeniusMix:
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Server.HandleHTTPException(request, e);
                return false;
            }

            return true;
        }

        #endregion

        #region Commands

        internal DACPRequest GetPlayQueueEditRequest(string command, DACPQueryElement query, PlayQueueMode mode)
        {
            DACPRequest request = new DACPRequest("/ctrl-int/1/playqueue-edit");
            request.QueryParameters["command"] = command;
            if (query != null)
                request.QueryParameters["query"] = query.ToString();
            request.QueryParameters["mode"] = ((int)mode).ToString();

            if (this != Server.MainDatabase)
                request.QueryParameters["srcdatabase"] = "0x" + PersistentID.ToString("x16");

            // TODO: Handle this separately
            if (mode == PlayQueueMode.Replace)
                request.QueryParameters["clear-previous"] = "1";

            return request;
        }

        internal DACPRequest GetCueSongRequest(DACPQueryElement query, string sort, int index)
        {
            DACPRequest request = GetCueRequest(query, sort);
            request.QueryParameters["index"] = index.ToString();
            return request;
        }

        internal DACPRequest GetCueShuffleRequest(DACPQueryElement query, string sort)
        {
            DACPRequest request = GetCueRequest(query, sort);
            request.QueryParameters["dacp.shufflestate"] = "1";
            return request;
        }

        private DACPRequest GetCueRequest(DACPQueryElement query, string sort)
        {
            DACPRequest request = new DACPRequest("/ctrl-int/1/cue");
            request.QueryParameters["command"] = "play";
            if (query != null)
                request.QueryParameters["query"] = query.ToString();
            request.QueryParameters["sort"] = sort;
            request.QueryParameters["srcdatabase"] = "0x" + PersistentID.ToString("x16");
            request.QueryParameters["clear-first"] = "1";

            return request;
        }

        #endregion
    }
}
