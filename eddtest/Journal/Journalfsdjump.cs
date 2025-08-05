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
    public partial class JournalCreator
    {
        private CSVFile stargrid;

        // jump with args
        string WriteFSDJump(CommandArgs args, int repeatcount)
        {
            if ( stargrid != null)
            {
                if (args.Left >= 1)
                {
                    string starnameroot = args.Next();
                    double x, y, z;
                    long sysaddr;

                    int row = stargrid.FindInColumn(1, starnameroot);       // export of Search | Stars

                    if (row >= 0)
                    {
                        x = stargrid[row].GetDouble(6).Value;
                        y = stargrid[row].GetDouble(7).Value;
                        z = stargrid[row].GetDouble(8).Value;
                        sysaddr = stargrid[row].GetLong(9).Value;
                        z = z + 100 * repeatcount;

                        string starname = starnameroot + ((repeatcount > 0 && z > 0) ? "_" + z.ToStringInvariant("0") : "");

                        return FSDJump(starname, sysaddr, x, y, z);
                    }
                }
            }
            else if (args.Left >= 4)
            {
                string starnameroot = args.Next();
                long? sysaddr = args.LongNull();
                double? x, y, z;
                x = args.DoubleNull();      // zero if wrong
                y = args.DoubleNull();
                z = args.DoubleNull();

                if (z.HasValue && starnameroot!=null && sysaddr.HasValue)
                {
                    z = z.Value + 100 * repeatcount;

                    string starname = starnameroot + ((repeatcount > 0 && z > 0) ? "_" + z.ToStringInvariant("0") : "");

                    return FSDJump(starname, sysaddr.Value, x.Value, y.Value, z.Value);
                }
            }
            return null;
        }

        static string FSDTravel(CommandArgs args)
        {
            if (args.Left < 8)
            {
                Console.WriteLine("** More parameters");
                return null;
            }

            double x = double.NaN, y = 0, z = 0, dx = 0, dy = 0, dz = 0;
            double percent = 0;
            string starnameroot = args.Next();

            if (!double.TryParse(args.Next(), out x) || !double.TryParse(args.Next(), out y) || !double.TryParse(args.Next(), out z) ||
                !double.TryParse(args.Next(), out dx) || !double.TryParse(args.Next(), out dy) || !double.TryParse(args.Next(), out dz) ||
                !double.TryParse(args.Next(), out percent))
            {
                Console.WriteLine("** X,Y,Z,dx,dy,dz,percent must be numbers");
                return null;
            }

            Console.WriteLine("{0} {1} {2}", dx, dy, dz);

            x = (dx - x) * percent / 100.0 + x;
            y = (dy - y) * percent / 100.0 + y;
            z = (dz - z) * percent / 100.0 + z;

            string starname = starnameroot + percent.ToStringInvariant("0");

            return FSDJump(starname, 10, x, y, z);
        }


        static string FSDJump(string starname, long sysaddr, double x, double y, double z)
        {
            JSONFormatter qj = new JSONFormatter();
            qj.Object().UTC("timestamp").V("event", "FSDJump");
            qj.V("StarSystem", starname);
            qj.V("SystemAddress", sysaddr);
            if (!double.IsNaN(x))
                qj.Array("StarPos").V(null, x).V(null, y).V(null, z).Close();
            qj.V("Allegiance", "");
            qj.V("Economy", "$economy_None;");
            qj.V("Economy_Localised", "None");
            qj.V("Government", "$government_None;");
            qj.V("Government_Localised", "None");
            qj.V("Security", "$SYSTEM_SECURITY_low;");
            qj.V("Security_Localised", "Low Security");
            qj.V("JumpDist", 10.2);
            qj.V("FuelUsed", 2.3);
            qj.V("FuelLevel", 23.3);
            return qj.Get();
        }



    }
}
