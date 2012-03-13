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

namespace Komodex.Common.Phone.Controls
{
    public class LongListSelectorEx : LongListSelector
    {
        protected List<ContentPresenter> _currentContentPresenters = new List<ContentPresenter>();

        public LongListSelectorEx()
            : base()
        {
            Link += new EventHandler<LinkUnlinkEventArgs>(LongListSelectorEx_Link);
            Unlink += new EventHandler<LinkUnlinkEventArgs>(LongListSelectorEx_Unlink);

            GotFocus += new RoutedEventHandler(LongListSelectorEx_GotFocus);
        }

        void LongListSelectorEx_Link(object sender, LinkUnlinkEventArgs e)
        {
            _currentContentPresenters.Add(e.ContentPresenter);
        }

        void LongListSelectorEx_Unlink(object sender, LinkUnlinkEventArgs e)
        {
            _currentContentPresenters.Remove(e.ContentPresenter);
        }

        void LongListSelectorEx_GotFocus(object sender, RoutedEventArgs e)
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

        #region Properties

        private bool _preventFocus = true;
        public bool PreventFocus
        {
            get { return _preventFocus; }
            set { _preventFocus = value; }
        }

        public ContentPresenter SelectedContentPresenter
        {
            get { return GetContentPresenterForItem(SelectedItem); }
        }

        #endregion

        #region Methods

        public ContentPresenter GetContentPresenterForItem(object item)
        {
            return _currentContentPresenters.FirstOrDefault(c => c.DataContext == item);
        }

        #endregion

    }
}
