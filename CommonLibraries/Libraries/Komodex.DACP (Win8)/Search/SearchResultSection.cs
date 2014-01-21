using Komodex.Common;
using Komodex.DACP.Containers;
using Komodex.DACP.Databases;
using Komodex.DACP.Items;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Komodex.DACP.Search
{
    public class SearchResultSection<T> : ObservableCollection<T>, ISearchResultSection
        where T : DacpElement
    {
        internal SearchResultSection(DacpDatabase database, Func<DacpDatabase, CancellationToken, Task<IEnumerable<T>>> action)
        {
            Database = database;
            _action = action;
        }

        private Func<DacpDatabase, CancellationToken, Task<IEnumerable<T>>> _action;

        private Type _resultType = typeof(T);
        public Type ResultType { get { return _resultType; } }

        public DacpDatabase Database { get; private set; }
        public bool HasItems { get { return Count > 0; } }

        public async Task SearchAsync(CancellationToken cancellationToken)
        {
            var items = await _action(Database, cancellationToken).ConfigureAwait(false);
            if (items == null)
                return;
            Utility.BeginInvokeOnUIThread(() => AddRange(items));
        }

        public void AddRange(IEnumerable<T> collection)
        {
            foreach (var item in collection)
                Items.Add(item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
