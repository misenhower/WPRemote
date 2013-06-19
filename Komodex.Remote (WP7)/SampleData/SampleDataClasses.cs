using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if DEBUG
namespace Komodex.Remote.SampleData
{
    class SampleDataDACPServer
    {
        public string LibraryName { get; set; }
        public string CurrentArtist { get; set; }
        public string CurrentAlbum { get; set; }
        public string CurrentSongName { get; set; }
        public int Volume { get; set; }
        public List<SampleDataAirPlaySpeaker> Speakers { get; set; }
        public List<SampleDataPlayQueue> PlayQueues { get; set; }

        public List<SampleDataNamedItemBase> LibraryPlaylists { get; set; }
        public List<SampleDataGeniusPlaylist> LibraryGeniusMixes { get; set; }
        public List<SampleDataNamedItemGroup> LibraryArtists { get; set; }
        public List<SampleDataNamedItemGroup> LibraryAlbums { get; set; }
        public List<SampleDataNamedItemGroup> LibraryGenres { get; set; }
        public List<SampleDataNamedItemGroup> LibraryPodcasts { get; set; }
        public List<SampleDataNamedItemGroup> LibraryMovies { get; set; }
        public List<SampleDataNamedItemGroup> LibraryTVShows { get; set; }
    }

    class SampleDataNamedItemBase
    {
        public string Name { get; set; }
        public string SecondLine { get; set; }
    }

    #region Server Connection Info

    class SampleDataServerConnectionInfoCollection : List<SampleDataServerConnectionInfo>
    {

    }

    class SampleDataServerConnectionInfo
    {
        public string Name { get; set; }
        public bool IsAvailable { get; set; }
    }

    #endregion

    #region Groups

    class SampleDataGroupItems<T> : List<T>
    {
        public string Key { get; set; }
    }

    class SampleDataNamedItemGroup : SampleDataGroupItems<SampleDataNamedItemBase> { }

    #endregion

    #region Library Items

    class SampleDataPlaylist : SampleDataNamedItemBase
    {
        public List<SampleDataPlaylistSong> Songs { get; set; }
    }

    class SampleDataPlaylistSong : SampleDataNamedItemBase
    {
        public string ArtistAndAlbum { get; set; }
    }

    class SampleDataGeniusPlaylist : SampleDataNamedItemBase
    {
        public string GeniusMixDescription { get; set; }
    }

    class SampleDataArtist : SampleDataNamedItemBase
    {
        public List<SampleDataNamedItemBase> Albums { get; set; }
        public List<SampleDataArtistSong> Songs { get; set; }
    }

    class SampleDataArtistSong : SampleDataNamedItemBase
    {
        public string AlbumName { get; set; }
    }

    class SampleDataAlbum : SampleDataNamedItemBase
    {
        public string ArtistName { get; set; }
        public List<SampleDataNamedItemBase> Songs { get; set; }
    }

    class SampleDataGenre : SampleDataNamedItemBase
    {
        public List<SampleDataNamedItemGroup> Artists { get; set; }
        public List<SampleDataNamedItemGroup> Albums { get; set; }
        public List<SampleDataNamedItemGroup> Songs { get; set; }
    }

    class SampleDataPodcast : SampleDataNamedItemBase
    {
        public List<SampleDataNamedItemBase> Episodes { get; set; }
    }

    #endregion

    #region AirPlay Speakers

    class SampleDataAirPlaySpeaker
    {
        public string Name { get; set; }
        public bool BindableActive { get; set; }
        public bool HasVideo { get; set; }
        public bool HasPassword { get; set; }
        public int BindableVolume { get; set; }
    }

    #endregion

    #region Play Queue

    class SampleDataPlayQueue : List<SampleDataPlayQueueItem>
    {
        public string Title1 { get; set; }
        public string Title2 { get; set; }
    }

    class SampleDataPlayQueueItem
    {
        public string SongName { get; set; }
        public string SecondLine { get; set; }
    }

    #endregion
}
#endif
