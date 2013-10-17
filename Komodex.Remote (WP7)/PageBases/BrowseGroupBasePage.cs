﻿using Komodex.DACP.Containers;
using Komodex.DACP.Databases;
using Komodex.DACP.Groups;
using Komodex.Remote.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace Komodex.Remote
{
    public abstract class BrowseGroupBasePage<TContainer, TGroup> : BrowseContainerBasePage<TContainer>
        where TContainer : DACPContainer
        where TGroup : DACPGroup
    {
        private bool _initialized;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!_initialized)
            {
                // Get query string parameters
                var queryString = NavigationContext.QueryString;
                _groupID = int.Parse(queryString["groupID"]);

                _initialized = true;
            }

            base.OnNavigatedTo(e);
        }

        protected override void OnContainerChanged()
        {
            base.OnContainerChanged();

            UpdateCurrentGroup();
        }

        #region CurrentGroup

        protected int _groupID;

        public static readonly DependencyProperty CurrentGroupProperty =
            DependencyProperty.Register("CurrentGroup", typeof(TGroup), typeof(BrowseGroupBasePage<TContainer,TGroup>), new PropertyMetadata(CurrentGroupPropertyChanged));

        public TGroup CurrentGroup
        {
            get { return (TGroup)GetValue(CurrentGroupProperty); }
            set { SetValue(CurrentGroupProperty, value); }
        }

        private static void CurrentGroupPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BrowseGroupBasePage<TContainer, TGroup> page = (BrowseGroupBasePage<TContainer, TGroup>)d;
            page.OnGroupChanged();
        }

        protected virtual void OnGroupChanged()
        {
            UpdateViewSources();
            GetDataForCurrentPivotItem();
        }

        protected async void UpdateCurrentGroup()
        {
            if (CurrentContainer == null)
            {
                CurrentGroup = null;
                return;
            }

            CurrentGroup = await GetGroup(CurrentContainer, _groupID);
        }

        protected abstract Task<TGroup> GetGroup(TContainer container, int groupID);

        #endregion

        #region View Sources

        protected DACPElementViewSource<TGroup> GetGroupViewSource(Func<TGroup, Task<IList>> dataRetrievalAction)
        {
            var viewSource = new DACPElementViewSource<TGroup>(dataRetrievalAction);
            _viewSources.Add(viewSource);
            return viewSource;
        }

        protected override void UpdateViewSources()
        {
            base.UpdateViewSources();

            foreach (var groupViewSource in _viewSources.OfType<DACPElementViewSource<TGroup>>())
                groupViewSource.Source = CurrentGroup;
        }

        #endregion
    }

    public abstract class BrowsePodcastBasePage : BrowseGroupBasePage<PodcastsContainer, Podcast>
    {
        protected override PodcastsContainer GetContainer(DACPDatabase database)
        {
            return database.PodcastsContainer;
        }

        protected override Task<Podcast> GetGroup(PodcastsContainer container, int groupID)
        {
            return container.GetShowByIDAsync(groupID);
        }
    }

    public abstract class BrowseTVShowBasePage : BrowseGroupBasePage<TVShowsContainer, TVShow>
    {
        protected override TVShowsContainer GetContainer(DACPDatabase database)
        {
            return database.TVShowsContainer;
        }

        protected override Task<TVShow> GetGroup(TVShowsContainer container, int groupID)
        {
            return container.GetShowByIDAsync(groupID);
        }
    }
}
