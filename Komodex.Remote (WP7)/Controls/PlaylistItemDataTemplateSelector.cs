using Komodex.Common.Controls;
using Komodex.DACP.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Komodex.Remote.Controls
{
    public class PlaylistItemDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate PlaylistTemplate { get; set; }
        public DataTemplate ItemTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
#if DEBUG
            if (item is SampleData.SampleDataDACPContainer)
                return PlaylistTemplate;
#endif

            if (item is Playlist)
                return PlaylistTemplate;
            return ItemTemplate;
        }
    }
}
