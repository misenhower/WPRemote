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

namespace Komodex.WP7DACPRemote.Controls
{
    public partial class LibraryListBox : UserControl
    {
        public LibraryListBox()
        {
            InitializeComponent();

            DataContext = this;
        }

        #region Properties

        public bool IsFlatList
        {
            get { return ListBox.IsFlatList; }
            set { ListBox.IsFlatList = value; }
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
                        ListBox.ItemTemplate = (DataTemplate)Resources["PlayButtonItemTemplate"];
                        break;
                    case LibraryListItemTemplate.PlayButtonItemWithSubtitle:
                        break;
                    case LibraryListItemTemplate.AlbumArtLargeItem:
                        break;
                    case LibraryListItemTemplate.AlbumArtSmallItem:
                        break;
                    case LibraryListItemTemplate.Default:
                    default:
                        ListBox.ItemTemplate = null;
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
    }

    public enum LibraryListItemTemplate
    {
        Default,
        PlayButtonItem,
        PlayButtonItemWithSubtitle,
        AlbumArtLargeItem,
        AlbumArtSmallItem,
    }
}
