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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static EliteDangerousCore.Recipes;

// take the frontier data excel they send us
// run this VB script (replace the folder etc and see if it needs 3.0 changed in the replace statement)
/*
Sub SaveAsCSV()
Dim WS As Excel.Worksheet
Dim SaveToDirectory As String
Dim name As String
Dim CurrentWorkbook As String
Dim CurrentFormat As Long

CurrentWorkbook = ThisWorkbook.FullName
CurrentFormat = ThisWorkbook.FileFormat
' Store current details for the workbook
SaveToDirectory = Application.ActiveWorkbook.Path

'MsgBox (SaveToDirectory)

For Each WS In ThisWorkbook.Worksheets
    Sheets(WS.name).Copy
    name = Replace(WS.name, "4.0", "")
    name = Replace(name, " ", "")
    ActiveWorkbook.SaveAs Filename:=SaveToDirectory & "\" & name & ".csv", FileFormat:=xlCSVUTF8
    ActiveWorkbook.Close savechanges:=False
    ThisWorkbook.Activate
Next
End Sub
*/

// run it.
// will verify materials in MaterialCommododities vs the sheet
// will verify any replacement lists of fdname
// will verify commodities in MaterialCommododities vs the sheet
// will check weapon data vs the ItemData file
// will check modules vs the ItemData file
// will read tech broker info and write out a new tech broker lines - import this manually into MaterialRecipesData.cs
// will read the recipes cvs and print out the recipe lines for MaterialRecipesData.cs. 
//      Engineer list in not in the cvs - previous is used as a reference, or marked as unknown if its new. Use Inara to find list
// will read special effects data and write out a new special effects lines - import this manually into MaterialRecipesData.cs

// Keep rare list at the bottom up to date manually with Inara and other sources

// it will print out any missing/mismatched data - some if to be expected.  Decide as you go.  Note legal drugs are not called correctly in frontier data


namespace EDDTest
{
    static public class FrontierData
    {
        const string Version = "";

        static string MatName(long? matid, long? matcount, CSVFile mat)
        {
            if (matid != null && matcount != null)
            {
                int matrow = mat.FindInColumn("A", matid.Value.ToString());
                if (matrow >= 0)
                {
                    string fdname = mat[matrow]["B"].Trim();
                    EliteDangerousCore.MaterialCommodityMicroResourceType mc = EliteDangerousCore.MaterialCommodityMicroResourceType.GetByFDName(fdname);

                    if (mc != null)
                        return matcount.Value.ToString() + mc.Shortname; //+ "(" + mc.name + ")";
                    else
                        return matcount.Value.ToString() + fdname + "( **** UNKNOWN *****)";

                }
            }
            return "";
        }

