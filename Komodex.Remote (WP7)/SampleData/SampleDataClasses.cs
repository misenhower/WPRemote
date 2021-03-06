﻿using Komodex.Common;
using Komodex.DACP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Komodex.Remote.SampleData
{
#if DEBUG
    using Komodex.Common.SampleData;

    #region DACP Elements

    public class SampleDataDACPElement : SampleDataBase
    {
        public string Name { get; set; }
    }

    #region Databases

    public class SampleDataDACPDatabase : SampleDataDACPElement
    {
        public List<SampleDataDACPContainer> Playlists { get; set; }
        public List<SampleDataGeniusMix> GeniusMixes { get; set; }
        public List<SampleDataDACPContainer> Stations { get; set; }
        public List<SampleDataDACPContainer> FeaturedStations { get; set; }
    }

    #endregion

    #region Containers

    public class SampleDataDACPContainer : SampleDataDACPElement
    {
        public int ItemCount { get; set; }
    }

    public class SampleDataGeniusMix : SampleDataDACPContainer
    {
        public string Description { get; set; }
    }

    #endregion

    #region Groups

    public class SampleDataDACPGroup : SampleDataDACPElement
    {
    }

    public class SampleDataArtist : SampleDataDACPGroup
    {
    }

    public class SampleDataAlbum : SampleDataDACPGroup
    {
        public string ArtistName { get; set; }
    }

    public class SampleDataTVShow : SampleDataDACPGroup
    {
        public int SeasonNumber { get; set; }
    }

    public class SampleDataPodcast : SampleDataDACPGroup
    {
        public string ArtistName { get; set; }
    }

    public class SampleDataiTunesUCourse : SampleDataDACPGroup
    {
        public string ArtistName { get; set; }
    }

    public class SampleDataAudiobook : SampleDataDACPGroup
    {
        public string ArtistName { get; set; }
    }

    #endregion

    #region Genres

    public class SampleDataDACPGenre:SampleDataDACPElement
    {
    }

    #endregion

    #region Composers

    public class SampleDataDACPComposer : SampleDataDACPElement
    {
    }

    #endregion

    #region Items

    public class SampleDataDACPItem : SampleDataDACPElement
    {
        public bool IsDisabled { get; set; }
        public string ArtistName { get; set; }
        public string AlbumName { get; set; }
        public TimeSpan Duration { get; set; }
        public string FormattedDuration { get { return Duration.ToShortTimeString(); } }
        public bool HasBeenPlayed { get; set; }
        public int PlayCount { get; set; }

        public string ArtistAndAlbumName
        {
            get { return Utility.JoinNonEmptyStrings(" – ", ArtistName, AlbumName); }
        }

        public ItemPlayedState PlayedState
        {
            get
            {
                if (HasBeenPlayed)
                {
                    if (PlayCount > 0)
                        return ItemPlayedState.HasBeenPlayed;
                    return ItemPlayedState.PartiallyPlayed;
                }
                return ItemPlayedState.Unplayed;
            }
        }
    }

    public class SampleDataSong : SampleDataDACPItem
    {
        public string AlbumArtistName { get; set; }

        public string SecondLine
        {
            get
            {
                if (string.IsNullOrEmpty(AlbumArtistName) || ArtistName == AlbumArtistName)
                    return FormattedDuration;
                return Utility.JoinNonEmptyStrings(" – ", ArtistName, FormattedDuration);
            }
        }
    }

    public class SampleDataMovie : SampleDataDACPItem
    {
    }

    public class SampleDataTVShowEpisode : SampleDataDACPItem
    {
        public int SeasonNumber { get; set; }
        public int EpisodeNumber { get; set; }
        public string SeriesName { get; set; }
        public bool IsHD { get; set; }

        public string SecondLine
        {
            get { return string.Join(" – ", DateTime.Now.ToShortDateString(), FormattedDuration); }
        }
    }

    public class SampleDataPodcastEpisode : SampleDataDACPItem
    {
        public string SecondLine
        {
            get { return string.Join(" – ", DateTime.Now.ToShortDateString(), FormattedDuration); }
        }
    }

    public class SampleDataiTunesUEpisode:SampleDataDACPItem
    {
        public string SecondLine
        {
            get { return string.Join(" – ", DateTime.Now.ToShortDateString(), FormattedDuration); }
        }
    }

    public class SampleDataAudiobookEpisode : SampleDataDACPItem
    {
    }

    #endregion

    #region View Sources

    public class SampleDataRemoteBasePage : SampleDataBase
    {
        public SampleDataDACPServer CurrentServer { get; set; }
    }

    public class SampleDataBrowseDatabaseBasePage : SampleDataRemoteBasePage
    {
        public SampleDataDACPDatabase CurrentDatabase { get; set; }

        public string PageTitleText { get; set; }
        public Visibility MainDatabaseVisibility { get { return Visibility.Visible; } }
        public Visibility SharedDatabaseVisibility { get { return Visibility.Collapsed; } }
        public Visibility PageTitleTextVisibility { get { return Visibility.Visible; } }
    }

    public class SampleDataBrowseContainerBasePage<T> : SampleDataBrowseDatabaseBasePage
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
        public virtual bool IsGroupedList { get { return false; } }
    }

    public class SampleDataLibraryPage : SampleDataBrowseContainerBasePage<SampleDataDACPContainer>
    {
        public SampleDataDACPElementViewSource<SampleDataArtist> ArtistsViewSource { get; set; }
        public SampleDataDACPElementViewSource<SampleDataAlbum> AlbumsViewSource { get; set; }
        public SampleDataDACPElementViewSource<SampleDataDACPGenre> GenresViewSource { get; set; }
        public SampleDataDACPElementViewSource<SampleDataDACPContainer> PlaylistsViewSource { get; set; }
    }

    public class SampleDataMusicGenrePage : SampleDataBrowseContainerBasePage<SampleDataDACPContainer>
    {
        public string CurrentGenreName { get; set; }
        public SampleDataDACPElementViewSource<SampleDataArtist> ArtistsViewSource { get; set; }
        public SampleDataDACPElementViewSource<SampleDataAlbum> AlbumsViewSource { get; set; }
        public SampleDataDACPElementViewSource<SampleDataSong> SongsViewSource { get; set; }
    }

    public class SampleDataComposersPage : SampleDataBrowseContainerBasePage<SampleDataDACPContainer>
    {
        public SampleDataDACPElementViewSource<SampleDataDACPComposer> ComposersViewSource { get; set; }
    }

    public class SampleDataComposerPage : SampleDataBrowseContainerBasePage<SampleDataDACPContainer>
    {
        public string CurrentComposerName { get; set; }
        public SampleDataDACPElementViewSource<SampleDataSong> SongsViewSource { get; set; }
    }

    public class SampleDataArtistPage : SampleDataBrowseGroupBasePage<SampleDataDACPContainer, SampleDataArtist>
    {
        public SampleDataDACPElementViewSource<SampleDataAlbum> AlbumsViewSource { get; set; }
        public SampleDataDACPElementViewSource<SampleDataSong> SongsViewSource { get; set; }
    }

    public class SampleDataAlbumPage : SampleDataBrowseGroupBasePage<SampleDataDACPContainer, SampleDataAlbum>
    {
        public SampleDataDACPElementViewSource<SampleDataSong> SongsViewSource { get; set; }
    }

    public class SampleDataPlaylistPage : SampleDataBrowseContainerBasePage<SampleDataDACPContainer>
    {
        public SampleDataPlaylistPage()
        {
            PlaylistViewSource = new SampleDataDACPElementViewSource<SampleDataDACPElement>();
            PlaylistViewSource.Items = new List<SampleDataDACPElement>();
            PlaylistViewSource.Items.AddRange(SamplePlaylists.Cast<SampleDataDACPElement>().Take(3));
            PlaylistViewSource.Items.AddRange(SampleItems.Cast<SampleDataDACPElement>());
        }

        public List<SampleDataDACPContainer> SamplePlaylists { get; set; }
        public List<SampleDataDACPItem> SampleItems { get; set; }
        public SampleDataDACPElementViewSource<SampleDataDACPElement> PlaylistViewSource { get; private set; }
    }

    public class SampleDataInternetRadioStationsPage : SampleDataBrowseContainerBasePage<SampleDataDACPContainer>
    {
        public SampleDataDACPElementViewSource<SampleDataDACPItem> StationsViewSource { get; set; }
    }

    public class SampleDataMoviesPage : SampleDataBrowseContainerBasePage<SampleDataDACPContainer>
    {
        public SampleDataDACPElementViewSource<SampleDataMovie> MoviesViewSource { get; set; }
        public SampleDataDACPElementViewSource<SampleDataMovie> UnwatchedMoviesViewSource { get; set; }
        public SampleDataDACPElementViewSource<SampleDataDACPGenre> MovieGenresViewSource { get; set; }
    }

    public class SampleDataMovieGenrePage : SampleDataBrowseContainerBasePage<SampleDataDACPContainer>
    {
        public string CurrentGenreName { get; set; }
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

    public class SampleDataiTunesUCoursesPage : SampleDataBrowseContainerBasePage<SampleDataDACPContainer>
    {
        public SampleDataDACPElementViewSource<SampleDataiTunesUCourse> CoursesViewSource { get; set; }
        public SampleDataDACPElementViewSource<SampleDataiTunesUCourse> UnplayedCoursesViewSource { get; set; }
    }

    public class SampleDataiTunesUCourseEpisodesPage : SampleDataBrowseGroupBasePage<SampleDataDACPContainer, SampleDataiTunesUCourse>
    {
        public SampleDataDACPElementViewSource<SampleDataiTunesUEpisode> EpisodesViewSource { get; set; }
        public SampleDataDACPElementViewSource<SampleDataiTunesUEpisode> UnplayedEpisodesViewSource { get; set; }
    }

    public class SampleDataAudiobooksPage : SampleDataBrowseContainerBasePage<SampleDataDACPContainer>
    {
        public SampleDataDACPElementViewSource<SampleDataAudiobook> AudiobooksViewSource { get; set; }
    }

    public class SampleDataAudiobookEpisodesPage : SampleDataBrowseGroupBasePage<SampleDataDACPContainer, SampleDataAudiobook>
    {
        public SampleDataDACPElementViewSource<SampleDataAudiobookEpisode> EpisodesViewSource { get; set; }
    }

    #endregion

    #endregion

    public class SampleDataDACPServer : SampleDataBase
    {
        public string LibraryName { get; set; }
        public string CurrentArtist { get; set; }
        public string CurrentAlbum { get; set; }
        public string CurrentSongName { get; set; }
        public int BindableVolume { get; set; }
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

        public string CurrentAppleTVKeyboardTitle { get; set; }
        public string CurrentAppleTVKeyboardSubText { get; set; }
        public string CurrentAppleTVKeyboardString { get; set; }
        public string BindableAppleTVKeyboardString { get { return CurrentAppleTVKeyboardString; } }
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
