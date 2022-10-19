using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDDTest
{
    public static class QuickAssist
    {
        public static QuickJSON.JSONFormatter UTC(this QuickJSON.JSONFormatter fmt, string name)
        {
            fmt.V(name, DateTime.UtcNow.ToStringZulu());
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
