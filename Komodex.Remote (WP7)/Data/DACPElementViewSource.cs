using Komodex.Common;
using Komodex.DACP;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.Remote.Data
{
    public class DACPElementViewSource<T> : IDACPElementViewSource
        where T : DACPElement
    {
        protected Func<T, Task<IList>> _dataRetrievealAction;

        public DACPElementViewSource(Func<T, Task<IList>> dataRetrievalAction)
        {
            _dataRetrievealAction = dataRetrievalAction;
        }

        public bool NeedsReload { get; protected set; }

        private IList _items;
        public IList Items
        {
            get { return _items; }
            protected set
            {
                if (_items == value)
                    return;
                _items = value;
                PropertyChanged.RaiseOnUIThread(this, "Items");
            }
        }

        public async Task ReloadItemsAsync()
        {
            if (Source == null)
            {
                Items = null;
                NeedsReload = false;
                return;
            }

            try
            {
                Items = await _dataRetrievealAction(Source);
                NeedsReload = false;
            }
            catch
            {
                Items = null;
                NeedsReload = true;
            }
        }

        private T _source;
        public T Source
        {
            get { return _source; }
            set
            {
                if (_source == value)
                    return;
                _source = value;
                NeedsReload = true;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
