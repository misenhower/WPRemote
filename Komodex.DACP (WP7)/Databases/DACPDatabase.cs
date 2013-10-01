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
                var containers = DACPUtility.GetListFromNodes(response.Nodes, data => DACPContainer.GetContainer(this, DACPNodeDictionary.Parse(data)));

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
                           await Music.RequestArtistsAsync();
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
    }
}
