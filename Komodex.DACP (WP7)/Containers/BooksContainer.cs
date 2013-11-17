using Komodex.DACP.Databases;
using Komodex.DACP.Groups;
using Komodex.DACP.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Komodex.DACP.Containers
{
    public class BooksContainer : DACPContainer
    {
        public BooksContainer(DACPDatabase database, DACPNodeDictionary nodes)
            : base(database, nodes)
        { }

        protected override int[] MediaKinds
        {
            get { return new[] { 8 }; }
        }

        internal override DACPQueryElement GroupsQuery
        {
            get { return MediaKindQuery; }
        }

        #region Audiobooks

        private List<Audiobook> _audiobooks;
        private Dictionary<int, Audiobook> _audiobooksByID;
        private int _audiobookCacheRevision;

        public async Task<List<Audiobook>> GetAudiobooksAsync()
        {
            if (_audiobooks != null && _audiobookCacheRevision == Server.CurrentLibraryUpdateNumber)
                return _audiobooks;

            _audiobooks = null;
            _audiobooksByID = null;

            _audiobooks = await GetGroupsAsync(GroupsQuery, n => new Audiobook(this, n)).ConfigureAwait(false);
            if (_audiobooks != null)
                _audiobooksByID = _audiobooks.ToDictionary(a => a.ID);

            _audiobookCacheRevision = Server.CurrentLibraryUpdateNumber;

            return _audiobooks;
        }

        public async Task<Audiobook> GetAudiobookByIDAsync(int audiobookID)
        {
            await GetAudiobooksAsync().ConfigureAwait(false);
            if (_audiobooksByID == null || !_audiobooksByID.ContainsKey(audiobookID))
                return null;

            return _audiobooksByID[audiobookID];
        }

        #endregion

        #region Audiobook Search

        public async Task<List<Audiobook>> SearchAudiobooksAsync(string searchString, CancellationToken cancellationToken)
        {
            DACPQueryElement query = DACPQueryCollection.And(DACPQueryPredicate.Is("daap.songalbum", searchString), MediaKindQuery);
            DACPRequest request = GetGroupsRequest(query, false, "albums");
            request.CancellationToken = cancellationToken;
            try
            {
                var response = await Server.SubmitRequestAsync(request).ConfigureAwait(false);
                return DACPUtility.GetItemsFromNodes(response.Nodes, n => new Audiobook(this, n)).ToList();
            }
            catch { return null; }
        }

        #endregion
    }
}
