using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace Komodex.Common
{
    public class ObservableCollectionEx<T> : ObservableCollection<T>
    {
        public ObservableCollectionEx()
        { }

        public ObservableCollectionEx(IEnumerable<T> collection)
            : base(collection)
        { }

        public ObservableCollectionEx(List<T> list)
            : base(list)
        { }

        public void AddRange(IEnumerable<T> collection)
        {
            foreach (var item in collection)
                Items.Add(item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void ReplaceContents(IEnumerable<T> collection)
        {
            Items.Clear();
            foreach (var item in collection)
                Items.Add(item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
