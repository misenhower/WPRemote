using Clarity.Phone.Controls.Animations;
using Clarity.Phone.Extensions;
using Komodex.Common;
using Komodex.Common.Phone.Controls;
using Komodex.DACP;
using Komodex.DACP.Containers;
using Komodex.DACP.Databases;
using Komodex.DACP.Items;
using Komodex.Remote.Data;
using Komodex.Remote.Localization;
using Komodex.Remote.ServerManagement;
using Microsoft.Phone.Shell;
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
    public abstract class BrowseDatabaseBasePage : RemoteBasePage
    {
        private bool _initialized;

        public BrowseDatabaseBasePage()
        {
            InitializeApplicationBar();
        }

        protected override void InitializeApplicationBar()
        {
            base.InitializeApplicationBar();

            ApplicationBar.Mode = ApplicationBarMode.Minimized;

            // Icon Buttons
            AddAppBarNowPlayingButton();
            AddApplicationBarIconButton(LocalizedStrings.BrowseLibraryAppBarButton, ResolutionUtility.GetUriWithResolutionSuffix("/Assets/Icons/Browse.png"), () => NavigationManager.OpenLibraryPage(CurrentServer.MainDatabase));
            AddApplicationBarIconButton(LocalizedStrings.SearchAppBarButton, ResolutionUtility.GetUriWithResolutionSuffix("/Assets/Icons/Search.png"), SearchAppBarButton_Click);
#if WP8
            EnableAppleTVControlButton();
#endif
        }

        private void SearchAppBarButton_Click()
        {
            var server = CurrentServer;
            var db = CurrentDatabase;

            if (server == null || db == null)
                return;

            if (db != server.MainDatabase)
            {
                // If this isn't the main database, make sure it's a shared DB (and not the iTunes Radio or Internet Radio DB).
                if (server.SharedDatabases.Contains(db))
                {
                    NavigationManager.OpenSearchPage(db);
                    return;
                }
            }

            NavigationManager.OpenSearchPage(server.MainDatabase);
        }

        protected override void UpdateBusyState()
        {
            // Do nothing
            // TODO: Remove the server-wide busy state indicator
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!_initialized)
            {
                // Get query string parameters
                var queryString = NavigationContext.QueryString;
                _databaseID = int.Parse(queryString["databaseID"]);

                // Find the pivot control
                _pivotControl = (Pivot)FindName("PivotControl");
                if (_pivotControl != null)
                {
                    _pivotControl.SelectionChanged += PivotControl_SelectionChanged;

                    // Hook into the Tap event of each pivot item's list
                    foreach (PivotItem pivotItem in _pivotControl.Items)
                    {
                        LongListSelector list = pivotItem.Content as LongListSelector;
                        if (list == null)
                            continue;
                        list.Tap += List_Tap;
                    }
                }

                _initialized = true;
            }

            base.OnNavigatedTo(e);
        }

        protected override void OnServerChanged()
        {
            base.OnServerChanged();

            ThreadUtility.RunOnUIThread(UpdateCurrentDatabase);
        }

        protected override void ServerManager_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            base.ServerManager_ConnectionStateChanged(sender, e);

            if (e.State == ServerConnectionState.Connected)
                ThreadUtility.RunOnUIThread(UpdateCurrentDatabase);
        }

        protected override void CurrentServer_LibraryUpdate(object sender, EventArgs e)
        {
            base.CurrentServer_LibraryUpdate(sender, e);

            //ThreadUtility.RunOnUIThread(UpdateCurrentDatabase);
        }

        protected virtual bool ShouldShowContinuumTransition(AnimationType animationType, Uri toOrFrom)
        {
            var uri = toOrFrom.OriginalString;
            if (uri.StartsWith("/Pages/Browse/") || uri.StartsWith("/Pages/Search/"))
                return true;
            return false;
        }

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            if (toOrFrom != null && ShouldShowContinuumTransition(animationType, toOrFrom))
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
            DependencyProperty.Register("CurrentDatabase", typeof(DACPDatabase), typeof(BrowseDatabaseBasePage), new PropertyMetadata(CurrentDatabasePropertyChanged));

        public DACPDatabase CurrentDatabase
        {
            get { return (DACPDatabase)GetValue(CurrentDatabaseProperty); }
            set { SetValue(CurrentDatabaseProperty, value); }
        }

        private static void CurrentDatabasePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BrowseDatabaseBasePage page = (BrowseDatabaseBasePage)d;
            page.OnDatabaseChanged();
        }

        protected virtual void OnDatabaseChanged()
        {
        }

        protected void UpdateCurrentDatabase()
        {
            var server = CurrentServer;
            if (server == null || !server.IsConnected)
                CurrentDatabase = null;
            else
                CurrentDatabase = GetDatabase(server);

            UpdateDBAndTitleVisibilities();
        }

        protected virtual DACPDatabase GetDatabase(DACPServer server)
        {
            return server.GetDatabaseByID(_databaseID);
        }

        #endregion

        #region Page Title Text

        public static readonly DependencyProperty HidePageTitleWhenViewingSharedDatabasesProperty =
            DependencyProperty.Register("HidePageTitleWhenViewingSharedDatabases", typeof(bool), typeof(BrowseDatabaseBasePage), new PropertyMetadata(false, HidePageTitleWhenViewingSharedDatabasesPropertyChanged));

        public bool HidePageTitleWhenViewingSharedDatabases
        {
            get { return (bool)GetValue(HidePageTitleWhenViewingSharedDatabasesProperty); }
            set { SetValue(HidePageTitleWhenViewingSharedDatabasesProperty, value); }
        }

        private static void HidePageTitleWhenViewingSharedDatabasesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BrowseDatabaseBasePage)d).UpdateDBAndTitleVisibilities();
        }

        #endregion

        #region Main/Shared Database and Title Text Visibility

        public static readonly DependencyProperty MainDatabaseVisibilityProperty =
            DependencyProperty.Register("MainDatabaseVisibility", typeof(Visibility), typeof(BrowseDatabaseBasePage), new PropertyMetadata(Visibility.Visible));

        public Visibility MainDatabaseVisibility
        {
            get { return (Visibility)GetValue(MainDatabaseVisibilityProperty); }
            set { SetValue(MainDatabaseVisibilityProperty, value); }
        }

        public static readonly DependencyProperty SharedDatabaseVisibilityProperty =
            DependencyProperty.Register("SharedDatabaseVisibility", typeof(Visibility), typeof(BrowseDatabaseBasePage), new PropertyMetadata(Visibility.Collapsed));

        public Visibility SharedDatabaseVisibility
        {
            get { return (Visibility)GetValue(SharedDatabaseVisibilityProperty); }
            set { SetValue(SharedDatabaseVisibilityProperty, value); }
        }

        public static readonly DependencyProperty PageTitleTextVisibilityProperty =
            DependencyProperty.Register("PageTitleTextVisibility", typeof(Visibility), typeof(BrowseDatabaseBasePage), new PropertyMetadata(Visibility.Visible));

        public Visibility PageTitleTextVisibility
        {
            get { return (Visibility)GetValue(PageTitleTextVisibilityProperty); }
            set { SetValue(PageTitleTextVisibilityProperty, value); }
        }

        private void UpdateDBAndTitleVisibilities()
        {
            if (CurrentServer == null || CurrentDatabase == null || (CurrentDatabase == CurrentServer.MainDatabase))
            {
                MainDatabaseVisibility = Visibility.Visible;
                SharedDatabaseVisibility = Visibility.Collapsed;
                PageTitleTextVisibility = Visibility.Visible;
            }
            else
            {
                MainDatabaseVisibility = Visibility.Collapsed;
                SharedDatabaseVisibility = Visibility.Visible;
                PageTitleTextVisibility = (HidePageTitleWhenViewingSharedDatabases) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

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
            if (_pivotControl == null)
                return;

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

            // Determine whether a play button was tapped
            bool isPlayButton = false;
            var originalSource = e.OriginalSource as DependencyObject;
            if (originalSource != null)
                isPlayButton = originalSource.GetVisualAncestors().AnyElementsWithName("PlayButton");

            OnListItemTap(selectedItem, list, isPlayButton);
        }

        protected virtual void OnListItemTap(DACPElement item, LongListSelector list, bool isPlayButton)
        {
            if (item is DACPItem)
            {
                RemoteUtility.HandleLibraryPlayTask(((DACPItem)item).Play());
            }
        }

        #endregion

        #region View Sources

        protected List<IDACPElementViewSource> _viewSources = new List<IDACPElementViewSource>();

        protected DACPElementViewSource<DACPDatabase> GetDatabaseViewSource(Func<DACPDatabase, Task<IList>> dataRetrievalAction)
        {
            var viewSource = new DACPElementViewSource<DACPDatabase>(dataRetrievalAction);
            _viewSources.Add(viewSource);
            return viewSource;
        }

        protected DACPElementViewSource<DACPDatabase> GetDatabaseViewSource(Func<DACPDatabase, IList> dataRetrievalAction)
        {
            Func<DACPDatabase, Task<IList>> action;
            action = db => TaskUtility.FromResult(dataRetrievalAction(db));
            return GetDatabaseViewSource(action);
        }

        protected virtual void UpdateViewSources()
        {
            foreach (var viewSource in _viewSources.OfType<DACPElementViewSource<DACPDatabase>>())
                viewSource.Source = CurrentDatabase;
        }

        #endregion
    }
}
