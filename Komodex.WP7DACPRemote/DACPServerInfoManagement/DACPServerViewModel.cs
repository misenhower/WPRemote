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

namespace Komodex.WP7DACPRemote.DACPServerInfoManagement
{
    public class DACPServerViewModel
    {
        public DACPServerViewModel()
        {
            Items = new ObservableCollection<DACPServerInfo>();
        }

        public ObservableCollection<DACPServerInfo> Items { get; private set; }
    }
}
