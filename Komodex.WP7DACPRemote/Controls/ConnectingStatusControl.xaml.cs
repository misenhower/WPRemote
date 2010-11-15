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

namespace Komodex.WP7DACPRemote.Controls
{
    public partial class ConnectingStatusControl : UserControl
    {
        public ConnectingStatusControl()
        {
            InitializeComponent();

            LayoutRoot.DataContext = this;
        }

        public static readonly DependencyProperty ShowProgressProperty =
            DependencyProperty.Register("ShowProgress", typeof(bool), typeof(ConnectingStatusControl),
            new PropertyMetadata((bool)false));

        public bool ShowProgress
        {
            get { return (bool)GetValue(ShowProgressProperty); }
            set { SetValue(ShowProgressProperty, value); }
        }

        public static readonly DependencyProperty LibraryNameProperty =
            DependencyProperty.Register("LibraryName", typeof(string), typeof(ConnectingStatusControl),
            new PropertyMetadata((string)string.Empty));

        public string LibraryName
        {
            get { return (string)GetValue(LibraryNameProperty); }
            set { SetValue(LibraryNameProperty, value); }
        }

    }
}
