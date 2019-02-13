using BaseUtils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EDDTest
{
    public class Status
    {
        private enum StatusFlagsShip                        // PURPOSELY PRIVATE - don't want users to get into low level detail of BITS
        {
            Docked = 0, // (on a landing pad)
            Landed = 1, // (on planet surface)
            LandingGear = 2,
            Supercruise = 4,
            FlightAssist = 5,
            HardpointsDeployed = 6,
            InWing = 7,
            CargoScoopDeployed = 9,
            SilentRunning = 10,
            ScoopingFuel = 11,
            FsdMassLocked = 16,
            FsdCharging = 17,
            FsdCooldown = 18,
            OverHeating = 20,
            BeingInterdicted = 23,
            HUDInAnalysisMode = 27,     // 3.3
        }

        private enum StatusFlagsSRV
        {
            SrvHandbrake = 12,
            SrvTurret = 13,
            SrvUnderShip = 14,
            SrvDriveAssist = 15,
        }

        private enum StatusFlagsAll
        {
            ShieldsUp = 3,
            Lights = 8,
            LowFuel = 19,
            HasLatLong = 21,
            IsInDanger = 22,
            NightVision = 28,             // 3.3
        }

        private enum StatusFlagsShipType
        {
            InMainShip = 24,        // -> Degenerates to UIShipType
            InFighter = 25,
            InSRV = 26,
            ShipMask = (1 << InMainShip) | (1 << InFighter) | (1 << InSRV),
        }


        public static void StatusMove(CommandArgs args)
        {
            long flags = 0;

            double latitude = 0;
            double longitude = 0;
            double latstep = 0;
            double longstep = 0;
            double heading = 0;
            double headstep = 1;
            int steptime = 100;

            if (!double.TryParse(args.Next(), out latitude) || !double.TryParse(args.Next(), out longitude) ||
                !double.TryParse(args.Next(), out latstep) || !double.TryParse(args.Next(), out longstep) ||
                !double.TryParse(args.Next(), out heading) || !double.TryParse(args.Next(), out headstep) ||
                !int.TryParse(args.Next(), out steptime))
            {
                Console.WriteLine("** More/Wrong parameters: statusjson lat long latstep lonstep heading headstep steptimems");
                return;
            }

            while (true)
            {
                //{ "timestamp":"2018-03-01T21:51:36Z", "event":"Status", "Flags":18874376, 
                //"Pips":[4,8,0], "FireGroup":1, "GuiFocus":0, "Latitude":-18.978821, "Longitude":-123.642052, "Heading":308, "Altitude":20016 }

                string j = "{ " + Journal.TimeStamp() + Journal.F("event", "Status") + Journal.F("Flags", flags) + Journal.F("Pips", new int[] { 4, 8, 0 }) +
                            Journal.F("FireGroup", 1) + Journal.F("GuiFocus", 0) + Journal.F("Latitude", latitude) + Journal.F("Longitude", longitude) + 
                            Journal.F("Heading", heading) + Journal.F("Altitude", 20)
                            + "}";

                File.WriteAllText("Status.json", j);
                System.Threading.Thread.Sleep(steptime);

                if (Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.Escape)
                {
                    break;
                }

                latitude += latstep;
                longitude = longitude + longstep;
                heading = (heading + headstep) % 360;

            }
        }

        public static void StatusSet(CommandArgs args)
        {
            long flags = 0;
            int cargo = 0;
            double fuel = 0;
            int gui = 0;
            int fg = 1;

            string v;
            while ((v = args.Next()) != null)
            {
                if (v.Equals("Supercruise", StringComparison.InvariantCultureIgnoreCase))
                {
                    flags = (1L << (int)StatusFlagsShipType.InMainShip) |
                                (1L << (int)StatusFlagsShip.Supercruise) |
                                (1L << (int)StatusFlagsAll.ShieldsUp);
                }
                else if (v.Equals("Normal", StringComparison.InvariantCultureIgnoreCase))
                {
                    flags = (1L << (int)StatusFlagsShipType.InMainShip) |
                                (1L << (int)StatusFlagsAll.ShieldsUp);
                }
                else if (v.Equals("Landed", StringComparison.InvariantCultureIgnoreCase))
                {
                    flags = (1L << (int)StatusFlagsShipType.InMainShip) |
                                (1L << (int)StatusFlagsShip.Landed) |
                                (1L << (int)StatusFlagsShip.LandingGear) |
                                (1L << (int)StatusFlagsAll.ShieldsUp) |
                                (1L << (int)StatusFlagsAll.Lights);
                }
                else if (v.Equals("SRV", StringComparison.InvariantCultureIgnoreCase))
                {
                    flags = (1L << (int)StatusFlagsShipType.InSRV) |
                                (1L << (int)StatusFlagsShip.Landed) |
                                (1L << (int)StatusFlagsAll.ShieldsUp) |
                                (1L << (int)StatusFlagsAll.Lights);
                }
                else if (v.Equals("Fight", StringComparison.InvariantCultureIgnoreCase))
                {
                    flags = (1L << (int)StatusFlagsShipType.InMainShip) |
                                (1L << (int)StatusFlagsAll.ShieldsUp) |
                                (1L << (int)StatusFlagsShip.HardpointsDeployed);
                }
                else if (v.StartsWith("C:"))
                {
                    cargo = v.Mid(2).InvariantParseInt(0);
                }
                else if (v.StartsWith("F:"))
                {
// TBD FIX
                    fuel = v.Mid(2).InvariantParseDouble(0);
                }
                else if (v.StartsWith("FG:"))
                {
                    fg = v.Mid(3).InvariantParseInt(0);
                }
                else if (v.StartsWith("G:"))
                {
                    gui = v.Mid(2).InvariantParseInt(0);

                }
                else if (Enum.TryParse<StatusFlagsShip>(v, true, out StatusFlagsShip s))
                {
                    flags |= 1L << (int)s;
                }
                else if (Enum.TryParse<StatusFlagsSRV>(v, true, out StatusFlagsSRV sv))
                {
                    flags |= 1L << (int)sv;
                }
                else if (Enum.TryParse<StatusFlagsAll>(v, true, out StatusFlagsAll a))
                {
                    flags |= 1L << (int)a;
                }
                else if (Enum.TryParse<StatusFlagsShipType>(v, true, out StatusFlagsShipType st))
                {
                    flags |= 1L << (int)st;
                }
                else
                {
                    Console.WriteLine("Bad flag " + v);
                    Console.WriteLine("Flags " + String.Join(",", Enum.GetNames(typeof(StatusFlagsShip))));
                    Console.WriteLine("Flags " + String.Join(",", Enum.GetNames(typeof(StatusFlagsSRV))));
                    Console.WriteLine("Flags " + String.Join(",", Enum.GetNames(typeof(StatusFlagsAll))));
                    Console.WriteLine("Flags " + String.Join(",", Enum.GetNames(typeof(StatusFlagsShipType))));
                    return;
                }
            }

            BaseUtils.QuickJSONFormatter qj = new QuickJSONFormatter();

            qj.Object().UTC("timestamp").V("event", "Status");
            qj.V("Flags", flags);
            qj.V("Pips", new int[] { 2, 8, 2 });
            qj.V("FireGroup", fg);
            qj.V("GuiFocus", gui);
            qj.Object("Fuel").V("FuelMain",fuel).V("FuelReservoir",0.32).Close();
            qj.V("Cargo", cargo);
            qj.Close();

            string j = qj.Get();
            File.WriteAllText("Status.json", j);
            Console.Write(j);
        }

        static public void StatusRead(CommandArgs args)
        {
            string user = Environment.GetEnvironmentVariable("USERNAME");

            string path = @"c:\users\" + user + @"\saved games\frontier developments\elite dangerous\";
            string file = "status.json";
            string watchfile = Path.Combine(path, file);

            string laststatus = "";
            Console.Clear();

            while (true)
            {
                string nextstatus = null;

                Stream stream = null;
                try
                {
                    stream = File.Open(watchfile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                    StreamReader reader = new StreamReader(stream);

                    nextstatus = reader.ReadLine();

                    stream.Close();
                }
                catch
                { }
                finally
                {
                    if (stream != null)
                        stream.Dispose();
                }

                if (nextstatus != null && nextstatus != laststatus)
                {
                    JToken j = JToken.Parse(nextstatus);

                    Console.CursorTop = 0;
                    Console.WriteLine(j.ToString(Newtonsoft.Json.Formatting.Indented));
                    laststatus = nextstatus;
                }

                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo i = Console.ReadKey();

                    if (i.Key == ConsoleKey.Escape)
                    {
                        break;
                    }
                }

                Thread.Sleep(25);
            }
        }

    }
}
