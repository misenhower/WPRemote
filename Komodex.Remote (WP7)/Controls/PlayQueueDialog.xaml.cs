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
using Komodex.DACP;
using Clarity.Phone.Extensions;

namespace Komodex.Remote.Controls
{
    public partial class PlayQueueDialog : DialogUserControlBase
    {
        public PlayQueueDialog()
        {
            InitializeComponent();

            DataContext = ServerManager.CurrentServer;
        }

        private void PlayQueueList_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            LongListSelector list = (LongListSelector)sender;
            PlayQueueItem item = list.SelectedItem as PlayQueueItem;
            if (item == null)
                return;

            DependencyObject originalSource = e.OriginalSource as DependencyObject;
            if (originalSource == null)
                return;

            var ancestors = originalSource.GetVisualAncestors();
            bool isDeleteButton = ancestors.Any(a => (a is FrameworkElement) && ((FrameworkElement)a).Name == "DeleteButton");

            if (isDeleteButton)
                item.SendDeleteCommand();
            else
                item.SendPlayCommand();
        }
    }
}
