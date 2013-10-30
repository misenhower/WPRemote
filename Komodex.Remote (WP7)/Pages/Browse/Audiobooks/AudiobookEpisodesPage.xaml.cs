using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Komodex.Remote.Pages.Browse.Audiobooks
{
    public partial class AudiobookEpisodesPage : BrowseAudiobookBasePage
    {
        public AudiobookEpisodesPage()
        {
            InitializeComponent();

            EpisodesViewSource = GetGroupViewSource(async g => await g.GetEpisodesAsync());
        }

        public object EpisodesViewSource { get; private set; }
    }
}