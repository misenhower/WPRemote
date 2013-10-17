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
using Komodex.Common.Phone;
using Komodex.Remote.Localization;
using Komodex.DACP;
using Komodex.Remote.ServerManagement;

namespace Komodex.Remote.LibraryPages
{
    public partial class LibraryViewDialog : DialogUserControlBase
    {
        public LibraryViewDialog()
        {
            InitializeComponent();

            Loaded += LibraryViewDialog_Loaded;
        }

        private void LibraryViewDialog_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= LibraryViewDialog_Loaded;

            // TODO: Create list dynamically from available server containers
            List<LibraryViewItem> items = new List<LibraryViewItem>();

            items.Add(new LibraryViewItem("movies", "/Assets/Icons/Videos.png", () => NavigationManager.OpenBrowseLibraryPage(0, ContainerType.Movies)));
            items.Add(new LibraryViewItem("tv shows", "/Assets/Icons/Videos.png", () => NavigationManager.OpenBrowseLibraryPage(0, ContainerType.TVShows)));
            items.Add(new LibraryViewItem("podcasts", "/Assets/Icons/Podcasts.png", () => NavigationManager.OpenPodcastsPage(ServerManager.CurrentServer.MainDatabase)));

            Items = items;
        }

        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register("Items", typeof(List<LibraryViewItem>), typeof(LibraryViewDialog), new PropertyMetadata(null));

        public List<LibraryViewItem> Items
        {
            get { return (List<LibraryViewItem>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        private void MediaButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            LibraryViewItem item = button.DataContext as LibraryViewItem;
            if (item == null)
                return;

            item.ClickAction();
        }

        public class LibraryViewItem
        {
            public LibraryViewItem(string title, string iconURI, Action clickAction)
            {
                Title = title;
                IconURI = iconURI;
                ClickAction = clickAction;
            }

            public string Title { get; private set; }
            public string IconURI { get; private set; }
            public Action ClickAction { get; private set; }
        }
    }
}
