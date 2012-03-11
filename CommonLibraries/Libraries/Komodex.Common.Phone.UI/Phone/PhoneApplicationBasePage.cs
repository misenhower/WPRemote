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
using Clarity.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Diagnostics;
using System.Windows.Navigation;
using System.Linq;

namespace Komodex.Common.Phone
{
    public class PhoneApplicationBasePage : AnimatedBasePage
    {
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var queryString = NavigationContext.QueryString;

#if DEBUG
            Debug.WriteLine("Navigated ({0}) to page: {1}", e.NavigationMode, e.Uri);
            if (queryString != null && queryString.Count > 0)
            {
                Debug.WriteLine("{0} query parameter(s):", queryString.Count);
                foreach (var item in queryString)
                    Debug.WriteLine(" {0} => {1}", item.Key, item.Value);
            }
#endif

            // Remove the previous page's entry in the back stack if requested
            if (queryString.GetBoolValue(PhoneApplicationUtility.NavigationRemoveBackEntryParameterName))
                NavigationService.RemoveBackEntry();
        }

        #region Application Bar

        protected void InitializeApplicationBar()
        {
            if (ApplicationBar == null)
                ApplicationBar = new ApplicationBar();

            ApplicationBar.StateChanged += new EventHandler<ApplicationBarStateChangedEventArgs>(ApplicationBar_StateChanged);
        }

        protected bool IsApplicationBarMenuVisible { get; private set; }

        protected virtual void ApplicationBar_StateChanged(object sender, ApplicationBarStateChangedEventArgs e)
        {
            IsApplicationBarMenuVisible = e.IsMenuVisible;
            UpdateApplicationBarOpacity();
        }

        #region Automatic Opacity

        private double _applicationBarClosedOpacity = 100;
        protected double ApplicationBarClosedOpacity
        {
            get { return _applicationBarClosedOpacity; }
            set
            {
                if (_applicationBarClosedOpacity == value)
                    return;

                _applicationBarClosedOpacity = value;
                UpdateApplicationBarOpacity();
            }
        }

        private double _applicationBarOpenOpacity = 100;
        protected double ApplicationBarOpenOpacity
        {
            get { return _applicationBarOpenOpacity; }
            set
            {
                if (_applicationBarOpenOpacity == value)
                    return;

                _applicationBarOpenOpacity = value;
                UpdateApplicationBarOpacity();
            }
        }

        protected void UpdateApplicationBarOpacity()
        {
            if (IsApplicationBarMenuVisible)
                ApplicationBar.Opacity = ApplicationBarOpenOpacity;
            else
                ApplicationBar.Opacity = ApplicationBarClosedOpacity;
        }

        #endregion

        #region Icon Buttons

        protected ApplicationBarIconButton AddApplicationBarIconButton(string buttonText, string buttonIcon, Action a)
        {
            Uri uri = new Uri(buttonIcon, UriKind.Relative);
            return AddApplicationBarIconButton(buttonText, uri, a);
        }

        protected ApplicationBarIconButton AddApplicationBarIconButton(string buttonText, Uri buttonIcon, Action a)
        {
            ApplicationBarIconButton button = new ApplicationBarIconButton();
            button.Text = buttonText;
            button.IconUri = buttonIcon;
            button.Click += (sender, e) => a();

            ApplicationBar.Buttons.Add(button);

            return button;
        }

        #endregion

        #region Menu Bar Items

        protected ApplicationBarMenuItem AddApplicationBarMenuItem(string menuItemText, Action a)
        {
            ApplicationBarMenuItem menuItem = new ApplicationBarMenuItem();
            menuItem.Text = menuItemText;
            menuItem.Click += (sender, e) => a();

            ApplicationBar.MenuItems.Add(menuItem);

            return menuItem;
        }

        #endregion

        #endregion
    }
}
