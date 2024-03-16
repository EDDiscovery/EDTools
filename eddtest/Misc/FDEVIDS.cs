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

namespace EDDTest
{
    static public class FDevIDs
    {
        static public void Process(string rootpath)            // overall index of items
        {

            {
                CSVFile filecommods = new CSVFile();
                if (filecommods.Read(Path.Combine(rootpath, "material.csv"), FileShare.ReadWrite))
                {
                    Console.WriteLine("******************** Check Mats");

                    foreach (CSVFile.Row rw in filecommods.RowsExcludingHeaderRow)
                    {
                        long? id = rw.GetLong(0);
                        string fdname = rw[1].Trim();
                        int? rarity = rw.GetInt(2);
                        string type = rw[3].Trim();
                        string engname = rw[4].Trim();
                        System.Diagnostics.Debug.WriteLine($"{id} {fdname} {rarity} {type} {engname}");

                        var mcd = MaterialCommodityMicroResourceType.GetByFDName(fdname);
                        if (mcd == null)
                            Console.WriteLine($"{id} {fdname} {engname} Missing");
                    }
                }
                else
                    Console.WriteLine("No Mats CSV");
            }
            {
                CSVFile file = new CSVFile();
                if (file.Read(Path.Combine(rootpath, "microresources.csv"), FileShare.ReadWrite))
                {
                    Console.WriteLine("******************** Check MR");

                    foreach (CSVFile.Row rw in file.RowsExcludingHeaderRow)
                    {
                        long? id = rw.GetLong(0);
                        string fdname = rw[1].Trim();
                        string type = rw[2].Trim();
                        string engname = rw[3].Trim();
                        System.Diagnostics.Debug.WriteLine($"{id} {fdname} {type} {engname}");

                        var mcd = MaterialCommodityMicroResourceType.GetByFDName(fdname);
                        if (mcd == null)
                            Console.WriteLine($"{id} {fdname} {engname} Missing");
                    }
                }
                else
                    Console.WriteLine("No MR CSV");
            }
            {
                CSVFile file = new CSVFile();
                if (file.Read(Path.Combine(rootpath, "rare_commodity.csv"), FileShare.ReadWrite))
                {
                    Console.WriteLine("******************** Check Rares");

                    foreach (CSVFile.Row rw in file.RowsExcludingHeaderRow)
                    {
                        long? id = rw.GetLong(0);
                        string fdname = rw[1].Trim();
                        long? marketid = rw.GetLong(2);
                        string type = rw[3].Trim();
                        string engname = rw[4].Trim();
                        System.Diagnostics.Debug.WriteLine($"{id} {fdname} {marketid} {type} {engname}");

                        var mcd = MaterialCommodityMicroResourceType.GetByFDName(fdname);
                        if (mcd == null)
                            Console.WriteLine($"{id} {fdname} {engname} Missing");
                    }
                }
                else
                    Console.WriteLine("No Rare CSV");
            }

            {
                CSVFile file = new CSVFile();
                if (file.Read(Path.Combine(rootpath, "commodity.csv"), FileShare.ReadWrite))
                {
                    Console.WriteLine("******************** Check Commds");

                    foreach (CSVFile.Row rw in file.RowsExcludingHeaderRow)
                    {
                        long? id = rw.GetLong(0);
                        string fdname = rw[1].Trim();
                        string type = rw[2].Trim();
                        string engname = rw[3].Trim();
                        System.Diagnostics.Debug.WriteLine($"{id} {fdname} {type} {engname}");

                        var mcd = MaterialCommodityMicroResourceType.GetByFDName(fdname);
                        if (mcd == null)
                            Console.WriteLine($"{id} {fdname} {engname} Missing");
                    }
                }
                else
                    Console.WriteLine("No Commds CSV");
            }

        }

    }
}

