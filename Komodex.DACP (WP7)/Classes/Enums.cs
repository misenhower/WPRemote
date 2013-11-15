using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Komodex.DACP
{
    public enum ConnectionResult
    {
        Success,
        InvalidPIN,
        ConnectionError,
    }

    public enum PlayStates
    {
        Stopped = 2,
        Paused = 3,
        Playing = 4,
        FastForward = 5,
        Rewind = 6,
    }

    public enum RepeatStates
    {
        None = 0,
        RepeatOne = 1,
        RepeatAll = 2,
    }

    public enum SearchResultsType
    {
        Albums,
        Artists,
        Songs,
        Movies,
        Podcasts,
    }

    public enum PlayQueueMode
    {
        Replace = 1,
        AddToQueue = 0,
        PlayNext = 3,
        Shuffle = 2,
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
    }

    public enum ItemPlayedState
    {
        Unplayed,
        PartiallyPlayed,
        HasBeenPlayed,
    }
}
