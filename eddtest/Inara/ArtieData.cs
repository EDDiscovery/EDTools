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
 *
 */

using BaseUtils;
using EliteDangerousCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace EDDTest
{
    // artierecipes "c:\code\edcd\inara\blueprints_afteroperations_oct26.csv"
    // compile up the EDC core and either change the ref to it directly, or copy it into the EDC folder here
    // run and it will compare recipes between the excel and the EDC core

    static public class ArtieData
    {
        static public void ProcessRecipes(string file)            // overall index of items
        {
            EliteDangerousCore.MaterialCommodityMicroResourceType.Initialise();

            {
                string updatefile = @"c:\code\update.lst";
                FileHelpers.DeleteFileNoError(updatefile);
                CSVFile filerecipes = new CSVFile();
                if (filerecipes.Read(file, FileShare.ReadWrite))
                {
                    Console.WriteLine("******************** Check Recipes");

                    foreach (CSVFile.Row row in filerecipes.RowsExcludingHeaderRow)
                    {
                        string name = row["B"];
                        string fdname = row["AY"];
                        string modname = row["F"];
                        string rtype = row["C"];
                        int? preengineered = row.GetInt("G");
                        int grade = row.GetInt("I").Value;
                        int merccoins = row.GetInt("AX") ?? 0;

                        if (modname == "Discovery Scanner" && fdname != "detailedsurfacescanner_longrange")     // old stuff, artie says
                            continue;

                        var mt = ItemData.ShipModule.GetModuleType(new ModFDName(modname));

                        if (mt == ItemData.ShipModule.ModuleTypes.LightweightAlloy)
                            mt = ItemData.ShipModule.ModuleTypes.Armour;

                        System.Diagnostics.Debug.WriteLine($"Recipe {row.Index + 1} : {fdname}:{grade} : `{modname}` {mt} {rtype}");


                        if (mt == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"**** Can't match Inara module type");
                            System.Diagnostics.Debug.Assert(false);
                        }

                        List<Tuple<MaterialCommodityMicroResourceType, int>> materials = new List<Tuple<MaterialCommodityMicroResourceType, int>>();
                        string matingr = "";

                        for (int i = CSVFile.Row.CellNameToIndex("AK"); i < CSVFile.Row.CellNameToIndex("AV"); i += 2)
                        {
                            string ingr = row[i];
                            int? number = row.GetInt(i + 1);
                            if (number.HasValue)
                            {
                                var mcd = MaterialCommodityMicroResourceType.GetByEnglishName(ingr);
                                if (mcd == null)
                                {
                                    System.Diagnostics.Debug.WriteLine($"**** NOT FOUND INGR `{ingr}` {number.Value} ");
                                    System.Diagnostics.Debug.Assert(false);
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"  `{ingr}` ({mcd?.FDName}):{number.Value} ");
                                    materials.Add(Tuple.Create(mcd, number.Value));
                                    matingr = matingr.AppendPrePad($"{number.Value}{mcd.Shortname}",",");
                                }
                            }
                            else
                                break;
                        }

                        var fdx = new EngineeringRecipeFDName(fdname);

                        var rcp = EngineeringRecipe.FindEngineering(fdx, grade,mt.Value,preengineered == 1);

                        if (rcp != null)
                        {
                            if (materials.Count != rcp.Ingredients.Length)
                            {
                                System.Diagnostics.Debug.WriteLine($"**** {row.Index + 1} DISAGREE WITH MATERIALS COUNT {fdname}:{grade} {modname}");
                                System.Diagnostics.Debug.Assert(false);
                            }
                            else if (materials.Count == 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"**** No ingrediants");
                            }
                            else if ( rcp.MercCoins != merccoins)
                            {
                                System.Diagnostics.Debug.WriteLine($"**** {row.Index + 1} DISAGREE WITH MERC COINS {fdname}:{grade} {modname} {merccoins}");
                                System.Diagnostics.Debug.Assert(false);
                            }
                            else
                            {
                                for (int i = 0; i < rcp.Ingredients.Length; i++)
                                {
                                    if (materials[i].Item1 != rcp.Ingredients[i] || materials[i].Item2 != rcp.Amount[i])
                                    {
                                        System.Diagnostics.Debug.WriteLine($"**** DISAGREE WITH MATERIALS {row.Index + 1} : {fdname}:{grade} {modname} {materials[i].Item1.EnglishName}:{materials[i].Item2} {rcp.Ingredients[i].EnglishName} {rcp.Amount[i]}");
                                        System.Diagnostics.Debug.Assert(false);
                                    }
                                }
                            }

                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"**** Recipe NOT KNOWN {row.Index + 1} : {fdname}:{grade} : `{modname}` {mt} {rtype}");
                            if (materials.Count>  0)
                            {
                                foreach(var r in EngineeringRecipe.GetPlayerEngineering())
                                {
                                    if ( r.Ingredients.Length == materials.Count)
                                    {
                                        int i = 0;
                                        for(; i < r.Ingredients.Length; i++)
                                        {
                                            if (r.Ingredients[i] != materials[i].Item1 || r.Amount[i] != materials[i].Item2)
                                                break;

                                        }

                                        if ( i == r.Ingredients.Length)
                                        {
                                            System.Diagnostics.Debug.WriteLine($" .... matches {r.FDName} {r.ModuleListSplitCaps}");
                                        }
                                    }

                                }
                            }

                            string englist = preengineered == 1 ? "" : "tbd";
                            string merccoinslist = merccoins > 0 ? $",{merccoins}" : "";
                            string rpline = $" new EngineeringRecipe(\"{name}\",\"{fdname}\",\"{matingr}\",ItemData.ShipModule.ModuleTypes.{mt.ToString()},{grade},\"{englist}\"{merccoinslist}),";

                            System.Diagnostics.Debug.WriteLine(rpline);
                            File.AppendAllText(updatefile, rpline + Environment.NewLine);
                        }
                    }
                }
                else
                    Console.WriteLine("No Recipe CSV");
                }


        }

    }
}