        static public void Process(string rootpath)            // overall index of items
        {
            string de = "", fr = "", es = "", ru = "", pr = "";

            // check out replacement lists

            {
                List<MaterialCommodityMicroResourceType> ourcommods = MaterialCommodityMicroResourceType.GetAll().ToList();

                foreach (KeyValuePair<string, string> kvp in MaterialCommodityMicroResourceType.fdnamemangling)
                {
                    if (ourcommods.Find(x => x.FDName.Equals(kvp.Value)) == null)
                    {
                        Console.WriteLine("Error " + kvp.Value + " replacement is not in our cache");
                    }
                }

            }

            // check for non cororolis now being in frontier data in main module section - this is due to previous undeclared modes becoming public

            {
                foreach (var x in ItemData.noncorolismodules)
                {
                    if (ItemData.modules.ContainsKey(x.Key))
                        Console.WriteLine("Error Non cororolis list contains " + x.Key + " and so does main module list");
                }
            }

            // check ships

            {
                CSVFile filecommods = new CSVFile();
                if (filecommods.Read(Path.Combine(rootpath, "ShipData" + Version + ".csv"), FileShare.ReadWrite))
                {
                    Console.WriteLine("******************** Check ships");

                    foreach (CSVFile.Row rw in filecommods.RowsExcludingHeaderRow)
                    {
                        string fdname = rw[0].Trim();
                        string ukname = rw[1].Trim();
                        int? mass = rw.GetInt("V");
                        int? basespeed = rw.GetInt("X");
                        int? boostspeed = rw.GetInt("Y");

                        var si = ItemData.Instance.GetShipProperties(fdname);

                        if (si == null)
                            Console.WriteLine("Error " + fdname + " not found in ship data");
                        else
                        {
                            if (ukname != ((ItemData.ShipInfoString)si[ItemData.ShipPropID.Name]).Value)
                            {
                                Console.WriteLine("Error " + fdname + " disagrees with uk name");
                            }

                            if (mass != null && mass.Value != ((ItemData.ShipInfoDouble)si[ItemData.ShipPropID.HullMass]).Value)
                            {
                                Console.WriteLine("Error " + fdname + " disagrees with mass spreadsheet " + mass + " Currently " + ((ItemData.ShipInfoDouble)si[ItemData.ShipPropID.HullMass]).Value);
                            }

                            // corolis has very different value for these.. not sure who is right

                            if (basespeed != null && basespeed.Value != ((ItemData.ShipInfoInt)si[ItemData.ShipPropID.Speed]).Value)
                            {
                                Console.WriteLine("Error " + fdname + " disagrees with speed " + basespeed + " Currently " + ((ItemData.ShipInfoInt)si[ItemData.ShipPropID.Speed]).Value);
                            }

                            if (boostspeed != null && boostspeed.Value != ((ItemData.ShipInfoInt)si[ItemData.ShipPropID.Boost]).Value)
                            {
                                Console.WriteLine("Error " + fdname + " disagrees with boost " + boostspeed + "  Currently " + ((ItemData.ShipInfoInt)si[ItemData.ShipPropID.Boost]).Value);

                            }
                        }
                    }
                }
                else
                    Console.WriteLine("No Ships CSV");
            }

            // check commodities

            {
                CSVFile filecommods = new CSVFile();
                if (filecommods.Read(Path.Combine(rootpath, "Commodities" + Version + ".csv"), FileShare.ReadWrite))
                {
                    Console.WriteLine("******************** Check Commodities from our data");

                    List<MaterialCommodityMicroResourceType> ourcommods = MaterialCommodityMicroResourceType.GetCommodities(true).ToList();

                    foreach (MaterialCommodityMicroResourceType m in ourcommods)     // check our list vs the excel
                    {
                        int n = filecommods.FindInColumn(3, m.Name, StringComparison.InvariantCultureIgnoreCase, true);     // find name..
                        int f = filecommods.FindInColumn(2, m.FDName, StringComparison.InvariantCultureIgnoreCase);         // find fdname

                        bool isinararare = InaraRares.Contains(m.Name);

                        if (n == -1)    // no name in excel
                        {
                            if (f != -1)    // N but Fd
                                Console.WriteLine("Error Data item name '" + m.Name + "' not found in spreadsheet, but fdname is present - misnaming " + filecommods.Rows[f][2]);
                            else           // No N, No Fd
                                Console.WriteLine("Error " + m.Name + " not found in frontier data");
                        }
                        else
                        {            // name found,
                            if (f == -1)
                            {
                                Console.WriteLine("Error FDNAME " + m.FDName + " not found in frontier data but Name is");
                            }
                        }

                        if (m.Rarity != isinararare)
                            Console.WriteLine("Rarity flag incorrect for " + m.FDName);
                    }

                    Console.WriteLine("******************** Check Commodities from excel");

                    foreach (CSVFile.Row rw in filecommods.RowsExcludingHeaderRow)          // check file vs our list
                    {
                        string type = rw[1].Trim();
                        string fdname = rw[2].Trim();
                        string ukname = rw[3].Trim();

                        if (type.Length > 0)
                        {
                            string[] legaldrugcorrections = {
                            "Tobacco","Beer","Wine","Liquor","EraninPearlWhisky","LavianBrandy","KonggaAle","WuthieloKuFroth","BastSnakeGin",
                            "ThrutisCream","KamitraCigars","RusaniOldSmokey","YasoKondiLeaf","ChateauDeAegaeon","SaxonWine","CentauriMegaGin","GerasianGueuzeBeer",
                            "BurnhamBileDistillate","IndiBourbon","LeestianEvilJuice","BlueMilk","BootlegLiquor" };

                            if (Array.IndexOf(legaldrugcorrections, fdname) != -1)       // correct frontier calling them narcotics
                                type = "Legal Drugs";

                            bool isinararare = InaraRares.Contains(ukname);

                            MaterialCommodityMicroResourceType cached = MaterialCommodityMicroResourceType.GetByFDName(fdname);

                            if (cached == null)
                            {
                                Console.WriteLine("+ AddCommodity" + (isinararare ? "Rare" : "") + "(\"" + ukname + "\", ItemType." + type.Replace(" ", "") + ", \"" + fdname + "\");");
                            }
                            else if (cached.Type.ToString() != type.Replace(" ", ""))
                            {
                                // excel has tobacco as are narcotic, but its a legal drug!!
                                // this will produce errors until fixed

                                Console.WriteLine("Error " + fdname + " type " + type + " disagrees with " + cached.Type);
                            }
                        }
                    }

                    foreach (CSVFile.Row rw in filecommods.RowsExcludingHeaderRow)
                    {
                        string fdname = rw["C"].Trim();
                        string ukname = rw["D"].Trim();
                        string dename = rw["F"].Trim();
                        string frname = rw["H"].Trim();
                        string esname = rw["J"].Trim();
                        string runame = rw["L"].Trim();
                        string prname = rw["N"].Trim();

                        de += "MaterialCommodityMicroResourceType." + fdname.ToLowerInvariant() + ": " + ukname.AlwaysQuoteString() + " => " + dename.AlwaysQuoteString() + Environment.NewLine;
                        fr += "MaterialCommodityMicroResourceType." + fdname.ToLowerInvariant() + ": " + ukname.AlwaysQuoteString() + " => " + frname.AlwaysQuoteString() + Environment.NewLine;
                        es += "MaterialCommodityMicroResourceType." + fdname.ToLowerInvariant() + ": " + ukname.AlwaysQuoteString() + " => " + esname.AlwaysQuoteString() + Environment.NewLine;
                        ru += "MaterialCommodityMicroResourceType." + fdname.ToLowerInvariant() + ": " + ukname.AlwaysQuoteString() + " => " + runame.AlwaysQuoteString() + Environment.NewLine;
                        pr += "MaterialCommodityMicroResourceType." + fdname.ToLowerInvariant() + ": " + ukname.AlwaysQuoteString() + " => " + prname.AlwaysQuoteString() + Environment.NewLine;
                    }
                }
                else
                    Console.WriteLine("No Commodity CSV");
            }



            // Check weapons as much as we can..

            {
                CSVFile fileweapons = new CSVFile();
                if (fileweapons.Read(Path.Combine(rootpath, "WeaponValues" + Version + ".csv"), FileShare.ReadWrite))
                {
                    Console.WriteLine("******************** Check Weapon Values");
                    foreach (CSVFile.Row rw in fileweapons.RowsExcludingHeaderRow)
                    {
                        string fdname = rw[0].Trim();
                        string ukname = rw[1].Trim();
                        string type = rw["N"].Trim();
                        string mount = rw["O"].Trim();
                        string size = rw["P"].Trim();
                        double powerdraw = rw["AA"].InvariantParseDouble(0);

                        ItemData.ShipModule minfo = null;

                        if (ItemData.modules.ContainsKey(fdname.ToLowerInvariant()))
                            minfo = ItemData.modules[fdname.ToLowerInvariant()];
                        else if (ItemData.noncorolismodules.ContainsKey(fdname.ToLowerInvariant()))
                            minfo = ItemData.noncorolismodules[fdname.ToLowerInvariant()];

                        if (minfo != null)
                        {
                            if (Math.Abs(minfo.Power - powerdraw) > 0.05)
                                Console.WriteLine("Weapon " + fdname + " incorrect power draw " + minfo.Power + " vs " + powerdraw);
                        }
                        else
                        {
                            string name = fdname.Replace("Hpt_", "");
                            Console.WriteLine("+ { \"" + fdname.ToLower() + "\", new ShipModule(-1, 1, " + powerdraw.ToString("#.#") + ", \"" + name.SplitCapsWordFull() + "\", \"Weapon\")},");
                        }
                    }
                }
                else
                    Console.WriteLine("No Weapons CSV");

            }

            // Check modules
            {
                CSVFile filemodules = new CSVFile();
                if (filemodules.Read(Path.Combine(rootpath, "ModuleData" + Version + ".csv"), FileShare.ReadWrite))
                {
                    Console.WriteLine("******************** Check Modules values");

                    foreach (CSVFile.Row rw in filemodules.RowsExcludingHeaderRow)
                    {
                        string fdname = rw[0].Trim();
                        string ukname = rw[1].Trim();
                        string ukdesc = rw[2].Trim();
                        string size = rw["N"].Trim();
                        double mass = rw["P"].InvariantParseDouble(0);
                        double powerdraw = rw["Q"].InvariantParseDouble(0);

                        if (ukdesc.IndexOf("(Information)", StringComparison.InvariantCultureIgnoreCase) == -1 && !fdname.Contains("_free"))
                        {
                            ItemData.ShipModule minfo = null;

                            if (ItemData.modules.ContainsKey(fdname.ToLowerInvariant()))
                                minfo = ItemData.modules[fdname.ToLowerInvariant()];
                            else if (ItemData.noncorolismodules.ContainsKey(fdname.ToLowerInvariant()))
                                minfo = ItemData.noncorolismodules[fdname.ToLowerInvariant()];

                            if (minfo != null)
                            {
                                if (Math.Abs(minfo.Power - powerdraw) > 0.05)
                                    Console.WriteLine("Module " + fdname + " incorrect power draw " + minfo.Power + " vs " + powerdraw);
                                if (Math.Abs(minfo.Mass - mass) > 0.05)
                                    Console.WriteLine("Module " + fdname + " incorrect mass " + minfo.Mass + " vs " + mass);
                            }
                            else
                            {
                                Console.WriteLine("Missing Module " + fdname);
                                Console.WriteLine("+ { \"" + fdname.ToLower() + "\", new ShipModule(-1, 1, \"" + ukname.SplitCapsWordFull() + "\", \"Internal Module\")},");
                            }
                        }
                        else
                        {
                            //Console.WriteLine("Rejected Module " + fdname + " "+ ukdesc);
                        }
                    }
                }
                else
                    Console.WriteLine("No Module Data CSV");
            }

            // tech broker
            {
                CSVFile filetechbroker = new CSVFile();

                if (filetechbroker.Read(Path.Combine(rootpath, "TechBroker" + Version + ".csv"), FileShare.ReadWrite))
                {
                    Console.WriteLine("******************** Check Tech Broker");

                    foreach (CSVFile.Row rw in filetechbroker.RowsExcludingHeaderRow)
                    {
                        string fdname = rw[0].Trim();
                        string type = rw["C"].Trim();
                        string size = rw["D"].Trim();
                        string mount = rw["E"].Trim();

                        string nicename = fdname.Replace("hpt_", "").Replace("Hpt_", "").Replace("int_", "").Replace("Int_", "");
                        nicename = nicename.Replace("guardian", "Guardian", StringComparison.InvariantCultureIgnoreCase);
                        nicename = nicename.Replace("gausscannon", "Gauss Cannon", StringComparison.InvariantCultureIgnoreCase);
                        nicename = nicename.Replace("fsdbooster", "FSD Booster ", StringComparison.InvariantCultureIgnoreCase);
                        nicename = nicename.Replace("powerdistributor", "Power Distributor ", StringComparison.InvariantCultureIgnoreCase);
                        nicename = nicename.Replace("plasmalauncher", "Plasma Launcher", StringComparison.InvariantCultureIgnoreCase);
                        nicename = nicename.Replace("causticmissile", "Enzyme Missile Rack", StringComparison.InvariantCultureIgnoreCase);
                        nicename = nicename.Replace("plasma", "Plasma ", StringComparison.InvariantCultureIgnoreCase);
                        nicename = nicename.Replace("shockcannon", "Shock Cannon", StringComparison.InvariantCultureIgnoreCase);
                        nicename = nicename.Replace("meta", "Meta ", StringComparison.InvariantCultureIgnoreCase);
                        nicename = nicename.Replace("alloy", "Alloy ", StringComparison.InvariantCultureIgnoreCase);
                        nicename = nicename.Replace("hullreinforcement", "Hull Reinforcement", StringComparison.InvariantCultureIgnoreCase);
                        nicename = nicename.Replace("flechettelauncher", "Flechette Launcher", StringComparison.InvariantCultureIgnoreCase);
                        nicename = nicename.Replace("corrosionproof", "Corrosion Resistant ", StringComparison.InvariantCultureIgnoreCase);
                        nicename = nicename.Replace("cargorack", "Cargo Rack", StringComparison.InvariantCultureIgnoreCase);
                        nicename = nicename.Replace("powerplant", "Power Plant", StringComparison.InvariantCultureIgnoreCase);
                        nicename = nicename.Replace("shardcannon", "Shard Cannon", StringComparison.InvariantCultureIgnoreCase);
                        nicename = nicename.Replace("GDN", "Guardian");
                        nicename = nicename.SplitCapsWordFull();


                        int[] count = new int[10];
                        MaterialCommodityMicroResourceType[] mat = new MaterialCommodityMicroResourceType[10];
                        int ic = 0;

                        while (true)
                        {
                            string ingfd = rw["F", ic * 2];

                            if (ingfd.HasChars())
                            {
                                ingfd = ingfd.Trim();
                                count[ic] = rw["G", ic * 2].InvariantParseInt(0);

                                mat[ic] = MaterialCommodityMicroResourceType.GetByFDName(ingfd);
                                if (mat[ic] == null)
                                {
                                    Console.WriteLine("Material DB does not have " + ingfd);
                                    break;
                                }
                                else if (!mat[ic].Shortname.HasChars())
                                {
                                    Console.WriteLine("Material DB entry " + ingfd + " does not have a shortname for recipe " + rw["F", ic * 2]);
                                    break;
                                }

                                ic++;
                            }
                            else
                                break;
                        }

                        string ilist = "";
                        for (int i = 0; i < ic; i++)
                            ilist = ilist.AppendPrePad(count[i].ToStringInvariant() + mat[i].Shortname, ",");

                        var techunlocks = Recipes.EngineeringRecipes;
                        var find = techunlocks.Find(x => x.IngredientsString.Replace(" ", "") == ilist);
                        if (find == null)
                        {
                            Console.WriteLine("Missing tech broker new EngineeringRecipe(\"" + nicename + "\",\"" + type + "\",\"?\",\"" + ilist + "\")," + Environment.NewLine);
                        }
                    }
                }
                else
                    Console.WriteLine("No Tech Broker CSV");

            }

            // check materials, and later recipes

            {
                string materials = Path.Combine(rootpath, "Materials" + Version + ".csv");
                CSVFile filemats = new CSVFile();

                if (filemats.Read(materials, FileShare.ReadWrite))
                {
                    Console.WriteLine("******************** Check Materials");

                    MaterialCommodityMicroResourceType[] ourmats = MaterialCommodityMicroResourceType.GetMaterials(true);

                    foreach (MaterialCommodityMicroResourceType m in ourmats)
                    {
                        if (filemats.FindInColumn(5, m.Name, StringComparison.InvariantCultureIgnoreCase, true) == -1)
                            Console.WriteLine("Error " + m.FDName + ":" + m.Name + " name differs");
                        if (filemats.FindInColumn(1, m.FDName, StringComparison.InvariantCultureIgnoreCase) == -1)
                            Console.WriteLine("Error " + m.FDName + ":" + m.Name + " not found in frontier data");
                    }

                    Console.WriteLine("******************** Check Materials from excel");

                    foreach (CSVFile.Row rw in filemats.RowsExcludingHeaderRow)
                    {
                        string fdname = rw[1];
                        string rarity = rw[2];
                        string category = rw[3];
                        string ukname = rw[5].Trim();

                        MaterialCommodityMicroResourceType cached = MaterialCommodityMicroResourceType.GetByFDName(fdname);

                        if (cached == null)
                        {
                            Console.WriteLine("Error " + fdname + " not found in cache");
                        }
                        else if (cached.Category.ToString() != category)
                        {
                            Console.WriteLine("Error " + fdname + " type " + category + " disagrees with " + cached.Type);
                        }
                        else if (cached.Name != ukname)
                        {
                            Console.WriteLine("Error " + fdname + " name '" + ukname + "' disagrees with '" + cached.Name + "'");
                        }
                        else if ( (int)cached.Type +1 != rw[2].InvariantParseInt(-1))
                        {
                            Console.WriteLine("Error " + fdname + " name '" + ukname + "' rarity " + cached.Type + " disagrees with " + rarity);
                        }
                    }

                    foreach (CSVFile.Row rw in filemats.RowsExcludingHeaderRow)
                    {
                        string fdname = rw["B"].Trim();
                        string ukname = rw["F"].Trim();
                        string dename = rw["H"].Trim();
                        string frname = rw["J"].Trim();
                        string esname = rw["L"].Trim();
                        string runame = rw["N"].Trim();
                        string prname = rw["P"].Trim();

                        de += "MaterialCommodityMicroResourceType." + fdname.ToLowerInvariant() + ": " + ukname.AlwaysQuoteString() + " => " + dename.AlwaysQuoteString() + Environment.NewLine;
                        fr += "MaterialCommodityMicroResourceType." + fdname.ToLowerInvariant() + ": " + ukname.AlwaysQuoteString() + " => " + frname.AlwaysQuoteString() + Environment.NewLine;
                        es += "MaterialCommodityMicroResourceType." + fdname.ToLowerInvariant() + ": " + ukname.AlwaysQuoteString() + " => " + esname.AlwaysQuoteString() + Environment.NewLine;
                        ru += "MaterialCommodityMicroResourceType." + fdname.ToLowerInvariant() + ": " + ukname.AlwaysQuoteString() + " => " + runame.AlwaysQuoteString() + Environment.NewLine;
                        pr += "MaterialCommodityMicroResourceType." + fdname.ToLowerInvariant() + ": " + ukname.AlwaysQuoteString() + " => " + prname.AlwaysQuoteString() + Environment.NewLine;
                    }

                    CSVFile filerecipes = new CSVFile();
                    string recipies = Path.Combine(rootpath, "RecipeData.csv");

                    if (filerecipes.Read(recipies, FileShare.ReadWrite))
                    {
                        Console.WriteLine("******************** Check Recipes");

                        foreach (CSVFile.Row line in filerecipes.Rows)
                        {
                            string fdname = line["A"];
                            string ukname = line["C"];
                            string descr = line["D"];
                            int? level = line.GetInt("P");
                            long? matid1 = line.GetInt("Q");
                            long? matid1count = line.GetInt("R");
                            long? matid2 = line.GetInt("S");
                            long? matid2count = line.GetInt("T");
                            long? matid3 = line.GetInt("U");
                            long? matid3count = line.GetInt("V");

                            if (ukname.StartsWith("Misc "))
                                ukname = ukname.Substring(5);

                            if (level != null)
                            {
                                fdname = fdname.Substring(0, fdname.LastIndexOf('_'));      //examples, AFM_Shielded, Armour_Heavy Duty
                                string fdfront = fdname.Substring(0, fdname.IndexOf('_'));
                                string fdback = fdname.Substring(fdname.IndexOf('_') + 1).Replace(" ", "");

                                string ing = MatName(matid1, matid1count, filemats);
                                ing = ing.AppendPrePad(MatName(matid2, matid2count, filemats), ",");
                                ing = ing.AppendPrePad(MatName(matid3, matid3count, filemats), ",");

                                string cat = fdname.Word(new char[] { '_' }, 1).SplitCapsWordFull();
                                if (cat == "FS Dinterdictor")
                                    cat = "FSD Interdictor";

                                string engnames = "Not Known";

                                EngineeringRecipe er = Recipes.EngineeringRecipes.Find((x) => x.Name == ukname && x.level == level.Value.ToString());

                                if (er != null)
                                {
                                    if (er.IngredientsString.Replace(" ", "") != ing)
                                        Console.WriteLine("Engineering disagree on " + ukname + " F: " + ing + " Data: " + er.IngredientsString);
                                }
                                else
                                    Console.WriteLine("Engineering missing new EngineeringRecipe(\"" + ukname + "\", \"" + ing + "\", \"" + cat + "\", \"" + level.Value.ToString() + "\", \"" + engnames + "\" ),");
                            }
                        }
                    }
                    else
                        Console.WriteLine("No Recipe CSV");

                }
                else
                    Console.WriteLine("No Materials CSV");

            }


            // check special data

            {
                CSVFile filesd = new CSVFile();
                CSVFile filemats = new CSVFile();

                if (filesd.Read(Path.Combine(rootpath, "SpecialData" + Version + ".csv"), FileShare.ReadWrite) && filemats.Read(Path.Combine(rootpath, "Materials" + Version + ".csv"), FileShare.ReadWrite))
                {
                    Console.WriteLine("******************** Check Special Data");

                    foreach (CSVFile.Row rw in filesd.RowsExcludingHeaderRow)
                    {
                        string fdname = rw[0]?.Trim();
                        string ukname = rw.Next()?.Trim();
                        string ukdesc = rw.Next()?.Trim();

                        rw.SetPosition("X");
                        string modules = "";
                        string mn;
                        while ((mn = rw.Next()).HasChars())
                        {
                            modules = modules.AppendPrePad(mn.SplitCapsWord(), ",");
                        }

                        string[] name = new string[5];
                        string[] ingr = new string[5];
                        int?[] count = new int?[5];

                        string ilist = "";

                        rw.SetPosition("N");

                        for (int i = 0; i < 5; i++)
                        {
                            name[i] = rw.Next();
                            count[i] = rw.NextInt();

                            if (name[i].Contains("N/A") || count[i] == null)
                                name[i] = "";

                            if (name[i].HasChars())
                            {
                                name[i] = name[i].Replace(" ", "").Trim();          // for some reason, its been spaced by caps.. unspace it and trim it 

                                int row = filemats.FindInColumn("B", name[i], StringComparison.InvariantCultureIgnoreCase, true);

                                if (row == -1)
                                {
                                    Console.WriteLine("Special recipe " + fdname + " cannot find material " + name[i]);
                                    name[i] = "";
                                }
                                else
                                {
                                    name[i] = filemats[row]["B"];

                                    MaterialCommodityMicroResourceType mcd = MaterialCommodityMicroResourceType.GetByFDName(name[i]);
                                    if (mcd != null)
                                    {
                                        ingr[i] = mcd.Shortname;
                                        ilist = ilist.AppendPrePad(count[i].ToStringInvariant() + ingr[i], ",");
                                    }
                                    else
                                        Console.WriteLine("Special recipe " + fdname + " mat " + name[i] + " Not in our DB");
                                }
                            }
                        }

                        var sf = Recipes.EngineeringRecipes;
                        var find = sf.Find(x => x.IngredientsString.Replace(" ", "") == ilist);
                        if (find == null)
                        {
                            Console.WriteLine("Missing Special effects EngineeringRecipe(\"" + ukname + "\",\"" + modules + "\",\"" + ilist + "\")," + Environment.NewLine);
                        }
                    }

                }
                else
                    Console.WriteLine("No Special data CSV and/or Materials.csv");
            }

            // Check MRs

            {
                string file = Path.Combine(rootpath, "OnFootAssest" + Version + ".csv");
                CheckMR(file, MaterialCommodityMicroResourceType.CatType.Component);
            }

            {
                string file = Path.Combine(rootpath, "OnFootConsumables" + Version + ".csv");
                CheckMR(file, MaterialCommodityMicroResourceType.CatType.Consumable);
            }

            {
                string file = Path.Combine(rootpath, "OnFootData" + Version + ".csv");
                CheckMR(file, MaterialCommodityMicroResourceType.CatType.Data);
            }

            {
                string file = Path.Combine(rootpath, "OnFootGoods" + Version + ".csv");
                CheckMR(file, MaterialCommodityMicroResourceType.CatType.Item);
            }

            // SuitValues..

            // ToolValues.. not in itemdata

            // WeaponValues

            {
                string components = Path.Combine(rootpath, "On-FootWeaponValues" + Version + ".csv");
                CSVFile filecomponents = new CSVFile();

                if (filecomponents.Read(components, FileShare.ReadWrite))
                {
                    Console.WriteLine("******************** Check Weapon Values");

                    foreach (var kvp in ItemData.weapons)
                    {
                        if (filecomponents.FindInColumn(0, kvp.Key, StringComparison.InvariantCultureIgnoreCase) == -1)
                            Console.WriteLine("Error " + kvp.Key + ":" + kvp.Value.Name + " not found in frontier data");
                    }

                    Console.WriteLine("******************** Check Weapons from Excel");

                    foreach (CSVFile.Row rw in filecomponents.RowsExcludingHeaderRow)
                    {
                        string fdname = rw[0].ToLower();
                        string ukname = rw[1].Trim();

                        if (ItemData.weapons.TryGetValue(fdname, out ItemData.Weapon w))
                        {
                            if (!((w.Primary && rw[14] == "Primary") || (!w.Primary && rw[14] == "Secondary")))
                            {
                                Console.WriteLine("Error " + fdname + " Class type wrong");
                            }

                            //etc later
                        }
                        else
                        {
                            Console.WriteLine("Error " + fdname + " not found in data");
                        }
                        MaterialCommodityMicroResourceType cached = MaterialCommodityMicroResourceType.GetByFDName(fdname);

                    }
                }
                else
                    Console.WriteLine("No On foot asset csv");

            }


            // upgrade recipes
            {
                CSVFile upgrade = new CSVFile();

                if (upgrade.Read(Path.Combine(rootpath, "UpgradeRecipes" + Version + ".csv"), FileShare.ReadWrite))
                {
                    Console.WriteLine("******************** Check Suit/Weapon Upgrades");

                    foreach (CSVFile.Row rw in upgrade.RowsExcludingHeaderRow)
                    {
                        string type = rw[0].Trim();

                        if (type.HasChars())
                        {
                            string manu = rw[1].Trim();
                            string level = rw[2].Trim();

                            int[] count = new int[10];
                            MaterialCommodityMicroResourceType[] mat = new MaterialCommodityMicroResourceType[10];
                            int ic = 0;

                            while (true)
                            {
                                string ingfd = rw["D", ic * 2];

                                if (ingfd.HasChars())
                                {
                                    ingfd = ingfd.Trim();
                                    count[ic] = rw["E", ic * 2].InvariantParseInt(0);

                                    mat[ic] = MaterialCommodityMicroResourceType.GetByName(ingfd);
                                    if (mat[ic] == null)
                                    {
                                        Console.WriteLine("Material DB does not have " + ingfd);
                                        break;
                                    }
                                    else if (!mat[ic].Shortname.HasChars())
                                    {
                                        Console.WriteLine("Material DB entry " + ingfd + " does not have a shortname for recipe " + type + ":" + manu + ":" + level);
                                        break;
                                    }

                                    ic++;
                                }
                                else
                                    break;
                            }

                            string ilist = "";
                            for (int i = 0; i < ic; i++)
                                ilist = ilist.AppendPrePad(count[i].ToStringInvariant() + mat[i].Shortname, ",");

                            var sf = Recipes.EngineeringRecipes;
                            var find = sf.Find(x => x.IngredientsString.Replace(" ", "") == ilist);
                            if (find == null)
                            {
                                Console.WriteLine("Missing Upgrade new EngineeringRecipe(\"" + type + "\",\"" + manu + "\"," + level[6] + ",\"" + ilist +"\")," );
                            }
                        }

                    }
                }
                else
                    Console.WriteLine("No Upgrade Receipes suits/weapons CSV");

                // engineering recipes for suits/weapons
                {
                    CSVFile engrec = new CSVFile();

                    if (engrec.Read(Path.Combine(rootpath, "EngineeringRecipes" + Version + ".csv"), FileShare.ReadWrite))
                    {
                        Console.WriteLine("******************** Check Engineer Recp for suit/weapons Upgrades");

                        foreach (CSVFile.Row rw in engrec.RowsExcludingHeaderRow)
                        {
                            string type = rw[0].Trim();

                            if (type.HasChars())
                            {
                                string manu = rw[1].Trim();
                                string modtext = rw["S"].Trim();
                                string cost = rw[3].Trim();

                                int[] count = new int[5];
                                MaterialCommodityMicroResourceType[] mat = new MaterialCommodityMicroResourceType[10];
                                int ic = 0;

                                while (ic<count.Length)
                                {
                                    string ingfd = rw["E", ic * 2];

                                    if (ingfd.HasChars())
                                    {
                                        ingfd = ingfd.Trim();
                                        ingfd = ingfd.Replace("Surveilleance Logs", "Surveillance Logs");
                                        ingfd = ingfd.Replace("Medical Trial Records", "Medical Records");
                                        ingfd = ingfd.Replace("\xa0", " ");     // non breaking space found !! confusing

                                        count[ic] = rw["F", ic * 2].InvariantParseInt(0);

                                        mat[ic] = MaterialCommodityMicroResourceType.GetByName(ingfd);
                                        if (mat[ic] == null)
                                        {
                                            Console.WriteLine("Material DB does not have " + ingfd);
                                            break;
                                        }
                                        else if (!mat[ic].Shortname.HasChars())
                                        {
                                            Console.WriteLine("Material DB entry " + ingfd + " does not have a shortname for recipe " + type + ":" + manu );
                                            break;
                                        }

                                        ic++;
                                    }
                                    else
                                        break;
                                }

                                string ilist = "";
                                for (int i = 0; i < ic; i++)
                                    ilist = ilist.AppendPrePad(count[i].ToStringInvariant() + mat[i].Shortname, ",");

                                var sf = Recipes.EngineeringRecipes;
                                var find = sf.Find(x => x.IngredientsString.Replace(" ", "") == ilist);
                                if (find == null)
                                {
                                    Console.WriteLine("Missing Eng Recp new EngineeringRecipe(\"" + type + "\",\"" + manu + "\",\"" + modtext + "\"," + 
                                                cost.Replace(",","") + ",\"" + ilist + "\",\"Eng?\"),");
                                }
                            }

                        }
                    }
                    else
                        Console.WriteLine("No Upgrade Receipes suits/weapons CSV");

                }






                //if (de.Length > 0)
                //{
                //    File.WriteAllText(Path.Combine(rootpath, "mat-de.part.txt"), de, Encoding.UTF8);
                //    File.WriteAllText(Path.Combine(rootpath, "mat-fr.part.txt"), fr, Encoding.UTF8);
                //    File.WriteAllText(Path.Combine(rootpath, "mat-es.part.txt"), es, Encoding.UTF8);
                //    File.WriteAllText(Path.Combine(rootpath, "mat-ru.part.txt"), ru, Encoding.UTF8);
                //    File.WriteAllText(Path.Combine(rootpath, "mat-pr.part.txt"), pr, Encoding.UTF8);
                //}
            }
        }

