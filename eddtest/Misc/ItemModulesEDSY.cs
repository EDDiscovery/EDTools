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
using System.Text;

namespace EDDTest
{
    public class ItemModulesEDSY
    {
        // using chrome and the inspector, open up edsy and grab eddb.js and save it as a file - you need to do it this way, not from https://github.com/taleden/EDSY.git, as chrome spaces out the data
        // use this function to read eddb.js, convert to json, write a report, and check itemmodules.cs vs it, and replace itemmodules.cs if its wrong

        string[] itemmodules = null;

        public void ReadEDSY(string filename, string fileitemmodules)
        {
            itemmodules = File.ReadAllLines(fileitemmodules);
            if (itemmodules == null)
                return;

            // convert EDSY file to json

            StringBuilder jsontext = new StringBuilder(200000);

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
                ["agzresist"] = null,
                ["unlimit"] = null,
                ["unlimitcount"] = null,

                ["jamdur"] = "Time", // s
                ["ecmdur"] = "Time",
                ["hsdur"] = "Time",
                ["duration"] = "Time",
                ["emgcylife"] = "Time",

                ["limpettime"] = "Time",
                ["fuelxfer"] = "FuelTransfer",
                ["missile"] = "MissileType",
                ["cabincls"] = "CabinClass",
                ["spinup"] = "SCBSpinUp",
                ["scbdur"] = "SCBDuration",
                ["shieldrnfps"] = "ShieldReinforcement",

                ["thmdrain"] = "WasteHeat",
                ["ecmpwr"] = "ActivePower", // MW/use
                ["ecmheat"] = "WasteHeat",  //units/sec
                ["pwrdraw"] = "Power",
                ["brcdmg"] = "BreachDamage", // damage to target modules
                ["minbrc"] = "BreachMin",
                ["maxbrc"] = "BreachMax",
                ["thmwgt"] = "ThermalProportion",
                ["kinwgt"] = "KineticProportion",
                ["expwgt"] = "ExplosiveProportion",
                ["abswgt"] = "AbsolutePortionDamage",
                ["cauwgt"] = "CausticPortionDamage",
                ["axewgt"] = "AXPortionDamage",
                ["maxbrc"] = "BreachMax",
                ["distdraw"] = "DistributorDraw",
                ["repairrtg"] = "RepairCostPerMat",
                ["repaircon"] = "RateOfRepairConsumption",
                ["bstrof"] = "BurstRateOfFire",
                ["bstsize"] = "BurstSize",
                ["mass"] = "Mass",
                ["integ"] = "Integrity",
                ["afmrepcap"] = "Ammo",
                ["ammocost"] = "AmmoCost",
                ["ammoclip"] = "Clip",
                ["damage"] = "Damage",
                ["dps"] = "DamagePerSecond",
                ["maxrng"] = "Range",
                ["timerng"] = "TargetMaxTime", // sec to intercept
                ["dmgfall"] = "FallOff",
                ["rof"] = "RateOfFire",
                ["bstint"] = "BurstInterval",
                ["dmgmul"] = "DamageMultiplierFulLCharge",
                ["scantime"] = "Time",
                ["ecmcool"] = "ReloadTime",
                ["rldtime"] = "ReloadTime",
                ["pierce"] = "Pierce",

                ["fsdoptmass"] = "OptMass",
                ["genoptmass"] = "OptMass",
                ["engoptmass"] = "OptMass",

                ["engmaxmass"] = "MaxMass",
                ["genmaxmass"] = "MaxMass",
                ["engmaxmass"] = "MaxMass",

                ["engminmass"] = "MinMass",
                ["genminmass"] = "MinMass",
                ["engminmass"] = "MinMass",
                
                ["genpwr"] = "MWPerUnit",
                ["barrierdur"] = "Time",
                ["barrierpwr"] = "MWPerSec",
                ["barriercool"] = "ReloadTime",

                ["expres"] = "Explosive",
                ["kinres"] = "Kinetic",
                ["thmres"] = "Thermal",
                ["axeres"] = "AXResistance",
                ["genrate"] = "RegenRate",
                ["bgenrate"] = "BrokenRegenRate",
                ["genminmul"] = "MinStrength",
                ["genoptmul"] = "OptStrength",
                ["genmaxmul"] = "MaxStrength",
                ["shieldrnf"] = "AdditionalStrength",
                ["caures"] = "CausticReinforcement",
                ["shieldbst"] = "ShieldReinforcement",
                ["hullrnf"] = "HullReinforcement",
                ["engoptmul"] = "EngineOptMultiplier",
                ["engminmul"] = "EngineMinMultiplier",
                ["engmaxmul"] = "EngineMaxMultiplier",
                ["fuelpower"] = "PowerConstant",
                ["fuelmul"] = "LinearConstant",
                ["maxfuel"] = "MaxFuelPerJump",
                ["engheat"] = "ThermL",
                ["fsdheat"] = "ThermL",
                ["thmload"] = "ThermL",
                ["syschg"] = "SysMW",
                ["pwrbst"] = "PowerBonus",
                ["engchg"] = "EngMW",
                ["wepchg"] = "WepMW",
                ["syscap"] = "SysCap",
                ["engcap"] = "EngCap",
                ["wepcap"] = "WepCap",
                ["vslots"] = "Size",
                ["cargocap"] = "Size",
                ["fuelcap"] = "Size",
                ["scospd"] = "SCOSpeedIncrease",
                ["scoacc"] = "SCOAccelerationRate",
                ["scoheat"] = "SCOHeatGenerationRate",
                ["scoconint"] = "SCOControlInterference",
                ["maxangle"] = "Angle",
                ["scanangle"] = "Angle",
                ["maxangle"] = "Angle",
                ["facinglim"] = "Angle",
                ["typemis"] = "TypicalEmission",
                ["vcount"] = "Rebuilds",
                ["bins"] = "Bins",
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
                ["scbheat"] = "SCBHeat",
                ["dmgprot"] = "Protection",
                ["boottime"] = "BootTime",
                ["pwrdraw"] = "Power",
                ["integ"] = "Integrity",
                ["ammomax"] = "Ammo",
                ["ammoclip"] = "Clip",
                ["shotspd"] = "Speed",
                ["maxspd"] = "Speed",
                ["multispd"] = "MultiTargetSpeed",
                ["dps"] = "DPS",
                ["maximumrng"] = "Range",
                ["lpactrng"] = "Range",
                ["ecmrng"] = "Range",
                ["barrierrng"] = "Range",
                ["scanrng"] = "Range",
                ["maxrng"] = "Range",
                ["dmgfall"] = "Falloff",
                ["lmprepcap"] = "MaxRepairMaterialCapacity",
                ["minebonus"] = "MineBonus",

                ["minmulspd"] = "MinimumSpeedModifier",
                ["optmulspd"] = "OptimalSpeedModifier",
                ["maxmulspd"] = "MaximumSpeedModifier",
                
                ["minmulacc"] = "MinimumAccelerationModifier",
                ["optmulacc"] = "OptimalAccelerationModifier",
                ["maxmulacc"] = "MaximumAccelerationModifier",

                ["minmulrot"] = "MinimumRotationModifier",
                ["optmulrot"] = "OptimumRotationModifier",
                ["maxmulrot"] = "MaximumRotationModifier",

                ["proberad"] = "ProbeRadius"
            };

        
            //foreach( var kvp in PropertiesToEDD) System.Diagnostics.Debug.WriteLine($"[{kvp.Value.AlwaysQuoteString()}] = {kvp.Key.AlwaysQuoteString()},");

