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

namespace Komodex.WP7DACPRemote.Controls
{
    public class LongListSelectorEx : LongListSelector
    {
        protected List<ContentPresenter> _currentContentPresenters = new List<ContentPresenter>();

        public LongListSelectorEx()
            : base()
        {
            Link += new EventHandler<LinkUnlinkEventArgs>(LongListSelectorEx_Link);
            Unlink += new EventHandler<LinkUnlinkEventArgs>(LongListSelectorEx_Unlink);
        }

        void LongListSelectorEx_Link(object sender, LinkUnlinkEventArgs e)
        {
            _currentContentPresenters.Add(e.ContentPresenter);
        }

        void LongListSelectorEx_Unlink(object sender, LinkUnlinkEventArgs e)
        {
            _currentContentPresenters.Remove(e.ContentPresenter);
        }

        #region Properties

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
