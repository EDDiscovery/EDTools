using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDDTest
{
    public static class QuickAssist
    {
        static public TimeSpan DateTimeOffset = new TimeSpan(0);

        public static QuickJSON.JSONFormatter UTC(this QuickJSON.JSONFormatter fmt, string name)
        {
            DateTime utc = DateTime.UtcNow.Add(DateTimeOffset);
            fmt.V(name, utc.ToStringZulu());
            return fmt;
        }

        public static string NewLine(this string s)
        {
            if (s.Length > 0 && !s.EndsWith(Environment.NewLine))
                s += Environment.NewLine;
            return s;

        }
    }

}
