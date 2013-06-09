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
    public class MultiResolutionImage : ContentPresenter
    {
        public MultiResolutionImage()
        {
            UpdateImageBinding();
        }

        private void UpdateImageBinding()
        {
            Image image = Content as Image;
            Binding binding;
            if (image == null)
            {
                image = new Image();
                Content = image;

                // Apply standard bindings
                // Stretch property
                binding = new Binding();
                binding.Mode = BindingMode.OneWay;
                binding.Path = new PropertyPath("Stretch");
                binding.Source = this;
                image.SetBinding(Image.StretchProperty, binding);
            }

            // Create/recreate image source binding
            binding = new Binding();
            binding.Mode = BindingMode.OneWay;
            binding.Path = new PropertyPath("Source");
            binding.Source = this;
            if (UseResolutionSuffix)
                binding.Converter = new MultiResolutionImageSourceConverter();
            image.SetBinding(Image.SourceProperty, binding);
        }

        #region Source

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(ImageSource), typeof(MultiResolutionImage), new PropertyMetadata(null));

        public ImageSource Source
        {
            get { return (ImageSource)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        #endregion

        #region Stretch

        public static readonly DependencyProperty StretchProperty =
            DependencyProperty.Register("Stretch", typeof(Stretch), typeof(MultiResolutionImage), new PropertyMetadata(Stretch.Uniform));

        public Stretch Stretch
        {
            get { return (Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        #endregion

        #region UseResolutionSuffix

        public static readonly DependencyProperty UseResolutionSuffixProperty =
            DependencyProperty.Register("UseResolutionSuffix", typeof(bool), typeof(MultiResolutionImage), new PropertyMetadata(true, UseResolutionSuffixChanged));

        public bool UseResolutionSuffix
        {
            get { return (bool)GetValue(UseResolutionSuffixProperty); }
            set { SetValue(UseResolutionSuffixProperty, value); }
        }

        private static void UseResolutionSuffixChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MultiResolutionImage)d;
            control.UpdateImageBinding();
        }

        #endregion
    }
}
