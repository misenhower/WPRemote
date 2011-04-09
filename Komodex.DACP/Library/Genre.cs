using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Komodex.DACP.Library
{
    public class Genre : LibraryElementBase
    {
        public Genre(DACPServer server, byte[] data)
            : base() // Genres are a bit different so don't call the base(server, data) constructor
        {
            Server = server;
            Name = data.GetStringValue();
        }
    }
}
