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
using System.Collections.ObjectModel;
using Komodex.Common.Phone;

namespace Komodex.CommonLibrariesTestApp.Lists
{
    public partial class ListTests : PhoneApplicationBasePage
    {

        public ListTests()
        {
            InitializeComponent();
        }

        #region Flat List

        protected int _flatListItemNumber = 1;
        protected ObservableCollection<ListItem> _flatListItems = new ObservableCollection<ListItem>();

        private void SetFlatItemsSource(object sender, RoutedEventArgs e)
        {
            flatList.ItemsSource = _flatListItems;
        }

        private void ClearFlatItemsSource(object sender, RoutedEventArgs e)
        {
            flatList.ItemsSource = null;
        }

        private void AddFlatItem(object sender, RoutedEventArgs e)
        {
            _flatListItems.Add(new ListItem("Item " + _flatListItemNumber++));
        }

        private void RemoveFlatItem(object sender, RoutedEventArgs e)
        {
            if (_flatListItems.Count > 0)
                _flatListItems.RemoveAt(0);
        }

        #endregion

        #region Grouped List

        protected int _groupNumber = 1;
        protected int _groupItemNumber = 1;
        protected ObservableCollection<ListItemGroup<ListItem>> _groupedListItems = new ObservableCollection<ListItemGroup<ListItem>>();

        private void SetGroupedItemsSource(object sender, RoutedEventArgs e)
        {
            groupedList.ItemsSource = _groupedListItems;
        }

        private void ClearGroupedItemsSource(object sender, RoutedEventArgs e)
        {
            groupedList.ItemsSource = null;
        }

        private void AddGroup(object sender, RoutedEventArgs e)
        {
            _groupedListItems.Add(new ListItemGroup<ListItem>("Group " + _groupNumber++));
        }

        private void RemoveGroup(object sender, RoutedEventArgs e)
        {
            if (_groupedListItems.Count > 0)
                _groupedListItems.RemoveAt(0);
        }

        private void AddGroupItem(object sender, RoutedEventArgs e)
        {
            if (_groupedListItems.Count > 0)
                _groupedListItems[_groupedListItems.Count - 1].Add(new ListItem("Item " + _groupItemNumber++));
        }

        private void RemoveGroupItem(object sender, RoutedEventArgs e)
        {
            if (_groupedListItems.Count > 0 && _groupedListItems[0].Count > 0)
                _groupedListItems[0].RemoveAt(0);
        }

        #endregion
    }
}