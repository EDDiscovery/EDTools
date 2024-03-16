/*
 * Copyright © 2015 - 2024 robbyxp @ github.com
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
 */

using BaseUtils;
using QuickJSON;
using System;
using System.IO;

namespace EDDTest
{
    public static partial class Journal
    {
        static void WriteToLog(string filename, string cmdrname, string lineout, string gameversion, string build, bool nogameversiononloadgame, bool odyssey, int part, bool checkjson  )
        {
            if (lineout != null && checkjson)
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
                        JSONFormatter fileheader = new JSONFormatter();


                        fileheader.Object().UTC("timestamp",-1).V("event", "Fileheader").V("language", "English\\\\UK").V("part",part);

                        fileheader.V("gameversion", gameversion).V("build", build);

                        JSONFormatter loadgame = new JSONFormatter();

                        loadgame.Object().UTC("timestamp",-1).V("event", "LoadGame").V("FID", "F1962222").V("Commander", cmdrname)
                                .V("Horizons", true);

                        if (odyssey)
                            loadgame.V("Odyssey", odyssey);

                        loadgame.V("Ship", "Anaconda").V("Ship_Localised", "Anaconda")
                                .V("ShipID", 5).V("ShipName", "CAT MINER").V("ShipIdent", "BUD-2")
                                .V("FuelLevel", 32.000000).V("FuelCapacity", 32.000000)
                                .V("GameMode", "Group").V("Group", "FleetComm").V("Credits", 3815287).V("Loan", 0);


                        if ( !nogameversiononloadgame)
                        {
                            loadgame.V("gameversion", gameversion).V("build", build);
                        }

                        Console.WriteLine(fileheader.Get());
                        Console.WriteLine(loadgame.Get());
                        sr.WriteLine(fileheader.Get());
                        sr.WriteLine(loadgame.Get());
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

        // used by -dayoffset to write logs at a different time point
        static public TimeSpan DateTimeOffset = new TimeSpan(0);

        public static QuickJSON.JSONFormatter UTC(this QuickJSON.JSONFormatter fmt, string name, int offset = 0)
        {
            DateTime utc = DateTime.UtcNow.Add(DateTimeOffset).AddSeconds(offset);
            fmt.V(name, utc.ToStringZulu());
            return fmt;
        }

        public static string NewLine(this string s)
        {
            if (s.Length > 0 && !s.EndsWith(Environment.NewLine))
                s += Environment.NewLine;
            return s;

        }
    }

}
