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
using Clarity.Phone.Extensions;
using System.ComponentModel;

namespace Komodex.Common.Phone
{
    public class DialogUserControlBase : UserControl
    {
        protected DialogService _dialogService;
        protected MessageBoxResult _result;

        #region Properties

        public bool IsOpen
        {
            get
            {
                if (_dialogService != null)
                    return _dialogService.IsOpen;
                return false;
            }
        }

        #endregion

        #region Methods

        internal virtual void Show(ContentPresenter container)
        {
            if (_dialogService != null && _dialogService.IsOpen)
                return;

            _result = default(MessageBoxResult);

            _dialogService = new DialogService();
            _dialogService.PopupContainer = container;
            _dialogService.AnimationType = DialogService.AnimationTypes.Slide;
            _dialogService.Opened += DialogService_Opened;
            _dialogService.Closing += DialogService_Closing;
            _dialogService.Closed += DialogService_Closed;

            _dialogService.Child = this;

            _dialogService.Show();
        }

        public virtual void Hide(MessageBoxResult result = MessageBoxResult.None)
        {
            _result = result;

            if (_dialogService != null && _dialogService.IsOpen)
                _dialogService.Hide();
        }

        private void UnhookEvents()
        {
            if (_dialogService == null)
                return;

            _dialogService.Opened -= DialogService_Opened;
            _dialogService.Closing -= DialogService_Closing;
            _dialogService.Closed -= DialogService_Closed;
        }

        #endregion

        #region Events

        public event EventHandler<DialogControlClosingEventArgs> Closing;

        public event EventHandler<DialogControlClosedEventArgs> Closed;

        #endregion

        protected virtual void DialogService_Opened(object sender, EventArgs e)
        {
            // Do nothing
        }

        protected virtual void DialogService_Closing(object sender, CancelEventArgs e)
        {
            DialogControlClosingEventArgs args = new DialogControlClosingEventArgs(_result);
            Closing.RaiseOnUIThread(this, args);
            if (args.Cancel)
                e.Cancel = true;
        }

        protected virtual void DialogService_Closed(object sender, EventArgs e)
        {
            Closed.RaiseOnUIThread(this, new DialogControlClosedEventArgs(_result));
            UnhookEvents();
            _dialogService = null;
        }
    }

    public class DialogControlClosingEventArgs : CancelEventArgs
    {
        public DialogControlClosingEventArgs(MessageBoxResult result)
        {
            Result = result;
        }

        public MessageBoxResult Result { get; protected set; }
    }

    public class DialogControlClosedEventArgs : EventArgs
    {
        public DialogControlClosedEventArgs(MessageBoxResult result)
        {
            Result = result;
        }

        public MessageBoxResult Result { get; protected set; }
    }
}
