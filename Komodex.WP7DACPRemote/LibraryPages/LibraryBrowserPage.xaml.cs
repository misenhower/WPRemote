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
using Komodex.WP7DACPRemote.Controls;
using Komodex.DACP.Library;
using System.Windows.Navigation;

namespace Komodex.WP7DACPRemote.LibraryPages
{
    public partial class LibraryBrowserPage : DACPServerBoundPhoneApplicationPage
    {
        public LibraryBrowserPage()
        {
            InitializeComponent();

            AnimationContext = LayoutRoot;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Temporary items for testing
            PivotControl.Title = "TESTING";

            PivotItem pivotItem = new PivotItem();
            pivotItem.Header = "testing";

            LibraryListBox listBox = new LibraryListBox();
            listBox.IsFlatList = true;
            listBox.ListItemTemplate = LibraryListItemTemplate.PlayButtonItem;

            List<LibraryElementBase> list = new List<LibraryElementBase>();
            list.Add(new Album(null, 1, "Test 1", null, 0));
            list.Add(new Album(null, 1, "Test 2", null, 0));
            list.Add(new Album(null, 1, "Test 3", null, 0));
            list.Add(new Album(null, 1, "Test 4", null, 0));
            listBox.ItemsSource = list;

            pivotItem.Content = listBox;
            PivotControl.Items.Add(pivotItem);

        }

        #region Methods

        protected void AddPivotItem()
        {

        }

        #endregion
    }
}