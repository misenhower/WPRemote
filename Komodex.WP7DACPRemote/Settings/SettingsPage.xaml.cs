using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.ComponentModel;

namespace Komodex.WP7DACPRemote.Settings
{
    public partial class SettingsPage : DACPServerBoundPhoneApplicationPage
    {
        public SettingsPage()
        {
            InitializeComponent();

            AnimationContext = LayoutRoot;

            ContentPanel.DataContext = SettingsManager.Current;
        }

        #region Overrides

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            if (lpArtistTap.ListPickerMode != ListPickerMode.Normal)
            {
                lpArtistTap.ListPickerMode = ListPickerMode.Normal;
                e.Cancel = true;
                return;
            }

            base.OnBackKeyPress(e);
        }

        #endregion
    }
}