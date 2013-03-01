using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Komodex.CommonLibrariesTestApp.Lists
{
    public class ListItemGroup<T> : ObservableCollection<T>
    {
        public ListItemGroup(string title)
        {
            Title = title;
        }

        public string Title { get; protected set; }

        public override string ToString()
        {
            return Title;
        }
    }
}
