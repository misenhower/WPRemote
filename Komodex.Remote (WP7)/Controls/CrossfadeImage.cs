using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Komodex.Remote.Controls
{
    public class CrossfadeImage : ContentPresenter
    {
        private Image _image1;
        private Image _image2;

        private bool _image2Active;

        private Storyboard _currentStoryboard;

        public CrossfadeImage()
        {
            Grid grid = new Grid();

            _image1 = new Image();
            _image1.Stretch = Stretch.UniformToFill;
            _image1.HorizontalAlignment = HorizontalAlignment.Center;
            grid.Children.Add(_image1);

            _image2 = new Image();
            _image2.Stretch = Stretch.UniformToFill;
            _image2.HorizontalAlignment = HorizontalAlignment.Center;
            grid.Children.Add(_image2);

            Content = grid;
        }

        #region Delay Property

        public static readonly DependencyProperty DelayProperty =
            DependencyProperty.Register("Delay", typeof(int), typeof(CrossfadeImage), new PropertyMetadata(1500));

        public int Delay
        {
            get { return (int)GetValue(DelayProperty); }
            set { SetValue(DelayProperty, value); }
        }

        #endregion

        public void SetImageSource(ImageSource imageSource, bool useTransitions = true)
        {
            Image oldImage = (_image2Active) ? _image2 : _image1;
            Image newImage = (_image2Active) ? _image1 : _image2;
            _image2Active = !_image2Active;

            // If a storyboard is running, cancel transitions and switch images immediately to prevent issues with switching between the right images.
            if (_currentStoryboard != null)
                useTransitions = false;

            if (!useTransitions)
            {
                if (_currentStoryboard != null)
                {
                    _currentStoryboard.Stop();
                    _currentStoryboard = null;
                }
                newImage.Source = imageSource;
                oldImage.Source = null;
                return;
            }

            // Set up the storyboard and animation
            _currentStoryboard = new Storyboard();
            DoubleAnimation animation = new DoubleAnimation();
            animation.Duration = new Duration(TimeSpan.FromMilliseconds(Delay));
            Storyboard.SetTargetProperty(animation, new PropertyPath(Image.OpacityProperty));
            _currentStoryboard.Children.Add(animation);
            _currentStoryboard.Completed += (sender, e) =>
            {
                oldImage.Source = null;
                _currentStoryboard.Stop();
                _currentStoryboard = null;
            };

            // If the new ImageSource is null, we need to fade out the current image
            if (imageSource == null)
            {
                Storyboard.SetTarget(animation, oldImage);
                animation.From = 1;
                animation.To = 0;
                
                _currentStoryboard.Begin();
            }
            // Otherwise, fade in the new image over the old one
            else
            {
                newImage.Source = imageSource;

                Canvas.SetZIndex(newImage, 10);
                Canvas.SetZIndex(oldImage, 1);

                Storyboard.SetTarget(animation, newImage);
                animation.From = 0;
                animation.To = 1;

                _currentStoryboard.Begin();
            }
        }
    }
}
