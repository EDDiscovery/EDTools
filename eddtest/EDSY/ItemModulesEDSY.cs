﻿/*
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
    // EDSY when run locally will error, so to remove the error, change the updateUIFitHash function so it does nothing and just returns true:
    // edsy.js: ish 7234
    //	var updateUIFitHash = function(buildhash) {
    //		return true;
    //
    //to get eddb JSON out: at the end of the onDomContentLoaded, place:
    //        var onDOMContentLoaded = function(e) {
    //        ... at end
    //        var out = JSON.stringify(eddb);
    //		console.log(out);
    //
    // run it. Open inspector (ctrl-shift_i). Go to console output.
    // Inspector will cut the line to size, it will show "Show More(467kb) Copy" text. Copy it to clipboard, paster into np++, edit and save
    // open file in notepad++, remove to just JSON
    // Eddtest json filein >edsyoutput.json
    //
    // usage
    // eddtest edsy c:\code\edsyoutput.json "c:\Code\EDDiscovery\EliteDangerousCore\EliteDangerous\Items\ItemModules.cs"

    public class ItemModulesEDSY
    {
        string[] itemmodules = null;

        public void ReadEDSY(string jsoneddbfilepath, string itemmodulesfilepath)
        {
            // convert EDSY file to json

            Dictionary<string, string> PropertiesToEDD = new Dictionary<string, string>
            {
                ["mtype"] = null,
                ["name"] = null ,
                ["fdid"] = null ,
                ["fdname"] = null,
                ["eddbid"] = null,
                ["namekey"] = null,
                ["hidden"] = null,
                ["limit"] = null,
                ["tag"] = null,
                ["passive"] = null,
                ["reserved"] = null,
                ["noblueprints"] = null,
                ["powerlock"] = null,
                ["noundersize"] = null,
                ["sco"] = null,
                ["mlctype"] = null, // already in name (multi control type)
                ["noexpeffects"] = null,
                ["unlimit"] = null,
                ["unlimitcount"] = null,
                ["mats"] = null,
                ["special"] = null,

                ["integ"] = "Integrity",
                ["cost"] = "Cost",
                ["mount"] = "Mount",
                ["class"] = "Class",
                ["rating"] = "Rating",
                ["mass"] = "Mass",
                ["boottime"] = "BootTime",
                ["pwrdraw"] = "PowerDraw",
                ["distdraw"] = "DistributorDraw",
                ["rounds"] = "Rounds",
                ["jitter"] = "Jitter",
                ["hullbst"] = "HullStrengthBonus",

                ["jamdur"] = "Time", // s
                ["ecmdur"] = "Time",
                ["hsdur"] = "Time",
                ["duration"] = "Time",
                ["emgcylife"] = "Time",
                ["limpettime"] = "Time",
                ["scantime"] = "Time",
                ["barrierdur"] = "Time",

                ["maximumrng"] = "Range",
                ["lpactrng"] = "Range",
                ["ecmrng"] = "Range",
                ["barrierrng"] = "Range",
                ["scanrng"] = "Range",
                ["maxrng"] = "Range",

                ["cargocap"] = "Size",      // t
                ["fuelcap"] = "Size",       // t

                ["bins"] = "Capacity",
                ["vslots"] = "Capacity",

                ["vcount"] = "Rebuilds",

                ["shotspd"] = "Speed",      // m/s
                ["maxspd"] = "Speed",
//here
                ["fsdoptmass"] = "OptMass",
                ["genoptmass"] = "OptMass",
                ["engoptmass"] = "OptMass",

                ["engmaxmass"] = "MaxMass",
                ["genmaxmass"] = "MaxMass",
                ["engmaxmass"] = "MaxMass",

                ["engminmass"] = "MinMass",
                ["genminmass"] = "MinMass",
                ["engminmass"] = "MinMass",

                ["ecmcool"] = "ReloadTime",     // s
                ["rldtime"] = "ReloadTime",
                ["barriercool"] = "ReloadTime",

                ["maxangle"] = "Angle",     // deg
                ["scanangle"] = "Angle",
                ["facinglim"] = "Angle",

                ["engheat"] = "ThermalLoad",
                ["fsdheat"] = "ThermalLoad",
                ["thmload"] = "ThermalLoad",
                ["scbheat"] = "ThermalLoad",
                ["ecmheat"] = "ThermalLoad",  //units/sec
                ["thmdrain"] = "ThermalDrain",

                ["afmrepcap"] = "Ammo",
                ["ammomax"] = "Ammo",

                ["ammocost"] = "AmmoCost",
                ["ammoclip"] = "Clip",
                ["damage"] = "Damage",
                ["dps"] = "DPS",
                ["dmgfall"] = "Falloff",

                ["fuelxfer"] = "FuelTransfer",
                ["missile"] = "MissileType",
                ["cabincls"] = "CabinClass",

                ["spinup"] = "SCBSpinUp",
                ["scbdur"] = "SCBDuration",
                ["shieldrnfps"] = "ShieldReinforcement",

                ["genminmul"] = "MinStrength",      // shields
                ["genoptmul"] = "OptStrength",
                ["genmaxmul"] = "MaxStrength",
                ["genrate"] = "RegenRate",
                ["genpwr"] = "MWPerUnit",

                ["ecmpwr"] = "ActivePower", // MW/use


                ["brcdmg"] = "BreachDamage", // damage to target modules
                ["minbrc"] = "BreachMin",
                ["maxbrc"] = "BreachMax",

                ["thmwgt"] = "ThermalProportionDamage",
                ["kinwgt"] = "KineticProportionDamage",
                ["expwgt"] = "ExplosiveProportionDamage",
                ["abswgt"] = "AbsoluteProportionDamage",
                ["cauwgt"] = "CausticPorportionDamage",
                ["axewgt"] = "AXPorportionDamage",

                ["agzresist"] = "GuardianModuleResistance",

                ["repairrtg"] = "RepairCostPerMat",
                ["repaircon"] = "RateOfRepairConsumption",
                ["bstrof"] = "BurstRateOfFire",
                ["bstsize"] = "BurstSize",
                ["timerng"] = "TargetMaxTime", // sec to intercept
                ["rof"] = "RateOfFire",
                ["bstint"] = "BurstInterval",
                ["dmgmul"] = "DamageMultiplierFullCharge",
                ["pierce"] = "ArmourPiercing",

                ["barrierpwr"] = "MWPerSec",

                ["expres"] = "ExplosiveResistance",           // shield booster, hull reinforcer, shields (also used armour)
                ["kinres"] = "KineticResistance",
                ["thmres"] = "ThermalResistance",
                ["caures"] = "CausticResistance",
                ["axeres"] = "AXResistance",

                ["bgenrate"] = "BrokenRegenRate",
                ["shieldrnf"] = "AdditionalReinforcement",
                ["shieldbst"] = "ShieldReinforcement",
                ["hullrnf"] = "HullReinforcement",

                ["engoptmul"] = "EngineOptMultiplier",
                ["engminmul"] = "EngineMinMultiplier",
                ["engmaxmul"] = "EngineMaxMultiplier",

                ["fuelpower"] = "PowerConstant",
                ["fuelmul"] = "LinearConstant",
                ["maxfuel"] = "MaxFuelPerJump",

                ["pwrbst"] = "PowerBonus",

                ["syschg"] = "SystemsRechargeRate",
                ["syscap"] = "SystemsCapacity",

                ["engchg"] = "EngineRechargeRate",
                ["engcap"] = "EngineCapacity",

                ["wepchg"] = "WeaponsRechargeRate",
                ["wepcap"] = "WeaponsCapacity",


                ["scospd"] = "SCOSpeedIncrease",
                ["scoacc"] = "SCOAccelerationRate",
                ["scoheat"] = "SCOHeatGenerationRate",
                ["scoconint"] = "SCOControlInterference",

                ["typemis"] = "TypicalEmission",
                ["pwrcap"] = "PowerGen",
                ["heateff"] = "HeatEfficiency",
                ["scooprate"] = "RefillRate",
                ["maxlimpet"] = "Limpets",
                ["targetrng"] = "TargetRange",
                ["hacktime"] = "HackTime",
                ["mincargo"] = "MinCargo",
                ["maxcargo"] = "MaxCargo",
                ["cabincap"] = "Passengers",
                ["jumpbst"] = "AdditionalRange",
                ["dmgprot"] = "Protection",
                ["integ"] = "Integrity",
                ["multispd"] = "MultiTargetSpeed",

                ["lmprepcap"] = "MaxRepairMaterialCapacity",
                ["minebonus"] = "MineBonus",

                ["minmulspd"] = "MinimumSpeedModifier",
                ["optmulspd"] = "OptimalSpeedModifier",
                ["maxmulspd"] = "MaximumSpeedModifier",

                ["minmulacc"] = "MinimumAccelerationModifier",
                ["optmulacc"] = "OptimalAccelerationModifier",
                ["maxmulacc"] = "MaximumAccelerationModifier",

                ["minmulrot"] = "MinimumRotationModifier",
                ["optmulrot"] = "OptimalRotationModifier",
                ["maxmulrot"] = "MaximumRotationModifier",

                ["proberad"] = "ProbeRadius"
            };

            string jsontext = File.ReadAllText(jsoneddbfilepath);

            JObject jo = JObject.Parse(jsontext, out string error, JToken.ParseOptions.AllowTrailingCommas);

            if (jo != null)
            {
                File.WriteAllText(Path.GetFileNameWithoutExtension(jsoneddbfilepath) + "_full.json", jo.ToString(true));

                itemmodules = File.ReadAllLines(itemmodulesfilepath);
                if (itemmodules == null)
                    return;

                JObject modules = jo["module"].Object();
                JObject shiplist = jo["ship"].Object();

                string shipdatatext = "";

                foreach (var item in shiplist)
                {
                    JObject ship = item.Value.Object();

                    long shipfid = ship["fdid"].Long();
                    string shipfdname = ship["fdname"].Str();
                    string shipname = ship["name"].Str();
                    //  System.Diagnostics.Debug.WriteLine($"fid {shipfid} {shipfdname} {shipname}");

                    string pad = "        ";
                    string shipdata = pad + $"private static ShipProperties {shipfdname.ToLowerInvariant().Replace(" ","_")} = new ShipProperties()" + Environment.NewLine;
                    shipdata += pad + "{" + Environment.NewLine;
                    shipdata += pad + $"    FDID = \"{shipfdname}\"," + Environment.NewLine;
                    shipdata += pad + $"    HullMass = {ship["mass"].Int()}F," + Environment.NewLine;
                    shipdata += pad + $"    Name = \"{ship["name"].Str()}\"," + Environment.NewLine;
                    shipdata += pad + $"    Speed = {ship["topspd"].Int()}," + Environment.NewLine;
                    shipdata += pad + $"    Boost = {ship["bstspd"].Int()}," + Environment.NewLine;
                    shipdata += pad + $"    HullCost = {ship["cost"].Int()}," + Environment.NewLine;
                    shipdata += pad + $"    Class = {ship["class"].Int()}," + Environment.NewLine;
                    shipdata += pad+$"    Shields = {ship["shields"].Double()}," + Environment.NewLine;
                    shipdata += pad+$"    Armour = {ship["armour"].Double()}," + Environment.NewLine;
                    shipdata += pad + $"    MinThrust = {ship["minthrust"].Double()}," + Environment.NewLine;
                    shipdata += pad + $"    BoostCost = {ship["boostcost"].Double()}," + Environment.NewLine;
                    shipdata += pad + $"    FuelReserve = {ship["fuelreserve"].Double()}," + Environment.NewLine;
                    shipdata += pad + $"    HeatCap = {ship["heatcap"].Double()}," + Environment.NewLine;
                    shipdata += pad + $"    HeatDispMin = {ship["heatdismin"].Double()}," + Environment.NewLine;
                    shipdata += pad + $"    HeatDispMax = {ship["heatdismax"].Double()}," + Environment.NewLine;
                    shipdata += pad + $"    FuelCost = {ship["fuelcost"].Double()}," + Environment.NewLine;
                    shipdata += pad+$"    Hardness = {ship["hardness"].Double()}," + Environment.NewLine;
                    shipdata += pad + $"    Crew = {ship["crew"].Int()}," + Environment.NewLine;
                    shipdata += pad + $"    FwdAcc = {ship["fwdacc"].Double()}," + Environment.NewLine;
                    shipdata += pad + $"    RevAcc = {ship["revacc"].Double()}," + Environment.NewLine;
                    shipdata += pad + $"    LatAcc = {ship["latacc"].Double()}" + Environment.NewLine;
                    shipdata += pad + "};" + Environment.NewLine + Environment.NewLine;

                    shipdatatext += shipdata;

                    JObject minship = ship["module"].Object();

                    foreach (var mitem in minship)
                    {
                        JObject mod = mitem.Value.Object();
                        long fid = mod["fdid"].Long();
                        double mass = mod["mass"].Double();
                        string armourfdname = mod["fdname"].Str();

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

                            ProcessData(fid, armourfdname, edsyname, report);

                        }
                        else
                            System.Diagnostics.Debug.WriteLine($".. ERROR!");
                    }
                }

                File.WriteAllText("ships.txt", shipdatatext);

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

                    ProcessData(fid, fdname, edsyname, properties);

                    // free versions
                    if (fid == 128064258)
                        ProcessData(128666641, fdname, edsyname, properties);
                    if (fid == 128064218)
                        ProcessData(128666640, fdname, edsyname, properties);
                    if (fid == 128049381)
                        ProcessData(128049673, fdname, edsyname, properties);
                    if (fid == 128064033)
                        ProcessData(128666635, fdname, edsyname, properties);
                    if (fid == 128064178)
                        ProcessData(128666639, fdname, edsyname, properties);
                    if (fid == 128662535)
                        ProcessData(128666642, fdname, edsyname, properties);
                    if (fid == 128064068)
                        ProcessData(128666636, fdname, edsyname, properties);
                    if (fid == 128064103)
                        ProcessData(128666637, fdname, edsyname, properties);


                }

                File.WriteAllLines(itemmodulesfilepath, itemmodules);

                System.Diagnostics.Debug.WriteLine(Environment.NewLine + "Special Effects:" + Environment.NewLine);

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
                    System.Diagnostics.Debug.WriteLine($"{sline}");

                }

                System.Diagnostics.Debug.WriteLine(Environment.NewLine + "Modifiers:" + Environment.NewLine);

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

                        if (modset)
                            modstring += $"    [{name.AlwaysQuoteString()}]=0,\r\n";
                        else if (modadd)
                            modstring += $"    [{name.AlwaysQuoteString()}]=1,\r\n";
                        else if ( modmod.HasValue)
                            modstring += $"    [{name.AlwaysQuoteString()}]={modmod},\r\n";

                        if (fdattr != null)
                        {
                            // known complex variables we need to manually craft

                            if (fdattr != "DamagePerSecond" && fdattr != "Damage" && fdattr != "RateOfFire" && fdattr != "ShieldGenStrength" && fdattr != ""
                                            && fdattr != "ShieldGenOptimalMass" && fdattr != "EngineOptimalMass" && fdattr != "EngineOptPerformance" && fdattr != "Range")
                            {
                                fdmapping += $"    [{fdattr.AlwaysQuoteString()}] = new string[] {{ {name.AlwaysQuoteString()}, }},\r\n";
                            }
                            else
                            {
                             //   fdmapping += $"    [{fdattr.AlwaysQuoteString()}] = ??? complex,\r\n";
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
                        fdmapping += $"    [{fo.Key.AlwaysQuoteString()}] = new string[] {{ {PropertiesToEDD[fo.Value.Str()].AlwaysQuoteString()} }},\r\n";
                    }
                    else
                        fdmapping += $"    [{fo.Key.AlwaysQuoteString()}] = new string[] {{}},\r\n";
                }



                modstring += $"}};\r\n";
                fdmapping += $"}};\r\n";
                System.Diagnostics.Debug.WriteLine(modstring);
                System.Diagnostics.Debug.WriteLine(fdmapping);
            }
            else
                File.WriteAllText(@"c:\code\errors.txt", error);
        }


        void ProcessData(long fid, string fdname, string edsyname, string parameters)
        {
            if (fdname.StartsWithIIC("hpt_railgun_fixed_medium_burst"))
            {

            }

            int lineno = -1;

            if (fid > 0)       // find by fid if its there
            {
                string fids = "(" + fid.ToStringInvariant() + ",";
                lineno = Array.FindIndex(itemmodules, x => x.Contains(fids));
            }
            else
            {
                string fdnamef = fdname.AlwaysQuoteString();
                lineno = Array.FindIndex(itemmodules, x => x.ContainsIIC(fdnamef));     // try fdname quoted

                if ( lineno == -1 )
                {
                    lineno = Array.FindIndex(itemmodules, x => x.ContainsIIC(edsyname));    // else try text
                }
            }

            int pos = lineno >= 0 ? itemmodules[lineno].IndexOf("new ShipModule(") + 15 : -1;

            if (lineno >= 0 && pos > 0)
            {
                StringParser sp = new StringParser(itemmodules[lineno], pos);
                long? fidread = sp.NextLongComma(",");    // fid
                string edtype = sp.NextWordComma();  // type
                string name = sp.NextQuotedWord();
                if (fidread.HasValue && edtype != null && name != null && sp.IsCharMoveOn(')'))
                {
                    edsyname = edsyname.Replace("-", " ");      // Multi-Cannon
                    name = name.Replace("-", " ");      // type-6

                    if (name.Length < edsyname.Length || !edsyname.EqualsIIC(name.Substring(0, edsyname.Length)))
                    {
                        // don't turn diff on - human needed
                        System.Diagnostics.Debug.WriteLine($"Difference name {fid} {fdname} EDSY: '{edsyname}' vs ItemModules.cs: '{name}'");
                    }

                    string lineshouldbe = $"{{ {parameters} }} }},";
                    bool diff = sp.LineLeft != lineshouldbe;

                    if (diff)
                    {
                        System.Diagnostics.Debug.WriteLine($"Difference {fid} {fdname} `{sp.LineLeft}` vs `{lineshouldbe}`");

                        itemmodules[lineno] = itemmodules[lineno].Left(pos) + $"{fidread.Value},{edtype},{name.AlwaysQuoteString()}){lineshouldbe}";
                    }
                }
                else
                    System.Diagnostics.Debug.WriteLine($"Not in normalised form (front part) {fid} {fdname} : {itemmodules[lineno]}");
            }
            else
            {
                fdname = fdname.ToLower();
                string moduletype = fdname.Contains("_grade1") ? "LightweightAlloy" :
                                    fdname.Contains("_grade2") ? "ReinforcedAlloy" :
                                    fdname.Contains("_grade3") ? "MilitaryGradeComposite" :
                                    fdname.Contains("_mirrored") ? "MirroredSurfaceComposite" :
                                    fdname.Contains("_reactive") ? "ReactiveSurfaceComposite" :
                                    "Unknown";

                System.Diagnostics.Debug.WriteLine($"{{ \"{fdname}\", new ShipModule({fid}, ShipModule.ModuleTypes.{moduletype}, \"{edsyname}\") }},");
            }
        }
    }
}


