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

namespace Komodex.Common.Phone.Controls
{
    public class WizardControl : ItemsControl
    {
        public WizardControl()
        {
            DefaultStyleKey = typeof(WizardControl);

            SizeChanged += WizardControl_SizeChanged;
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
            set
            {
                if (_selectedIndex == value)
                    return;

                _selectedIndex = value;
                // TODO: Update display
            }
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

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateDesignModeDisplay();
        }

        private void WizardControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateDesignModeDisplay();
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            UpdateDesignModeDisplay();
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            WizardItem wizardItem = (WizardItem)element;
            wizardItem.RenderTransform = new TranslateTransform();
        }

    }
}
