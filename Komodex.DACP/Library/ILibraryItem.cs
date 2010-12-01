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
using System.ComponentModel;

namespace Komodex.DACP.Library
{
    public interface ILibraryItem : INotifyPropertyChanged
    {
        string Name { get; }
        string SecondLine { get; }
        string AlbumArtURL { get; }
    }
}
