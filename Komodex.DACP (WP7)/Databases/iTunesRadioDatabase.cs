﻿using Komodex.DACP.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP.Databases
{
    public class iTunesRadioDatabase : DACPDatabase
    {
        public iTunesRadioDatabase(DACPServer server, DACPNodeDictionary nodes)
            : base(server, nodes)
        { }

        #region Stations

        private List<iTunesRadioStation> _stations;
        public List<iTunesRadioStation> Stations
        {
            get { return _stations; }
            private set
            {
                if (_stations == value)
                    return;
                _stations = value;
                SendPropertyChanged();
            }
        }

        private List<iTunesRadioStation> _featuredStations;
        public List<iTunesRadioStation> FeaturedStations
        {
            get { return _featuredStations; }
            private set
            {
                if (_featuredStations == value)
                    return;
                _featuredStations = value;
                SendPropertyChanged();
            }
        }

        public async Task<bool> RequestStationsAsync()
        {
            DACPRequest request = new DACPRequest("/databases/{0}/containers", ID);
            request.QueryParameters["meta"] = "dmap.itemname,dmap.itemid,com.apple.itunes.cloud-id,dmap.downloadstatus,dmap.persistentid,daap.baseplaylist,com.apple.itunes.special-playlist,com.apple.itunes.smart-playlist,com.apple.itunes.saved-genius,dmap.parentcontainerid,dmap.editcommandssupported,com.apple.itunes.jukebox-current,daap.songcontentdescription,dmap.haschildcontainers";

            try
            {
                var response = await Server.SubmitRequestAsync(request).ConfigureAwait(false);
                var containers = DACPUtility.GetItemsFromNodes(response.Nodes, n => DACPContainer.GetContainer(this, n));

                List<iTunesRadioStation> newStations = new List<iTunesRadioStation>();
                List<iTunesRadioStation> newFeaturedStations = new List<iTunesRadioStation>();

                // There is a property (aeRf) that indicates whether a station is a "featured" station, but in some cases
                // featured stations may not have this property set to "true".
                // For now, checking the index of the last "featured" station as a workaround.

                int lastFeaturedStationIndex = 0;
                var stations = containers.Where(c => c.Type == ContainerType.iTunesRadio).Select(c => (iTunesRadioStation)c).ToArray();

                for (int i = 0; i < stations.Length; i++)
                {
                    if (stations[i].IsFeaturedStation)
                        lastFeaturedStationIndex = i;
                }

                for (int i = 0; i < stations.Length; i++)
                {
                    if (i <= lastFeaturedStationIndex)
                        newFeaturedStations.Add(stations[i]);
                    else
                        newStations.Add(stations[i]);
                }

                Stations = newStations;
                FeaturedStations = newFeaturedStations;
            }
            catch (Exception e)
            {
                Stations = new List<iTunesRadioStation>();
                FeaturedStations = new List<iTunesRadioStation>();

                Server.HandleHTTPException(request, e);
                return false;
            }

            return true;
        }

        #endregion
    }
}
