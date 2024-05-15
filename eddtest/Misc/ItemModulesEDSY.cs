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
    public static class ItemModulesEDSY
    {
        // using chrome and the inspector, open up edsy and grab edsy.js and save it as a file
        // use this function to read edsy.js, convert to json, write a report, and check itemmodules.cs vs it, and replace itemmodules.cs if its wrong


        static public void ReadEDSY(string filename, string reportout, string fileitemmodules)
        {

            // convert EDSY file to json

            StringBuilder jsontext = new StringBuilder(200000);

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
                                    string lineleft = sp.LineLeft.Replace("'", "\"").Replace("NaN", "-999").Replace("1 / 0", "null");
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

            // read json and compare

            string[] itemmodules = File.ReadAllLines(fileitemmodules);

            if ( jo != null)
            {
                JObject ship = jo["ship"].Object();
                foreach (var item in ship)
                {
                    long fid = item.Value["fdid"].Long();
                    string fdname = item.Value["fdname"].Str();
                    //System.Diagnostics.Debug.WriteLine($"fid {fid} {fdname,50}");
                }

                Dictionary<long, string> rep = new Dictionary<long, string>();

                JObject modules = jo["module"].Object();
                foreach (var item in modules)
                {
                    JObject mod = item.Value.Object();

                    long fid = mod["fdid"].Long();
                    if (fid == 0)
                        continue;

                    // we are picking out config names, and organising the output approp.  trial and error
                    // note the json file has definitions for each parameter type (ammoclip) at the top - its all data driven


                    string fdname = mod["fdname"].Str();
                    if (mod.Contains("ammomax") || mod.Contains("damage"))
                    {
                        string report = FieldBuilder.Build("Ammo:", mod["ammomax"].IntNull(),
                            "Clip:", mod["ammoclip"].IntNull(),
                            "Speed:", mod["shotspd"].IntNull(),
                            "Damage:;;0.###", mod["damage"].DoubleNull(),
                            "Range:", mod["maximumrng"].IntNull(),
                            "FallOff:", mod["dmgfall"].IntNull(),
                            "RateOfFire:;;0.##", mod["rof"].DoubleNull(),
                            "BurstInterval:;;0.##", mod["bstint"].DoubleNull(),
                            "Reload:;;0.#", mod["rldtime"].DoubleNull(),
                            "ThermL:;;0.###", mod["thmload"].DoubleNull(),
                            "ThermL:;;0.###", mod["scbheat"].DoubleNull()
                            );

                        rep[fid] = $"fid {fid} {fdname,50} :: {report} ) }},";
                    }
                    else if (mod.Contains("ecmrng"))
                    {
                        string report = FieldBuilder.Build("Range:", mod["ecmrng"].IntNull(),
                            "Time:", mod["ecmdur"].IntNull(),
                            "Reload:", mod["ecmcool"].IntNull());
                        rep[fid] = $"fid {fid} {fdname,50} :: {report} ) }},";
                    }
                    else if (mod.Contains("barrierrng"))
                    {
                        string report = FieldBuilder.Build("Range:", mod["barrierrng"].IntNull(),
                            "Time:", mod["barrierdur"].IntNull(),
                            "Reload:", mod["barriercool"].IntNull());
                        rep[fid] = $"fid {fid} {fdname,50} :: {report} ) }},";
                    }
                    else if (mod.Contains("ecmrng"))
                    {
                        string report = FieldBuilder.Build("Range:", mod["ecmrng"].IntNull(),
                            "Time:", mod["ecmdur"].IntNull(),
                            "Reload:", mod["ecmcool"].IntNull());
                        rep[fid] = $"fid {fid} {fdname,50} :: {report} ) }},";
                    }
                    else if (mod.Contains("afmrepcap"))
                    {
                        string report = FieldBuilder.Build("Ammo:", mod["afmrepcap"].IntNull()

                            );
                        rep[fid] = $"fid {fid} {fdname,50} :: {report} ) }},";

                    }
                    else if (mod.Contains("genoptmass") || mod.Contains("thmres") || mod.Contains("shieldrnf") || mod.Contains("caures"))
                    {
                        string report = FieldBuilder.Build("OptMass:", mod["genoptmass"].IntNull(),
                            "MaxMass:", mod["genmaxmass"].IntNull(),
                            "MinMass:", mod["genminmass"].IntNull(),
                            "Explosive:;;0.##", mod["expres"].DoubleNull(),
                            "Kinetic:;;0.##", mod["kinres"].DoubleNull(),
                            "Thermal:;;0.##", mod["thmres"].DoubleNull(),
                            "AXResistance:;;0.##", mod["axeres"].DoubleNull(),
                            "RegenRate:;;0.##", mod["genrate"].DoubleNull(),
                            "BrokenRegenRate:;;0.##", mod["bgenrate"].DoubleNull(),
                            "MinStrength:;;0.##", mod["genminmul"].DoubleNull(),
                            "OptStrength:;;0.##", mod["genoptmul"].DoubleNull(),
                            "MaxStrength:;;0.##", mod["genmaxmul"].DoubleNull(),
                            "CausticReinforcement:;;0.##", mod["caures"].DoubleNull(),
                            "ShieldReinforcement:;;0.##", mod["shieldrnf"].DoubleNull(),
                            "ShieldReinforcement:;;0.##", mod["shieldbst"].DoubleNull(),
                            "HullReinforcement:;;0.##", mod["hullrnf"].DoubleNull()


                            );
                        rep[fid] = $"fid {fid} {fdname,50} :: {report} ) }},";

                    }
                    else if (mod.Contains("engminmass"))
                    {
                        string report = FieldBuilder.Build("OptMass:", mod["engoptmass"].IntNull(),
                            "MaxMass:", mod["engmaxmass"].IntNull(),
                            "MinMass:", mod["engminmass"].IntNull(),
                            "ThermL:;;0.###", mod["engheat"].DoubleNull(),
                            "EngineOptMultiplier:", mod["engoptmul"].DoubleNull(),
                            "EngineMinMultiplier:", mod["engminmul"].DoubleNull(),
                            "EngineMaxMultiplier:", mod["engmaxmul"].DoubleNull()
                            );
                        rep[fid] = $"fid {fid} {fdname,50} :: {report} ) }},";

                    }
                    else if (mod.Contains("fsdoptmass"))
                    {
                        string report = FieldBuilder.Build("OptMass:", mod["fsdoptmass"].IntNull(),
                            "PowerConstant:;;0.###", mod["fuelpower"].DoubleNull(),
                            "LinearConstant:;;0.###", mod["fuelmul"].Double() * 1000,
                            "MaxFuelPerJump:;;0.###", mod["maxfuel"].DoubleNull(),
                            "ThermL:;;0.###", mod["fsdheat"].DoubleNull(),
                            "SCOSpeedIncrease:;;0.###", mod["scospd"].DoubleNull(),
                            "SCOAccelerationRate:;;0.###", mod["scoacc"].DoubleNull(),
                            "SCOHeatGenerationRate:;;0.###", mod["scoheat"].DoubleNull(),
                            "SCOControlInterference:;;0.###", mod["scoconint"].DoubleNull()
                            );
                        rep[fid] = $"fid {fid} {fdname,50} :: {report} ) }},";

                    }
                    else if (mod.Contains("scanangle"))
                    {
                        string report = FieldBuilder.Build("FacingLimit:", mod["scanangle"].DoubleNull(),
                            "Range:", mod["maxrng"].Double() * 1000,
                            "TypicalEmission:;;0.###", mod["typemis"].Double()
                            );
                        rep[fid] = $"fid {fid} {fdname,50} :: {report} ) }},";

                    }
                    else if (mod.Contains("wepcap"))
                    {
                        string report = FieldBuilder.Build(
                            "SysMW:;;0.###", mod["syschg"].DoubleNull(),
                            "EngMW:;;0.###", mod["engchg"].DoubleNull(),
                            "WepMW:;;0.###", mod["wepchg"].DoubleNull(),
                            "SysCap:;;0.###", mod["syscap"].DoubleNull(),
                            "EngCap:;;0.###", mod["engcap"].DoubleNull(),
                            "WepCap:;;0.###", mod["wepcap"].DoubleNull()
                            );
                        rep[fid] = $"fid {fid} {fdname,50} :: {report} ) }},";

                    }
                    else if (mod.Contains("fuelcap"))
                    {
                        string report = FieldBuilder.Build(
                            "Size:;;0.###", mod["fuelcap"].DoubleNull()
                            );
                        rep[fid] = $"fid {fid} {fdname,50} :: {report} ) }},";

                    }
                    else if (mod.Contains("vslots"))
                    {
                        string report = FieldBuilder.Build(
                            "Size:", mod["vslots"].IntNull(),
                            "Rebuilds:", mod["vcount"].IntNull()
                            );
                        rep[fid] = $"fid {fid} {fdname,50} :: {report} ) }},";

                    }
                    else if (mod.Contains("cargocap"))
                    {
                        string report = FieldBuilder.Build(
                            "Size:;;0.###", mod["cargocap"].DoubleNull()
                            );
                        rep[fid] = $"fid {fid} {fdname,50} :: {report} ) }},";

                    }
                    else if (mod.Contains("emgcylife"))
                    {
                        string report = FieldBuilder.Build("Time:", mod["emgcylife"].IntNull()
                            );
                        rep[fid] = $"fid {fid} {fdname,50} :: {report} ) }},";

                    }
                    else if (mod.Contains("bins"))
                    {
                        string report = FieldBuilder.Build("Bins:", mod["bins"].IntNull()

                            );
                        rep[fid] = $"fid {fid} {fdname,50} :: {report} ) }},";

                    }
                    else if (mod.Contains("pwrcap"))
                    {
                        string report = FieldBuilder.Build("PowerGen:;;0.##", mod["pwrcap"].DoubleNull(), "HeatEfficiency:;;0.##", mod["heateff"].DoubleNull()

                            );
                        rep[fid] = $"fid {fid} {fdname,50} :: {report} ) }},";

                    }
                    else if (mod.Contains("scooprate"))
                    {
                        string report = FieldBuilder.Build("RefillRate:;;0.###", mod["scooprate"].DoubleNull()

                            );
                        rep[fid] = $"fid {fid} {fdname,50} :: {report} ) }},";

                    }
                    else if (mod.Contains("maxlimpet"))
                    {
                        string report = FieldBuilder.Build("Limpets:", mod["maxlimpet"].IntNull(),
                            "Speed:", mod["maxspd"].IntNull(),
                            "Range:", mod["lpactrng"].IntNull(),
                            "TargetRange:", mod["targetrng"].IntNull(),
                            "HackTime:", mod["hacktime"].IntNull(),
                            "MinCargo:", mod["mincargo"].IntNull(),
                            "MaxCargo:", mod["maxcargo"].IntNull(),
                            "Time:", mod["limpettime"].IntNull()

                            );
                        rep[fid] = $"fid {fid} {fdname,50} :: {report} ) }},";

                    }
                    else if (mod.Contains("cabincap"))
                    {
                        string report = FieldBuilder.Build("Passengers:", mod["cabincap"].IntNull()

                            );
                        rep[fid] = $"fid {fid} {fdname,50} :: {report} ) }},";

                    }
                    else if (mod.Contains("facinglim"))
                    {
                        string report = FieldBuilder.Build("FacingLimit:", mod["facinglim"].IntNull(), "Time:", mod["timerng"].IntNull()

                            );
                        rep[fid] = $"fid {fid} {fdname,50} :: {report} ) }},";

                    }
                    else if (mod.Contains("maxangle"))
                    {
                        string report = FieldBuilder.Build("FacingLimit:", mod["maxangle"].DoubleNull(), "Range:", mod["scanrng"].IntNull(), "Time:", mod["scantime"].IntNull()

                            );
                        rep[fid] = $"fid {fid} {fdname,50} :: {report} ) }},";

                    }
                    else if (mod.Contains("jumpbst"))
                    {
                        string report = FieldBuilder.Build("AdditionalRange:;;0.##", mod["jumpbst"].DoubleNull()

                            );
                        rep[fid] = $"fid {fid} {fdname,50} :: {report} ) }},";

                    }
                    else if (mod.Contains("dmgprot"))
                    {
                        string report = FieldBuilder.Build("Protection:", mod["dmgprot"].DoubleNull()

                            );
                        rep[fid] = $"fid {fid} {fdname,50} :: {report} ) }},";

                    }
                    else
                    {
                        //   System.Diagnostics.Debug.WriteLine($"?? fid {fid} {fdname,50}");
                        continue;
                    }

                    double? edsymass = mod["mass"].DoubleNull();
                    double? edsypower = mod["pwrdraw"].DoubleNull();

                    bool found = false;

                    string fids = fid.ToStringInvariant();
                    for(int i = 0; i < itemmodules.Length; i++ )
                    {
                        int pos = itemmodules[i].IndexOf(fids);

                        if (pos > 0)
                        {
                            found = true;

                            StringParser sp = new StringParser(itemmodules[i], pos);
                            sp.NextDoubleComma(",");    // fid
                            sp.NextWordComma();  // type
                            int masspos = sp.Position;
                            double? mass = sp.NextDoubleComma(",");
                            double? power = sp.NextDoubleComma(",");
                            string name = sp.NextQuotedWord();
                            sp.IsCharMoveOn(',');

                            sp.SkipSpace();

                            System.Diagnostics.Debug.Assert(!(mass == null || power == null || name == null));

                            bool diff = false;
                            if (edsymass != null && edsymass != mass)
                            {
                                System.Diagnostics.Debug.WriteLine($"Difference mass {fid} {fdname}");
                                diff = true;
                            }

                            if (edsypower != null && edsypower != power)
                            {
                                System.Diagnostics.Debug.WriteLine($"Difference power {fid} {fdname}");
                                diff = true;
                            }

                            string left = sp.LineLeft.Trim();
                            string repleft = rep[fid].Substring(rep[fid].IndexOf("::") + 3).Trim();

                            diff |= left != repleft;

                            if ( diff )
                            {
                                System.Diagnostics.Debug.WriteLine($"Difference {fid} {fdname} `{repleft}` vs `{left}`");

                                itemmodules[i] = itemmodules[i].Left(masspos) + $"{(edsymass??0):0.###},{(edsypower??0):0.###},{name.AlwaysQuoteString()}, {repleft}";
                            }
                        }
                    }

                    if ( !found )
                    {
                        System.Diagnostics.Debug.WriteLine($"Can't find {fid} {fdname}");
                    }

                }

                List<long> keys = rep.Keys.ToList();
                keys.Sort();
                string reportstr = "";
                foreach( var key in keys)
                {
                    reportstr += rep[key] + Environment.NewLine;
                }

                File.WriteAllText(reportout, reportstr);

                File.WriteAllLines(fileitemmodules, itemmodules);


            }
            else
                File.WriteAllText(@"c:\code\errors.txt", error);
        }




    }
}
