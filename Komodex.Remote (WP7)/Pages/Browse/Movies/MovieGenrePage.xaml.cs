using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Komodex.DACP.Genres;

namespace Komodex.Remote.Pages.Browse.Movies
{
    public partial class MovieGenrePage : BrowseMoviesContainerBasePage
    {
        public MovieGenrePage()
        {
            InitializeComponent();

            MoviesViewSource = GetContainerViewSource(async c => await c.GetGenreMoviesAsync(CurrentGenreName));
        }

        public string CurrentGenreName { get; private set; }
        public object MoviesViewSource { get; private set; }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var queryString = NavigationContext.QueryString;
            CurrentGenreName = queryString["genre"];

            base.OnNavigatedTo(e);
        }
    }
}