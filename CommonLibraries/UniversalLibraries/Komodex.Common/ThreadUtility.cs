using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Komodex.Common
{
    public static class ThreadUtility
    {
        /// <summary>
        /// Captures the current SynchronizationContext for use with UI thread calls.
        /// </summary>
        public static void Initialize()
        {
            UIThreadSynchronizationContext = SynchronizationContext.Current;
        }

        public static SynchronizationContext UIThreadSynchronizationContext { get; private set; }

        public static bool IsOnUIThread
        {
            get { return SynchronizationContext.Current == UIThreadSynchronizationContext; }
        }

        public static void RunOnUIThread(Action a)
        {
            var context = UIThreadSynchronizationContext;
            if (context == null)
                throw new Exception("Initialize ThreadUtility class from the UI thread before calling RunOnUIThread.");

            if (IsOnUIThread)
                a();
            else
                context.Post((state) => a(), null);
        }

        public static Task RunOnUIThreadAsync(Action a)
        {
            var context = UIThreadSynchronizationContext;
            if (context == null)
                throw new Exception("Initialize ThreadUtility class from the UI thread before calling RunOnUIThreadAsync.");

            return context.SendAsync((state) => a(), null);
        }

        #region SynchronizationContext Extensions

        public static Task SendAsync(this SynchronizationContext context, SendOrPostCallback d, object state)
        {
            var tcs = new TaskCompletionSource<bool>();

            context.Post((s) =>
            {
                try
                {
                    d(s);
                    tcs.SetResult(true);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            }, state);

            return tcs.Task;
        }

        #endregion

        #region Event Raise/RaiseOnUIThread Extension Methods

        #region EventHandler<T>

        public static void Raise<T>(this EventHandler<T> eventHandler, object sender, T e)
            where T : EventArgs
        {
            if (eventHandler == null)
                return;

            eventHandler(sender, e);
        }

        public static void RaiseOnUIThread<T>(this EventHandler<T> eventHandler, object sender, T e)
            where T : EventArgs
        {
            RunOnUIThread(() =>
            {
                eventHandler.Raise(sender, e);
            });
        }

        #endregion

        #region PropertyChangedEventHandler

        public static void RaiseOnUIThread(this PropertyChangedEventHandler eventHandler, object sender, string firstPropertyName, params string[] additionalPropertyNames)
        {
            RunOnUIThread(() =>
            {
                if (eventHandler == null)
                    return;

                eventHandler(sender, new PropertyChangedEventArgs(firstPropertyName));

                foreach (string propertyName in additionalPropertyNames)
                    eventHandler(sender, new PropertyChangedEventArgs(propertyName));
            });
        }

        #endregion

        #endregion
    }
}