        static void CheckMR(string file, MaterialCommodityMicroResourceType.CatType cattype )
        {
            CSVFile filecomponents = new CSVFile();

            if (filecomponents.Read(file, FileShare.ReadWrite))
            {
                Console.WriteLine("******************** Check " + cattype.ToString());

                MaterialCommodityMicroResourceType[] ourmats = MaterialCommodityMicroResourceType.Get(x => x.Category == cattype);

                foreach (MaterialCommodityMicroResourceType m in ourmats)
                {
                    if (filecomponents.FindInColumn(2, m.FDName, StringComparison.InvariantCultureIgnoreCase) == -1)
                        Console.WriteLine("Error " + m.FDName + ":" + m.Name + " not found in frontier data");
                    else if (filecomponents.FindInColumn(3, m.Name, StringComparison.InvariantCultureIgnoreCase, true) == -1)
                        Console.WriteLine("Error " + m.FDName + ":" + m.Name + " name differs");
                }

                Console.WriteLine("******************** Check " + cattype.ToString() + " from excel");

                string ret = "";
                foreach (CSVFile.Row rw in filecomponents.RowsExcludingHeaderRow)
                {
                    string category = rw[1];        // not used
                    string fdname = rw[2];
                    string ukname = rw[3].Trim();

                    MaterialCommodityMicroResourceType cached = MaterialCommodityMicroResourceType.GetByFDName(fdname);

                    if (cached == null)
                    {
                        string code = "AddMicroResource(CatType." + cattype.ToString() + ", \"" + ukname + "\",\"" + fdname + "\", \"MR" + cattype.ToString()[0] + Shortname(ukname) + "\");";
                        Console.WriteLine("Error " + fdname + " not found in cache " + code);
                        ret += code + Environment.NewLine;
                    }
                    else if (cached.Name != ukname)
                    {
                        Console.WriteLine("Error " + fdname + " name '" + ukname + "' disagrees with '" + cached.Name + "'");
                    }
                    else
                    {

                    }
                }

                if (ret.HasChars())
                    Console.WriteLine("Code:" + Environment.NewLine + ret);
            }
            else
                Console.WriteLine("No csv");
        }

