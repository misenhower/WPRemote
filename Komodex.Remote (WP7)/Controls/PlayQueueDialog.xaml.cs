using Clarity.Phone.Extensions;
using Komodex.Common.Phone;
using Komodex.DACP;
using Komodex.Remote.ServerManagement;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;

namespace Komodex.Remote.Controls
{
    public partial class PlayQueueDialog : DialogUserControlBase
    {
        public PlayQueueDialog()
        {
            InitializeComponent();

#if WP7
            PlayPositionProgressBar.Background = Resources["PhoneForegroundBrush"] as Brush;
#endif

            DataContext = ServerManager.CurrentServer;
        }

        private async void PlayQueueList_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            LongListSelector list = (LongListSelector)sender;

            FrameworkElement originalSource = e.OriginalSource as FrameworkElement;
            if (originalSource == null)
                return;

            PlayQueueItem item = list.SelectedItem as PlayQueueItem;
            if (item == null)
            {
                item = originalSource.DataContext as PlayQueueItem;
                if (item == null)
                    return;
            }

            var ancestors = originalSource.GetVisualAncestors();
            bool isDeleteButton = ancestors.Any(a => (a is FrameworkElement) && ((FrameworkElement)a).Name == "DeleteButton");

            if (isDeleteButton)
            {
                // Setting the selected item to null will prevent an issue where, after deleting an item, tapping anywhere
                // in the listbox will cause an item to begin playing.  This can be caused by tapping in an "empty" margin
                // area, etc.
                list.SelectedItem = null;
                await item.Delete();
            }
            else
            {
                await item.Play();
                Hide();
            }
        }
    }
}
