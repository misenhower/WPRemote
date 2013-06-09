using Komodex.Common.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Komodex.Common.Phone.Controls
{
    public class MultiResolutionImageMask : Control
    {
        private UIElement _imageContainer;
        private ImageBrush _imageBrush;
        private MultiResolutionImageSourceConverter _converter = new MultiResolutionImageSourceConverter();

        public MultiResolutionImageMask()
        {
            DefaultStyleKey = typeof(MultiResolutionImageMask);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _imageContainer = null;
            UpdateImage();
        }

        private void UpdateImage()
        {
            if (_imageContainer == null)
            {
                _imageContainer = GetTemplateChild("ImageContainer") as UIElement;
                if (_imageContainer == null)
                    return;

                _imageBrush = new ImageBrush();
                _imageContainer.OpacityMask = _imageBrush;
            }

            if (UseResolutionSuffix)
                _imageBrush.ImageSource = _converter.Convert(Source, typeof(ImageSource), null, null) as ImageSource;
            else
                _imageBrush.ImageSource = Source;
            _imageBrush.Stretch = Stretch;

            if (Source == null)
                _imageContainer.Visibility = Visibility.Collapsed;
            else
                _imageContainer.Visibility = Visibility.Visible;
        }

        private static void PropertyUpdated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MultiResolutionImageMask)d;
            control.UpdateImage();
        }

        #region Source

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(ImageSource), typeof(MultiResolutionImageMask), new PropertyMetadata(PropertyUpdated));

        public ImageSource Source
        {
            get { return (ImageSource)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        #endregion

        #region Stretch

        public static readonly DependencyProperty StretchProperty =
            DependencyProperty.Register("Stretch", typeof(Stretch), typeof(MultiResolutionImageMask), new PropertyMetadata(Stretch.Uniform, PropertyUpdated));

        public Stretch Stretch
        {
            get { return (Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        #endregion

        #region UseResolutionSuffix

        public static readonly DependencyProperty UseResolutionSuffixProperty =
            DependencyProperty.Register("UseResolutionSuffix", typeof(bool), typeof(MultiResolutionImageMask), new PropertyMetadata(true, PropertyUpdated));

        public bool UseResolutionSuffix
        {
            get { return (bool)GetValue(UseResolutionSuffixProperty); }
            set { SetValue(UseResolutionSuffixProperty, value); }
        }

        #endregion
    }
}
