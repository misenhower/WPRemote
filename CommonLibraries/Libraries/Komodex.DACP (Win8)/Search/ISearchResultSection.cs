using Komodex.DACP.Databases;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Komodex.DACP.Search
{
    public interface ISearchResultSection : IList
    {
        Type ResultType { get; }
        DacpDatabase Database { get; }
        Task SearchAsync(CancellationToken cancellationToken);
    }
}
