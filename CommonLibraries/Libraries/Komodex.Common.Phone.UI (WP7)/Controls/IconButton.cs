using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Komodex.Common.Phone.Controls
{
    public class IconButton : Button
    {
        public IconButton()
        {
            DefaultStyleKey = typeof(IconButton);
        }

        #region ImageSource

        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register("ImageSource", typeof(ImageSource), typeof(IconButton), new PropertyMetadata(null));

        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }

        #endregion

        #region UseMultiResolutionImages

        public static readonly DependencyProperty UseMultiResolutionImagesProperty =
            DependencyProperty.Register("UseMultiResolutionImages", typeof(bool), typeof(IconButton), new PropertyMetadata(false));

        public bool UseMultiResolutionImages
        {
            get { return (bool)GetValue(UseMultiResolutionImagesProperty); }
            set { SetValue(UseMultiResolutionImagesProperty, value); }
        }

        #endregion

        #region IconTemplate

        public static readonly DependencyProperty IconTemplateProperty =
           DependencyProperty.Register("IconTemplate", typeof(ControlTemplate), typeof(IconButton), new PropertyMetadata(null));

        public ControlTemplate IconTemplate
        {
            get { return (ControlTemplate)GetValue(IconTemplateProperty); }
            set { SetValue(IconTemplateProperty, value); }
        }

        #endregion
    }
}
