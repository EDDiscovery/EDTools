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

    interface JournalAnalyse
    {
        void Process(int lineno, JObject jr, string eventname);
        void Report();
    }

    class ScanAnalyse : JournalAnalyse
    {
        public void Process(int lineno, JObject jr, string eventname)
        {
            if (eventname == "Scan")
            {
                if (jr["BodyName"].Str().Contains("Ring", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine(jr.ToString());
                }
            }
        }

        public void Report()
        {
        }
    }

    class BodyTypeAnalyse : JournalAnalyse
    {
        Dictionary<string, int> rep = new Dictionary<string, int>();
        public void Process(int lineno, JObject jr, string eventname)
        {
            if (jr.Contains("BodyType"))
            {
                string bt = jr["BodyType"].Str();
                if (rep.TryGetValue(bt, out int v))
                    rep[bt]++;
                else
                    rep[bt] = 1;
                // Console.WriteLine(jr.ToString());
            }
        }

        public void Report()
        {
            foreach( var kvp in rep )
            {
               Console.WriteLine($"{kvp.Key} {kvp.Value}");
            }
        }
    }


    public static class JournalReader
    {
        public static void ReadJournals(string path)
        {
            FileInfo[] allFiles = Directory.EnumerateFiles(path, "*.log", SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

            JournalAnalyse ja = new BodyTypeAnalyse();

            foreach (var fi in allFiles)
            {
                Console.WriteLine(fi.FullName);
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
                                string eventname = jr["event"].Str();
                                ja.Process(lineno, jr, eventname);
                            }
                        }

                        lineno++;
                    }
                }
            }

            ja.Report();
        }
    }
}


        

