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
using System.Collections;
using Clarity.Phone.Extensions;
using Komodex.Common;

namespace Komodex.WP7DACPRemote.Controls
{
    public partial class LibraryListBox : UserControl
    {
        public LibraryListBox()
        {
            InitializeComponent();
        }

        #region Properties

        public bool IsFlatList
        {
            get { return LongListSelector.IsFlatList; }
            set { LongListSelector.IsFlatList = value; }
        }

        private LibraryListItemTemplate _listItemTemplate;
        public LibraryListItemTemplate ListItemTemplate
        {
            get { return _listItemTemplate; }
            set
            {
                _listItemTemplate = value;

                switch (_listItemTemplate)
                {
                    case LibraryListItemTemplate.PlayButtonItem:
                        LongListSelector.ItemTemplate = (DataTemplate)Resources["PlayButtonItemTemplate"];
                        break;
                    case LibraryListItemTemplate.PlayButtonItemWithSubtitle:
                        LongListSelector.ItemTemplate = (DataTemplate)Resources["PlayButtonItemWithSubtitleTemplate"];
                        break;
                    case LibraryListItemTemplate.AlbumArtLargeItem:
                        LongListSelector.ItemTemplate = (DataTemplate)Resources["AlbumArtLargeItemTemplate"];
                        break;
                    case LibraryListItemTemplate.AlbumArtSmallItem:
                        LongListSelector.ItemTemplate = (DataTemplate)Resources["AlbumArtSmallItemTemplate"];
                        break;
                    case LibraryListItemTemplate.Default:
                    default:
                        LongListSelector.ItemTemplate = null;
                        break;
                }
            }
        }

        #endregion

        #region Dependency Properties

        #region ItemsSource

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(LibraryListBox), new PropertyMetadata(null));

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        #endregion

        #region EmptyText

        public static readonly DependencyProperty EmptyTextProperty =
            DependencyProperty.Register("EmptyText", typeof(string), typeof(LibraryListBox), new PropertyMetadata(null));

        public string EmptyText
        {
            get { return (string)GetValue(EmptyTextProperty); }
            set { SetValue(EmptyTextProperty, value); }
        }

        #endregion

        #endregion

        #region Actions/Events

        public event EventHandler<ItemTappedEventArgs> ItemTapped;

        private void LongListSelector_Tap(object sender, GestureEventArgs e)
        {
            if (sender != LongListSelector)
                return;

            // Get the selected item
            var selectedItem = LongListSelector.SelectedItem;

            // Determine if a "play" button was used to select the item
            bool isPlayButton = false;
            DependencyObject originalSource = e.OriginalSource as DependencyObject;
            if (originalSource != null)
            {
                var ancestors = originalSource.GetVisualAncestors();
                isPlayButton = ancestors.Any(a => (a is FrameworkElement) && ((FrameworkElement)a).Name == "PlayButton");
            }

            ItemTapped.RaiseOnUIThread(this, new ItemTappedEventArgs(selectedItem, isPlayButton));
        }

        #endregion
    }

    public enum LibraryListItemTemplate
    {
        Default,
        PlayButtonItem,
        PlayButtonItemWithSubtitle,
        AlbumArtLargeItem,
        AlbumArtSmallItem,
    }

    public class ItemTappedEventArgs : EventArgs
    {
        public ItemTappedEventArgs(object item, bool playButtonTapped)
        {
            Item = item;
            PlayButtonTapped = playButtonTapped;
        }

        public object Item { get; protected set; }
        public bool PlayButtonTapped { get; protected set; }
    }
}
