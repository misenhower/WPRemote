using Clarity.Phone.Controls.Animations;
using Komodex.Common;
using Komodex.Common.Phone.Controls;
using Komodex.DACP;
using Komodex.DACP.Containers;
using Komodex.DACP.Databases;
using Komodex.DACP.Genres;
using Komodex.DACP.Items;
using Komodex.Remote.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace Komodex.Remote
{
    public abstract class BrowseContainerBasePage<T> : BrowseDatabaseBasePage
        where T : DACPContainer
    {
        protected override void OnDatabaseChanged()
        {
            base.OnDatabaseChanged();

            UpdateCurrentContainer();
        }

        #region CurrentContainer

        public static readonly DependencyProperty CurrentContainerProperty =
            DependencyProperty.Register("CurrentContainer", typeof(T), typeof(BrowseContainerBasePage<T>), new PropertyMetadata(CurrentContainerPropertyChanged));

        public T CurrentContainer
        {
            get { return (T)GetValue(CurrentContainerProperty); }
            set { SetValue(CurrentContainerProperty, value); }
        }

        private static void CurrentContainerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BrowseContainerBasePage<T> page = (BrowseContainerBasePage<T>)d;
            page.OnContainerChanged();
        }

        protected virtual void OnContainerChanged()
        {
            UpdateCurrentGenre();

            UpdateViewSources();
            GetDataForCurrentPivotItem();
        }

        protected void UpdateCurrentContainer()
        {
            if (CurrentDatabase == null)
            {
                CurrentContainer = null;
                return;
            }

            CurrentContainer = GetContainer(CurrentDatabase);
        }

        protected abstract T GetContainer(DACPDatabase database);

        #endregion

        #region CurrentGenre

        public static readonly DependencyProperty CurrentGenreProperty =
            DependencyProperty.Register("CurrentGenre", typeof(DACPGenre), typeof(BrowseContainerBasePage<T>), new PropertyMetadata(CurrentGenrePropertyChanged));

        public DACPGenre CurrentGenre
        {
            get { return (DACPGenre)GetValue(CurrentGenreProperty); }
            set { SetValue(CurrentGenreProperty, value); }
        }

        private static void CurrentGenrePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BrowseContainerBasePage<T> page = (BrowseContainerBasePage<T>)d;
            page.OnGenreChanged();
        }

        protected virtual void OnGenreChanged()
        {
            UpdateViewSources();
            GetDataForCurrentPivotItem();
        }

        protected void UpdateCurrentGenre()
        {
            if (CurrentContainer == null)
            {
                CurrentGenre = null;
                return;
            }

            CurrentGenre = GetGenre(CurrentContainer);
        }

        protected virtual DACPGenre GetGenre(T container)
        {
            return null;
        }

        #endregion

        #region View Sources

        protected DACPElementViewSource<T> GetContainerViewSource(Func<T, Task<IList>> dataRetrievalAction)
        {
            var viewSource = new DACPElementViewSource<T>(dataRetrievalAction);
            _viewSources.Add(viewSource);
            return viewSource;
        }

        protected DACPElementViewSource<T> GetContainerViewSource(Func<T, IList> dataRetrievalAction)
        {
            Func<T, Task<IList>> action;
#if WP7
            action = c => TaskEx.FromResult(dataRetrievalAction(c));
#else
            action = c => Task.FromResult(dataRetrievalAction(c));
#endif
            return GetContainerViewSource(action);
        }

        protected DACPElementViewSource<DACPGenre> GetGenreViewSource(Func<DACPGenre, Task<IList>> dataRetrievalAction)
        {
            var viewSource = new DACPElementViewSource<DACPGenre>(dataRetrievalAction);
            _viewSources.Add(viewSource);
            return viewSource;
        }

        protected DACPElementViewSource<DACPGenre> GetGenreViewSource(Func<DACPGenre, IList> dataRetrievalAction)
        {
            Func<DACPGenre, Task<IList>> action;
#if WP7
            action = g => TaskEx.FromResult(dataRetrievalAction(g));
#else
            action = g => Task.FromResult(dataRetrievalAction(g));
#endif
            return GetGenreViewSource(action);
        }

        protected override void UpdateViewSources()
        {
            base.UpdateViewSources();

            foreach (var viewSource in _viewSources.OfType<DACPElementViewSource<T>>())
                viewSource.Source = CurrentContainer;

            foreach (var viewSource in _viewSources.OfType<DACPElementViewSource<DACPGenre>>())
                viewSource.Source = CurrentGenre;
        }

        #endregion
    }

    public abstract class BrowseMusicContainerBasePage : BrowseContainerBasePage<MusicContainer>
    {
        protected override MusicContainer GetContainer(DACPDatabase database)
        {
            return database.MusicContainer;
        }
    }

    public abstract class BrowsePlaylistBasePage : BrowseContainerBasePage<Playlist>
    {
    }

    public abstract class BrowsePodcastsContainerBasePage : BrowseContainerBasePage<PodcastsContainer>
    {
        protected override PodcastsContainer GetContainer(DACPDatabase database)
        {
            return database.PodcastsContainer;
        }
    }

    public abstract class BrowseMoviesContainerBasePage : BrowseContainerBasePage<MoviesContainer>
    {
        protected override MoviesContainer GetContainer(DACPDatabase database)
        {
            return database.MoviesContainer;
        }
    }

    public abstract class BrowseTVShowsContainerBasePage : BrowseContainerBasePage<TVShowsContainer>
    {
        protected override TVShowsContainer GetContainer(DACPDatabase database)
        {
            return database.TVShowsContainer;
        }
    }

    public abstract class BrowseBooksContainerBasePage : BrowseContainerBasePage<BooksContainer>
    {
        protected override BooksContainer GetContainer(DACPDatabase database)
        {
            return database.BooksContainer;
        }
    }
}
