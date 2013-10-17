using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.Remote.Data
{
    public interface IDACPElementViewSource : INotifyPropertyChanged
    {
        IList Items { get; }
        bool NeedsReload { get; }
        Task ReloadItemsAsync();
    }
}
