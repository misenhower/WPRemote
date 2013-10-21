using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.DACP
{
    public class DACPList<T> : List<T>, IDACPList
    {
        public DACPList(bool isGroupedList)
        {
            IsGroupedList = isGroupedList;
        }

        public DACPList(bool isGroupedList, IEnumerable<T> collection)
            : base(collection)
        {
            IsGroupedList = isGroupedList;
        }

        public bool IsGroupedList { get; private set; }
    }
}
