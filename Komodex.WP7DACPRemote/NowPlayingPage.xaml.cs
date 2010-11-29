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
using Microsoft.Phone.Shell;
using Komodex.DACP;

namespace Komodex.WP7DACPRemote
{
    public partial class NowPlayingPage : DACPServerBoundPhoneApplicationPage
    {
        public NowPlayingPage()
        {
            InitializeComponent();

            InitializeStandardTransportApplicationBar();
        }

        #region Overrides

        #endregion

        #region Methods

        #endregion

        #region Actions

        private void mnuSettings_Click(object sender, EventArgs e)
        {
            NavigationManager.OpenLibraryChooserPage();
        }

        #endregion

    }
}