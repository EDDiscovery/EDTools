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
using System.Runtime.CompilerServices;

namespace EDDTest
{
    // adjust to your preference
    enum ProcessResult { Nothing, Found, StopProcessing }

    interface JournalAnalyse
    {
        ProcessResult Process(string filename, int lineno, string cmdrname, JObject jr, string eventname);
        string Report();

        string OutputName { get; }
    }

    class CommonAnalyse : JournalAnalyse
    {
        public string OutputName { get; set; }
        Dictionary<string, int> rep1 = new Dictionary<string, int>();
        Dictionary<string, int> rep2 = new Dictionary<string, int>();
        Dictionary<string, int> rep3 = new Dictionary<string, int>();

        public void Incr(string value)
        {
            if (rep1.ContainsKey(value))
                rep1[value]++;
            else
                rep1[value] = 1;
        }
        public void Incr2(string value)
        {
            if (rep2.ContainsKey(value))
                rep2[value]++;
            else
                rep2[value] = 1;
        }
        public void Incr3(string value)
        {
            if (rep3.ContainsKey(value))
                rep3[value]++;
            else
                rep3[value] = 1;
        }

        public virtual ProcessResult Process(string filename, int lineno, string cmdrname, JObject jr, string eventname)
        {
            return ProcessResult.Nothing;
        }

        public string Report()
        {
            var keys = rep1.Keys.ToList();
            keys.Sort();
            string str = "";
            foreach (var key in keys)
            {
                str += $"{key} : {rep1[key]}" + Environment.NewLine;
            }

            str += "------------" + Environment.NewLine;

            keys = rep2.Keys.ToList();
            keys.Sort();
            foreach (var key in keys)
            {
                str += $"{key} : {rep2[key]}" + Environment.NewLine;
            }

            str += "------------" + Environment.NewLine;

            keys = rep3.Keys.ToList();
            keys.Sort();
            foreach (var key in keys)
            {
                str += $"{key} : {rep3[key]}" + Environment.NewLine;
            }
            return str;
        }
    }


    class ScanAnalyse : JournalAnalyse
    {
        public string OutputName { get; }
        [Flags]
        public enum EDAtmosphereProperty
        {
            None = 0,
            Hot = 1,
            Thick = 2,
            Thin = 4,
            Rich = 64,
        }

        public enum EDAtmosphereType   // from the journal
        {
            Unknown = 0,
            No = 1,         // No atmosphere
            Earth_Like,
            Ammonia,
            Water,
            Carbon_Dioxide,
            Methane,
            Helium,
            Argon,
            Neon,
            Sulphur_Dioxide,
            Nitrogen,
            Silicate_Vapour,
            Metallic_Vapour,
            Oxygen,
        }

        [Flags]
        public enum EDVolcanismProperty
        {
            None = 0,
            Minor = 1,
            Major = 2,
        }

        public enum EDVolcanism
        {
            Unknown = 0,
            No,     // No volcanism
            Water_Magma = 100,
            Sulphur_Dioxide_Magma = 200,
            Ammonia_Magma = 300,
            Methane_Magma = 400,
            Nitrogen_Magma = 500,
            Silicate_Magma = 600,
            Metallic_Magma = 700,
            Water_Geysers = 800,
            Carbon_Dioxide_Geysers = 900,
            Ammonia_Geysers = 1000,
            Methane_Geysers = 1100,
            Nitrogen_Geysers = 1200,
            Helium_Geysers = 1300,
            Silicate_Vapour_Geysers = 1400,
            Rocky_Magma = 1500,
        }




        private static Dictionary<EDAtmosphereType, string> atmoscomparestrings = null;

        private static Dictionary<string, EDVolcanism> volcanismStr2EnumLookup = null;
        public ScanAnalyse()
        {
            atmoscomparestrings = new Dictionary<EDAtmosphereType, string>();

            foreach (EDAtmosphereType atm in Enum.GetValues(typeof(EDAtmosphereType)))
            {
                atmoscomparestrings[atm] = atm.ToString().ToLowerInvariant().Replace("_", " ");
            }

            volcanismStr2EnumLookup = new Dictionary<string, EDVolcanism>(StringComparer.InvariantCultureIgnoreCase);
            foreach (EDVolcanism atm in Enum.GetValues(typeof(EDVolcanism)))
            {
                volcanismStr2EnumLookup[atm.ToString().Replace("_", "")] = atm;
            }

        }

