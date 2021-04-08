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
using BaseUtils.JSON;
using System;
using System.IO;

namespace EDDTest
{
    public static partial class Journal
    {
        static void WriteToLog(string filename, string cmdrname, string lineout, bool checkjson ,int part = 1 )
        {
            if (checkjson)
            {
                JToken jk = JToken.Parse(lineout, out string error, JToken.ParseOptions.CheckEOL);
                if ( jk == null )
                {
                    Console.WriteLine("Error in JSON " + error);
                    return;
                }
            }

            if (!File.Exists(filename))
            {
                using (Stream fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    using (StreamWriter sr = new StreamWriter(fs))
                    {
                        QuickJSONFormatter l1 = new QuickJSONFormatter();
                        l1.Object().UTC("timestamp").V("event", "Fileheader").V("part", 1).V("language", "English\\\\UK").V("gameversion", "2.2 (Beta 2)").V("build", "r121783/r0");
                        QuickJSONFormatter l2 = new QuickJSONFormatter();
                        l2.Object().UTC("timestamp").V("event", "LoadGame").V("FID","F1962222").V("Commander", cmdrname)
                                .V("Horizons", true).V("Ship", "Anaconda").V("Ship_Localised", "Anaconda")
                                .V("ShipID",5).V("ShipName","CAT MINER").V("ShipIdent","BUD-2")
                                .V("FuelLevel",32.000000).V("FuelCapacity",32.000000)
                                .V("GameMode", "Group").V("Group", "FleetComm").V("Credits", 3815287).V("Loan", 0);

                        Console.WriteLine(l1.Get());
                        Console.WriteLine(l2.Get());
                        sr.WriteLine(l1.Get());
                        sr.WriteLine(l2.Get());
                    }
                }
            }

            if (lineout != null)
            {
                using (Stream fs = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                {
                    using (StreamWriter sr = new StreamWriter(fs))
                    {
                        sr.WriteLine(lineout);
                        Console.WriteLine(lineout);
                    }
                }
            }
        }
    }
}
