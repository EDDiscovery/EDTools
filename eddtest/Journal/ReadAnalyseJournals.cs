/*
 * Copyright © 2015 - 2024 robbyxp @ github.com
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
        bool Process(int lineno, JObject jr, string eventname);
        void Report();
    }

    class ScanAnalyse : JournalAnalyse
    {
        public bool Process(int lineno, JObject jr, string eventname)
        {
            if (eventname == "Scan")
            {
                if (jr["BodyName"].Str().Contains("Ring", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine(jr.ToString());
                    return true;
                }
            }

            return false;
        }

        public void Report()
        {
        }
    }

    class BodyTypeAnalyse : JournalAnalyse
    {
        Dictionary<string, int> rep = new Dictionary<string, int>();
        public bool Process(int lineno, JObject jr, string eventname)
        {
            if (jr.Contains("BodyType"))
            {
                string bt = jr["BodyType"].Str();
                if (rep.TryGetValue(bt, out int v))
                    rep[bt]++;
                else
                    rep[bt] = 1;
                return true;
            }

            return false;
        }

        public void Report()
        {
            foreach (var kvp in rep)
            {
                Console.WriteLine($"{kvp.Key} {kvp.Value}");
            }
        }
    }

    class EconomyAnalyse : JournalAnalyse
    {
        Dictionary<string, int> rep = new Dictionary<string, int>();
        public bool Process(int lineno, JObject jr, string eventname)
        {
            if (jr.Contains("Economy"))
            {
                string bt = jr["Economy"].Str();
                if (rep.TryGetValue(bt, out int v))
                    rep[bt]++;
                else
                    rep[bt] = 1;
                return true;
            }
            if (jr.Contains("StationEconomy"))
            {
                string bt = jr["StationEconomy"].Str();
                if (rep.TryGetValue(bt, out int v))
                    rep[bt]++;
                else
                    rep[bt] = 1;
                return true;
            }

            return false;
        }

        public void Report()
        {
            foreach (var kvp in rep)
            {
                Console.WriteLine($"{kvp.Key} {kvp.Value}");
            }
        }
    }


    class ServicesAnalyse : JournalAnalyse
    {
        Dictionary<string, int> rep = new Dictionary<string, int>();
        public bool Process(int lineno, JObject jr, string eventname)
        {
            if (jr.Contains("StationServices"))
            {
                JArray je = jr["StationServices"].Array();
                foreach (var ss in je)
                {
                    string bt = ss.Str().ToLower();
                    if (rep.TryGetValue(bt, out int v))
                        rep[bt]++;
                    else
                        rep[bt] = 1;
                    return true;

                }
            }

            return false;
        }

        public void Report()
        {
            foreach (var kvp in rep)
            {
                Console.WriteLine($"[\"{kvp.Key}\"] = \"{kvp.Key}\",");
            }
        }
    }


    class StationTypeAnalyse : JournalAnalyse
    {
        Dictionary<string, int> rep = new Dictionary<string, int>();
        public bool Process(int lineno, JObject jr, string eventname)
        {
            if (jr.Contains("StationType"))
            {
                string bt = jr["StationType"].Str();
                if (rep.TryGetValue(bt, out int v))
                    rep[bt]++;
                else
                    rep[bt] = 1;
                return true;

            }

            return false;
        }

        public void Report()
        {
            foreach (var kvp in rep)
            {
                Console.WriteLine($"[\"{kvp.Key}\"] = \"{kvp.Key}\",");
            }
        }
    }

    class PassengerAnalyse : JournalAnalyse
    {
        Dictionary<string, int> rep = new Dictionary<string, int>();
        public bool Process(int lineno, JObject jr, string eventname)
        {
            if (jr.Contains("PassengerType"))
            {
                string bt = jr["PassengerType"].Str();
                if (rep.TryGetValue(bt, out int v))
                    rep[bt]++;
                else
                    rep[bt] = 1;
                return true;

            }

            return false;
        }

        public void Report()
        {
            foreach (var kvp in rep)
            {
                Console.WriteLine($"[\"{kvp.Key}\"] = {kvp.Value},");
            }
        }
    }

    class AlleiganceAnalyse : JournalAnalyse
    {
        Dictionary<string, int> rep = new Dictionary<string, int>();
        public bool Process(int lineno, JObject jr, string eventname)
        {
            if (jr.Contains("SystemAllegiance"))
            {
                string bt = jr["SystemAllegiance"].Str();
                if (bt != "")
                {
                    if (rep.TryGetValue(bt, out int v))
                        rep[bt]++;
                    else
                        rep[bt] = 1;
                    return true;
                }
            }

            return false;
        }

        public void Report()
        {
            foreach (var kvp in rep)
            {
                Console.WriteLine($"[\"{kvp.Key}\"] = {kvp.Value},");
            }
        }
    }
    class FactionsAnalyse : JournalAnalyse
    {
        Dictionary<string, int> rep = new Dictionary<string, int>();
        public bool Process(int lineno, JObject jr, string eventname)
        {
            string bt = jr.MultiStr(new string[] { "SpawningFaction","Faction","VictimFaction" });

            if ( bt != null && bt.StartsWith("$faction_"))
            {
                if (rep.TryGetValue(bt, out int v))
                    rep[bt]++;
                else
                    rep[bt] = 1;
                return true;
            }

            return false;
        }

        public void Report()
        {
            foreach (var kvp in rep)
            {
                Console.WriteLine($"[\"{kvp.Key}\"] = {kvp.Value},");
            }
        }
    }


    public static class JournalReader
    {
        public static void ReadJournals(string path, string filename)
        {
            FileInfo[] allFiles = Directory.EnumerateFiles(path, filename, SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

            JournalAnalyse ja = new FactionsAnalyse();

            foreach (var fi in allFiles)
            {
                int found = 0;
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
                                if (ja.Process(lineno, jr, eventname))
                                    found++;
                            }
                        }

                        lineno++;
                    }
                }

                if ( found>0 )
                    Console.WriteLine($"Found {found} : {fi.FullName}");
                else
                {
                   // Console.WriteLine($"Nothing in : {fi.FullName}");
                }


            }

            ja.Report();
        }
    }
}


        

