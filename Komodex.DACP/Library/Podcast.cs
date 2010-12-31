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
using System.Collections.ObjectModel;

namespace Komodex.DACP.Library
{
    public class Podcast : LibraryGroupElementBase
    {
        private Podcast()
            : base()
        { }

        public Podcast(DACPServer server, byte[] data)
            : base(server, data)
        { }

        #region Properties

        private ObservableCollection<MediaElement> _Episodes = null;
        public ObservableCollection<MediaElement> Episodes
        {
            get { return _Episodes; }
            protected set
            {
                if (_Episodes == value)
                    return;
                _Episodes = value;
                SendPropertyChanged("Episodes");
            }
        }

        #endregion
    }
}
