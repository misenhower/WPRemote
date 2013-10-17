using Clarity.Phone.Controls.Animations;
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
    public abstract class BrowseContainerBasePage<T> : RemoteBasePage
        where T: DACPContainer
    {
        private bool _initialized;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!_initialized)
            {
                // Get query string parameters
                var queryString = NavigationContext.QueryString;
                _databaseID = int.Parse(queryString["databaseID"]);

                // Find the pivot control
                _pivotControl = (Pivot)FindName("PivotControl");
                _pivotControl.SelectionChanged += PivotControl_SelectionChanged;

                // Hook into the Tap event of each pivot item's list
                foreach (PivotItem pivotItem in _pivotControl.Items)
                {
                    LongListSelector list = pivotItem.Content as LongListSelector;
                    if (list == null)
                        continue;
                    list.Tap += List_Tap;
                }

                _initialized = true;
            }

            base.OnNavigatedTo(e);
        }

        protected override void OnServerChanged()
        {
            base.OnServerChanged();

            UpdateCurrentDatabase();
        }

        protected override void CurrentServer_ServerUpdate(object sender, DACP.ServerUpdateEventArgs e)
        {
            base.CurrentServer_ServerUpdate(sender, e);

            if (e.Type == ServerUpdateType.ServerConnected)
                UpdateCurrentDatabase();
        }

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            string uri = toOrFrom.OriginalString;

            if (uri.StartsWith("/Pages/Browse/"))
            {
                if (animationType == AnimationType.NavigateForwardIn || animationType == AnimationType.NavigateBackwardOut)
                    return GetContinuumAnimation(_pivotControl, animationType);
                if (_lastTappedList != null && _lastTappedItem != null)
                {
                    var contentPresenter = _lastTappedList.GetContentPresenterForItem(_lastTappedItem);
                    if (contentPresenter != null)
                        return GetContinuumAnimation(contentPresenter, animationType);
                }
            }

            return base.GetAnimation(animationType, toOrFrom);
        }

        #region CurrentDatabase

        protected int _databaseID;

        public static readonly DependencyProperty CurrentDatabaseProperty =
            DependencyProperty.Register("CurrentDatabase", typeof(DACPDatabase), typeof(BrowseContainerBasePage<T>), new PropertyMetadata(CurrentDatabasePropertyChanged));

        public DACPDatabase CurrentDatabase
        {
            get { return (DACPDatabase)GetValue(CurrentDatabaseProperty); }
            set { SetValue(CurrentDatabaseProperty, value); }
        }

        private static void CurrentDatabasePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BrowseContainerBasePage<T> page = (BrowseContainerBasePage<T>)d;
            page.OnDatabaseChanged();
        }

        protected virtual void OnDatabaseChanged()
        {
            UpdateCurrentContainer();
        }

        protected void UpdateCurrentDatabase()
        {
            if (CurrentServer == null || !CurrentServer.IsConnected)
            {
                CurrentDatabase = null;
                return;
            }

            CurrentDatabase = CurrentServer.MainDatabase;
        }

        #endregion

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

        #region Pivot Control and Items

        private Pivot _pivotControl;
        private LongListSelectorEx _lastTappedList;
        private object _lastTappedItem;

        protected virtual void PivotControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender != _pivotControl)
                return;

            GetDataForCurrentPivotItem();
        }

        protected async void GetDataForCurrentPivotItem()
        {
            PivotItem item = _pivotControl.SelectedItem as PivotItem;
            if (item == null)
                return;

            var viewSource = item.DataContext as IDACPElementViewSource;
            if (viewSource == null)
                return;

            if (viewSource.NeedsReload)
            {
                SetProgressIndicator(null, true);
                await viewSource.ReloadItemsAsync();
                ClearProgressIndicator();
            }
        }

        protected virtual void List_Tap(object sender, GestureEventArgs e)
        {
            LongListSelectorEx list = (LongListSelectorEx)sender;
            var selectedItem = list.SelectedItem as DACPElement;

            // Clear out the selected item
            list.SelectedItem = null;

            // Make sure a DACPElement was selected
            if (selectedItem == null)
                return;

            _lastTappedList = list;
            _lastTappedItem = selectedItem;

            OnListItemTap(selectedItem, list);
        }

        protected virtual void OnListItemTap(DACPElement item, LongListSelector list)
        {
            if (item is DACPItem)
            {
                DACPItem dacpItem = (DACPItem)item;
                // TODO: Send play command
            }
        }

        #endregion

        #region View Sources

        protected List<IDACPElementViewSource> _viewSources = new List<IDACPElementViewSource>();

        protected DACPElementViewSource<T> GetContainerViewSource(Func<T, Task<IList>> dataRetrievalAction)
        {
            var viewSource = new DACPElementViewSource<T>(dataRetrievalAction);
            _viewSources.Add(viewSource);
            return viewSource;
        }

        protected virtual void UpdateViewSources()
        {
            foreach (var containerViewSource in _viewSources.OfType<DACPElementViewSource<T>>())
                containerViewSource.Source = CurrentContainer;
        }

        #endregion
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
