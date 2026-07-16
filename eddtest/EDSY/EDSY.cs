/*
 * Copyright © 2015 - 2024 robbyxp @ github.com
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
using System.Text;

namespace EDDTest
{
    // EDSY Debugging and extracting eddb info
    // check out EDSY : https://github.com/taleden/EDSY.git on branch master
    // copy the SLN from the folder where this source file is to the EDSY folder
    //
    // we want to output the eddb structure to a file
    //
    // We will run it in visual studio to do this.
    //
    // Open the EDSY SLN . Select google chrome as the debugger target (on toolbar)
    //
    // Fixed Dec 25 : EDSY when run locally will error, so to remove the error, change the updateUIFitHash function so it does nothing and just returns true:
    //                      edsy.js: ish 7234	var updateUIFitHash = function(buildhash) { return true;
    //
    //to get eddb JSON out: at the end of the onDomContentLoaded, place:
    //        var onDOMContentLoaded = function(e) {
    //        ... at end
    //        var out = JSON.stringify(eddb,null," ");
    //		console.log(out);
    //
    // run it. Open inspector (ctrl-shift_i). Go to console output.
    // Inspector will cut the line to size, it will show "Show More(467kb) Copy" text (hard to find I know).
    // Copy it to clipboard, paster into np++, edit and save open file in notepad++, remove to just JSON the front part
    //
    // usage
    // eddtest edsy c:\code\edsy.json "c:\Code\EDDiscovery\EliteDangerousCore\EliteDangerous\FrontierData\Items\ModulesList.cs"
    // or
    // eddtest edsy c:\code\edsy.json "c:\Code\EDDiscovery\EliteDangerousCore\EliteDangerous\FrontierData\Items\ModuleList.cs"  "c:\Code\EDDiscovery\EliteDangerousCore\EliteDangerous\FrontierData\Items\Ships.cs"
    // edsy c:\code\edsy.json "c:\Code\EDDiscovery\EliteDangerousCore\EliteDangerous\FrontierData\Items\ModuleList.cs"  "c:\Code\EDDiscovery\EliteDangerousCore\EliteDangerous\FrontierData\Items\Ships.cs"

    public partial class ItemModulesEDSY
    {
        public void ReadEDSY(string jsoneddbfilepath, string itemmodulesfilepath, string shipsmodulesfilepath)
        {
            // convert EDSY file to json
            string jsontext = FileHelpers.TryReadAllTextFromFile(jsoneddbfilepath);
            if ( jsontext == null)
            {
                Console.WriteLine($"Cannot find {jsoneddbfilepath}");
                return;
            }

            if (!File.Exists(itemmodulesfilepath))
            {
                Console.WriteLine($"Cannot find items file {itemmodulesfilepath}");
                return;
            }

            JObject jo = JObject.Parse(jsontext, out string error, JToken.ParseOptions.AllowTrailingCommas);

            if (jo != null)
            {
                string writeback = Path.GetFullPath(jsoneddbfilepath).Replace(".json","_full.json");
                File.WriteAllText(writeback, jo.ToString(true));

                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // PROCESS MODULES AND UPDATE

                itemmodules = File.ReadAllLines(itemmodulesfilepath);
                if (itemmodules == null)
                {
                    Console.WriteLine($"Can't find file {itemmodulesfilepath}");
                    return;
                }

                string[] shipmodulescsfile = null;
                if ( File.Exists(shipsmodulesfilepath??"klwkw"))
                {
                    shipmodulescsfile = File.ReadAllLines(shipsmodulesfilepath);
                }

                JObject modules = jo["module"].Object();
                JObject shiplist = jo["ship"].Object();

                int processed = 0;

                string textout = "";

                foreach (var item in modules)
                {
                    JObject mod = item.Value.Object();

                    long fid = mod["fdid"].Long();
                    if (fid == 0)
                        continue;

                    string fdname = mod["fdname"].Str();

                    if ( !mod.Contains("jitter") && (fdname.StartsWithIIC("hpt_pulselaserburst_")       // EDSY does not include jitter in some modules but its engineerable. based on mtype, add jitter
                                                || fdname.StartsWithIIC("hpt_beamlaser_")
                                                || fdname.StartsWithIIC("hpt_cannon_")
                                                || fdname.StartsWithIIC("hpt_guardian_shardcannon_")
                                                || fdname.StartsWithIIC("hpt_slugshot_")
                                                || fdname.StartsWithIIC("hpt_minelauncher_")

                                                || fdname.StartsWithIIC("hpt_atdumbfiremissile_")
                                                || fdname.StartsWithIIC("hpt_dumbfiremissilerack_")
                                                || fdname.StartsWithIIC("hpt_atventdisruptorpylon_")
                                                || fdname.StartsWithIIC("hpt_basicmissilerack_")
                                                || fdname.StartsWithIIC("hpt_advancedtorppylon_")
                                                || fdname.StartsWithIIC("hpt_drunkmissilerack_")
                                                || fdname.StartsWithIIC("hpt_causticmissile_")

                                                || fdname.StartsWithIIC("hpt_atmulticannon_")
                                                || fdname.StartsWithIIC("hpt_multicannon_")
                                                || fdname.StartsWithIIC("hpt_plasmaaccelerator_")
                                                || fdname.StartsWithIIC("hpt_railgun_")
                                                || fdname.StartsWithIIC("hpt_plasmapointdefence_")
                                                || fdname.StartsWithIIC("hpt_plasmashockcannon_")
                                                || fdname.StartsWithIIC("hpt_pulselaser_")
                                ))
                    {
                        mod["jitter"] = 0;
                    }

                    if (fdname.StartsWithIIC("int_hullreinforcement") && !mod.Contains("hullbst"))
                    {
                        mod["hullbst"] = 0;
                    }

                    if (fdname.StartsWithIIC("int_hyperdrive_") )
                    {
                        mod["neutronmult"] = fid == 129038968 ? 6 : 4;          // EDD Dodge - add in a neutron fake edsy field, this module has a 6
                    }

                    if (fdname.StartsWithIIC("hpt_slugshot") || fdname.StartsWithIIC("hpt_guardian_gausscannon"))
                    {
                        if (!mod.Contains("bstrof"))
                        {
                            mod["bstrof"] = 1;
                        }

                        if (!mod.Contains("bstsize"))
                        {
                            mod["bstsize"] = 1;
                        }
                    }

                    if (fdname.EqualsIIC("hpt_guardian_gausscannon_fixed_small"))       // these in the file do not match the edsy view on screen...
                    {
                        mod["dps"] = 19.7;
                        mod["rof"] = 0.4926;
                    }

                    if (fdname.EqualsIIC("hpt_guardian_gausscannon_fixed_medium"))      // these in the file do not match the edsy view on screen...
                    {
                        mod["dps"] = 34.46;
                        mod["rof"] = 0.4926;
                    }

                    if ( fdname.EqualsIIC("int_detailedsurfacescanner_tiny"))       // older engineering changed its mass and powerdraw, so lets add it in so it does not crash it
                    {
                        mod["mass"] = 0;
                        mod["pwrdraw"] = 0;
                    }


                    // note detailedsurface scanner is the only one without a power draw but we have seen a engineering recipe with it in
                    //if ( !mod.Contains("pwrdraw") && !fdname.ContainsIIC("powerplant") && !fdname.ContainsIIC("fueltank") && !fdname.ContainsIIC("cargorack") && !fdname.ContainsIIC("reinforcement") && !fdname.ContainsIIC("passengercabin"))
                    //{
                    //    System.Diagnostics.Debug.WriteLine($"Module {fdname} no power draw");
                    //}

                    string properties = "";

                    foreach( var kvp in mod)        // progamatically spit out the parameters
                    {
                        if (!mod[kvp.Key].IsNull)   // if we want it
                        {
                            string value = null;
                            if (kvp.Key == "fuelmul" || kvp.Key == "maxrng")
                                value = $"{mod[kvp.Key].Double(1000,-1):0.###}";
                            else
                                value = mod[kvp.Key].IsString ? mod[kvp.Key].Str().AlwaysQuoteString() : $"{mod[kvp.Key].Double():0.###}";

                            if (PropertiesToEDD.ContainsKey(kvp.Key))
                            {
                                if (PropertiesToEDD[kvp.Key] != null)
                                {
                                    if ( fdname.StartsWith("Hpt_Railgun_Fixed"))
                                    {
                                        if (kvp.Key == "dps")
                                        {
                                            value = fdname.Contains("Small") ? 14.319.ToString() : fdname.Contains("Burst") ? 23.28.ToString() : 20.46.ToString();
                                        }
                                        else if (kvp.Key == "rof")
                                        {
                                            value = fdname.Contains("Small") ? 0.6135.ToString() : fdname.Contains("Burst") ? 0.4926.ToString() : 1.5517.ToString();
                                        }
                                        else if (kvp.Key == "bstint")
                                        {
                                            value = fdname.Contains("Small") ? 0.63.ToString() : fdname.Contains("Burst") ? 0.4.ToString() : 0.63.ToString();
                                        }
                                    }
                                    properties = properties.AppendPrePad($"{PropertiesToEDD[kvp.Key]} = {value}", ", ");
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.Assert(false,$"*** ERROR unknown property {kvp.Key}");
                            }
                        }
                    }

                    //System.Diagnostics.Debug.WriteLine($"{fid}: {properties}");

                    string edsyname = mod["name"].Str();

                    bool found = ProcessData(fid, fdname, edsyname, properties, ref textout);
                    if (found)
                        processed++;

                    // free versions
                    if (fid == 128064258)
                        ProcessData(128666641, fdname, edsyname, properties, ref textout, false);
                    if (fid == 128064218)
                        ProcessData(128666640, fdname, edsyname, properties, ref textout, false);
                    if (fid == 128049381)
                        ProcessData(128049673, fdname, edsyname, properties, ref textout, false);
                    if (fid == 128064033)
                        ProcessData(128666635, fdname, edsyname, properties, ref textout, false);
                    if (fid == 128064178)
                        ProcessData(128666639, fdname, edsyname, properties, ref textout, false);
                    if (fid == 128662535)
                        ProcessData(128666642, fdname, edsyname, properties, ref textout, false);
                    if (fid == 128064068)
                        ProcessData(128666636, fdname, edsyname, properties, ref textout, false);
                    if (fid == 128064103)
                        ProcessData(128666637, fdname, edsyname, properties, ref textout, false);
                }

                if (processed == 0)
                    Console.WriteLine($"WARNING CANNOT FIND ANY MODULES IN GIVEN FILE");

                //---------------------------------------------------------------------------
                // other data

                string shiptotaltext = "";

                foreach (var item in shiplist)
                {
                    JObject ship = item.Value.Object();

                    long shipfid = ship["fdid"].Long();
                    string shipfdname = ship["fdname"].Str();
                    string shipname = ship["name"].Str();
                    //  System.Diagnostics.Debug.WriteLine($"fid {shipfid} {shipfdname} {shipname}");

                    string pad = "        ";
                    string stext = pad + $"private static ShipProperties {shipfdname.ToLowerInvariant().Replace(" ", "_")} = new ShipProperties()" + Environment.NewLine;

                    stext += pad + "{" + Environment.NewLine;

                    stext += pad + $"    FDID = \"{shipfdname}\"," + Environment.NewLine;
                    stext += pad + $"    HullMass = {ship["mass"].Int()}F," + Environment.NewLine;
                    stext += pad + $"    Name = \"{ship["name"].Str()}\"," + Environment.NewLine;
                    stext += pad + $"    Speed = {ship["topspd"].Int()}," + Environment.NewLine;
                    stext += pad + $"    Boost = {ship["bstspd"].Int()}," + Environment.NewLine;
                    stext += pad + $"    HullCost = {ship["cost"].Int()}," + Environment.NewLine;
                    stext += pad + $"    Class = {ship["class"].Int()}," + Environment.NewLine;
                    stext += pad + $"    Shields = {ship["shields"].Double()}," + Environment.NewLine;
                    stext += pad + $"    Armour = {ship["armour"].Double()}," + Environment.NewLine;
                    stext += pad + $"    MinThrust = {ship["minthrust"].Double()}," + Environment.NewLine;
                    stext += pad + $"    BoostCost = {ship["boostcost"].Double()}," + Environment.NewLine;
                    stext += pad + $"    FuelReserve = {ship["fuelreserve"].Double()}," + Environment.NewLine;
                    stext += pad + $"    HeatCap = {ship["heatcap"].Double()}," + Environment.NewLine;
                    stext += pad + $"    HeatDispMin = {ship["heatdismin"].Double()}," + Environment.NewLine;
                    stext += pad + $"    HeatDispMax = {ship["heatdismax"].Double()}," + Environment.NewLine;
                    stext += pad + $"    FuelCost = {ship["fuelcost"].Double()}," + Environment.NewLine;
                    stext += pad + $"    Hardness = {ship["hardness"].Double()}," + Environment.NewLine;
                    stext += pad + $"    Crew = {ship["crew"].Int()}," + Environment.NewLine;
                    stext += pad + $"    FwdAcc = {ship["fwdacc"].Double()}," + Environment.NewLine;
                    stext += pad + $"    RevAcc = {ship["revacc"].Double()}," + Environment.NewLine;
                    stext += pad + $"    LatAcc = {ship["latacc"].Double()}," + Environment.NewLine;

                    {
                        JArray array = ship["slots"].I("hardpoint").Array();
                        JArray slotnames = ship["slotnames"].I("hardpoint").Array();        // may be null

                        string[] name = { "Small", "Medium", "Large", "Huge" };
                        int[] count = { 1, 1, 1, 1 };
                        int index = 1;

                        stext += pad + "    Hardpoints = new ShipSlots.SlotAndSize[] {";
                        foreach (var x in array)
                        {
                            var value = x.Int();
                            if (slotnames != null)
                                stext += $"new ShipSlots.SlotAndSize(ShipSlots.Slot.{slotnames[index - 1].Str()}, {value}), ";
                            else
                                stext += "new ShipSlots.SlotAndSize(ShipSlots.Slot." + name[value - 1] + "Hardpoint" + (count[value - 1]++).ToString() + ", " + value.ToString() + "), ";
                            index++;
                        }

                        stext = stext.Left(stext.Length - 2) + "}, " + Environment.NewLine;
                    }
                    {
                        int index = 1;
                        JArray array = ship["slots"].I("utility").Array();
                        stext += pad + "    Utility = new ShipSlots.SlotAndSize[] {";
                        foreach (var x in array)
                        {
                            var value = x.Int();
                            stext += "new ShipSlots.SlotAndSize(ShipSlots.Slot.TinyHardpoint" + (index).ToString() + ", " + value.ToString() + "), ";
                            index++;
                        }

                        stext = stext.Left(stext.Length - 2) + "}, " + Environment.NewLine;
                    }

                    {
                        string[] name = { "Armour", "PowerPlant", "MainEngines", "FrameShiftDrive", "LifeSupport", "PowerDistributor", "Radar", "FuelTank" };
                        JArray array = ship["slots"].I("component").Array();
                        stext += pad + "    Component = new ShipSlots.SlotAndSize[] {";
                        int index = 0;
                        foreach (var x in array)
                        {
                            var value = x.Int();
                            stext += "new ShipSlots.SlotAndSize(ShipSlots.Slot." + name[index] + ", " + value.ToString() + "), ";
                            index++;
                        }

                        stext = stext.Left(stext.Length - 2) + "}, " + Environment.NewLine;
                    }

                    {
                        JArray array = ship["slots"].I("internal").Array();
                        JArray slotnames = ship["slotnames"].I("internal").Array();        // may be null

                        stext += pad + "    Internal = new ShipSlots.SlotAndSize[] {";
                        int index = 1;

                        foreach (var x in array)
                        {
                            var value = x.Int();
                            if (slotnames != null)
                                stext += $"new ShipSlots.SlotAndSize(ShipSlots.Slot.{slotnames[index - 1].Str()}, {value}), ";
                            else
                                stext += $"new ShipSlots.SlotAndSize(ShipSlots.Slot.Slot{index:00}_Size{value}, {value}), ";
                            index++;
                        }

                        stext = stext.Left(stext.Length - 2) + "}, " + Environment.NewLine;
                    }


                    {
                        JArray array = ship["slots"].I("military").Array();
                        stext += pad + "    Military = new ShipSlots.SlotAndSize[] {";
                        int count = 1;
                        foreach (var x in array)
                        {
                            var value = x.Int();
                            stext += $"new ShipSlots.SlotAndSize(ShipSlots.Slot.Military{count:00}, {value}), ";
                            count++;
                        }

                        if (stext.EndsWith(", "))
                            stext = stext.Left(stext.Length - 2);
                        stext += "}, " + Environment.NewLine;
                    }

                    {
                        string[] items = { "CargoHatch", "PlanetaryApproachSuite", "ShipCockpit", };
                        stext += pad + "    Other = new ShipSlots.SlotAndSize[] {";
                        int count = 0;
                        foreach (var x in items)
                            stext += $"new ShipSlots.SlotAndSize(ShipSlots.Slot.{items[count++]},1), ";
                        stext = stext.Left(stext.Length - 2) + "}, " + Environment.NewLine;
                    }

                    stext += pad + "};" + Environment.NewLine + Environment.NewLine;

                    shiptotaltext += stext;

                    textout += stext;

                    JObject minship = ship["module"].Object();

                    foreach (var mitem in minship)
                    {
                        JObject mod = mitem.Value.Object();
                        long fid = mod["fdid"].Long();
                        double mass = mod["mass"].Double();
                        string armourfdname = mod["fdname"].Str();
                        int hidden = mod["hidden"].Int(0);

                        if (hidden == 0)
                        {
                            if (modules.Contains(mitem.Key))
                            {
                                JObject infoline = modules[mitem.Key].Object();
                                string edsyname = shipname + " " + infoline["name"].Str();
                                double kinres = infoline["kinres"].Double();
                                double thmres = infoline["thmres"].Double();
                                double expres = infoline["expres"].Double();
                                double axres = infoline["axeres"].Double();
                                double hullbst = infoline["hullbst"].Double();

                                string report = $"Mass={mass}, ExplosiveResistance={expres}, KineticResistance={kinres}, ThermalResistance={thmres}, AXResistance={axres}, HullStrengthBonus={hullbst}";
                                //  System.Diagnostics.Debug.WriteLine($".. {fid} {armourfdname} {mass} {kinres} {thmres} {expres} {axres} {hullbst} = {report}");

                                ProcessData(fid, armourfdname, edsyname, report, ref textout);

                            }
                            else
                                System.Diagnostics.Debug.WriteLine($".. ERROR!");
                        }
                    }

                }

                if (shipmodulescsfile != null)
                {
                    string[] shiplines = shiptotaltext.Split(Environment.NewLine);
                    int countfront = Array.FindIndex(shipmodulescsfile, x => x.Contains("EDSY START")) + 1;
                    int lineend = Array.FindIndex(shipmodulescsfile, x => x.Contains("EDSY END"));
                    int endportion = shipmodulescsfile.Length - lineend;
                    string[] newarray = new string[shiplines.Length + countfront + endportion];
                    Array.Copy(shipmodulescsfile, 0, newarray, 0, countfront);
                    Array.Copy(shiplines, 0, newarray, countfront, shiplines.Length);
                    Array.Copy(shipmodulescsfile, lineend, newarray, countfront + shiplines.Length, shipmodulescsfile.Length - lineend);
                    File.WriteAllLines(shipsmodulesfilepath, newarray);
                }


                textout += Environment.NewLine + "Special Effects:" + Environment.NewLine;
                
                // now process special effects and write out ship modules with delta values to debug
                JObject speff = jo["expeffect"].Object();
                foreach (KeyValuePair<string, JToken> sp in speff)
                {
                    string fdname = sp.Value["fdname"].Str();
                    string name = sp.Value["special"].Str();
                    string sline = $"[{fdname.AlwaysQuoteString()}] = new ItemData.ShipModule(0,ItemData.ShipModule.ModuleTypes.SpecialEffect,{name.AlwaysQuoteString()}) {{";
                    bool prop = false;
                    foreach (KeyValuePair<string, JToken> para in (JObject)sp.Value)
                    {
                        if (fdname == "special_thermalshock" && para.Key == "damage")       // does not seem to work
                            continue;

                        if (PropertiesToEDD.ContainsKey(para.Key))
                        {
                            if (PropertiesToEDD[para.Key] != null)
                            {
                                if (prop)
                                    sline += ", ";
                                sline += $"{PropertiesToEDD[para.Key]}={para.Value}";
                                prop = true;
                            }
                        }
                        else
                            System.Diagnostics.Debug.Assert(false, $"*** ERROR unknown property {para.Key}");
                    }

                    sline += "},";
                    textout += sline + Environment.NewLine;
                }

                textout += Environment.NewLine + "Modifiers:" + Environment.NewLine;

                string modstring = $"Dictionary<string, double> modifiercontrolvalue = new Dictionary<string, double> {{\r\n";
                string fdmapping = $"Dictionary<string, string> modifierfdmapping = new Dictionary<string, string> {{\r\n";

                JArray attr = jo["attributes"].Array();

                foreach (JObject at in attr)
                {
                    string name = at["attr"].Str();
                    string fdattr = at["fdattr"].StrNull();

                    if (name.HasChars() && PropertiesToEDD.ContainsKey(name))
                    {
                        var modset = at.Contains("modset");
                        var modadd = at.Contains("modadd");
                        double? modmod = at["modmod"].DoubleNull();

                        name = PropertiesToEDD[name];

                        if (name != null)
                        {
                            if (modset)
                                modstring += $"    [{name.AlwaysQuoteString()}]=0,\r\n";
                            else if (modadd)
                                modstring += $"    [{name.AlwaysQuoteString()}]=1,\r\n";
                            else if (modmod.HasValue)
                                modstring += $"    [{name.AlwaysQuoteString()}]={modmod},\r\n";

                            if (fdattr != null)
                            {
                                // known complex variables we need to manually craft

                                if (fdattr != "DamagePerSecond" && fdattr != "Damage" && fdattr != "RateOfFire" && fdattr != "ShieldGenStrength" && fdattr != ""
                                                && fdattr != "ShieldGenOptimalMass" && fdattr != "EngineOptimalMass" && fdattr != "EngineOptPerformance" && fdattr != "Range")
                                {
                                    fdmapping += $"    [{fdattr.AlwaysQuoteString()}] = new string[] {{ nameof(ItemData.ShipModule.{name}), }},\r\n";
                                }
                                else
                                {
                                    //   fdmapping += $"    [{fdattr.AlwaysQuoteString()}] = ??? complex,\r\n";
                                }
                            }
                        }
                    }
                    else if ( fdattr != null )
                    {
                        // many have fdattr but we don't have an attribute to match

                        fdmapping += $"    [{fdattr.AlwaysQuoteString()}] = new string[] {{}},\r\n";
                    }

                }

                fdmapping += Environment.NewLine + "Triage these:" + Environment.NewLine;

                JObject fattr = jo["fdfieldattr"].Object();

                foreach( var fo in fattr)
                {
                    if ( !fo.Value.IsNull)
                    {
                        fdmapping += $"    [{fo.Key.AlwaysQuoteString()}] = new string[] {{ nameof(ItemData.ShipModule.{PropertiesToEDD[fo.Value.Str()]}) }},\r\n";
                    }
                    else
                        fdmapping += $"    [{fo.Key.AlwaysQuoteString()}] = new string[] {{}},\r\n";
                }

                modstring += $"}};\r\n";
                fdmapping += $"}};\r\n";

                textout += modstring + fdmapping;

                File.WriteAllLines(itemmodulesfilepath, itemmodules);

                //   Console.WriteLine(textout);
                File.WriteAllText(@"report.txt", textout);
            }
            else
                Console.WriteLine(error);
         }
    }
}


