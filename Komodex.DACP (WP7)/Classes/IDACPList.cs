using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.DACP
{
    public interface IDACPList : IList
    {
        bool IsGroupedList { get; }
    }
}
