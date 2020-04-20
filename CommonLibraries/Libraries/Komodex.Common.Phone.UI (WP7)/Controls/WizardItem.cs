using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Komodex.Common.Phone.Controls
{
    public class WizardItem : ContentControl
    {
        public WizardItem()
        {
            DefaultStyleKey = typeof(WizardItem);

            RenderTransform = new TranslateTransform();
        }

        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.Register("IsVisible", typeof(bool), typeof(WizardItem), new PropertyMetadata(false));

        public bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }
    }
}
