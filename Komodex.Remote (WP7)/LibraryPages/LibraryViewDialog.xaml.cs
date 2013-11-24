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
using Komodex.DACP.Databases;
using Komodex.Common.Phone.Controls;

namespace Komodex.Remote.LibraryPages
{
    public partial class LibraryViewDialog : DialogUserControlBase
    {
        public LibraryViewDialog(DACPDatabase database)
        {
            InitializeComponent();

            CurrentDatabase = database;
            Loaded += LibraryViewDialog_Loaded;
        }

        public DACPDatabase CurrentDatabase { get; private set; }

        private void LibraryViewDialog_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= LibraryViewDialog_Loaded;

            var server = CurrentDatabase.Server;

            List<LibraryViewItem> items = new List<LibraryViewItem>();

            if (CurrentDatabase.MoviesContainer != null)
                items.Add(new LibraryViewItem(LocalizedStrings.BrowseMovies, "/Assets/Icons/Movies.png", () => NavigationManager.OpenMoviesPage(CurrentDatabase)));
            if (CurrentDatabase.TVShowsContainer != null)
                items.Add(new LibraryViewItem(LocalizedStrings.BrowseTVShows, "/Assets/Icons/TVShows.png", () => NavigationManager.OpenTVShowsPage(CurrentDatabase)));
            if (CurrentDatabase.PodcastsContainer != null)
                items.Add(new LibraryViewItem(LocalizedStrings.BrowsePodcasts, "/Assets/Icons/Podcasts.png", () => NavigationManager.OpenPodcastsPage(CurrentDatabase)));
            if (CurrentDatabase.iTunesUContainer != null)
                items.Add(new LibraryViewItem(LocalizedStrings.BrowseiTunesU, "/Assets/Icons/iTunesU.png", () => NavigationManager.OpeniTunesUCoursesPage(CurrentDatabase)));
            if (CurrentDatabase.BooksContainer != null)
                items.Add(new LibraryViewItem(LocalizedStrings.BrowseAudiobooks, "/Assets/Icons/Audiobooks.png", () => NavigationManager.OpenAudiobooksPage(CurrentDatabase)));
            if (CurrentDatabase.GeniusMixes != null && CurrentDatabase.GeniusMixes.Count > 0)
                items.Add(new LibraryViewItem(LocalizedStrings.BrowseGeniusMixes, "/Assets/Icons/GeniusMixes.png", () => NavigationManager.OpenGeniusMixesPage(CurrentDatabase)));

            if (CurrentDatabase == server.MainDatabase)
            {
                if (server.InternetRadioDatabase != null)
                    items.Add(new LibraryViewItem(LocalizedStrings.BrowseInternetRadio, "/Assets/Icons/InternetRadio.png", () => NavigationManager.OpenInternetRadioCategoriesPage(CurrentDatabase.Server.InternetRadioDatabase)));

                foreach (var db in CurrentDatabase.Server.SharedDatabases)
                    items.Add(new LibraryViewItem(db.Name, "/Assets/Icons/SharedLibrary.png", () => NavigationManager.OpenLibraryPage(db)));
            }

            Items = items;
        }

        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register("Items", typeof(List<LibraryViewItem>), typeof(LibraryViewDialog), new PropertyMetadata(null));

        public List<LibraryViewItem> Items
        {
            get { return (List<LibraryViewItem>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        private void List_Tap(object sender, GestureEventArgs e)
        {
            e.Handled = true;
            LongListSelector list = (LongListSelector)sender;
            LibraryViewItem item = list.SelectedItem as LibraryViewItem;
            list.SelectedItem = null;

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
