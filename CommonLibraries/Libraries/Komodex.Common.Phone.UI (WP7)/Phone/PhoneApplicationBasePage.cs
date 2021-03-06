﻿using System;
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
using System.Windows.Threading;

namespace Komodex.Common.Phone
{
    public class PhoneApplicationBasePage : AnimatedBasePage
    {
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var queryString = NavigationContext.QueryString;

#if DEBUG
            Log.Main.Info("Navigated ({0}) to page: {1}", e.NavigationMode, e.Uri);
            if (queryString != null && queryString.Count > 0)
            {
                string navDetails = string.Format("{0} query parameter(s):", queryString.Count);
                foreach (var item in queryString)
                    navDetails += string.Format("\n {0} => {1}", item.Key, item.Value);
                Log.Main.Debug(navDetails);
            }
#endif

            // Remove the previous page's entry in the back stack if requested
            if (e.NavigationMode == NavigationMode.New && queryString.GetBoolValue(PhoneApplicationUtility.NavigationRemoveBackEntryParameterName))
                NavigationService.RemoveBackEntry();

            InitializeApplicationBar(false);
            ClearProgressIndicator();
        }

        #region Application Bar

        protected virtual void InitializeApplicationBar()
        {
            InitializeApplicationBar(true);
        }

        private void InitializeApplicationBar(bool createIfNull)
        {
            if (ApplicationBar == null)
            {
                if (!createIfNull)
                    return;

                ApplicationBar = new ApplicationBar();
            }

            ApplicationBar.StateChanged += ApplicationBar_StateChanged;

            UpdateApplicationBarOpacity();
        }

        protected bool IsApplicationBarMenuVisible { get; private set; }

        protected virtual void ApplicationBar_StateChanged(object sender, ApplicationBarStateChangedEventArgs e)
        {
            IsApplicationBarMenuVisible = e.IsMenuVisible;
            UpdateApplicationBarOpacity();
        }

        #region Automatic Opacity

        private double _applicationBarMenuClosedOpacity = 100;
        protected double ApplicationBarMenuClosedOpacity
        {
            get { return _applicationBarMenuClosedOpacity; }
            set
            {
                if (_applicationBarMenuClosedOpacity == value)
                    return;

                _applicationBarMenuClosedOpacity = value;
                UpdateApplicationBarOpacity();
            }
        }

        private double _applicationBarMenuOpenOpacity = 100;
        protected double ApplicationBarMenuOpenOpacity
        {
            get { return _applicationBarMenuOpenOpacity; }
            set
            {
                if (_applicationBarMenuOpenOpacity == value)
                    return;

                _applicationBarMenuOpenOpacity = value;
                UpdateApplicationBarOpacity();
            }
        }

        protected virtual void UpdateApplicationBarOpacity()
        {
            if (ApplicationBar == null)
                return;

            if (IsApplicationBarMenuVisible)
                ApplicationBar.Opacity = ApplicationBarMenuOpenOpacity;
            else
                ApplicationBar.Opacity = ApplicationBarMenuClosedOpacity;
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
            InitializeApplicationBar(true);

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
            InitializeApplicationBar(true);

            ApplicationBarMenuItem menuItem = new ApplicationBarMenuItem();
            menuItem.Text = menuItemText;
            menuItem.Click += (sender, e) => a();

            ApplicationBar.MenuItems.Add(menuItem);

            return menuItem;
        }

        #endregion

        #endregion

        #region Progress Indicator

        private TimeSpan _defaultProgressIndicatorClearDelay = TimeSpan.FromSeconds(1);
        private DispatcherTimer _progressIndicatorTimer;

        private void InitializeProgressIndicator(bool? isVisible)
        {
            if (_progressIndicatorTimer != null)
                _progressIndicatorTimer.Stop();

            ProgressIndicator progressIndicator = SystemTray.ProgressIndicator;
            if (progressIndicator == null)
            {
                progressIndicator = new ProgressIndicator();
                SystemTray.ProgressIndicator = progressIndicator;
            }

            if (isVisible.HasValue)
                progressIndicator.IsVisible = isVisible.Value;
        }

        protected void SetProgressIndicator(string text, bool isIndeterminate = false)
        {
            InitializeProgressIndicator(true);

            ProgressIndicator progressIndicator = SystemTray.ProgressIndicator;
            progressIndicator.Text = text;
            progressIndicator.IsIndeterminate = isIndeterminate;
            progressIndicator.Value = 0;
        }

        protected void SetProgressIndicator(string text, double value)
        {
            InitializeProgressIndicator(true);

            ProgressIndicator progressIndicator = SystemTray.ProgressIndicator;
            progressIndicator.Text = text;
            progressIndicator.IsIndeterminate = false;
            progressIndicator.Value = value;
        }

        protected void ClearProgressIndicator()
        {
            ClearProgressIndicator(null, TimeSpan.Zero);
        }

        protected void ClearProgressIndicator(string text)
        {
            ClearProgressIndicator(text, _defaultProgressIndicatorClearDelay);
        }

        protected void ClearProgressIndicator(string text, TimeSpan delay)
        {
            InitializeProgressIndicator(null);

            ProgressIndicator progressIndicator = SystemTray.ProgressIndicator;

            if (string.IsNullOrEmpty(text) || delay == TimeSpan.Zero)
            {
                progressIndicator.IsVisible = false;
                return;
            }

            progressIndicator.Text = text;
            progressIndicator.IsIndeterminate = false;

            if (_progressIndicatorTimer == null)
            {
                _progressIndicatorTimer = new DispatcherTimer();
                _progressIndicatorTimer.Tick += (sender, e) => ClearProgressIndicator();
            }

            _progressIndicatorTimer.Interval = delay;
            _progressIndicatorTimer.Start();
        }

        #endregion

        #region Dialog Service

        /// <summary>
        /// The ContentPresenter that will be used to display dialogs.
        /// </summary>
        protected ContentPresenter DialogContainer { get; set; }

        private DialogUserControlBase _currentDialogControl;
        protected DialogUserControlBase CurrentDialogControl
        {
            get { return _currentDialogControl; }
        }

        public event EventHandler<DialogControlClosedEventArgs> DialogClosed;

        /// <summary>
        /// Returns true if a dialog is currently open.
        /// </summary>
        protected bool IsDialogOpen
        {
            get { return (_currentDialogControl != null && _currentDialogControl.IsOpen); }
        }

        protected void ShowDialog(DialogUserControlBase dialog)
        {
            if (DialogContainer == null)
            {
                Panel layoutRoot = this.FindName("LayoutRoot") as Panel;
                if (layoutRoot == null)
                    throw new InvalidOperationException("Could not automatically create dialog container.");
                ContentPresenter dialogContainer = new ContentPresenter();
                layoutRoot.Children.Add(dialogContainer);
                Grid.SetColumnSpan(dialogContainer, int.MaxValue);
                Grid.SetRowSpan(dialogContainer, int.MaxValue);
                DialogContainer = dialogContainer;
            }

            if (IsDialogOpen)
                return;

            _currentDialogControl = dialog;
            _currentDialogControl.Closed += Dialog_Closed;
            _currentDialogControl.Show(DialogContainer);
        }

        private void Dialog_Closed(object sender, DialogControlClosedEventArgs e)
        {
            if (_currentDialogControl == sender)
            {
                _currentDialogControl = null;
                DialogClosed.Raise(sender, e);
            }
        }

        #endregion
    }
}
