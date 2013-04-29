using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Komodex.Remote.ServerManagement
{
    [XmlType("Servers")]
    public class ServerConnectionInfoCollection : ObservableCollection<ServerConnectionInfo>
    {
    }
}
