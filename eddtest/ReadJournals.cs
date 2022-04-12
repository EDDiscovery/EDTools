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

using BaseUtils;
using QuickJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EDDTest
{
    // adjust to your preference

    public static class JournalReader
    {
        public static void ReadJournals(string path)
        {
            FileInfo[] allFiles = Directory.EnumerateFiles(path, "*.log", SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

            Dictionary<string, string> dict = new Dictionary<string, string>();

            foreach (var fi in allFiles)
            {
                using (StreamReader sr = new StreamReader(fi.FullName))         // read directly from file.. presume UTF8 no bom
                {
                    int lineno = 1;
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line != "")
                        {
                            JObject jr = JObject.Parse(line, out string error, JToken.ParseOptions.CheckEOL);

                            if (jr != null)
                            {
                                string ln = jr["timestamp"].Str() + ":" + jr["event"].Str();

                                if (jr["event"].Str() == "Scan")
                                {
                                    string ts = jr["TerraformState"].Str();
                                    if (ts.HasChars())
                                    {
                                        if (!dict.TryGetValue(ts, out string v))
                                        {
                                            Console.WriteLine("TS " + ts);
                                            dict[ts] = "1";
                                        }

                                    }

                                }

                            }
                            else
                                Console.WriteLine("Bad Journal line" + error);
                        }

                        lineno++;
                    }
                }
            }

        }
        public static void ReadFile(string path)
        {
            using (StreamReader sr = new StreamReader(path))         // read directly from file.. presume UTF8 no bom
            {
                Dictionary<string, int> stype = new Dictionary<string, int>();

                int lineno = 1;
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line != "" && line.IndexOf("{")>=0)
                    {
                        line = line.Substring(line.IndexOf("{"));

                        JObject jr = JObject.Parse(line, out string error, JToken.ParseOptions.CheckEOL);

                        if (jr != null)
                        {
                            JArray services = jr["StationServices"].Array();

                            if ( services!=null)
                            {
                                foreach (string je in services)
                                {
                                    string x = (string)je;
                                    x = x.ToLower();
                                    if (stype.ContainsKey(x))
                                        stype[x] = stype[x] + 1;
                                    else
                                        stype[x] = 1;

                                    if ( x == "initiatives")
                                    {
                                        System.Diagnostics.Debug.WriteLine($"{line}");
                                    }
                                }
                            }
                        }
                    }

                    lineno++;
                }

                List<string> stypes = new List<string>();
                foreach (var st in stype)
                {
                    stypes.Add(st.Key);
                    Console.WriteLine($"Service {st.Key} = {st.Value} ");
                }

                stypes.Sort();
                foreach (var st in stypes)
                {
                    Console.WriteLine($"Service {st} ");
                }

            }


        }
    }
}

