using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Shell;
using Komodex.DACP;
using Komodex.Common;
using Komodex.DACP.Databases;
using System.Threading.Tasks;
using System.Collections;
using Komodex.Common.Phone.Controls;
using Komodex.DACP.Groups;
using Komodex.DACP.Containers;

namespace Komodex.Remote.LibraryPages
{
    public partial class BrowseLibraryPage : RemoteBasePage
    {
        public BrowseLibraryPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            Initialize();
        }

        #region Page Content

        protected bool _initialized;
        protected int _databaseID;
        protected ContainerType _containerType;
        protected int _playlistID;
        protected int? _groupID;

        protected Dictionary<PivotItem, Func<DACPDatabase, Task<IList>>> _pivotItemDataSourceActions = new Dictionary<PivotItem, Func<DACPDatabase, Task<IList>>>();

        protected void Initialize()
        {
            if (_initialized)
                return;

            // Retrieve parameters from query string
            var queryString = NavigationContext.QueryString;
            _databaseID = int.Parse(queryString["databaseID"]);
            _containerType = Enum<ContainerType>.Parse(queryString["containerType"], true);
            if (_containerType == ContainerType.Playlist)
                _playlistID = int.Parse(queryString["playlistID"]);
            if (queryString.ContainsKey("groupID"))
                _groupID = int.Parse(queryString["groupID"]);
            else
                _groupID = null;

            // Add pivot items
            PivotControl.Items.Clear();
            switch (_containerType)
            {
                case ContainerType.Playlist:
                    break;
                case ContainerType.Music:
                    break;
                case ContainerType.Movies:
                    AddPivotItem("movies", "MovieTemplate", async db => await db.MoviesContainer.GetMoviesAsync());
                    break;
                case ContainerType.TVShows:
                    if (!_groupID.HasValue)
                    {
                        AddPivotItem("tv shows", "TVShowTemplate", async db => await db.TVShowsContainer.GetShowsAsync());
                        AddPivotItem("unplayed", "TVShowTemplate", async db => await db.TVShowsContainer.GetUnwatchedShowsAsync());
                    }
                    else
                    {
                        AddPivotItem("episodes", "TVShowEpisodeTemplate", async db => await (await db.TVShowsContainer.GetShowByIDAsync(_groupID.Value)).GetEpisodesAsync());
                    }
                    break;
                case ContainerType.Podcasts:
                    if (!_groupID.HasValue)
                    {
                        AddPivotItem("podcasts", "PodcastTemplate", async db => await db.PodcastsContainer.GetShowsAsync());
                        AddPivotItem("unplayed", "PodcastTemplate", async db => await db.PodcastsContainer.GetUnplayedShowsAsync());
                    }
                    else
                    {
                        AddPivotItem("episodes", "PodcastEpisodeTemplate", async db => await (await db.PodcastsContainer.GetShowByIDAsync(_groupID.Value)).GetEpisodesAsync());
                    }
                    break;
                case ContainerType.iTunesU:
                    break;
                case ContainerType.Books:
                    break;
                case ContainerType.Purchased:
                    break;
                case ContainerType.Rentals:
                    break;
                case ContainerType.GeniusMixes:
                    break;
                case ContainerType.GeniusMix:
                    break;
                default:
                    break;
            }

            _initialized = true;
        }

        protected void AddPivotItem(PivotItem pivotItem, Func<DACPDatabase, Task<IList>> pivotItemDataSourceAction)
        {
            _pivotItemDataSourceActions[pivotItem] = pivotItemDataSourceAction;
            PivotControl.Items.Add(pivotItem);
        }

        protected void AddPivotItem(string header, string itemTemplateKey, Func<DACPDatabase, Task<IList>> pivotItemDataSourceAction)
        {
            AddPivotItem(GetPivotItem(header, GetList(itemTemplateKey)), pivotItemDataSourceAction);
        }

        protected PivotItem GetPivotItem(string header, object content)
        {
            PivotItem pivotItem = new PivotItem();
            pivotItem.Header = header;
            pivotItem.Content = content;

            return pivotItem;
        }

        protected LongListSelector GetList(string itemTemplateKey)
        {
            LongListSelectorEx list = new LongListSelectorEx();

            DataTemplate itemTemplate = Resources[itemTemplateKey] as DataTemplate;
            if (itemTemplate == null)
                itemTemplate = Application.Current.Resources[itemTemplateKey] as DataTemplate;
            list.ItemTemplate = itemTemplate;

            list.Tap += List_Tap;

            return list;
        }

        #endregion

        private void PivotControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetDataForPivotItem();
        }

        protected async void GetDataForPivotItem()
        {
            PivotItem pivotItem = PivotControl.SelectedItem as PivotItem;
            if (pivotItem == null || !_pivotItemDataSourceActions.ContainsKey(pivotItem))
                return;

            LongListSelectorEx list = pivotItem.Content as LongListSelectorEx;
            if (list == null)
                return;

            // Do we need data?
            if (list.ItemsSource != null)
                return;

            SetProgressIndicator(null, true);
            try
            {
                list.ItemsSource = await _pivotItemDataSourceActions[pivotItem](CurrentServer.MainDatabase);
            }
            catch
            {
                // TODO: Modify animated base page so this can work during a transition
                NavigationService.GoBack();
                return;
            }
            finally
            {
                ClearProgressIndicator();
            }
        }

        private void List_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            LongListSelector list = (LongListSelector)sender;
            var selectedItem = list.SelectedItem;

            if (selectedItem is Playlist)
            {
                Playlist playlist = (Playlist)selectedItem;
                NavigationManager.OpenBrowseLibraryPage(_databaseID, playlist.ID);
                return;
            }

            if (selectedItem is DACPGroup)
            {
                DACPGroup group = (DACPGroup)selectedItem;
                NavigationManager.OpenBrowseLibraryPage(_databaseID, _containerType, group.ID);
                return;
            }
        }
    }
}