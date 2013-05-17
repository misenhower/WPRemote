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
        private UIElement _iconContainer;
        private ImageBrush _imageBrush;

        public IconButton()
        {
            DefaultStyleKey = typeof(IconButton);
        }

        #region ImageSource

        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register("ImageSource", typeof(ImageSource), typeof(IconButton), new PropertyMetadata(ImageSourceChanged));

        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }

        private static void ImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            IconButton button = (IconButton)d;
            button.UpdateImageBrush();
        }

        #endregion

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            UpdateImageBrush();
        }

        protected void UpdateImageBrush()
        {
            if (_imageBrush == null)
            {
                _iconContainer = GetTemplateChild("IconContainer") as UIElement;
                if (_iconContainer == null)
                    return;

                _imageBrush = new ImageBrush();
                _iconContainer.OpacityMask = _imageBrush;
            }

            _imageBrush.ImageSource = ImageSource;

            // Hide the icon container if the image source is null. With no image, the icon container will appear as a solid square.
            if (ImageSource == null)
                _iconContainer.Visibility = System.Windows.Visibility.Collapsed;
            else
                _iconContainer.Visibility = System.Windows.Visibility.Visible;
        }
    }
}
