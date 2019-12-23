using System;
using BaseUtils;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json.Linq;

namespace EDDTest
{
    public static partial class Journal
    {
        static void WriteToLog(string filename, string cmdrname, string lineout, bool checkjson )
        {
            if (checkjson)
            {
                try
                {
                    JToken jk = JToken.Parse(lineout);
                    Console.WriteLine(jk.ToString(Newtonsoft.Json.Formatting.Indented));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in JSON " + ex.Message);
                    return;
                }
            }

            if (!File.Exists(filename))
            {
                using (Stream fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    using (StreamWriter sr = new StreamWriter(fs))
                    {
                        string line = "{ " + TimeStamp() + "\"event\":\"Fileheader\", \"part\":1, \"language\":\"English\\\\UK\", \"gameversion\":\"2.2 (Beta 2)\", \"build\":\"r121783/r0 \" }";
sr.WriteLine(line);
                        Console.WriteLine(line);

                        string line2 = "{ " + TimeStamp() + "\"event\":\"LoadGame\", \"Commander\":\"" + cmdrname + "\", \"Ship\":\"Anaconda\", \"ShipID\":14, \"GameMode\":\"Open\", \"Credits\":18670609, \"Loan\":0 }";
sr.WriteLine(line2);
                        Console.WriteLine(line2);
                    }
                }
            }

            using (Stream fs = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                using (StreamWriter sr = new StreamWriter(fs))
                {
                    sr.WriteLine(lineout);
                    Console.WriteLine(lineout);
                }
            }
        }


        #region DEPRECIATED Helpers for journal writing - USE QuickJSONFormatter!

        public static string TimeStamp()
        {
            DateTime dt = DateTime.Now.ToUniversalTime();
            return "\"timestamp\":\"" + dt.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'") + "\", ";
        }

        public static string F(string name, long v, bool term = false)
        {
            return "\"" + name + "\":" + v + (term ? " " : ", ");
        }

        public static string F(string name, double v, bool term = false)
        {
            return "\"" + name + "\":" + v.ToStringInvariant("0.######") + (term ? " " : "\", ");
        }

        public static string F(string name, bool v, bool term = false)
        {
            return "\"" + name + "\":" + (v ? "true" : "false") + (term ? " " : "\", ");
        }

        public static string F(string name, string v, bool term = false)
        {
            return "\"" + name + "\":\"" + v + (term ? "\" " : "\", ");
        }

        public static string F(string name, DateTime v, bool term = false)
        {
            return "\"" + name + "\":\"" + v.ToString("yyyy-MM-ddTHH:mm:ssZ") + (term ? "\" " : "\", ");
        }

        public static string F(string name, int[] array, bool end = false)
        {
            string s = "";
            foreach (int a in array)
            {
                if (s.Length > 0)
                    s += ", ";

                s += a.ToStringInvariant();
            }

            return "\"" + name + "\":[" + s + "]" + (end ? "" : ", ");
        }

        public static string FF(string name, string v)      // no final comma
        {
            return F(name, v, true);
        }

        public static string FF(string name, bool v)      // no final comma
        {
            return F(name, v, true);
        }

        public static string FF(string name, long v)      // no final comma
        {
            return F(name, v, true);
        }

        #endregion

    }
}
