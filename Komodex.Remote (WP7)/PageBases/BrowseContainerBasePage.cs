﻿using Komodex.Common.Phone.Controls;
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
    public abstract class BrowsePodcastsContainerBasePage : BrowseContainerBasePage<PodcastsContainer> { }
    public abstract class BrowseMoviesContainerBasePage : BrowseContainerBasePage<MoviesContainer> { }
    public abstract class BrowseTVShowsContainerBasePage : BrowseContainerBasePage<TVShowsContainer> { }

    public abstract class BrowseContainerBasePage<T> : RemoteBasePage
        where T: DACPContainer
    {
        private bool _initialized;
        private List<DACPElementViewSource<T>> _viewSources = new List<DACPElementViewSource<T>>();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

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
            foreach (var viewSource in _viewSources)
                viewSource.Container = CurrentContainer;
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

        protected DACPElementViewSource<T> SetPivotItemViewSource(PivotItem pivotItem, Func<T, Task<IList>> action)
        {
            var viewSource = new DACPElementViewSource<T>(action);
            pivotItem.DataContext = viewSource;
            _viewSources.Add(viewSource);
            return viewSource;
        }

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

            DACPElementViewSource<T> viewSource = item.DataContext as DACPElementViewSource<T>;
            if (viewSource == null)
                return;

            if (viewSource.NeedsReload)
            {
                SetProgressIndicator(null, true);
                await viewSource.ReloadItems();
                ClearProgressIndicator();
            }
        }

        protected virtual void List_Tap(object sender, GestureEventArgs e)
        {
            LongListSelector list = (LongListSelector)sender;
            var selectedItem = list.SelectedItem as DACPElement;
            if (selectedItem == null)
                return;

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
    }
}
