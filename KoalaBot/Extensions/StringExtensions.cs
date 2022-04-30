using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Extensions
{
    public static class StringExtensions
    {
        private const string YES = "yes";
        private const string TRUE = "true";
        private const string NO = "no";
        private const string FALSE = "false";

        /// <summary>
        /// Converts a command argument's <see cref="string"/> to a <see cref="bool"/>.
        /// </summary>
        /// <param name="_default">Fallback if the string could not be converted.</param>
        /// <exception cref="ArgumentException">When no default is provided and the string doesn't match any cases.</exception>
        public static void ToCommandBool(this string str, out bool output, bool? _default = null)
        {
            if (str.Equals(YES,  StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals(TRUE, StringComparison.InvariantCultureIgnoreCase))
            {
                output = true;
                return;
            }

            if (str.Equals(NO,    StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals(FALSE, StringComparison.InvariantCultureIgnoreCase))
            {
                output = false;
                return;
            }

            if (_default is null)
            {
                throw new ArgumentException(nameof(str), "String could not be converted to a bool.");
            }

            output = _default.Value;
        }
    }
}
