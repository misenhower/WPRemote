﻿using System;
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

namespace Komodex.WP7DACPRemote.LibraryPages
{
    public partial class SearchPage : DACPServerBoundPhoneApplicationPage
    {
        public SearchPage()
        {
            InitializeComponent();

            AnimationContext = LayoutRoot;
        }

        private void tbSearchString_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;

            DACPServer.GetSearchResults(textBox.Text);
        }
    }
}