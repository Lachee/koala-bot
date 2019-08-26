using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace KoalaBot.Extensions
{
    public static class TimeExtensions
    {
        /// <summary>
        /// Converts the DateTime into a Unix Epoch
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static long ToUnixEpoch(this DateTime time) => (long)(time - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

        /// <summary>
        /// Converts the long to a date time using Unix Epoch
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static DateTime ToDateTime(this long time) => new DateTime(1970, 1, 1, 0, 0, 0) + TimeSpan.FromSeconds(time);

        /// <summary>
        /// Resets the timer
        /// </summary>
        /// <param name="timer"></param>
        public static void Reset(this Timer timer)
        {
            if (timer.Enabled)
            {
                timer.Stop();
                timer.Start();
            }
        }
    }
}
