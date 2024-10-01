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
        string Report();
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

        public string Report()
        {
            return "";
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

        public string Report() 
        {
            string str = "";
            foreach (var kvp in rep)
            {
                str += $"{kvp.Key} {kvp.Value}" + Environment.NewLine;
            }
            return str;
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

        public string Report()
        {
            string str = "";
            foreach (var kvp in rep)
            {
                str += $"{kvp.Key} {kvp.Value}" + Environment.NewLine;
            }
            return str;

        }
    }


    class ServicesAnalyse : JournalAnalyse
    {
        SortedDictionary<string, int> rep = new SortedDictionary<string, int>();
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

                }
                return true;
            }

            return false;
        }

        public string Report()
        {
            string str = "";
            foreach (var kvp in rep)
            {
                str += $"{kvp.Key} {kvp.Value}" + Environment.NewLine;
            }
            return str;

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

        public string Report()
        {
            string str = "";
            foreach (var kvp in rep)
            {
                str += $"{kvp.Key} {kvp.Value}" + Environment.NewLine;
            }
            return str;

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

        public string Report()
        {
            string str = "";
            foreach (var kvp in rep)
            {
                str += $"{kvp.Key} {kvp.Value}" + Environment.NewLine;
            }
            return str;

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

        public string Report()
        {
            string str = "";
            foreach (var kvp in rep)
            {
                str += $"{kvp.Key} {kvp.Value}" + Environment.NewLine;
            }
            return str;

        }
    }
    class FactionsAnalyse : JournalAnalyse
    {
        Dictionary<string, int> rep = new Dictionary<string, int>();
        public bool Process(int lineno, JObject jr, string eventname)
        {
            string bt = jr.MultiStr(new string[] { "SpawningFaction", "Faction", "VictimFaction" });

            if (bt != null && bt.StartsWith("$faction_"))
            {
                if (rep.TryGetValue(bt, out int v))
                    rep[bt]++;
                else
                    rep[bt] = 1;
                return true;
            }

            return false;
        }

        public string Report()
        {
            string str = "";
            foreach (var kvp in rep)
            {
                str += $"{kvp.Key} {kvp.Value}" + Environment.NewLine;
            }
            return str;

        }
    }

    class CrimeAnalyse : JournalAnalyse
    {
        Dictionary<string, int> rep = new Dictionary<string, int>();
        public bool Process(int lineno, JObject jr, string eventname)
        {
            if (eventname == "CommitCrime")
            {
                string bt = jr["CrimeType"].Str();

                if (rep.TryGetValue(bt, out int v))
                    rep[bt]++;
                else
                    rep[bt] = 1;

                return true;
            }

            return false;
        }

        public string Report()
        {
            string str = "";
            foreach (var kvp in rep)
            {
                str += $"{kvp.Key} {kvp.Value}" + Environment.NewLine;
            }
            return str;

        }
    }

    class PowerPlayStateAnalyse : JournalAnalyse
    {
        Dictionary<string, int> rep = new Dictionary<string, int>();
        public bool Process(int lineno, JObject jr, string eventname)
        {
            string bt = jr["PowerplayState"].StrNull();

            if (bt != null)
            {
                if (rep.TryGetValue(eventname, out int v1))
                    rep[eventname]++;
                else
                    rep[eventname] = 1;

                if (rep.TryGetValue(bt, out int v))
                    rep[bt]++;
                else
                    rep[bt] = 1;

                return true;
            }

            return false;
        }

        public string Report()
        {
            string str = "";
            foreach (var kvp in rep)
            {
                str += $"{kvp.Key} {kvp.Value}" + Environment.NewLine;
            }
            return str;

        }
    }



    class FSDLocAnalyse : JournalAnalyse
    {
        void Incr(string hdr, string value)
        {
            value = hdr + (value == null ? "Missing" : value);

            if (rep.ContainsKey(value))
                rep[value]++;
            else
                rep[value] = 1;
        }

        Dictionary<string, int> rep = new Dictionary<string, int>();
        public bool Process(int lineno, JObject jr, string eventname)
        {
            if ( eventname == "FSDJump" || eventname == "Location" || eventname=="CarrierJump")
            {
                JArray conflicts = jr["Conflicts"].Array();
                bool ret = false;

                if ( conflicts != null)
                {
                    foreach( JObject o in conflicts)
                    {
                        string wartype = o["WarType"].StrNull();
                        Incr("Conflict-Wartype-", wartype);

                        string status = o["Status"].StrNull();
                        Incr("Conflict-Status-", status);

                    }

                    ret = true;
                }

                JArray factions = jr["Factions"].Array();
                if (factions != null)
                {
                    foreach (JObject o in factions)
                    {
                        string happiness = o["Happiness"].StrNull();
                        Incr("Factions-Happiness-", happiness);
                        ret = true;

                        JArray pendingstates = o["PendingStates"].Array();
                        foreach (JObject o1 in pendingstates.EmptyIfNull())
                        {
                            string state = o1["State"].StrNull();
                            Incr("Factions-PendingStates-", state);
                        }
                        JArray recoveringstates = o["RecoveringStates"].Array();
                        foreach (JObject o1 in recoveringstates.EmptyIfNull())
                        {
                            string state = o1["State"].StrNull();
                            Incr("Factions-RecoveringStates-", state);
                        }
                        JArray activestates = o["ActiveStates"].Array();
                        foreach (JObject o1 in activestates.EmptyIfNull())
                        {
                            string state = o1["State"].StrNull();
                            Incr("Factions-ActiveStates-", state);
                        }

                    }

                }

                JObject th = jr["ThargoidWar"].Object();
                if (th != null)
                {
                    string currentstate = th["CurrentState"].StrNull();
                    Incr("thargoidwar-currentstate-", currentstate);
                    string nextsuccessstate = th["NextStateSuccess"].StrNull();
                    Incr("thargoidwar-nextsuccessstate-", nextsuccessstate);
                    string nextfailurestate = th["NextStateFailure"].StrNull();
                    Incr("thargoidwar-nextfailurestate-", nextfailurestate);
                    ret = true;

                }

                return ret;
            }

            return false;
        }

        public string Report()
        {
            string str = "";
            var keys = rep.Keys.ToList();
            keys.Sort();
            foreach (var key in keys)
            {
                str += $"{key} {rep[key]}" + Environment.NewLine;
            }
            return str;

        }
    }


    class SlotAnalyse : JournalAnalyse
    {
        void Incr(string value, string entry)
        {
            if ( value.Contains("Shield"))
            {

            }
            if (rep.ContainsKey(value))
            {
                if (!rep[value].Contains(entry))
                    rep[value].Add(entry);
            }
            else
                rep[value] = new HashSet<string> { entry };
        }
        Dictionary<string, HashSet<string>> rep = new Dictionary<string, HashSet<string>>();
        public bool Process(int lineno, JObject jr, string eventname)
        {
            bool ok = false;
            string b1 = jr["Slot"].StrNull();
            string sh = jr["Ship"].StrNull();
            if (sh != null)
                sh = sh.ToLowerInvariant();
            if (b1 != null && sh != null)
            {
                Incr(b1, sh);
                ok = true;
            }

            string b2 = jr["FromSlot"].StrNull();
            if (b2 != null && sh != null)
            {
                Incr(b2, sh);
                ok = true;
            }

            string b3 = jr["ToSlot"].StrNull();
            if (b3 != null && sh != null)
            {
                Incr(b3, sh);
                ok = true;
            }

            if (eventname == "Loadout")
            {
                JArray modules = jr["Modules"].Array();

                foreach (JObject jo in modules.EmptyIfNull())
                {
                    string sb3 = jo["Slot"].StrNull();
                    if (sb3 != null)
                    {
                        Incr(sb3, sh);
                        ok = true;
                    }

                }
            }

            if (eventname == "ModuleInfo")
            {
                JArray modules = jr["Modules"].Array();

                foreach (JObject jo in modules.EmptyIfNull())
                {
                    string sb3 = jo["Slot"].StrNull();
                    if (sb3 != null)
                    {
                        Incr(sb3, sh);
                        ok = true;
                    }

                }
            }

            return ok;
        }
        public string Report()
        {
            string str = "";
            foreach (var kvp in rep)
            {
                str += $"[Slot.{kvp.Key}] = new HashSet {{";
                foreach (var x in kvp.Value)
                    str += $"ItemData.{x},";
                str += $"}}," + Environment.NewLine;
            }

            var values = rep.Keys.ToList();
            values.Sort();
            foreach (var x in values)
                str += $"{x}," + Environment.NewLine;

            return str;
        }
    }

    class ShipTypeAnalyse : JournalAnalyse
    {
        Dictionary<string, int> rep = new Dictionary<string, int>();

        void Incr(string value)
        {
            if (rep.ContainsKey(value))
                rep[value]++;
            else
                rep[value] = 1;
        }

        public bool Process(int lineno, JObject jr, string eventname)
        {
            bool ret = false;
            {
                string bt = jr["ShipType"].StrNull();

                if (bt != null)
                {
                    string loc = jr["ShipType_Localised"].StrNull();

                    if (loc == null)
                        Incr(eventname + " Shiptype No Loc");
                    else
                        Incr(eventname + " Shiptype Loc");
                    ret = true;
                }
            }

            {
                string bt = jr["StoreOldShip"].StrNull();

                if (bt != null)
                {
                    string loc = jr["StoreOldShip_Localised"].StrNull();

                    if (loc == null)
                        Incr(eventname + " StoreOldShip No Loc");
                    else
                        Incr(eventname + " StoreOldShip Loc");
                    ret = true;
                }
            }
            {
                string bt = jr["SellOldShip"].StrNull();

                if (bt != null)
                {
                    string loc = jr["SellOldShip_Localised"].StrNull();

                    if (loc == null)
                        Incr(eventname + " SellOldShip No Loc");
                    else
                        Incr(eventname + " SellOldShip Loc");
                    ret = true;
                }
            }

            return ret;
        }

        public string Report()
        {
            var keys = rep.Keys.ToList();
            keys.Sort();
            string str = "";
            foreach (var key in keys)
            {
                str += $"{key} {rep[key]}" + Environment.NewLine;
            }
            return str;
        }
    }

    class LoadoutAnalyse : JournalAnalyse
    {
        Dictionary<string, int> rep = new Dictionary<string, int>();

        void Incr(string value)
        {
            if (rep.ContainsKey(value))
                rep[value]++;
            else
                rep[value] = 1;
        }

        public bool Process(int lineno, JObject jr, string eventname)
        {
            bool ret = false;
            {
                if (eventname == "Loadout")
                {
                    JArray ja = jr["Modules"].Array();
                    foreach (var m in ja.EmptyIfNull())
                    {
                        JObject eng = m["Engineering"].Object();
                        if (eng != null)
                        {
                            string name = eng["BlueprintName"].Str();
                            if (name != null)
                            {
                                JArray mods = eng["Modifiers"].Array();
                                if (mods != null)
                                {
                                    foreach (var mod in mods)
                                    {
                                        // System.Diagnostics.Debug.WriteLine($"Modifier: {mod.ToString()}");
                                        Incr(name + ":" + mod["Label"].Str());
                                    }

                                    ret = true;
                                }
                            }
                        }
                    }
                }
            }
            return ret;
        }

        public string Report()
        {
            var keys = rep.Keys.ToList();
            keys.Sort();

            string str = "";
            foreach (var key in keys)
            {
                str += $"[\"{key}\"] = {rep[key]}," + Environment.NewLine;
            }

            return str;
        }
    }

    //  journalanalyse "c:\users\rk\saved games\frontier developments\elite dangerous" *.log loadout
    //  journalanalyse "c:\code\logs" *.log loadout

    public static class JournalAnalysis
    {
        public static void Analyse(string path, string filename, string type)
        {
            FileInfo[] allFiles = Directory.EnumerateFiles(path, filename, SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

            JournalAnalyse ja = null;
            type = type.ToLowerInvariant();
            if (type == "slot")
                ja = new SlotAnalyse();
            else if (type == "fsdLoc")
                ja = new SlotAnalyse();
            else if (type == "shiptype")
                ja = new ShipTypeAnalyse();
            else if (type == "loadout")
                ja = new LoadoutAnalyse();
            else if (type == "services")
                ja = new ServicesAnalyse();
            else if (type == "fsdloc")
                ja = new FSDLocAnalyse();
            else
            {
                Console.Error.WriteLine("Not recognised analysis type");
                return;
            }

            foreach (var fi in allFiles)
            {
                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey().Key == ConsoleKey.Escape)
                        break;
                }

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

                if (found > 0)
                {
                //    Console.Error.WriteLine($"Found {found} : {fi.FullName}");
                  //  Console.WriteLine($"Found {found} : {fi.FullName}");
                }
                else
                {
                    // Console.WriteLine($"Nothing in : {fi.FullName}");
                }


            }

            string rep = ja.Report();
            Console.WriteLine(rep);
            File.WriteAllText("report.txt", rep);
        }
    }
}


        

