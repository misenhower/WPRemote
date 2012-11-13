using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.Common
{
    /// <summary>
    /// This class contains methods used for helping code reuse between Windows and Windows Phone.
    /// </summary>
    public static class ThreadUtility
    {
        public static void RunOnBackgroundThread(Action a)
        {
#if WINDOWS_PHONE
            System.Threading.Thread t = new System.Threading.Thread(() => a());
            t.Start();
#else
            System.Threading.Tasks.Task t = new System.Threading.Tasks.Task(a);
            t.Start();
#endif
        }

        public static void Delay(int millisecondsDelay)
        {
#if WINDOWS_PHONE
            System.Threading.Thread.Sleep(millisecondsDelay);
#else
            System.Threading.Tasks.Task.Delay(millisecondsDelay).Wait();
#endif
        }

    }
}
