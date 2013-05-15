using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Komodex.Common.Phone.Controls
{
    public class WizardControl : ItemsControl
    {
        private Storyboard _currentStoryboard;
        private IEasingFunction _animationEasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseInOut };
        private const int _offscreenPosition = 1000;

        public WizardControl()
        {
            DefaultStyleKey = typeof(WizardControl);

            SizeChanged += WizardControl_SizeChanged;
            Loaded += WizardControl_Loaded;
        }

        #region Properties

        private TimeSpan _animationDuration = TimeSpan.FromMilliseconds(200);
        public TimeSpan AnimationDuration
        {
            get { return _animationDuration; }
            set { _animationDuration = value; }
        }

        private int _selectedIndex;
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set { SetSelectedIndex(value); }
        }

        public WizardItem SelectedItem
        {
            get
            {
                if (SelectedIndex >= 0)
                    return Items[SelectedIndex] as WizardItem;
                return null;
            }
            set { SelectedIndex = Items.IndexOf(value); }
        }

        #endregion

        #region Design Mode

        protected void UpdateDesignModeDisplay()
        {
            if (!DesignerProperties.IsInDesignTool)
                return;

            // Get the background panel
            Panel backgroundPanel = GetTemplateChild("BackgroundPanel") as Panel;
            if (backgroundPanel == null)
                return;

            // Remove any previous Border elements
            backgroundPanel.Children.Clear();

            // Item border properties
            Thickness borderBorderThickness = new Thickness(1);
            Thickness borderMargin = new Thickness(-1);
            Brush borderBorderBrush = new SolidColorBrush(Colors.DarkGray);
            Brush borderBackgroundBrush = (Brush)Application.Current.Resources["PhoneBackgroundBrush"];

            // Arrange the wizard items and add display borders
            for (int i = 0; i < Items.Count; i++)
            {
                WizardItem wizardItem = (WizardItem)Items[i];

                // Current position, adjusted for the SelectedIndex
                int position = i - SelectedIndex;

                // Get the item's TranslateTransform
                var transform = wizardItem.RenderTransform as TranslateTransform;
                if (transform == null)
                    continue;

                // Determine and set the x offset
                double offset = position * (this.ActualWidth + 36);
                if (position > 0)
                    offset += Margin.Right;
                else if (position < 0)
                    offset -= Margin.Left;
                transform.X = offset;

                // Add a border if this isn't the currently selected item
                if (position != 0)
                {
                    Border border = new Border();
                    border.BorderThickness = borderBorderThickness;
                    border.Margin = borderMargin;
                    border.BorderBrush = borderBorderBrush;
                    border.Background = borderBackgroundBrush;
                    border.RenderTransform = transform;
                    // Add an inner border to display this control's background
                    Border innerBorder = new Border();
                    innerBorder.SetBinding(Border.BackgroundProperty, new Binding("Background") { Source = this });
                    border.Child = innerBorder;
                    // Add the border to the background panel
                    backgroundPanel.Children.Add(border);
                }
            }
        }

        #endregion

        #region Overrides

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateDesignModeDisplay();
        }

        private void WizardControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateDesignModeDisplay();

            if (!DesignerProperties.IsInDesignTool)
            {
                var geometry = new RectangleGeometry();
                geometry.Rect = new Rect(0, 0, ActualWidth, ActualHeight);
                Clip = geometry;
            }
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                {
                    WizardItem wizardItem = item as WizardItem;
                    if (wizardItem == null)
                        continue;

                    wizardItem.RenderTransform = new TranslateTransform();
                }
            }

            UpdateDesignModeDisplay();
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            WizardItem wizardItem = (WizardItem)element;
            TranslateTransform transform = (TranslateTransform)wizardItem.RenderTransform;
            wizardItem.RenderTransform = transform;

            if (!DesignerProperties.IsInDesignTool)
            {
                // Don't set visibility to collapsed yet. This gives all items time to render, reducing the chance of lost animations.
                if (Items.IndexOf(wizardItem) != _selectedIndex)
                    transform.X = _offscreenPosition;
            }
        }

        private void WizardControl_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= WizardControl_Loaded;
            if (!DesignerProperties.IsInDesignTool)
            {
                // Set all off-screen items to Collapsed
                SetSelectedIndex(-1, _selectedIndex, false);
            }
        }

        #endregion

        #region Wizard Item Animation

        public void SetSelectedIndex(int value, bool useTransitions = true)
        {
            if (_selectedIndex == value)
                return;

            SetSelectedIndex(_selectedIndex, value, useTransitions);
        }

        public void SetSelectedItem(WizardItem item, bool useTransitions = true)
        {
            SetSelectedIndex(Items.IndexOf(item), useTransitions);
        }

        private void SetSelectedIndex(int oldValue, int value, bool useTransitions = true)
        {
            if (oldValue == value)
                return;

            int oldIndex = oldValue;
            _selectedIndex = value;

            if (oldIndex < 0 || value < 0)
                useTransitions = false;
            if (oldIndex >= Items.Count || value >= Items.Count)
                useTransitions = false;

            // If we're in design mode, just update the item positions and return
            if (DesignerProperties.IsInDesignTool)
            {
                UpdateDesignModeDisplay();
                return;
            }

            // If we aren't using transitions, cancel any previous animations and update the display
            if (!useTransitions)
            {
                if (_currentStoryboard != null)
                {
                    _currentStoryboard.Stop();
                    _currentStoryboard = null;
                }

                for (int i = 0; i < Items.Count; i++)
                {
                    WizardItem item = (WizardItem)Items[i];
                    if (i == _selectedIndex)
                    {
                        item.Visibility = System.Windows.Visibility.Visible;
                        ((TranslateTransform)item.RenderTransform).X = 0;
                    }
                    else
                    {
                        item.Visibility = System.Windows.Visibility.Collapsed;
                    }
                }

                return;
            }

            // Prepare the new item (off screen)
            // Doing this early prevents lag/jumpiness in the middle of the animation.
            WizardItem newItem = (WizardItem)Items[_selectedIndex];
            ((TranslateTransform)newItem.RenderTransform).X = ActualWidth;
            newItem.Visibility = System.Windows.Visibility.Visible;

            // If we're already animating, don't do anything since the new index will be handled automatically
            if (_currentStoryboard != null)
                return;

            // Set up transitions
            double sourceX, destX;
            if (oldIndex < SelectedIndex)
            {
                sourceX = ActualWidth;
                destX = -ActualWidth;
            }
            else
            {
                sourceX = -ActualWidth;
                destX = ActualWidth;
            }

            // Divide the animation time by two since it will be used twice (once for the outgoing item, and again for the incoming item)
            TimeSpan halfAnimationTime = new TimeSpan(AnimationDuration.Ticks / 2);
            Duration duration = new Duration(halfAnimationTime);

            if (_currentStoryboard != null)
                _currentStoryboard.Stop();
            _currentStoryboard = new Storyboard();
            _currentStoryboard.Duration = duration;

            // Outgoing item animation
            WizardItem oldItem = (WizardItem)Items[oldIndex];
            DoubleAnimation itemAnimation = new DoubleAnimation();
            itemAnimation.Duration = duration;
            itemAnimation.EasingFunction = _animationEasingFunction;
            itemAnimation.To = destX;
            Storyboard.SetTarget(itemAnimation, oldItem.RenderTransform);
            Storyboard.SetTargetProperty(itemAnimation, new PropertyPath(TranslateTransform.XProperty));
            _currentStoryboard.Children.Add(itemAnimation);
            _currentStoryboard.Completed += (s1, e1) =>
            {
                oldItem.Visibility = System.Windows.Visibility.Collapsed;
                _currentStoryboard.Stop();

                // Get the new item
                int newIndex = _selectedIndex;
                newItem = (WizardItem)Items[newIndex];

                // Set up the new item (off screen)
                ((TranslateTransform)newItem.RenderTransform).X = sourceX;
                newItem.Visibility = System.Windows.Visibility.Visible;

                // Create a new storyboard
                _currentStoryboard = new Storyboard();
                _currentStoryboard.Duration = duration;

                // Incoming item animation
                itemAnimation = new DoubleAnimation();
                itemAnimation.Duration = duration;
                itemAnimation.EasingFunction = _animationEasingFunction;
                itemAnimation.To = 0;
                Storyboard.SetTarget(itemAnimation, newItem.RenderTransform);
                Storyboard.SetTargetProperty(itemAnimation, new PropertyPath(TranslateTransform.XProperty));
                _currentStoryboard.Children.Add(itemAnimation);
                _currentStoryboard.Completed += (s2, e2) =>
                {
                    ((TranslateTransform)newItem.RenderTransform).X = 0;
                    _currentStoryboard.Stop();
                    _currentStoryboard = null;
                    SetSelectedIndex(newIndex, _selectedIndex);
                };

                _currentStoryboard.Begin();
            };
            _currentStoryboard.Begin();
        }

        #endregion

    }
}
