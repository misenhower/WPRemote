using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP
{
    public class DacpList<T> : List<T>, IDacpList
    {
        public DacpList(bool isGroupedList)
        {
            IsGroupedList = isGroupedList;
        }

        public DacpList(bool isGroupedList, IEnumerable<T> collection)
            : base(collection)
        {
            IsGroupedList = isGroupedList;
        }

        public bool IsGroupedList { get; private set; }
    }
}
