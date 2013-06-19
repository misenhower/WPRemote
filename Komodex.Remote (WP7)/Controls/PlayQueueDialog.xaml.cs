using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Komodex.Common.Phone;
using Komodex.Remote.ServerManagement;

namespace Komodex.Remote.Controls
{
    public partial class PlayQueueDialog : DialogUserControlBase
    {
        public PlayQueueDialog()
        {
            InitializeComponent();

            DataContext = ServerManager.CurrentServer;
        }
    }
}
