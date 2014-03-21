using Komodex.Common.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Komodex.Remote.Controls
{
    public class RemoteIconButton : IconButton
    {
        #region PressedIconTemplate Property

        public static readonly DependencyProperty PressedIconTemplateProperty =
           DependencyProperty.Register("PressedIconTemplate", typeof(ControlTemplate), typeof(RemoteIconButton), new PropertyMetadata(null));

        public ControlTemplate PressedIconTemplate
        {
            get { return (ControlTemplate)GetValue(PressedIconTemplateProperty); }
            set { SetValue(PressedIconTemplateProperty, value); }
        }

        #endregion
    }
}
