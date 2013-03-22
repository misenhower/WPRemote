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
using System.Collections.Specialized;
using System.Windows.Data;

namespace Komodex.Common.Phone.Controls
{
    public class LongListSelectorEx : LongListSelector
    {
        public LongListSelectorEx()
            : base()
        {
            // Locate the style in Generic.xaml
            DefaultStyleKey = typeof(LongListSelectorEx);

#if WP7
            Link += new EventHandler<LinkUnlinkEventArgs>(LongListSelectorEx_Link);
            Unlink += new EventHandler<LinkUnlinkEventArgs>(LongListSelectorEx_Unlink);
#else
            ItemRealized += LongListSelectorEx_ItemRealized;
            ItemUnrealized += LongListSelectorEx_ItemUnrealized;
#endif

            GotFocus += new RoutedEventHandler(LongListSelectorEx_GotFocus);

            // Bind to base control's ItemsSource property
            _itemsSourceListenerBinding = new Binding("ItemsSource");
            _itemsSourceListenerBinding.Mode = BindingMode.OneWay;
            _itemsSourceListenerBinding.Source = this;
            SetBinding(ItemsSourceListenerProperty, _itemsSourceListenerBinding);

#if WP7
            // Set default IsGroupingEnabled value to false for WP8 compatibility
            IsGroupingEnabled = false;

            // Bind HideEmptyGroups to DisplayAllGroups property
            _hideEmptyGroupsBinding = new Binding("DisplayAllGroups");
            _hideEmptyGroupsBinding.Mode = BindingMode.TwoWay;
            _hideEmptyGroupsBinding.Source = this;
            _hideEmptyGroupsBinding.Converter = _inverseBooleanConverter;
            SetBinding(HideEmptyGroupsProperty, _hideEmptyGroupsBinding);
#else
            // Set default HideEmptyGroups value to true
            HideEmptyGroups = true;
#endif
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            UpdateEmptyTextVisibility();
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

        #region Item Link/Unlink and ContentPresenter Tracking

        protected List<ContentPresenter> _currentContentPresenters = new List<ContentPresenter>();

#if WP7
        private void LongListSelectorEx_Link(object sender, LinkUnlinkEventArgs e)
        {
            LinkItem(e.ContentPresenter);
        }

        private void LongListSelectorEx_Unlink(object sender, LinkUnlinkEventArgs e)
        {
            UnlinkItem(e.ContentPresenter);
        }
#else
        private void LongListSelectorEx_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            LinkItem(e.Container);
        }

        private void LongListSelectorEx_ItemUnrealized(object sender, ItemRealizationEventArgs e)
        {
            UnlinkItem(e.Container);
        }
#endif

        private void LinkItem(ContentPresenter contentPresenter)
        {
            _currentContentPresenters.Add(contentPresenter);

            if (_emptyTextVisible)
                UpdateEmptyTextVisibility();
        }

        private void UnlinkItem(ContentPresenter contentPresenter)
        {
            _currentContentPresenters.Remove(contentPresenter);

            if (!_emptyTextVisible && _currentContentPresenters.Count == 0)
                UpdateEmptyTextVisibility();
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

        private bool _emptyTextVisible;

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
            DependencyProperty.Register("ShowEmptyTextWhenNull", typeof(bool), typeof(LongListSelectorEx), new PropertyMetadata(true, OnShowTemptyTextWhenNullChanged));

        public bool ShowEmptyTextWhenNull
        {
            get { return (bool)GetValue(ShowEmptyTextWhenNullProperty); }
            set { SetValue(ShowEmptyTextWhenNullProperty, value); }
        }

        private static void OnShowTemptyTextWhenNullChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((LongListSelectorEx)obj).UpdateEmptyTextVisibility();
        }

        #endregion

        #region ItemsSource Listener

        private Binding _itemsSourceListenerBinding;

        private static readonly DependencyProperty ItemsSourceListenerProperty =
            DependencyProperty.Register("ItemsSourceListener", typeof(IEnumerable), typeof(LongListSelectorEx), new PropertyMetadata(OnItemsSourceListenerChanged));

        private static void OnItemsSourceListenerChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((LongListSelectorEx)obj).UpdateEmptyTextVisibility();
        }

        #endregion

        protected void UpdateEmptyTextVisibility()
        {
            SetEmptyTextVisibility(ShouldShowEmptyText());
        }

        protected void SetEmptyTextVisibility(bool visible)
        {
            VisualStateManager.GoToState(this, (visible) ? "EmptyTextVisible" : "EmptyTextCollapsed", true);
            _emptyTextVisible = visible;
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

                // If this is a flat list or if empty groups are displayed, we don't need to do any further checks
                if (!IsGroupingEnabled || !HideEmptyGroups)
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

        #region Compatibility
#if WP7

        public bool IsGroupingEnabled
        {
            get { return !IsFlatList; }
            set { IsFlatList = !value; }
        }

        private Binding _hideEmptyGroupsBinding;
        private static Komodex.Common.Converters.InverseBooleanConverter _inverseBooleanConverter = new Komodex.Common.Converters.InverseBooleanConverter();

        public static readonly DependencyProperty HideEmptyGroupsProperty =
            DependencyProperty.Register("HideEmptyGroups", typeof(bool), typeof(LongListSelectorEx), new PropertyMetadata(false));

        public bool HideEmptyGroups
        {
            get { return (bool)GetValue(HideEmptyGroupsProperty); }
            set { SetValue(HideEmptyGroupsProperty, value); }
        }

#endif
        #endregion
    }
}