            using (Stream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader sr = new StreamReader(fs))
                {
                    string linein;
                    bool inlongcomment = false;

                    // massage javascript to json

                    while ((linein = sr.ReadLine()) != null)
                    {
                        string trimmed = linein.Trim();
                        if (inlongcomment)
                        {
                            if (trimmed.Contains("*/"))
                                inlongcomment = false;
                        }
                        else if (trimmed.StartsWith("/*"))
                        {
                            if (!trimmed.Contains("*/"))
                                inlongcomment = true;
                        }
                        else if (!trimmed.StartsWith("//") && !trimmed.StartsWith("'use"))
                        {
                            int comment = linein.IndexOf("// ");
                            if (comment == -1)
                                comment = linein.IndexOf("//\t");
                            if (comment == -1)
                                comment = linein.IndexOf("/* ");
                            if (comment == -1)
                                comment = linein.IndexOf("/*\t");
                            if (comment != -1)
                                linein = linein.Substring(0, comment);

                            string outstr = linein;

                            if (trimmed.StartsWith("var eddb = {"))
                                outstr = "{";
                            else if (trimmed == "};")
                                outstr = "}";
                            else
                            {
                                StringParser sp = new StringParser(linein);

                                string id = sp.NextWord(": ");
                                if (sp.IsCharMoveOn(':'))
                                {
                                    string lineleft = sp.LineLeft.Replace("'", "\"").Replace("NaN", "-999").Replace("1 / 0", "null").Replace("1/0", "null");
                                    if (lineleft.Contains("/") && !lineleft.Contains("\""))
                                    {
                                        // System.Diagnostics.Debug.WriteLine(lineleft);
                                        Eval evt = new Eval(lineleft, allowfp: true);
                                        if (evt.TryEvaluateDouble(false, false, out double value))
                                            lineleft = value.ToStringInvariant() + ",";
                                    }

                                    outstr = $"    {id.AlwaysQuoteString()}:{lineleft}";
                                }
                                else
                                    outstr = outstr.Replace("'", "\"");
                            }

                            jsontext.AppendLine(outstr);
                        }
                    }
                }
            }

