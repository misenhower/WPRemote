using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Collections.Generic;
using System.Linq;
using Clarity.Phone.Extensions;
using System.Collections;

namespace Komodex.Common.Phone.Controls
{
    public class LongListSelectorEx : LongListSelector
    {
        public LongListSelectorEx()
            : base()
        {
            // Locate the style in Generic.xaml
            DefaultStyleKey = typeof(LongListSelectorEx);

            Link += new EventHandler<LinkUnlinkEventArgs>(LongListSelectorEx_Link);
            Unlink += new EventHandler<LinkUnlinkEventArgs>(LongListSelectorEx_Unlink);

            GotFocus += new RoutedEventHandler(LongListSelectorEx_GotFocus);

            CollectionChanged += new EventHandler(LongListSelectorEx_CollectionChanged);
        }

        #region Focus Fix

        private bool _preventFocus = true;
        public bool PreventFocus
        {
            get { return _preventFocus; }
            set { _preventFocus = value; }
        }

        private void LongListSelectorEx_GotFocus(object sender, RoutedEventArgs e)
        {
            // Attempt to focus a parent control whenever this control is focused.
            // This seems to fix an issue with the current WP7.1 ListBox control where list items
            // with different heights can cause issues with scrolling.

            if (!_preventFocus)
                return;

            Control c = this.GetVisualAncestors().FirstOrDefault(a => a is Control) as Control;
            if (c != null)
                c.Focus();
        }

        #endregion

        #region ContentPresenter Tracking

        protected List<ContentPresenter> _currentContentPresenters = new List<ContentPresenter>();

        private void LongListSelectorEx_Link(object sender, LinkUnlinkEventArgs e)
        {
            _currentContentPresenters.Add(e.ContentPresenter);
        }

        private void LongListSelectorEx_Unlink(object sender, LinkUnlinkEventArgs e)
        {
            _currentContentPresenters.Remove(e.ContentPresenter);
        }

        public ContentPresenter GetContentPresenterForItem(object item)
        {
            return _currentContentPresenters.FirstOrDefault(c => c.DataContext == item);
        }

        public ContentPresenter SelectedContentPresenter
        {
            get { return GetContentPresenterForItem(SelectedItem); }
        }

        #endregion

        #region EmptyText

        #region EmptyText Property

        public static readonly DependencyProperty EmptyTextProperty =
            DependencyProperty.Register("EmptyText", typeof(string), typeof(LongListSelectorEx), new PropertyMetadata(null));

        public string EmptyText
        {
            get { return (string)GetValue(EmptyTextProperty); }
            set { SetValue(EmptyTextProperty, value); }
        }

        #endregion

        #region EmptyTextStyle Property

        public static readonly DependencyProperty EmptyTextStyleProperty =
            DependencyProperty.Register("EmptyTextStyle", typeof(Style), typeof(LongListSelectorEx), new PropertyMetadata(null));

        public Style EmptyTextStyle
        {
            get { return (Style)GetValue(EmptyTextStyleProperty); }
            set { SetValue(EmptyTextStyleProperty, value); }
        }

        #endregion

        #region ShowEmptyTextWhenNull Property

        public static readonly DependencyProperty ShowEmptyTextWhenNullProperty =
            DependencyProperty.Register("ShowEmptyTextWhenNull", typeof(bool), typeof(LongListSelectorEx), new PropertyMetadata(true));

        public bool ShowEmptyTextWhenNull
        {
            get { return (bool)GetValue(ShowEmptyTextWhenNullProperty); }
            set { SetValue(ShowEmptyTextWhenNullProperty, value); }
        }

        #endregion

        protected void SetEmptyTextVisibility(bool visible)
        {
            VisualStateManager.GoToState(this, (visible) ? "EmptyTextVisible" : "EmptyTextCollapsed", true);
        }

        private void LongListSelectorEx_CollectionChanged(object sender, EventArgs e)
        {
            SetEmptyTextVisibility(ShouldShowEmptyText());
        }

        private bool ShouldShowEmptyText()
        {
            var itemsSource = ItemsSource;

            if (itemsSource == null && ShowEmptyTextWhenNull)
                return true;

            if (itemsSource is IList)
            {
                IList list = (IList)itemsSource;
                if (list.Count == 0)
                    return true;

                // If this is a flat list, we found items so we don't need to do any further checks
                if (IsFlatList)
                    return false;

                // Check whether the sub-lists have items
                for (int i = 0; i < list.Count; i++)
                {
                    if (!(list[i] is IList) || ((IList)list[i]).Count > 0)
                        return false;
                }

                return true;
            }

            return false;
        }

        #endregion
    }
}
