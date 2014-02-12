using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Komodex.Common
{
    public static class TaskUtility
    {
        public static Task Delay(int millisecondsDelay)
        {
#if WP7
            return TaskEx.Delay(millisecondsDelay);
#else
            return Task.Delay(millisecondsDelay);
#endif
        }

        public static Task Delay(TimeSpan delay)
        {
#if WP7
            return TaskEx.Delay(delay);
#else
            return Task.Delay(delay);
#endif
        }

        public static Task Delay(int millisecondsDelay, CancellationToken cancellationToken)
        {
#if WP7
            return TaskEx.Delay(millisecondsDelay, cancellationToken);
#else
            return Task.Delay(millisecondsDelay, cancellationToken);
#endif
        }

        public static Task Delay(TimeSpan delay, CancellationToken cancellationToken)
        {
#if WP7
            return TaskEx.Delay(delay, cancellationToken);
#else
            return Task.Delay(delay, cancellationToken);
#endif
        }

        public static Task<TResult[]> WhenAll<TResult>(IEnumerable<Task<TResult>> tasks)
        {
#if WP7
            return TaskEx.WhenAll<TResult>(tasks);
#else
            return Task.WhenAll<TResult>(tasks);
#endif
        }

        public static Task WhenAll(IEnumerable<Task> tasks)
        {
#if WP7
            return TaskEx.WhenAll(tasks);
#else
            return Task.WhenAll(tasks);
#endif
        }

        public static Task<TResult[]> WhenAll<TResult>(params Task<TResult>[] tasks)
        {
#if WP7
            return TaskEx.WhenAll<TResult>(tasks);
#else
            return Task.WhenAll<TResult>(tasks);
#endif
        }

        public static Task WhenAll(params Task[] tasks)
        {
#if WP7
            return TaskEx.WhenAll(tasks);
#else
            return Task.WhenAll(tasks);
#endif
        }

        public static Task<Task<TResult>> WhenAny<TResult>(IEnumerable<Task<TResult>> tasks)
        {
#if WP7
            return TaskEx.WhenAny<TResult>(tasks);
#else
            return Task.WhenAny<TResult>(tasks);
#endif
        }

        public static Task<Task> WhenAny(IEnumerable<Task> tasks)
        {
#if WP7
            return TaskEx.WhenAny(tasks);
#else
            return Task.WhenAny(tasks);
#endif
        }

        public static Task<Task<TResult>> WhenAny<TResult>(params Task<TResult>[] tasks)
        {
#if WP7
            return TaskEx.WhenAny<TResult>(tasks);
#else
            return Task.WhenAny<TResult>(tasks);
#endif
        }

        public static Task<Task> WhenAny(params Task[] tasks)
        {
#if WP7
            return TaskEx.WhenAny(tasks);
#else
            return Task.WhenAny(tasks);
#endif
        }
    }
}
