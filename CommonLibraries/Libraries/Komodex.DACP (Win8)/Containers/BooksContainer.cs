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
    public class BooksContainer : DacpContainer
    {
        public BooksContainer(DacpDatabase database, DacpNodeDictionary nodes)
            : base(database, nodes)
        { }

        protected override int[] MediaKinds
        {
            get { return new[] { 8 }; }
        }

        internal override DacpQueryElement GroupsQuery
        {
            get { return MediaKindQuery; }
        }

        #region Audiobooks

        private List<Audiobook> _audiobooks;
        private Dictionary<int, Audiobook> _audiobooksByID;
        private int _audiobookCacheRevision;

        public async Task<List<Audiobook>> GetAudiobooksAsync()
        {
            if (_audiobooks != null && _audiobookCacheRevision == Client.CurrentLibraryUpdateNumber)
                return _audiobooks;

            _audiobooks = null;
            _audiobooksByID = null;

            _audiobooks = await GetGroupsAsync(GroupsQuery, n => new Audiobook(this, n)).ConfigureAwait(false);
            if (_audiobooks != null)
                _audiobooksByID = _audiobooks.ToDictionary(a => a.ID);

            _audiobookCacheRevision = Client.CurrentLibraryUpdateNumber;

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
            DacpQueryElement query = DacpQueryCollection.And(DacpQueryPredicate.Is("daap.songalbum", searchString), MediaKindQuery);
            DacpRequest request = GetGroupsRequest(query, false, "albums");
            request.CancellationToken = cancellationToken;
            try
            {
                var response = await Client.SendRequestAsync(request).ConfigureAwait(false);
                return DacpUtility.GetItemsFromNodes(response.Nodes, n => new Audiobook(this, n)).ToList();
            }
            catch { return null; }
        }

        #endregion
    }
}
