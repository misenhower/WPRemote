using Clarity.Phone.Controls.Animations;
using Clarity.Phone.Extensions;
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
    public abstract class BrowseDatabaseBasePage : RemoteBasePage
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

            Utility.BeginInvokeOnUIThread(UpdateCurrentDatabase);
        }

        protected override void CurrentServer_ServerUpdate(object sender, DACP.ServerUpdateEventArgs e)
        {
            base.CurrentServer_ServerUpdate(sender, e);

            if (e.Type == ServerUpdateType.ServerConnected)
                Utility.BeginInvokeOnUIThread(UpdateCurrentDatabase);
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
            if (ShouldShowContinuumTransition(animationType, toOrFrom))
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
            if (CurrentServer == null || !CurrentServer.IsConnected)
            {
                CurrentDatabase = null;
                return;
            }

            CurrentDatabase = CurrentServer.MainDatabase;
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
#if WP7
            action = db => TaskEx.FromResult(dataRetrievalAction(db));
#else
            action = db => Task.FromResult(dataRetrievalAction(db));
#endif
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