        public static EDAtmosphereType ToEnum(string v, out EDAtmosphereProperty atmprop)
        {
            atmprop = EDAtmosphereProperty.None;

            if (v.IsEmpty())
                return EDAtmosphereType.No;

            if (v.Equals("None", StringComparison.InvariantCultureIgnoreCase))
                return EDAtmosphereType.No;

            var searchstr = v.ToLowerInvariant();

            if (searchstr.Contains("rich"))
            {
                atmprop |= EDAtmosphereProperty.Rich;
            }
            if (searchstr.Contains("thick"))
            {
                atmprop |= EDAtmosphereProperty.Thick;
            }
            if (searchstr.Contains("thin"))
            {
                atmprop |= EDAtmosphereProperty.Thin;
            }
            if (searchstr.Contains("hot"))
            {
                atmprop |= EDAtmosphereProperty.Hot;
            }

            foreach (var kvp in atmoscomparestrings)
            {
                if (searchstr.Contains(kvp.Value))     // both are lower case, does it contain it?
                    return kvp.Key;
            }
            
            atmprop = EDAtmosphereProperty.None;
            return EDAtmosphereType.Unknown;
        }


        public static EDVolcanism ToEnum(string v, out EDVolcanismProperty vprop)
        {
            vprop = EDVolcanismProperty.None;

            if (v.IsEmpty())
                return EDVolcanism.No;

            string searchstr = v.ToLowerInvariant().Replace("_", "").Replace(" ", "").Replace("-", "").Replace("volcanism", "");

            if (searchstr.Contains("minor"))
            {
                vprop |= EDVolcanismProperty.Minor;
                searchstr = searchstr.Replace("minor", "");
            }
            if (searchstr.Contains("major"))
            {
                vprop |= EDVolcanismProperty.Major;
                searchstr = searchstr.Replace("major", "");
            }

            if (volcanismStr2EnumLookup.ContainsKey(searchstr))
                return volcanismStr2EnumLookup[searchstr];

            vprop = EDVolcanismProperty.None;
            return EDVolcanism.Unknown;
        }

        private static SortedDictionary<string, string> atmtypes = new SortedDictionary<string, string>();
        private static SortedDictionary<string, string> voltypes = new SortedDictionary<string, string>();
        private static SortedDictionary<string, string> unusualbelts = new SortedDictionary<string, string>();
        public class StarPlanetRing
        {
            public string Name { get; set; }
            public string RingClass { get; set; }               // FDName 
            public enum RingClassEnum { Unknown, Rocky, Metallic, Icy, MetalRich }

            public RingClassEnum RingClassID { get; set; }      // Default will be unknown

            public double MassMT { get; set; }
            public double InnerRad { get; set; }
            public double OuterRad { get; set; }
        }

