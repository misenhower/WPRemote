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
using Komodex.DACP;

namespace Komodex.Remote.Pages.Browse.Music
{
    public partial class ComposersPage : BrowseMusicContainerBasePage
    {
        public ComposersPage()
        {
            InitializeComponent();

            ComposersViewSource = GetContainerViewSource(async c => await c.GetGroupedComposersAsync());
        }

        public object ComposersViewSource { get; private set; }

        protected override void OnListItemTap(DACP.DACPElement item, Common.Phone.Controls.LongListSelector list, bool isPlayButton)
        {
            if (item is DACPComposer)
            {
                if (isPlayButton)
                    RemoteUtility.HandleLibraryPlayTask(((DACPComposer)item).Play());
                else
                    NavigationManager.OpenComposerPage((DACPComposer)item);
                return;
            }
        }

        private void PlayQueueButton_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            DACPComposer composer = menuItem.DataContext as DACPComposer;
            if (composer == null)
                return;

            PlayQueueMode mode;
            switch (menuItem.Name)
            {
                case "PlayNextButton": mode = PlayQueueMode.PlayNext; break;
                case "AddToUpNextButton": mode = PlayQueueMode.AddToQueue; break;
                default: return;
            }

            RemoteUtility.HandleLibraryQueueTask(composer.Play(mode));
        }
    }
}
