using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.Common
{
    // Adapted from:
    // http://blogs.msdn.com/b/pfxteam/archive/2012/02/12/10266983.aspx

    // This should only be needed on WP7. On WP8 and Win8, the SemaphoreSlim class can be used instead.

    public class AsyncSemaphore
    {
        private readonly static Task s_completed = TaskEx.FromResult(true);
        private readonly Queue<TaskCompletionSource<bool>> m_waiters = new Queue<TaskCompletionSource<bool>>();
        private int m_currentCount;

        public AsyncSemaphore(int initialCount)
        {
            if (initialCount < 0) throw new ArgumentOutOfRangeException("initialCount");
            m_currentCount = initialCount;
        }

        public Task WaitAsync()
        {
            lock (m_waiters)
            {
                if (m_currentCount > 0)
                {
                    --m_currentCount;
                    return s_completed;
                }
                else
                {
                    var waiter = new TaskCompletionSource<bool>();
                    m_waiters.Enqueue(waiter);
                    return waiter.Task;
                }
            }
        }

        public void Release()
        {
            TaskCompletionSource<bool> toRelease = null;
            lock (m_waiters)
            {
                if (m_waiters.Count > 0)
                    toRelease = m_waiters.Dequeue();
                else
                    ++m_currentCount;
            }
            if (toRelease != null)
                toRelease.SetResult(true);
        }
    }
}
