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
    public class DACPElementViewSource<T> : INotifyPropertyChanged
        where T : DACPElement
    {
        protected Func<T, Task<IList>> _action;

        public DACPElementViewSource(Func<T, Task<IList>> dataSource)
        {
            _action = dataSource;
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

        public async Task ReloadItems()
        {
            if (Container == null)
            {
                Items = null;
                NeedsReload = false;
                return;
            }

            try
            {
                Items = await _action(Container);
                NeedsReload = false;
            }
            catch
            {
                Items = null;
                NeedsReload = true;
            }
        }

        private T _container;
        public T Container
        {
            get { return _container; }
            set
            {
                if (_container == value)
                    return;
                _container = value;
                NeedsReload = true;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
