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
    public enum PlayStatuses
    {
        Stopped = 2,
        Paused = 3,
        Playing = 4,
    }

    public enum RepeatStatuses
    {
        None = 0,
        RepeatOne = 1,
        RepeatAll = 2,
    }
}