            JObject jo = JObject.Parse(jsontext.ToString(), out string error, JToken.ParseOptions.AllowTrailingCommas);

            if (jo != null)
            {
                JObject modules = jo["module"].Object();
                JObject shiplist = jo["ship"].Object();

                foreach (var item in shiplist)
                {
                    JObject ship = item.Value.Object();

                    long shipfid = ship["fdid"].Long();
                    string shipfdname = ship["fdname"].Str();
                    string shipname = ship["name"].Str();
                  //  System.Diagnostics.Debug.WriteLine($"fid {shipfid} {shipfdname} {shipname}");

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

                            string report = $"Mass={mass}, Explosive={expres}, Kinetic={kinres}, Thermal={thmres}, AXResistance={axres}, HullStrengthBonus={hullbst}";
                          //  System.Diagnostics.Debug.WriteLine($".. {fid} {armourfdname} {mass} {kinres} {thmres} {expres} {axres} {hullbst} = {report}");

                            ProcessData(fid, shipfdname, edsyname, report);

                        }
                        else
                            System.Diagnostics.Debug.WriteLine($".. ERROR!");
                    }
                }

                foreach (var item in modules)
                {
                    JObject mod = item.Value.Object();

                    long fid = mod["fdid"].Long();
                    if (fid == 0)
                        continue;

                    string fdname = mod["fdname"].Str();
                    
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
                                    properties = properties.AppendPrePad($"{PropertiesToEDD[kvp.Key]} = {value}", ", ");
                                }
                            }
                            else
                            {
                                string nameu = char.ToUpper(kvp.Key[0]) + kvp.Key.Substring(1);
                                properties = properties.AppendPrePad($"{nameu} = {value}", ", ");

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

                File.WriteAllLines(fileitemmodules, itemmodules);
            }
            else
                File.WriteAllText(@"c:\code\errors.txt", error);
        }


        void ProcessData(long fid, string fdname, string edsyname, string parameters)
        {
            if (fid == 0)
            {
                System.Diagnostics.Debug.WriteLine($"Bad FID {fdname} : {parameters}");
                return;
            }

            string fids = fid.ToStringInvariant();
            int i = Array.FindIndex(itemmodules, x => x.Contains(fids));
            if (i >= 0)
            {
                int pos = itemmodules[i].IndexOf(fids);

                StringParser sp = new StringParser(itemmodules[i], pos);
                sp.NextLongComma(",");    // fid
                string edtype = sp.NextWordComma();  // type
                string name = sp.NextQuotedWord();
                if (sp.IsCharMoveOn(')'))
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

                        itemmodules[i] = itemmodules[i].Left(pos) + $"{fid},{edtype},{name.AlwaysQuoteString()}){lineshouldbe}";
                    }
                }
                else
                    System.Diagnostics.Debug.WriteLine($"Not in normalised form {fid} {fdname}");
            }
            else
                System.Diagnostics.Debug.WriteLine($"Can't find {fid} {fdname}");
        }
    }
}
