using Clarity.Phone.Controls.Animations;
using Komodex.Common;
using Komodex.Common.Phone.Controls;
using Komodex.DACP;
using Komodex.DACP.Containers;
using Komodex.DACP.Databases;
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
        where T: DACPContainer
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

        protected override void UpdateViewSources()
        {
            base.UpdateViewSources();

            foreach (var viewSource in _viewSources.OfType<DACPElementViewSource<T>>())
                viewSource.Source = CurrentContainer;
        }

        #endregion
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
}
