/*
 * Copyright © 2015 - 2021 robbyxp @ github.com
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 * 
 * EDDiscovery is not affiliated with Frontier Developments plc.
 */

using System;
using System.Collections.Generic;
using System.IO;

namespace EDDTest
{
    public static class EDDB
    {
        public static void EDDBLog(string filename, string groupname, string field, string title)
        {
            using (Stream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                Dictionary<string, string> types = new Dictionary<string, string>();

                using (StreamReader sr = new StreamReader(fs))
                {
                    string s;
                    while ((s = sr.ReadLine()) != null)
                    {
                        if (s.Contains(groupname))
                        {
                            string v = GetField(s, field);

                            if (v != null)
                            {
                                types[v] = v;
                                //Console.WriteLine("Star " + v);
                            }
                        }
                    }
                }

                foreach (string s in types.Values)
                {
                    Console.WriteLine(title + " " + s);

                }
            }
        }


        public static string GetField(string s, string f)
        {
            int i = s.IndexOf(f);
            if (i >= 0)
            {
                //Console.WriteLine(s);
                i += f.Length;

                if (s.Substring(i, 5).Equals(":null"))
                    return "Null";

                //Console.WriteLine(s.Substring(i, 20));
                i = s.IndexOf("\"", i);
                //Console.WriteLine(s.Substring(i, 20));
                if (i >= 0)
                {
                    int j = s.IndexOf("\"", i + 1);

                    if (j >= 0)
                    {
                        string ret = s.Substring(i + 1, j - i - 1);
                        return ret;
                    }
                }
            }

            return null;
        }


    }
}
