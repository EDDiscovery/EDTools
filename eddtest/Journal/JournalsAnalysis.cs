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


        public bool Process(int lineno, JObject evt, string eventname)
        {
            if (eventname == "Scan")
            {
                string StarType = evt["StarType"].StrNull();
                string PlanetClass = evt["PlanetClass"].StrNull();
                string timestamp = evt["timestamp"].Str();

                //if (evt["BodyName"].Str().Contains("Ring", StringComparison.InvariantCultureIgnoreCase))
                //{
                //    Console.WriteLine(evt.ToString());
                //    return true;
                //}

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

            return false;
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
            return str;
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

    class BookTaxiAnalyse : JournalAnalyse
    {
        Dictionary<string, int> rep = new Dictionary<string, int>();
        public BookTaxiAnalyse()
        {
            rep["Count"] = 0;
            rep["Retreat"] = 0;
        }

        public bool Process(int lineno, JObject jr, string eventname)
        {
            if (eventname == "BookTaxi")
            {
                rep["Count"]++;

                if (jr.Contains("Retreat"))
                    rep["Retreat"]++;
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
        public static void Analyse(string path, string filename, DateTime starttime, string type)
        {
            if ( path== "J")
            {
                path = @"c:\users\rk\saved games\frontier developments\elite dangerous";
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


        

