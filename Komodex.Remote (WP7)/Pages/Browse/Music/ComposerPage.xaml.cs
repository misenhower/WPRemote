using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Komodex.DACP.Composers;
using Komodex.Remote.Data;
using Komodex.DACP.Items;
using Komodex.DACP;

namespace Komodex.Remote.Pages.Browse.Music
{
    public partial class ComposerPage : BrowseMusicContainerBasePage
    {
        public ComposerPage()
        {
            InitializeComponent();

            var songsViewSource = new DACPElementViewSource<DACPComposer>(async c => await c.GetGroupedItemsAsync());
            _viewSources.Add(songsViewSource);
            SongsViewSource = songsViewSource;
        }

        public DACPComposer CurrentComposer { get; private set; }
        public string CurrentComposerName { get; private set; }
        public object SongsViewSource { get; private set; }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var queryString = NavigationContext.QueryString;
            CurrentComposerName = queryString["composer"];

            base.OnNavigatedTo(e);
        }

        protected override void OnContainerChanged()
        {
            UpdateCurrentComposer();

            base.OnContainerChanged();
        }

        private void UpdateCurrentComposer()
        {
            var container = CurrentContainer;
            if (container == null)
            {
                CurrentComposer = null;
                return;
            }

            CurrentComposer = new DACPComposer(container, CurrentComposerName);
            UpdateViewSources();
            GetDataForCurrentPivotItem();
        }

        protected override void UpdateViewSources()
        {
            base.UpdateViewSources();

            foreach (var viewSource in _viewSources.OfType<DACPElementViewSource<DACPComposer>>())
                viewSource.Source = CurrentComposer;
        }

        protected override void OnListItemTap(DACP.DACPElement item, Common.Phone.Controls.LongListSelector list, bool isPlayButton)
        {
            var composer = CurrentComposer;
            if (composer == null)
                return;

            if (item is DACPItem)
            {
                RemoteUtility.HandleLibraryPlayTask(composer.PlayItem((DACPItem)item));
                return;
            }
        }

        private void PlayQueueButton_Click(object sender, RoutedEventArgs e)
        {
            var composer = CurrentComposer;
            if (composer == null)
                return;

            MenuItem menuItem = (MenuItem)sender;
            DACPElement item = menuItem.DataContext as DACPElement;
            if (item == null)
                return;

            PlayQueueMode mode;
            switch (menuItem.Name)
            {
                case "PlayNextButton": mode = PlayQueueMode.PlayNext; break;
                case "AddToUpNextButton": mode = PlayQueueMode.AddToQueue; break;
                default: return;
            }

            if (item is DACPItem)
            {
                RemoteUtility.HandleLibraryQueueTask(composer.PlayItem((DACPItem)item, mode));
                return;
            }
        }
    }
}