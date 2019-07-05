using System;
using System.Security.Cryptography;
using System.Text;

namespace KoalaBot.Util
{
    public static class StringExtension
    {
        /// <summary>
        /// Cuts a segment of the string
        /// </summary>
        /// <param name="str"></param>
        /// <param name="start">The starting index</param>
        /// <param name="end">The ending index</param>
        /// <returns></returns>
        public static string Cut(this string str, int start, int end)
        {
            if (end > str.Length) throw new ArgumentOutOfRangeException("end", "The end cannot be greater than the length of the string");
            if (start > end) throw new ArgumentOutOfRangeException("start", "The start cannot be greater than the end");
            if (start < 0) throw new ArgumentOutOfRangeException("start", "The start cannot be less than 0");
            return str.Substring(start, end - start);
        }

        /// <summary>
        /// Formats the timespan into the form of (Time) (Unit)[s].
        /// <para>TimeSpan of 0:03:59 will be 3 Minutes</para>
        /// <para>TimeSpan of 0:01:43 will be 1 Minute</para>
        /// <para>TimeSpan of 0:00:14 will be 14 Seconds</para>
        /// </summary>
        /// <param name="timespan"></param>
        /// <returns></returns>
        public static string Format(this TimeSpan timespan)
        {
            if (timespan.TotalDays >= 1) return Math.Round(timespan.TotalDays) + " Day" + (timespan.TotalDays != 1 ? "s" : "");
            if (timespan.TotalHours >= 1) return Math.Round(timespan.TotalHours) + " Hour" + (timespan.TotalHours != 1 ? "s" : "");
            if (timespan.TotalMinutes >= 1) return Math.Round(timespan.TotalMinutes) + " Minute" + (timespan.TotalMinutes != 1 ? "s" : "");
            if (timespan.TotalSeconds >= 1) return Math.Round(timespan.TotalSeconds) + " Second" + (timespan.TotalSeconds != 1 ? "s" : "");
            return Math.Round(timespan.TotalMilliseconds) + "ms";
        }

        /// <summary>
        /// Calculates the MD5 hash of a string
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string CalculateMD5(this string input)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Create a new Stringbuilder to collect the bytes
                // and create a string.
                StringBuilder sBuilder = new StringBuilder();

                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }

                // Return the hexadecimal string.
                return sBuilder.ToString();
            }
        }
    }
}
