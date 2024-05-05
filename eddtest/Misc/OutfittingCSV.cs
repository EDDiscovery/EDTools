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
using QuickJSON;
using System;
using System.Collections.Generic;
using System.IO;

namespace EDDTest
{
    static public class EDCDOutfitting
    {
        static public void Process(string file)
        {
            CSVFile outfitting = new CSVFile();
            if (outfitting.Read(file, FileShare.ReadWrite))
            {
                foreach (CSVFile.Row rw in outfitting.RowsExcludingHeaderRow)
                {
                    string id = rw[0].Trim();
                    string fdname = rw[1].Trim();
                    string ukname = rw[3].Trim();
                    string typename = rw[4].Trim();

                    //System.Diagnostics.Debug.WriteLine($"{id} : {fdname} : {ukname}");

                    if ( !ItemData.TryGetShipModule(fdname, out ItemData.ShipModule _, false))
                    {
                        System.Diagnostics.Debug.WriteLine($"Itemdata MISSING {{\"{fdname}\", new ShipModule({id},0,0,\"Unknown\",\"{ukname}\", ShipModule.ModuleTypes.UnknownType}},");

                    }
                }

                var allmodules = ItemData.GetShipModules(false, false,false, false);

                foreach( var kvp in allmodules)
                {
                    int row = outfitting.FindInColumn(1, kvp.Key, StringComparison.InvariantCultureIgnoreCase);
                    if ( row >= 0 )
                    {
                    }
                    else
                        System.Diagnostics.Debug.WriteLine($"Outfitting MISSING {kvp.Key}");
                }
            }
        }
    }
}
