using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP
{
    public interface IDacpList : IList
    {
        bool IsGroupedList { get; }
    }
}
