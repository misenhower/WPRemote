using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Shell;
using Komodex.DACP;
using Komodex.DACP.Groups;
using Komodex.Common.Phone.Controls;

namespace Komodex.Remote.Pages.Browse.Audiobooks
{
    public partial class AudiobooksPage : BrowseBooksContainerBasePage
    {
        public AudiobooksPage()
        {
            InitializeComponent();

            AudiobooksViewSource = GetContainerViewSource(async c => await c.GetAudiobooksAsync());
        }

        public object AudiobooksViewSource { get; private set; }

        protected override void OnListItemTap(DACPElement item, Common.Phone.Controls.LongListSelector list, bool isPlayButton)
        {
            if (item is Audiobook)
            {
                NavigationManager.OpenAudiobookEpisodesPage((Audiobook)item);
                return;
            }
        }
    }
}