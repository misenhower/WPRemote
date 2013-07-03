using Komodex.Common;
using Komodex.Common.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace Komodex.Remote.Controls
{
    public class IconRepeatButton : IconButton
    {
        private DispatcherTimer _timer;
        public event EventHandler<EventArgs> RepeatClick;
        public event EventHandler<EventArgs> RepeatBegin;
        public event EventHandler<EventArgs> RepeatEnd;
        private bool _sentRepeatClick;

        #region Delay Property

        public static readonly DependencyProperty DelayProperty =
            DependencyProperty.Register("Delay", typeof(int), typeof(IconRepeatButton), new PropertyMetadata(500));

        public int Delay
        {
            get { return (int)GetValue(DelayProperty); }
            set { SetValue(DelayProperty, value); }
        }

        #endregion

        #region Interval Property

        public static readonly DependencyProperty IntervalProperty =
            DependencyProperty.Register("Interval", typeof(int), typeof(IconRepeatButton), new PropertyMetadata(50));

        public int Interval
        {
            get { return (int)GetValue(IntervalProperty); }
            set { SetValue(IntervalProperty, value); }
        }

        #endregion

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            _sentRepeatClick = false;
            StartTimer();
        }

        protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            if (_sentRepeatClick)
                RepeatEnd.RaiseOnUIThread(this, new EventArgs());

            StopTimer();
        }

        protected void StartTimer()
        {
            if (_timer == null)
            {
                _timer = new DispatcherTimer();
                _timer.Tick += Timer_Tick;
            }

            else if (_timer.IsEnabled)
                return;

            _timer.Interval = TimeSpan.FromMilliseconds(Delay);
            _timer.Start();
        }

        protected void StopTimer()
        {
            if (_timer == null)
                return;

            _timer.Stop();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            int interval = Interval;
            if (_timer.Interval.Milliseconds != interval)
            {
                if (interval < 0)
                    _timer.Stop();
                else
                    _timer.Interval = TimeSpan.FromMilliseconds(interval);
            }

            if (!_sentRepeatClick)
                RepeatBegin.RaiseOnUIThread(this, new EventArgs());
            _sentRepeatClick = true;

            if (IsPressed)
                RepeatClick.RaiseOnUIThread(this, new EventArgs());
        }

        protected override void OnClick()
        {
            if (_sentRepeatClick)
                return;

            base.OnClick();
        }
    }
}
