using Komodex.DACP.Containers;
using Komodex.DACP.Databases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.DACP.Genres
{
    public class DACPGenre : DACPElement
    {
        public DACPGenre(DACPContainer container, byte[] data)
            : base(container.Server, null)
        {
            Name = DACPUtility.GetStringValue(data);
            Database = container.Database;
            Container = container;
        }

        public DACPDatabase Database { get; private set; }
        public DACPContainer Container { get; private set; }

        protected override void ProcessNodes(DACPNodeDictionary nodes)
        {
        }
    }
}