        public ProcessResult Process(string filename, int lineno, string cmdrname, JObject evt, string eventname)
        {
            if (eventname == "Scan")
            {
                string systemname = evt["StarSystem"].StrNull();
                string StarType = evt["StarType"].StrNull();
                string PlanetClass = evt["PlanetClass"].StrNull();
                string timestamp = evt["timestamp"].Str();
                string bodyname = evt["BodyName"].Str();

                if (systemname != null)
                {
                    string key = bodyname + "@" + systemname;

                    if (bodyname.ContainsIIC("Belt cluster") && !bodyname.ContainsIIC(systemname))
                    {
                        if (!unusualbelts.ContainsKey(key))
                            unusualbelts.Add(key, filename + ":" + lineno + " : " + cmdrname + " @ " + systemname);
                    }
                    if (bodyname.EndWithIIC(" Ring") || bodyname.EndWithIIC(" R1"))
                    {
                        if (!bodyname.ContainsIIC(systemname))
                        {
                            if (!unusualbelts.ContainsKey(key))
                                unusualbelts.Add(key, filename + ":" + lineno + " : " + cmdrname + " @ " + systemname);
                        }
                    }
                    StarPlanetRing[] Rings = evt["Rings"]?.ToObjectQ<StarPlanetRing[]>();            // Stars/Planets, may be Null, not belt clusters

                    foreach (var ring in Rings.EmptyIfNull())
                    {
                        if (!ring.Name.ContainsIIC(systemname) && !ring.Name.EndsWith(" Ring"))
                        {
                            string key2 = ring.Name + "@" + systemname;  
                            if (!unusualbelts.ContainsKey(key2))
                                unusualbelts.Add(key2, filename + ":" + lineno + " : " + cmdrname + " @ " + systemname);
                        }
                    }
                }

                if (PlanetClass != null)
                {
                    Dictionary<string, double> AtmosphereComposition = null;

                    JToken atmos = evt["AtmosphereComposition"];
                    if (!atmos.IsNull())
                    {
                        if (atmos.IsObject)
                        {
                            AtmosphereComposition = atmos?.ToObjectQ<Dictionary<string, double>>();
                            //System.Diagnostics.Debug.WriteLine($"Atmos list {AtmosphericComppositionList}");
                        }
                        else if (atmos.IsArray)
                        {
                            AtmosphereComposition = new Dictionary<string, double>();
                            foreach (JObject jo in atmos)
                            {
                                AtmosphereComposition[jo["Name"].Str("Default")] = jo["Percent"].Double();
                            }
                            //System.Diagnostics.Debug.WriteLine($"Atmos list {AtmosphericComppositionList}");
                        }
                    }

                    string Atmosphere = evt["Atmosphere"].StrNull();               // can be null, or empty

                    if (Atmosphere == "thick  atmosphere")            // obv a frontier bug, atmosphere type has the missing text
                    {
                        Atmosphere = "thick " + evt["AtmosphereType"].Str().SplitCapsWord() + " atmosphere";
                    }
                    else if (Atmosphere == "thin  atmosphere")
                    {
                        Atmosphere = "thin " + evt["AtmosphereType"].Str().SplitCapsWord() + " atmosphere";
                    }
                    else if (Atmosphere.IsEmpty())                         // try type.
                        Atmosphere = evt["AtmosphereType"].StrNull();       // it may still be null here or empty string

                    if (Atmosphere.IsEmpty())       // null or empty - nothing in either, see if there is composition
                    {
                        if ((AtmosphereComposition?.Count ?? 0) > 0)    // if we have some composition, synthesise name
                        {
                            foreach (var e in Enum.GetNames(typeof(EDAtmosphereType)))
                            {
                                if (AtmosphereComposition.ContainsKey(e.ToString()))       // pick first match in ID
                                {
                                    Atmosphere = e.ToString().SplitCapsWord().ToLowerInvariant();
                                    //   System.Diagnostics.Debug.WriteLine("Computed Atmosphere '" + Atmosphere + "'");
                                    break;
                                }
                            }
                        }

                        if (Atmosphere.IsEmpty())          // still nothing, set to None
                            Atmosphere = "none";
                    }
                    else
                    {
                        Atmosphere = Atmosphere.Replace("sulfur", "sulphur").SplitCapsWord().ToLowerInvariant();      // fix frontier spelling mistakes
                                                                                                                      //   System.Diagnostics.Debug.WriteLine("Atmosphere '" + Atmosphere + "'");
                    }

                    //System.IO.File.AppendAllText(@"c:\code\atmos.txt", $"Atmosphere {evt["Atmosphere"]} type {evt["AtmosphereType"]} => {Atmosphere}\r\n");

                    System.Diagnostics.Debug.Assert(Atmosphere.HasChars());

                    EDAtmosphereType AtmosphereID = ToEnum(Atmosphere.ToLowerInvariant(), out EDAtmosphereProperty ap);  // convert to internal ID
                    EDAtmosphereProperty AtmosphereProperty = ap;

                    string Volcanism = evt["Volcanism"].StrNull();
                    var VolcanismID = ToEnum(Volcanism, out EDVolcanismProperty vp);
                    var VolcanismProperty = vp;

                    if (AtmosphereID == EDAtmosphereType.Unknown)
                    {
                        System.Diagnostics.Trace.WriteLine($"Atmos {timestamp} {PlanetClass} `{Atmosphere}` => {AtmosphereID} {AtmosphereProperty} : '{evt["Atmosphere"].Str()}' '{evt["AtmosphereType"].Str()}'");
                        if ( ap != EDAtmosphereProperty.None)
                        { 
                        }

                    }

                    {
                        string key = "." + AtmosphereID.ToString() + ((AtmosphereProperty != EDAtmosphereProperty.None) ? "_" + AtmosphereProperty.ToString().Replace(", ", "_") : "");

                        string mainpart = AtmosphereID.ToString().Replace("_", " ") + ((AtmosphereProperty & EDAtmosphereProperty.Rich) != 0 ? " Rich" : "") + " Atmosphere";
                        EDAtmosphereProperty apnorich = AtmosphereProperty & ~(EDAtmosphereProperty.Rich);
                        string final = apnorich != EDAtmosphereProperty.None ? apnorich.ToString().Replace(",", "") + " " + mainpart : mainpart;
                        atmtypes[key] = final;// + " | " + Atmosphere;
                    }


                    {

                        string key = "." + VolcanismID.ToString() + (VolcanismProperty != EDVolcanismProperty.None ? "_" + VolcanismProperty.ToString() : "");

                        string mainpart = VolcanismID.ToString().Replace("_", " ") + " Volcanism";
                        string final = VolcanismProperty != EDVolcanismProperty.None ? VolcanismProperty.ToString() + " " + mainpart : mainpart;

                        voltypes[key] = final;// + " | " + Volcanism;
                    }
                }

            }

            return ProcessResult.Nothing;
        }

        public string Report()
        {
            string str = "";
            foreach (var kvp in atmtypes)
            {
                str += $"{kvp.Key}: \"{kvp.Value}\" @" + Environment.NewLine;
            }
            str += "---" + Environment.NewLine;

            foreach (var kvp in voltypes)
            {
                str += $"{kvp.Key}: \"{kvp.Value}\" @" + Environment.NewLine;
            }
            str += "---" + Environment.NewLine;

            foreach (var kvp in unusualbelts)
            {
                str += $"{kvp.Key}: {kvp.Value}" + Environment.NewLine;
            }
            return str;
        }
    }

