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
using Komodex.DACP;

namespace Komodex.Remote.Pages.Browse.Movies
{
    public partial class MoviesPage : BrowseMoviesContainerBasePage
    {
        public MoviesPage()
        {
            InitializeComponent();

            MoviesViewSource = GetContainerViewSource(async c => await c.GetMoviesAsync());
            UnwatchedMoviesViewSource = GetContainerViewSource(async c => await c.GetUnwatchedMoviesAsync());
            MovieGenresViewSource = GetContainerViewSource(async c => await c.GetGroupedGenresAsync());
        }

        public object MoviesViewSource { get; private set; }
        public object UnwatchedMoviesViewSource { get; private set; }
        public object MovieGenresViewSource { get; private set; }

        protected override void OnListItemTap(DACPElement item, Common.Phone.Controls.LongListSelector list, bool isPlayButton)
        {
            if (item is DACPGenre)
            {
                NavigationManager.OpenMovieGenrePage((DACPGenre)item);
                return;
            }

            base.OnListItemTap(item, list, isPlayButton);
        }
    }
}