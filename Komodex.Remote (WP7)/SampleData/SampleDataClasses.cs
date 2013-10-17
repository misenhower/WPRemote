using Komodex.Common.SampleData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.Remote.SampleData
#if DEBUG
{
    #region DACP Elements

    public class SampleDataDACPElement : SampleDataBase
    {
        public string Name { get; set; }
    }

    #region Containers

    public class SampleDataDACPContainer : SampleDataDACPElement
    {
    }

    #endregion

    #region Groups

    public class SampleDataDACPGroup : SampleDataDACPElement
    {
    }

    public class SampleDataTVShow : SampleDataDACPGroup
    {
    }

    public class SampleDataPodcast : SampleDataDACPGroup
    {
    }

    #endregion

    #region Items

    public class SampleDataDACPItem : SampleDataDACPElement
    {
        public bool IsDisabled { get; set; }
    }

    public class SampleDataMovie : SampleDataDACPItem
    {
    }

    public class SampleDataTVShowEpisode : SampleDataDACPItem
    {
    }

    public class SampleDataPodcastEpisode : SampleDataDACPItem
    {
    }

    #endregion

    #region View Sources

    public class SampleDataRemoteBasePage : SampleDataBase
    {
        public SampleDataDACPServer CurrentServer { get; set; }
    }

    public class SampleDataBrowseContainerBasePage<T> : SampleDataRemoteBasePage
        where T : SampleDataDACPContainer
    {
        public T CurrentContainer { get; set; }
    }

    public class SampleDataBrowseGroupBasePage<TContainer, TGroup> : SampleDataBrowseContainerBasePage<TContainer>
        where TContainer : SampleDataDACPContainer
        where TGroup : SampleDataDACPGroup
    {
        public TGroup CurrentGroup { get; set; }
    }

    public class SampleDataDACPElementViewSource<T> : SampleDataBase
        where T : SampleDataDACPElement
    {
        public List<T> Items { get; set; }
    }

    public class SampleDataMoviesPage : SampleDataBrowseContainerBasePage<SampleDataDACPContainer>
    {
        public SampleDataDACPElementViewSource<SampleDataMovie> MoviesViewSource { get; set; }
    }

    public class SampleDataTVShowsPage : SampleDataBrowseContainerBasePage<SampleDataDACPContainer>
    {
        public SampleDataDACPElementViewSource<SampleDataTVShow> TVShowsViewSource { get; set; }
        public SampleDataDACPElementViewSource<SampleDataTVShow> UnwatchedTVShowsViewSource { get; set; }
    }

    public class SampleDataTVShowEpisodesPage : SampleDataBrowseGroupBasePage<SampleDataDACPContainer, SampleDataTVShow>
    {
        public SampleDataDACPElementViewSource<SampleDataTVShowEpisode> EpisodesViewSource { get; set; }
        public SampleDataDACPElementViewSource<SampleDataTVShowEpisode> UnwatchedEpisodesViewSource { get; set; }
    }

    public class SampleDataPodcastsPage : SampleDataBrowseContainerBasePage<SampleDataDACPContainer>
    {
        public SampleDataDACPElementViewSource<SampleDataPodcast> PodcastsViewSource { get; set; }
        public SampleDataDACPElementViewSource<SampleDataPodcast> UnplayedPodcastsViewSource { get; set; }
    }

    public class SampleDataPodcastEpisodesPage : SampleDataBrowseGroupBasePage<SampleDataDACPContainer, SampleDataPodcast>
    {
        public SampleDataDACPElementViewSource<SampleDataPodcastEpisode> EpisodesViewSource { get; set; }
        public SampleDataDACPElementViewSource<SampleDataPodcastEpisode> UnplayedEpisodesViewSource { get; set; }
    }

    #endregion

    #endregion

    public class SampleDataDACPServer : SampleDataBase
    {
        public string LibraryName { get; set; }
        public string CurrentArtist { get; set; }
        public string CurrentAlbum { get; set; }
        public string CurrentSongName { get; set; }
        public int Volume { get; set; }
        public string CurrentTrackTimePositionString { get; set; }
        public string CurrentTrackTimeRemainingString { get; set; }
        public int CurrentTrackTimePercentage { get; set; }
        public List<SampleDataAirPlaySpeaker> Speakers { get; set; }
        public List<SampleDataPlayQueue> PlayQueues { get; set; }

        public List<SampleDataNamedItemBase> LibraryPlaylists { get; set; }
        public List<SampleDataGeniusPlaylist> LibraryGeniusMixes { get; set; }
        public List<SampleDataNamedItemGroup> LibraryArtists { get; set; }
        public List<SampleDataNamedItemGroup> LibraryAlbums { get; set; }
        public List<SampleDataNamedItemGroup> LibraryGenres { get; set; }
        public List<SampleDataNamedItemBase> LibraryPodcasts { get; set; }
        public List<SampleDataNamedItemGroup> LibraryMovies { get; set; }
        public List<SampleDataNamedItemGroup> LibraryTVShows { get; set; }
    }

    public class SampleDataNamedItemBase
    {
        public string Name { get; set; }
        public string SecondLine { get; set; }
    }

    #region Server Connection Info

    public class SampleDataServerConnectionInfoCollection : List<SampleDataServerConnectionInfo>
    {

    }

    public class SampleDataServerConnectionInfo
    {
        public string Name { get; set; }
        public bool IsAvailable { get; set; }
    }

    #endregion

    #region Groups

    public class SampleDataGroupItems<T> : List<T>
    {
        public string Key { get; set; }
    }

    public class SampleDataNamedItemGroup : SampleDataGroupItems<SampleDataNamedItemBase> { }

    #endregion

    #region Library Items

    public class SampleDataGeniusPlaylist : SampleDataNamedItemBase
    {
        public string GeniusMixDescription { get; set; }
    }

    #endregion

    #region AirPlay Speakers

    public class SampleDataAirPlaySpeaker
    {
        public string Name { get; set; }
        public bool BindableActive { get; set; }
        public bool HasVideo { get; set; }
        public bool HasPassword { get; set; }
        public int BindableVolume { get; set; }
    }

    #endregion

    #region Play Queue

    public class SampleDataPlayQueue : List<SampleDataPlayQueueItem>
    {
        public string Title1 { get; set; }
        public string Title2 { get; set; }
    }

    public class SampleDataPlayQueueItem
    {
        public string SongName { get; set; }
        public string SecondLine { get; set; }
    }

    #endregion
#endif
}