        private static string Shortname(string text)
        {
            Regex r = new Regex(@"\b[a-zA-Z]");
            string ret = "";
            r.Replace(text, m => { ret += m.Groups[0].Value; return ""; });
            return ret;
        }

        static string[] InaraRares = new string[]
        {
            // inara 2/7/2018
            "Apa Vietii",
            "Aepyornis Egg",
            "Aganippe Rush",
            "Alacarakmo Skin Art",
            "Albino Quechua Mammoth Meat",
            "Altairian Skin",
            "Alya Body Soap",
            "Anduliga Fire Works",
            "Any Na Coffee",
            "Arouca Conventual Sweets",
            "AZ Cancri Formula 42",
            "Azure Milk",
            "Baltah'sine Vacuum Krill",
            "Banki Amphibious Leather",
            "Bast Snake Gin",
            "Belalans Ray Leather",
            "Borasetani Pathogenetics",
            "Buckyball Beer Mats",
            "Burnham Bile Distillate",
            "CD-75 Kitten Brand Coffee",
            "Centauri Mega Gin",
            "Ceremonial Heike Tea",
            "Ceti Rabbits",
            "Chameleon Cloth",
            "Chateau de Aegaeon",
            "Cherbones Blood Crystals",
            "Chi Eridani Marine Paste",
            "Coquim Spongiform Victuals",
            "Crom Silver Fesh",
            "Crystalline Spheres",
            "Damna Carapaces",
            "Delta Phoenicis Palms",
            "Deuringas Truffles",
            "Diso Ma Corn",
            "Eden Apples Of Aerial",
            "Eleu Thermals",
            "Eranin Pearl Whisky",
            "Eshu Umbrellas",
            "Esuseku Caviar",
            "Ethgreze Tea Buds",
            "Fujin Tea",
            "Galactic Travel Guide",
            "Geawen Dance Dust",
            "Gerasian Gueuze Beer",
            "Giant Irukama Snails",
            "Giant Verrix",
            "Gilya Signature Weapons",
            "Goman Yaupon Coffee",
            "Haiden Black Brew",
            "Havasupai Dream Catcher",
            "Helvetitj Pearls",
            "HIP 10175 Bush Meat",
            "HIP 118311 Swarm",
            "HIP Organophosphates",
            "HIP Proto-Squid",
            "Holva Duelling Blades",
            "Honesty Pills",
            "HR 7221 Wheat",
            "Indi Bourbon",
            "Jaques Quinentian Still",
            "Jaradharre Puzzle Box",
            "Jaroua Rice",
            "Jotun Mookah",
            "Kachirigin Filter Leeches",
            "Kamitra Cigars",
            "Kamorin Historic Weapons",
            "Karetii Couture",
            "Karsuki Locusts",
            "Kinago Violins",
            "Kongga Ale",
            "Koro Kung Pellets",
            "Lavian Brandy",
            "Leathery Eggs",
            "Leestian Evil Juice",
            "Live Hecate Sea Worms",
            "LTT Hyper Sweet",
            "Lucan Onionhead",
            "Master Chefs",
            "Mechucos High Tea",
            "Medb Starlube",
            "Mokojing Beast Feast",
            "Momus Bog Spaniel",
            "Motrona Experience Jelly",
            "Mukusubii Chitin-os",
            "Mulachi Giant Fungus",
            "Neritus Berries",
            "Ngadandari Fire Opals",
            "Nguna Modern Antiques",
            "Njangari Saddles",
            "Non Euclidian Exotanks",
            "Ochoeng Chillies",
            "Onionhead",
            "Onionhead Alpha Strain",
            "Onionhead Beta Strain",
            "Ophiuch Exino Artefacts",
            "Orrerian Vicious Brew",
            "Pantaa Prayer Sticks",
            "Pavonis Ear Grubs",
            "Personal Gifts",
            "Rajukru Multi-Stoves",
            "Rapa Bao Snake Skins",
            "Rusani Old Smokey",
            "Sanuma Decorative Meat",
            "Saxon Wine",
            "Shan's Charis Orchid",
            "Soontill Relics",
            "Sothis Crystalline Gold",
            "Tanmark Tranquil Tea",
            "Tarach Spice",
            "Tauri Chimes",
            "Terra Mater Blood Bores",
            "The Hutton Mug",
            "The Waters of Shintara",
            "Thrutis Cream",
            "Tiegfries Synth Silk",
            "Tiolce Waste2Paste Units",
            "Toxandji Virocide",
            "Ultra-Compact Processor Prototypes",
            "Uszaian Tree Grub",
            "Utgaroar Millennial Eggs",
            "Uzumoku Low-G Wings",
            "V Herculis Body Rub",
            "Vanayequi Ceratomorpha Fur",
            "Vega Slimweed",
            "Vidavantian Lace",
            "Void Extract Coffee",
            "Volkhab Bee Drones",
            "Wheemete Wheat Cakes",
            "Witchhaul Kobe Beef",
            "Wolf Fesh",
            "Wulpa Hyperbore Systems",
            "Wuthielo Ku Froth",
            "Xihe Biomorphic Companions",
            "Yaso Kondi Leaf",
            "Zeessze Ant Grub Glue",

            // added from frontier data

            "Lyrae Weed",
            "Chateau De Aegaeon",
            "The Waters Of Shintara",
            "Baked Greebles",
            "Hip Organophosphates",
            "Harma Silver Sea Rum",
            "Earth Relics",


        };

    }
}

