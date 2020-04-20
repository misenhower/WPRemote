using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP
{
    public enum ConnectionResult
    {
        Success,
        InvalidPIN,
        ConnectionError,
    }

    public enum PlayState
    {
        Stopped = 2,
        Paused = 3,
        Playing = 4,
        FastForward = 5,
        Rewind = 6,
    }

    public enum RepeatMode
    {
        None = 0,
        RepeatOne = 1,
        RepeatAll = 2,
    }

    public enum PlayQueueMode
    {
        Replace = 1,
        AddToQueue = 0,
        PlayNext = 3,
        Shuffle = 2,
    }

    public enum DatabaseType
    {
        Main = 1,
        Shared = 2,
        InternetRadio = 100,
        iTunesRadio = 101,
    }

    public enum ContainerType
    {
        Playlist = 0,
        Music = 6,
        Movies = 4,
        TVShows = 5,
        Podcasts = 1,
        iTunesU = 13,
        Books = 7,
        Purchased = 8,
        Rentals = 10,
        GeniusMixes = 15,
        GeniusMix = 16,
        iTunesRadio = 230,
    }

    public enum ItemPlayedState
    {
        Unplayed,
        PartiallyPlayed,
        HasBeenPlayed,
    }

    [Flags]
    public enum iTunesRadioControlState
    {
        MenuEnabled = 1,
        NextButtonEnabled = 2,
        CurrentSongFavorited = 4,
    }
}
