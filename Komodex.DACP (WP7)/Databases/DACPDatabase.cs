using Komodex.DACP.Containers;
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
        public MusicContainer Music { get; private set; }

        internal async Task<bool> RequestContainersAsync()
        {
            DACPRequest request = new DACPRequest("/databases/{0}/containers", ID);
            request.QueryParameters["meta"] = "dmap.itemname,dmap.itemid,com.apple.itunes.cloud-id,dmap.downloadstatus,dmap.persistentid,daap.baseplaylist,com.apple.itunes.special-playlist,com.apple.itunes.smart-playlist,com.apple.itunes.saved-genius,dmap.parentcontainerid,dmap.editcommandssupported,com.apple.itunes.jukebox-current,daap.songcontentdescription";

            try
            {
                var response = await Server.SubmitRequestAsync(request).ConfigureAwait(false);
                var containers = DACPUtility.GetItemsFromNodes(response.Nodes, n => DACPContainer.GetContainer(this, n));

                foreach (DACPContainer container in containers)
                {
                    // Base Playlist
                    if (container.BasePlaylist)
                    {
                        if (BasePlaylist == null)
                            BasePlaylist = container;
                        continue;
                    }

                    // Music
                    if (container is MusicContainer)
                    {
                        if (Music == null)
                        {
                            Music = (MusicContainer)container;
                        }
                        continue;
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

        internal DACPRequest GetPlayQueueEditRequest(string command, string query, PlayQueueMode mode)
        {
            DACPRequest request = new DACPRequest("/ctrl-int/1/playqueue-edit");
            request.QueryParameters["command"] = command;
            request.QueryParameters["query"] = query;
            request.QueryParameters["mode"] = ((int)mode).ToString();

            if (this != Server.MainDatabase)
                request.QueryParameters["srcdatabase"] = "0x" + PersistentID.ToString("x16");

            // TODO: Handle this separately
            if (mode == PlayQueueMode.Replace)
                request.QueryParameters["clear-previous"] = "1";

            return request;
        }

        internal DACPRequest GetCueSongRequest(string query, string sort, int index)
        {
            DACPRequest request = GetCueRequest(query, sort);
            request.QueryParameters["index"] = index.ToString();
            return request;
        }

        internal DACPRequest GetCueShuffleRequest(string query, string sort)
        {
            DACPRequest request = GetCueRequest(query, sort);
            request.QueryParameters["dacp.shufflestate"] = "1";
            return request;
        }

        private DACPRequest GetCueRequest(string query, string sort)
        {
            DACPRequest request = new DACPRequest("/ctrl-int/1/cue");
            request.QueryParameters["command"] = "play";
            request.QueryParameters["query"] = query;
            request.QueryParameters["sort"] = sort;
            request.QueryParameters["srcdatabase"] = "0x" + PersistentID.ToString("x16");
            request.QueryParameters["clear-first"] = "1";

            return request;
        }

        #endregion
    }
}