    class BodyTypeAnalyse : JournalAnalyse
    {
        public string OutputName { get; }
        Dictionary<string, int> rep = new Dictionary<string, int>();
        public ProcessResult Process(string filename, int lineno, string cmdrname, JObject jr, string eventname)
        {
            if (jr.Contains("BodyType"))
            {
                string bt = jr["BodyType"].Str();
                if (rep.TryGetValue(bt, out int v))
                    rep[bt]++;
                else
                    rep[bt] = 1;
                return ProcessResult.Found;;
            }

            return ProcessResult.Nothing;
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

    class ScanFind : JournalAnalyse
    {
        public string BodyName;
        public bool Modern;
        public string OutputName { get; set; }

        List<Tuple<string, int, string>> rep = new List<Tuple<string, int, string>>();

        public ProcessResult Process(string filename, int lineno, string cmdrname, JObject jr, string eventname)
        {
            if (eventname == "Scan")
            {
                string bodyname = jr["BodyName"].Str();
                string starsystem= jr["StarSystem"].Str();
                if (bodyname.Contains(BodyName, StringComparison.InvariantCultureIgnoreCase) || starsystem.Contains(BodyName, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!Modern || (jr.Contains("BodyID")))
                        rep.Add(new Tuple<string, int, string>(filename, lineno, jr.ToString()));
                }
            }
            else if (eventname == "FSDJump")
            {
                string starsystem = jr["StarSystem"].Str();
                if (starsystem.Contains(BodyName, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!Modern || (jr.Contains("BodyID")))
                        rep.Add(new Tuple<string, int, string>(filename, lineno, jr.ToString()));
                }
            }

            return ProcessResult.Nothing;
        }

        public string Report()
        {
            OutputName = BodyName + ".json";

            string str = "";
            foreach (var v in rep)
            {
                str += $"{v.Item3}" + Environment.NewLine;
            }
            return str;
        }
    }

    class EconomyAnalyse : JournalAnalyse
    {
        public string OutputName { get; set; }
        Dictionary<string, int> rep = new Dictionary<string, int>();
        public ProcessResult Process(string filename, int lineno, string cmdrname, JObject jr, string eventname)
        {
            if (jr.Contains("Economy"))
            {
                string bt = jr["Economy"].Str();
                if (rep.TryGetValue(bt, out int v))
                    rep[bt]++;
                else
                    rep[bt] = 1;
                return ProcessResult.Found;;
            }
            if (jr.Contains("StationEconomy"))
            {
                string bt = jr["StationEconomy"].Str();
                if (rep.TryGetValue(bt, out int v))
                    rep[bt]++;
                else
                    rep[bt] = 1;
                return ProcessResult.Found;;
            }

            return ProcessResult.Nothing;
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
        public string OutputName { get; set; }
        SortedDictionary<string, int> rep = new SortedDictionary<string, int>();
        public ProcessResult Process(string filename, int lineno, string cmdrname, JObject jr, string eventname)
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
                return ProcessResult.Found;;
            }

            return ProcessResult.Nothing;
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
        public string OutputName { get; set; }
        Dictionary<string, int> rep = new Dictionary<string, int>();
        public ProcessResult Process(string filename, int lineno, string cmdrname, JObject jr, string eventname)
        {
            if (jr.Contains("StationType"))
            {
                string bt = jr["StationType"].Str();
                if (rep.TryGetValue(bt, out int v))
                    rep[bt]++;
                else
                    rep[bt] = 1;
                return ProcessResult.Found;;

            }

            return ProcessResult.Nothing;
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
        public string OutputName { get; set; }
        Dictionary<string, int> rep = new Dictionary<string, int>();

        void Incr(string value)
        {
            if (rep.ContainsKey(value))
                rep[value]++;
            else
                rep[value] = 1;
        }

        public ProcessResult Process(string filename, int lineno, string cmdrname, JObject jr, string eventname)
        {
            if ( eventname == "Passengers")
            {
                JArray mani = jr["Manifest"].Array();
                foreach(var item in mani)
                {
                    JObject o = item.Object();
                    Incr(o["Type"].Str());
                }

                return ProcessResult.Found;
            }

            return ProcessResult.Nothing;
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

    class BookTaxiAnalyse : JournalAnalyse
    {
        public string OutputName { get; set; }
        Dictionary<string, int> rep = new Dictionary<string, int>();
        public BookTaxiAnalyse()
        {
            rep["Count"] = 0;
            rep["Retreat"] = 0;
        }

        public ProcessResult Process(string filename, int lineno, string cmdrname, JObject jr, string eventname)
        {
            if (eventname == "BookTaxi")
            {
                rep["Count"]++;

                if (jr.Contains("Retreat"))
                    rep["Retreat"]++;
                return ProcessResult.Found;;

            }

            return ProcessResult.Nothing;
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
        public string OutputName { get; set; }
        Dictionary<string, int> rep = new Dictionary<string, int>();
        public ProcessResult Process(string filename, int lineno, string cmdrname, JObject jr, string eventname)
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
                    return ProcessResult.Found;;
                }
            }

            return ProcessResult.Nothing;
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
        public string OutputName { get; set; }
        Dictionary<string, int> rep = new Dictionary<string, int>();
        public ProcessResult Process(string filename, int lineno, string cmdrname, JObject jr, string eventname)
        {
            string bt = jr.MultiStr(new string[] { "SpawningFaction", "Faction", "VictimFaction" });

            if (bt != null && bt.StartsWith("$faction_"))
            {
                if (rep.TryGetValue(bt, out int v))
                    rep[bt]++;
                else
                    rep[bt] = 1;
                return ProcessResult.Found;;
            }

            return ProcessResult.Nothing;
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
        public string OutputName { get; set; }
        Dictionary<string, int> rep = new Dictionary<string, int>();
        public ProcessResult Process(string filename, int lineno, string cmdrname, JObject jr, string eventname)
        {
            if (eventname == "CommitCrime")
            {
                string bt = jr["CrimeType"].Str();

                if (rep.TryGetValue(bt, out int v))
                    rep[bt]++;
                else
                    rep[bt] = 1;

                return ProcessResult.Found;;
            }

            return ProcessResult.Nothing;
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
        public string OutputName { get; set; }
        Dictionary<string, int> rep = new Dictionary<string, int>();
        public ProcessResult Process(string filename, int lineno, string cmdrname, JObject jr, string eventname)
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

                return ProcessResult.Found;;
            }

            return ProcessResult.Nothing;
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
        public string OutputName { get; set; }
        void Incr(string hdr, string value)
        {
            value = hdr + (value == null ? "Missing" : value);

            if (rep.ContainsKey(value))
                rep[value]++;
            else
                rep[value] = 1;
        }

        Dictionary<string, int> rep = new Dictionary<string, int>();
        public ProcessResult Process(string filename, int lineno, string cmdrname, JObject jr, string eventname)
        {
            if ( eventname == "FSDJump" || eventname == "Location" || eventname=="CarrierJump")
            {
                JArray conflicts = jr["Conflicts"].Array();
                ProcessResult ret = ProcessResult.Nothing;

                if ( conflicts != null)
                {
                    foreach( JObject o in conflicts)
                    {
                        string wartype = o["WarType"].StrNull();
                        Incr("Conflict-Wartype-", wartype);

                        string status = o["Status"].StrNull();
                        Incr("Conflict-Status-", status);

                    }

                    ret = ProcessResult.Found;
                }

                JArray factions = jr["Factions"].Array();
                if (factions != null)
                {
                    foreach (JObject o in factions)
                    {
                        string happiness = o["Happiness"].StrNull();
                        Incr("Factions-Happiness-", happiness);
                        ret = ProcessResult.Found;

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
                    ret = ProcessResult.Found;
                }

                return ret;
            }

            return ProcessResult.Nothing;
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
        public string OutputName { get; set; }
        void Incr(string value, string entry)
        {
            if (value.Contains("Shield"))
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
        public ProcessResult Process(string filename, int lineno, string cmdrname, JObject jr, string eventname)
        {
            ProcessResult ok = ProcessResult.Nothing;
            string b1 = jr["Slot"].StrNull();
            string sh = jr["Ship"].StrNull();
            if (sh != null)
                sh = sh.ToLowerInvariant();
            if (b1 != null && sh != null)
            {
                Incr(b1, sh);
                ok = ProcessResult.Found;
            }

            string b2 = jr["FromSlot"].StrNull();
            if (b2 != null && sh != null)
            {
                Incr(b2, sh);
                ok = ProcessResult.Found;
            }

            string b3 = jr["ToSlot"].StrNull();
            if (b3 != null && sh != null)
            {
                Incr(b3, sh);
                ok = ProcessResult.Found;
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
                        ok = ProcessResult.Found;
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
                        ok = ProcessResult.Found;
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
        public string OutputName { get; set; }
        Dictionary<string, int> rep = new Dictionary<string, int>();

        void Incr(string value)
        {
            if (rep.ContainsKey(value))
                rep[value]++;
            else
                rep[value] = 1;
        }

        public ProcessResult Process(string filename, int lineno, string cmdrname, JObject jr, string eventname)
        {
            ProcessResult ret = ProcessResult.Nothing;

            {
                string bt = jr["ShipType"].StrNull();

                if (bt != null)
                {
                    string loc = jr["ShipType_Localised"].StrNull();

                    if (loc == null)
                        Incr(eventname + " Shiptype No Loc");
                    else
                        Incr(eventname + " Shiptype Loc");
                    ret = ProcessResult.Found;
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
                    ret = ProcessResult.Found;
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
                    ret = ProcessResult.Found;
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
        public string OutputName { get; set; }
        Dictionary<string, int> rep = new Dictionary<string, int>();

        void Incr(string value)
        {
            if (rep.ContainsKey(value))
                rep[value]++;
            else
                rep[value] = 1;
        }

        public ProcessResult Process(string filename, int lineno, string cmdrname, JObject jr, string eventname)
        {
            ProcessResult ret = ProcessResult.Nothing;
            {
                if (eventname == "Loadout")
                {
                    JArray ja = jr["Modules"].Array();
                    foreach (var m in ja.EmptyIfNull())
                    {
                        string item = m["Item"].Str().ToLowerInvariant();
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
                                        Incr(item + ":" + name + ":" + mod["Label"].Str());
                                    }

                                    ret = ProcessResult.Found;
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


    class MarketIDAnalyse : JournalAnalyse
    {
        public string OutputName { get; set; }
        Dictionary<long, string> rep = new Dictionary<long, string>();
        Dictionary<long, HashSet<string>> types = new Dictionary<long, HashSet<string>>();
        public ProcessResult Process(string filename, int lineno, string cmdrname, JObject evt, string eventname)
        {
            ProcessResult ret = ProcessResult.Nothing;
            {
                if (eventname == "Location" || eventname == "Docked")
                {
                    long? MarketID = evt["MarketID"].LongNull();
                    if (MarketID != null)
                    {
                        string sname = evt["StationName"].Str();
                        string stype = evt["StationType"].Str();
                        if (stype == "Bernal")
                            stype = "Ocellus";
                        string text = sname + ":" + stype;
                        if (rep.TryGetValue(MarketID.Value, out string v) && v != text)
                        {
                            Console.WriteLine($"Market {MarketID.Value} differ station {text} vs {v}");
                        }
                        rep[MarketID.Value] = text;

                        long typeid = MarketID.Value / 1000000;        // 128 xxx xxx just take first two digits
                        if (types.TryGetValue(typeid, out HashSet<string> typelist))
                        {
                            typelist.Add(stype);
                        }
                        else
                            types[typeid] = new HashSet<string>() { stype };
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

            var keys2 = types.Keys.ToList();
            keys2.Sort();

            foreach (var key in keys2)
            {
                List<string> list = types[key].ToList();
                list.Sort();
                str += $"{key} = {string.Join(",",list)}" + Environment.NewLine;
            }

            return str;
        }
    }

    class ScanDockingBodies : JournalAnalyse
    {
        public string OutputName { get; set; }

        Dictionary<string, List<Tuple<string, int>>> bodies = new Dictionary<string, List<Tuple<string, int>>>();

        void Add(string n, int id, string name)
        {
            if (!bodies.TryGetValue(n, out var b))
            {
                bodies[n] = b = new List<Tuple<string, int>>();
            }
            b.Add(new Tuple<string, int>(name, id));
        }
 
        public ProcessResult Process(string filename, int lineno, string cmdrname, JObject jr, string eventname)
        {
            if (eventname == "Scan")
            {
                string bodyname = jr["BodyName"].Str();
                int? bodyid = jr["BodyID"].IntNull();
                string planetclass = jr["PlanetClass"].StrNull();
                string starsystem = jr["StarSystem"].Str();
                JObject parentslist = jr["Parents"].Object();
                if ( bodyid>=1 && planetclass!=null )
                {
                    Add(starsystem, bodyid.Value, bodyname);
                    // System.Diagnostics.Debug.WriteLine($"Body {bodyname}:{bodyid} in {starsystem}");
                }
            }
            else if (eventname == "Docked")
            {
                int? bodyid = jr["BodyID"].IntNull();
                string stationname = jr["StationName"].Str();

                // System.Diagnostics.Debug.WriteLine($"Docked : {jr.ToString()}");
                if ( bodyid>=1)
                {
             //       System.Diagnostics.Debug.WriteLine($"Docked {stationname}:{bodyid}");
                }
            }
            else if (eventname == "Location")
            {
                string bodytype = jr["BodyType"].Str();
                int? bodyid = jr["BodyID"].IntNull();
                bool onfoot = jr["OnFoot"].Bool();
                string stationname = jr["StationName"].Str();
                string starsystem = jr["StarSystem"].Str();
                if (bodytype == "Station" && onfoot == false && stationname.HasChars())
                {
                    Add(starsystem, bodyid.Value, "STATION:" + stationname);
                    System.Diagnostics.Debug.WriteLine($"Station : {stationname} {bodyid} in {starsystem}");
                }
            }


            return ProcessResult.Nothing;
        }

        public string Report()
        {
            string str = "";
            foreach(var k in bodies)
            {
                k.Value.Sort(delegate (Tuple<string, int> left, Tuple<string, int> right) { return left.Item2.CompareTo(right.Item2); });

                bool hasstation = k.Value.Where(x => x.Item1.StartsWith("STATION")).Count() > 0;
                if (hasstation)
                {
                    foreach (var b in k.Value)
                    {
                        str += $"`{k.Key}` : {b.Item2} : {b.Item1}" + Environment.NewLine;
                    }
                }
            }

            return str;
        }
    }


    class DockedFind : JournalAnalyse
    {
        public string StationType;
        public double X, Y, Z;
        public string OutputName { get; set; }

        class Results
        {
            public string system, station;
            public double dist;

        }

        Dictionary<string, Tuple<double, double, double>> SystemLocations = new Dictionary<string, Tuple<double, double, double>>();

        Dictionary<string, Results> rep = new Dictionary<string, Results>();

        public ProcessResult Process(string filename, int lineno, string cmdrname, JObject jr, string eventname)
        {
            if (eventname == "FSDJump")
            {
                string starsystem = jr["StarSystem"].Str();
                double? x = jr["StarPos"][0].DoubleNull();
                double? y = jr["StarPos"][1].DoubleNull();
                double? z = jr["StarPos"][2].DoubleNull();

                if ( x.HasValue )
                    SystemLocations[starsystem] = new Tuple<double, double, double>(x.Value, y.Value, z.Value);
            }
            else if (eventname == "Docked")
            {
                string starsystem = jr["StarSystem"].Str();
                string stationtype = jr["StationType"].Str();
                string stationname = jr["StationName"].Str();

                if ( stationtype.EqualsIIC(StationType))
                {
                    if ( SystemLocations.TryGetValue(starsystem ,out Tuple<double,double,double> loc))
                    {
                        string key = starsystem + ":" + stationname;
                        if (!rep.ContainsKey(key))
                        {
                            double dist = Math.Sqrt(Math.Pow(loc.Item1 - X, 2) + Math.Pow(loc.Item2 - Y, 2) + Math.Pow(loc.Item3 - Z, 2));
                            rep.Add(key, new Results() { dist = dist, system = starsystem, station = stationname });
                        }
                    }
                }
            }

            return ProcessResult.Nothing;
        }

        public string Report()
        {
            var orderedlist = rep.Values.ToList();
            orderedlist.Sort(delegate (Results left, Results right) { return left.dist.CompareTo(right.dist); });

            string str = "";
            foreach (var v in orderedlist)
            {
                str += $"{v.dist} : {v.system} : {v.station}" + Environment.NewLine;
            }
            return str;
        }
    }


    class LoadGameAnalyse : JournalAnalyse
    {
        public string OutputName { get; set; }
        Dictionary<string, DateTime> repgame = new Dictionary<string, DateTime>();
        Dictionary<string, DateTime> repbuild = new Dictionary<string, DateTime>();

        void IncrGame(string value, DateTime t)
        {
            if (repgame.ContainsKey(value))
            {
                if (t < repgame[value])
                    repgame[value] = t;
            }
            else
                repgame[value] = t;
        }
        void IncrBuild(string value, DateTime t)
        {
            if (repbuild.ContainsKey(value))
            {
                if (t < repbuild[value])
                    repbuild[value] = t;
            }
            else
                repbuild[value] = t;
        }

        public ProcessResult Process(string filename, int lineno, string cmdrname, JObject jr, string eventname)
        {
            ProcessResult ret = ProcessResult.Nothing;
            if (eventname == "Fileheader")
            {
                DateTime t = jr["timestamp"].DateTimeUTC();
                string gameversion = jr["gameversion"].StrNull();
                if (gameversion != null)
                {
                    IncrGame(gameversion,t);
                    string build = jr["build"].StrNull();
                    if (build != null)
                        IncrBuild(gameversion + ":" + build,t);

                    ret = ProcessResult.StopProcessing;
                }

            }

            return ret;
        }

        public string Report()
        {
            string str = "";

            {
                str += "Games:" + Environment.NewLine;
                var bydates = repgame.ToDictionary(key => key.Value, value => value.Key);

                var keys = bydates.Keys.ToList();
                keys.Sort();

                foreach (var key in keys)
                {
                    str += $"{key.ToStringZulu()} : {bydates[key]}" + Environment.NewLine;
                }
            }

            {
                str += "Builds:" + Environment.NewLine;
                var bydates = repbuild.ToDictionary(key => key.Value, value => value.Key);
                
                var keys = bydates.Keys.ToList();
                keys.Sort();

                foreach (var key in keys)
                {
                    str += $"{key.ToStringZulu()} : \"{bydates[key]}\"" + Environment.NewLine;
                }
            }


            return str;
        }
    }


    class MissionCompleteAnalyse : JournalAnalyse
    {
        public class EffectTrend
        {
            public string Effect;
            public string Effect_Localised;
            public string Trend;
        }

        public class InfluenceTrend
        {
            public long SystemAddress;
            public string Trend;
            public string Influence; // not in very early ones
        }

        public class FactionEffectsEntry
        {
            public string Faction;
            public EffectTrend[] Effects;
            public InfluenceTrend[] Influence;
            public string Reputation;
            public string ReputationTrend;
        }

        public string OutputName { get; set; }
        Dictionary<string, int> rep = new Dictionary<string, int>();

        void Incr(string value)
        {
            if (rep.ContainsKey(value))
                rep[value]++;
            else
                rep[value] = 1;
        }

        public ProcessResult Process(string filename, int lineno, string cmdrname, JObject jr, string eventname)
        {
            ProcessResult ret = ProcessResult.Nothing;

            if (eventname == "MissionCompleted")
            {
                DateTime t = jr["timestamp"].DateTimeUTC();

                if (t > new DateTime(2022, 1, 1))
                {
                    var FactionEffects = jr["FactionEffects"]?.ToObjectQ<FactionEffectsEntry[]>();

                    foreach (var fee in FactionEffects.EmptyIfNull())
                    {
                        Incr($"Influence {fee.Influence.Length} Effects {fee.Effects.Length}");

                        if (fee.Influence.Length == 0 && fee.Effects.Length == 1)
                        {

                        }
                    }
                }

                return ProcessResult.Found;
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
                str += $"{key}: {rep[key]}" + Environment.NewLine;
            }
            return str;
        }
    }


    class MissionAcceptedAnalyse : CommonAnalyse
    {
        public override ProcessResult Process(string filename, int lineno, string cmdrname, JObject jr, string eventname)
        {
            ProcessResult ret = ProcessResult.Nothing;

            if (eventname == "MissionAccepted")
            {
                DateTime t = jr["timestamp"].DateTimeUTC();
                string targettype = jr["TargetType"].Str();
                Incr(targettype);
                string target = jr["Target"].Str();
                Incr2(target);
                string targetloc = jr["Target_Localised"].Str();
                if ( targetloc.HasChars())
                    Incr3(target + ":" + targetloc);

                return ProcessResult.Found;
            }
            return ret;
        }
    }


    //  journalanalyse "c:\users\rk\saved games\frontier developments\elite dangerous" *.log loadout
    //  journalanalyse loadout "c:\code\logs" *.log 

    public static class JournalAnalysis
    {
        public static void Analyse(CommandArgs args)
        {
            string type = args.Next();
            string path = args.Next();
            string filename = args.Next();
            DateTime starttime = args.Next().ParseDateTime(DateTime.MinValue, System.Globalization.CultureInfo.CurrentCulture);

            if (path == "J")
            {
                path = @"c:\users\rk\saved games\frontier developments\elite dangerous";
            }
            else if (path == "L")
            {
                path = @"c:\code\logs";
            }

            FileInfo[] allFiles = Directory.EnumerateFiles(path, filename, SearchOption.AllDirectories).Select(f => new FileInfo(f)).
                                Where(g=>g.LastWriteTimeUtc>=starttime).OrderBy(p => p.FullName).ToArray();

            JournalAnalyse ja = null;
            type = type.ToLowerInvariant();
            if (type == "slot")
                ja = new SlotAnalyse();
            else if (type == "scan")
                ja = new ScanAnalyse();
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
            else if (type == "booktaxi")
                ja = new BookTaxiAnalyse();
            else if (type == "marketid")
                ja = new MarketIDAnalyse();
            else if (type == "bodytype")
                ja = new BodyTypeAnalyse();
            else if (type == "passengers")
                ja = new PassengerAnalyse();
            else if (type == "loadgame")
                ja = new LoadGameAnalyse();
            else if (type == "missioncomplete")
                ja = new MissionCompleteAnalyse();
            else if (type == "missionaccepted")
                ja = new MissionAcceptedAnalyse();
            else if (type == "scanfind")
            {
                var sf = new ScanFind();
                sf.BodyName = args.Next();
                sf.Modern = args.Bool();
                System.Diagnostics.Debug.Assert(sf.BodyName != null);
                ja = sf;
            }
            else if (type == "docked")      // journalanalyse docked J *.log X Coriolis 1 2 3
            {
                var sf = new DockedFind();
                sf.StationType = args.Next();
                sf.X = args.Double();
                sf.Y = args.Double();
                sf.Z = args.Double();
                ja = sf;
            }
            else if (type == "stationdocked")
            {
                var sf = new ScanDockingBodies();
                ja = sf;
            }
            else
            {
                Console.Error.WriteLine("Not recognised analysis type");
                return;
            }

            int filecount = 0;

            foreach (var fi in allFiles)
            {
                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey().Key == ConsoleKey.Escape)
                        break;
                }

                if (filecount++ % 50 == 0)
                {
                    Console.WriteLine($"Processing {fi.FullName} count {filecount}");
                    System.Diagnostics.Debug.WriteLine($"Processing {fi.FullName} count {filecount}");
                }

                int found = 0;
                string cmdrname = "?";

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

                                if (eventname == "Commander")
                                    cmdrname = jr["Name"].Str();

                                ProcessResult pr = ja.Process(fi.FullName, lineno, cmdrname, jr, eventname);
                                if (pr == ProcessResult.Found)
                                    found++;
                                else if (pr == ProcessResult.StopProcessing)
                                    break;
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
            File.WriteAllText(ja.OutputName ?? "report.txt", rep);
        }
    }
}


        

