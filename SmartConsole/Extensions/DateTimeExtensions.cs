using System;
using System.Collections.Generic;
using System.Text;

namespace Bessett.SmartConsole.TimeExtensions
{
    public static class DateTimeExtensions
    {
        public static string ToText(this TimeSpan span)
        {
            if (span.TotalDays > 1)
            {
                return $"{span.TotalDays:f3} days ({span:G})";
            }
            if (span.TotalHours > 1)
            {
                return $"{span.TotalHours:f3} hours";
            }
            if (span.TotalMinutes > 1)
            {
                return $"{span.TotalMinutes:f3} minutes";
            }
            if (span.TotalSeconds > 1)
            {
                return $"{span.TotalSeconds:f3} seconds";
            }

            return $"{span.TotalMilliseconds:f3} msec";

        }
    }
}
