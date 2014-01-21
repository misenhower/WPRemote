using Komodex.DACP.Databases;
using Komodex.DACP.Items;
using Komodex.DACP.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Komodex.DACP.Search
{
    public class SongsSearchResultSection : SearchResultSection<Song>
    {
        protected string _searchString;

        internal SongsSearchResultSection(DacpDatabase database, string searchString, Func<DacpDatabase, CancellationToken, Task<IEnumerable<Song>>> action)
            : base(database, action)
        {
            _searchString = searchString;
        }

        #region Commands

        public async Task<bool> PlaySong(Song song)
        {
            try
            {
                int index = IndexOf(song);

                DacpQueryElement query = DacpQueryCollection.And(DacpQueryPredicate.Is("dmap.itemname", _searchString), Database.MusicContainer.MediaKindQuery);
                DacpRequest request = Database.GetCueSongRequest(query, "name", index);

                await Database.Client.SendRequestAsync(request).ConfigureAwait(false);
            }
            catch { return false; }
            return true;
        }

        #endregion
    }
}
