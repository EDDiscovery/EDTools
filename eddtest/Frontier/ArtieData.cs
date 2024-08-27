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
using EliteDangerousCore;
using System;
using System.IO;



namespace EDDTest
{
    static public class ArtieData
    {
        // artie supplied some csv of the suit/weapon engineering, rewrite into our form

        static public void Process(string rootpath)            // overall index of items
        {
            ItemData.Initialise();

            {
                CSVFile filerecipes = new CSVFile();
                string recipies = Path.Combine(rootpath, "defequiprecipes.csv");

                if (filerecipes.Read(recipies, FileShare.ReadWrite))
                {
                    Console.WriteLine("******************** Check Recipes");

                    foreach (CSVFile.Row line in filerecipes.RowsExcludingHeaderRow)
                    {
                        string fdname = line["U"];
                        string manu = line["B"];
                        string ukname = line["C"];
                        int price = line.GetInt("D").Value;
                        int[] counts = new int[] { line.GetInt("L") ?? 0, line.GetInt("N") ?? 0, line.GetInt("P") ?? 0, line.GetInt("R") ?? 0, line.GetInt("T") ?? 0 };
                        string[] names = new string[] { line["K"], line["M"], line["O"], line["Q"], line["S"] };
                        var rcp = EliteDangerousCore.Recipes.FindRecipe(fdname);
                        if (rcp != null)
                        {
                            MaterialCommodityMicroResourceType[] ourtypes = new MaterialCommodityMicroResourceType[counts.Length];
                            string ingrstr = "";
                            for (int i = 0; i < counts.Length; i++)
                            {
                                if (counts[i] > 0)
                                {
                                    if (names[i] == "Medical Trial Records")
                                        names[i] = "Clinical Trial Records";
                                    names[i] = names[i].Replace("\u00a0", " ");
                                    ourtypes[i] = MaterialCommodityMicroResourceType.GetByEnglishName(names[i]);
                                    System.Diagnostics.Debug.Assert(ourtypes[i] != null, $"Cannot find {names[i]}");
                                    ingrstr = ingrstr.AppendPrePad($"{counts[i]}{ourtypes[i].Shortname}", ",");
                                }
                            }

                            System.Diagnostics.Debug.WriteLine($"new EngineeringRecipe({rcp.ModuleList.AlwaysQuoteString()},{rcp.FDName.AlwaysQuoteString()},{manu.AlwaysQuoteString()},{rcp.Name.AlwaysQuoteString()},{price}," +
                                $"{ingrstr.AlwaysQuoteString()},{string.Join(",", rcp.Engineers).AlwaysQuoteString()}),"
                                );

                        }
                        else
                        {
                            Console.WriteLine($"ERROR missing recipe {fdname}");
                        }
                    }
                }
                else
                    Console.WriteLine("No Recipe CSV");
            }


            {
                CSVFile filerecipes = new CSVFile();
                string recipies = Path.Combine(rootpath, "defupgrades.csv");

                if (filerecipes.Read(recipies, FileShare.ReadWrite))
                {
                    Console.WriteLine("******************** Check Upgrades");

                    foreach (CSVFile.Row line in filerecipes.RowsExcludingHeaderRow)
                    {
                        string type = line["A"];
                        if (type.HasChars())
                        {
                            string manu = line["B"];
                            string level = line["C"];
                            int leveln = level.Substring(6, 1).InvariantParseInt(0);
                            int[] counts = new int[] { line.GetInt("E") ?? 0, line.GetInt("G") ?? 0, line.GetInt("K") ?? 0, line.GetInt("M") ?? 0, line.GetInt("O") ?? 0 };
                            string[] names = new string[] { line["D"], line["F"], line["J"], line["L"], line["N"] };

                            MaterialCommodityMicroResourceType[] ourtypes = new MaterialCommodityMicroResourceType[counts.Length];
                            string ingrstr = "";
                            for (int i = 0; i < counts.Length; i++)
                            {
                                if (counts[i] > 0)
                                {
                                    names[i] = names[i].Replace("\u00a0", " ");
                                    ourtypes[i] = MaterialCommodityMicroResourceType.GetByEnglishName(names[i]);
                                    System.Diagnostics.Debug.Assert(ourtypes[i] != null, $"Cannot find {names[i]}");
                                    ingrstr = ingrstr.AppendPrePad($"{counts[i]}{ourtypes[i].Shortname}", ",");
                                }
                            }


                            System.Diagnostics.Debug.WriteLine($"new EngineeringRecipe({type.AlwaysQuoteString()},{manu.AlwaysQuoteString()},{leveln},{ingrstr.AlwaysQuoteString()}),");
                        }
                    }
                }
                else
                    Console.WriteLine("No Recipe CSV");
            }


        }

    }
}

