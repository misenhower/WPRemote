using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Komodex.Remote.Pages.Browse.Movies
{
    public partial class MoviesPage : BrowseMoviesContainerBasePage
    {
        public MoviesPage()
        {
            InitializeComponent();

            MoviesViewSource = GetContainerViewSource(async c => await c.GetMoviesAsync());
        }

        public object MoviesViewSource { get; private set; }
    }
}