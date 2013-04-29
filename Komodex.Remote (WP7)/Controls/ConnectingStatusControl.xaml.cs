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
using Komodex.DACP;
using Komodex.Remote.Localization;

namespace Komodex.Remote.Controls
{
    public partial class ConnectingStatusControl : UserControl
    {
        public ConnectingStatusControl()
        {
            InitializeComponent();

            LayoutRoot.DataContext = this;
        }

        #region Properties

        public bool ShowProgressBar
        {
            get
            {
                return progressBar.IsIndeterminate;
            }
            set
            {
                progressBar.IsIndeterminate = value;
                progressBar.Visibility = (value) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }
        }

        public string LibraryName
        {
            get { return tbLibraryName.Text; }
            set { tbLibraryName.Text = value; }
        }

        public string LibraryConnectionText
        {
            get { return tbLibraryConnectionText.Text; }
            set { tbLibraryConnectionText.Text = value; }
        }

        #endregion

        #region Button

        public string ButtonText
        {
            get { return btnAction.Content as string; }
            set { btnAction.Content = value; }
        }

        public event RoutedEventHandler ButtonClick;

        private void btnAction_Click(object sender, RoutedEventArgs e)
        {
            if (ButtonClick != null)
                ButtonClick(sender, e);
        }

        #endregion
    }
}
