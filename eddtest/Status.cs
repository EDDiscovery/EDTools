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
using System.Threading;

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
            AltitudeFromAverageRadius = 29, // 3.4
        }

        private enum StatusFlagsShipType
        {
            InMainShip = 24,        // -> Degenerates to UIShipType
            InFighter = 25,
            InSRV = 26,
            ShipMask = (1 << InMainShip) | (1 << InFighter) | (1 << InSRV),
        }

        private enum StatusFlagsOnFoot
        {
            OnFoot = 0,                 // alpha4 Station Corolis has OnFoot | OnFootInStation
            InTaxi = 1,
            InMulticrew = 2,
            OnFootInStation = 3,
            OnFootOnPlanet = 4,
            AimDownSight = 5,
            LowOxygen = 6,
            LowHealth = 7,
            Cold = 8,
            Hot = 9,
            VeryCold = 10,
            VeryHot = 11,
            GlideMode = 12,
            OnFootInHangar = 13,
            OnFootInSocialSpace = 14,
            OnFootExterior = 15,
            BreathableAtmosphere = 16,
        }


        public static void StatusMove(CommandArgs args)
        {
            long flags = (1L << (int)StatusFlagsShipType.InSRV) |
                        (1L << (int)StatusFlagsShip.Landed) |
                        (1L << (int)StatusFlagsAll.ShieldsUp) |
                        (1L << (int)StatusFlagsAll.Lights);

            double latitude = 0;
            double longitude = 0;
            double latstep = 0;
            double longstep = 0;
            double heading = 0;
            double headstep = 1;
            int steptime = 100;
            int fg = 1;
            int gui = 0;
            string legalstate = "Clean";
            double fuel = 31.2;
            double fuelres = 0.23;
            int cargo = 23;

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

                Console.WriteLine("{0:0.00} {1:0.00} H {2:0.00} F {3:0.00}:{4:0.00}", latitude, longitude, heading, fuel, fuelres);
                BaseUtils.QuickJSONFormatter qj = new QuickJSONFormatter();

                double altitude = 404;

                qj.Object().UTC("timestamp").V("event", "Status");
                qj.V("Flags", flags);
                qj.V("Pips", new int[] { 2, 8, 2 });
                qj.V("FireGroup", fg);
                qj.V("GuiFocus", gui);
                qj.V("LegalState", legalstate);
                qj.V("Latitude", latitude);
                qj.V("Longitude", longitude);
                qj.V("Heading", heading);
                qj.V("Altitude", altitude);

                qj.Object("Fuel").V("FuelMain", fuel).V("FuelReservoir", fuelres).Close();
                qj.V("Cargo", cargo);
                qj.Close();

                File.WriteAllText("Status.json", qj.Get());
                System.Threading.Thread.Sleep(steptime);

                if (Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.Escape)
                {
                    break;
                }

                latitude += latstep;
                longitude = longitude + longstep;
                heading = (heading + headstep) % 360;

                fuelres -= 0.02;
                if ( fuelres < 0 )
                {
                    fuel--;
                    fuelres = 0.99;
                }

            }
        }

        public static void StatusSet(CommandArgs args)
        {
            long flags = 0,flags2=0;
            int cargo = 0;
            double fuel = 0;
            int gui = 0;
            int fg = 1;
            double oxygen = 1.0;
            double health = 1.0;
            double temperature = 293.0;
            string SelectedWeapon = "";
            string SelectedWeaponLoc = "";
            double gravity = 0.166399;
            double lat = 3.2;
            double lon = 6.2;
            double heading = 92.3;
            double altitude = -999;
            double planetradius = -999;
            string bodyname = "";

            string legalstate = "Clean";

            if ( args.Left == 0 )
            {
                Console.WriteLine("Status [C:cargo] [F:fuel] [FG:Firegroup] [G:Gui] [L:Legalstate] [0x:flag dec int]\n" +
                                  "       [GV:gravity] [H:health] [O:oxygen] [T:Temp] [S:selectedweapon] [B:bodyname]\n" +
                                  "       [normalspace | supercruise | dockedstarport | dockedinstallation | fight | fighter |\n" +
                                  "        landed | SRV | TaxiNormalSpace | TaxiSupercruise | Off\n" +
                                  "        onfootininstallation | onfootplanet |\n" +
                                  "        onfootinplanetaryporthangar | onfootinplanetaryportsocialspace |\n" +
                                  "        onfootinstarporthangar | onfootinstarportsocialspace |\n"
                                );
                Console.WriteLine("       " + string.Join(",", Enum.GetNames(typeof(StatusFlagsShip))));
                Console.WriteLine("       " + string.Join(",", Enum.GetNames(typeof(StatusFlagsSRV))));
                Console.WriteLine("       " + string.Join(",", Enum.GetNames(typeof(StatusFlagsAll))));
                Console.WriteLine("       " + string.Join(",", Enum.GetNames(typeof(StatusFlagsShipType))));
                Console.WriteLine("       " + string.Join(",", Enum.GetNames(typeof(StatusFlagsOnFoot))));
                return;
            }


            string v;
            while ((v = args.Next()) != null)
            {
                if (v.Equals("Off", StringComparison.InvariantCultureIgnoreCase))            
                {
                    flags = 0;
                }
                else if (v.Equals("Supercruise", StringComparison.InvariantCultureIgnoreCase))               // checked alpha 4
                {
                    flags = (1L << (int)StatusFlagsShipType.InMainShip) |
                                (1L << (int)StatusFlagsShip.Supercruise) |
                                (1L << (int)StatusFlagsAll.ShieldsUp);
                }
                else if (v.Equals("NormalSpace", StringComparison.InvariantCultureIgnoreCase))          // checked alpha 4
                {
                    flags = (1L << (int)StatusFlagsShipType.InMainShip) |
                                (1L << (int)StatusFlagsAll.ShieldsUp);
                }
                else if (v.Equals("TaxiSupercruise", StringComparison.InvariantCultureIgnoreCase))               // checked alpha 4
                {
                    flags = (1L << (int)StatusFlagsShipType.InMainShip) |
                                (1L << (int)StatusFlagsShip.Supercruise) |
                                (1L << (int)StatusFlagsAll.ShieldsUp);
                    flags2 = (1L << (int)StatusFlagsOnFoot.InTaxi);
                }
                else if (v.Equals("TaxiNormalSpace", StringComparison.InvariantCultureIgnoreCase))          // checked alpha 4
                {
                    flags = (1L << (int)StatusFlagsShipType.InMainShip) |
                                (1L << (int)StatusFlagsAll.ShieldsUp);
                    flags2 = (1L << (int)StatusFlagsOnFoot.InTaxi);
                }
                else if (v.Equals("Fight", StringComparison.InvariantCultureIgnoreCase))
                {
                    flags = (1L << (int)StatusFlagsShipType.InMainShip) |
                                (1L << (int)StatusFlagsAll.ShieldsUp) |
                                (1L << (int)StatusFlagsShip.HardpointsDeployed);
                }
                else if (v.Equals("Fighter", StringComparison.InvariantCultureIgnoreCase))
                {
                    flags = (1L << (int)StatusFlagsShipType.InFighter) |
                                (1L << (int)StatusFlagsAll.ShieldsUp);
                }
                else if (v.Equals("DockedStarPort", StringComparison.InvariantCultureIgnoreCase))                  // checked alpha 4
                {
                    flags = (1L << (int)StatusFlagsShipType.InMainShip) |
                                (1L << (int)StatusFlagsAll.ShieldsUp) |
                                (1L << (int)StatusFlagsShip.FsdMassLocked) |
                                (1L << (int)StatusFlagsShip.LandingGear) |
                                (1L << (int)StatusFlagsShip.Docked);
                }
                else if (v.Equals("DockedInstallation", StringComparison.InvariantCultureIgnoreCase))       // checked alpha 4
                {
                    flags = (1L << (int)StatusFlagsShip.Docked) |
                           (1L << (int)StatusFlagsShip.LandingGear) |
                            (1L << (int)StatusFlagsShip.FsdMassLocked) |
                                (1L << (int)StatusFlagsAll.ShieldsUp) |
                                (1L << (int)StatusFlagsAll.HasLatLong) |
                            (1L << (int)StatusFlagsShipType.InMainShip);

                    bodyname = "Nervi 2g";
                    altitude = 0;
                    planetradius = 2796748.25;
                }
                else if (v.Equals("OnFootInPlanetaryPortHangar", StringComparison.InvariantCultureIgnoreCase))
                {
                    flags = (1L << (int)StatusFlagsAll.HasLatLong);
                    flags2 = (1L << (int)StatusFlagsOnFoot.OnFoot) | (1L << (int)StatusFlagsOnFoot.OnFootInHangar);
                    bodyname = "Nervi 2g??";
                }
                else if (v.Equals("OnFootInPlanetaryPortSocialSpace", StringComparison.InvariantCultureIgnoreCase))
                {
                    flags = (1L << (int)StatusFlagsAll.HasLatLong);
                    flags2 = (1L << (int)StatusFlagsOnFoot.OnFoot) | (1L << (int)StatusFlagsOnFoot.OnFootInSocialSpace);
                    bodyname = "Nervi 2g??";
                }
                else if (v.Equals("OnFootInStarportHangar", StringComparison.InvariantCultureIgnoreCase))
                {
                    flags = (1L << (int)StatusFlagsAll.HasLatLong);
                    flags2 = (1L << (int)StatusFlagsOnFoot.OnFoot) | (1L << (int)StatusFlagsOnFoot.OnFootInHangar) | (1L << (int)StatusFlagsOnFoot.OnFootInStation);
                    bodyname = "Starport";
                }
                else if (v.Equals("OnFootInStarportSocialSpace", StringComparison.InvariantCultureIgnoreCase))
                {
                    flags = (1L << (int)StatusFlagsAll.HasLatLong);
                    flags2 = (1L << (int)StatusFlagsOnFoot.OnFoot) | (1L << (int)StatusFlagsOnFoot.OnFootInSocialSpace) | (1L << (int)StatusFlagsOnFoot.OnFootInStation);
                    bodyname = "Starport";
                }
                else if (v.Equals("OnFootInInstallation", StringComparison.InvariantCultureIgnoreCase))    
                {
                    flags = (1L << (int)StatusFlagsAll.HasLatLong);
                    flags2 = (1L << (int)StatusFlagsOnFoot.OnFoot) | (1L << (int)StatusFlagsOnFoot.OnFootExterior);     // tbd if this is correct
                    temperature = 82;
                    SelectedWeapon = "$humanoid_fists_name;";
                    SelectedWeaponLoc = "Unarmed";
                    bodyname = "Nervi 2g";
                }
                else if (v.Equals("OnFootPlanet", StringComparison.InvariantCultureIgnoreCase))    
                {
                    flags = (1L << (int)StatusFlagsAll.HasLatLong);
                    flags2 = (1L << (int)StatusFlagsOnFoot.OnFoot) | (1L << (int)StatusFlagsOnFoot.OnFootOnPlanet);
                    temperature = 78;
                    bodyname = "Nervi 2g";
                    SelectedWeapon = "$humanoid_fists_name;";
                    SelectedWeaponLoc = "Unarmed";
                }
                else if (v.Equals("Landed", StringComparison.InvariantCultureIgnoreCase))           // checked alpha 4
                {
                    flags = (1L << (int)StatusFlagsShipType.InMainShip) |
                                (1L << (int)StatusFlagsShip.Landed) |
                                (1L << (int)StatusFlagsShip.LandingGear) |
                                (1L << (int)StatusFlagsShip.FsdMassLocked) |
                                (1L << (int)StatusFlagsAll.ShieldsUp) |
                                (1L << (int)StatusFlagsAll.HasLatLong) |
                                (1L << (int)StatusFlagsAll.Lights);
                    bodyname = "Nervi 2g";
                    planetradius = 292892882.2;
                    altitude = 0;
                }
                else if (v.Equals("SRV", StringComparison.InvariantCultureIgnoreCase))              // checked alpha 4
                {
                    flags =     (1L << (int)StatusFlagsAll.ShieldsUp) |
                                (1L << (int)StatusFlagsAll.Lights) |
                                (1L << (int)StatusFlagsAll.HasLatLong) |
                                (1L << (int)StatusFlagsShipType.InSRV);
                    bodyname = "Nervi 2g";
                    planetradius = 292892882.2;
                    altitude = 0;
                }
                else if (v.StartsWith("C:"))
                {
                    cargo = v.Mid(2).InvariantParseInt(0);
                }
                else if (v.StartsWith("F:"))
                {
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
                else if (v.StartsWith("0x:"))
                {
                    flags = long.Parse(v.Mid(3), System.Globalization.NumberStyles.HexNumber);
                }
                else if (v.StartsWith("L:"))
                {
                    legalstate = v.Mid(2);
                }
                else if (v.StartsWith("H:"))
                {
                    health = v.Mid(2).InvariantParseDouble(0);
                }
                else if (v.StartsWith("T:"))
                {
                    temperature= v.Mid(2).InvariantParseDouble(0);
                }
                else if (v.StartsWith("O:"))
                {
                    oxygen = v.Mid(2).InvariantParseDouble(0);
                }
                else if (v.StartsWith("GV:"))
                {
                    gravity = v.Mid(3).InvariantParseDouble(0);
                }
                else if (v.StartsWith("B:"))
                {
                    bodyname = v.Mid(2);
                }
                else if (v.StartsWith("S:"))
                {
                    SelectedWeapon = v.Mid(2);
                    SelectedWeaponLoc = SelectedWeapon + "_loc";
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
                else if (Enum.TryParse<StatusFlagsOnFoot>(v, true, out StatusFlagsOnFoot of))
                {
                    flags2 |= 1L << (int)of;
                }
                else
                {
                    Console.WriteLine("Bad flag " + v);
                    Console.WriteLine("Flags " + String.Join(",", Enum.GetNames(typeof(StatusFlagsShip))));
                    Console.WriteLine("Flags " + String.Join(",", Enum.GetNames(typeof(StatusFlagsSRV))));
                    Console.WriteLine("Flags " + String.Join(",", Enum.GetNames(typeof(StatusFlagsAll))));
                    Console.WriteLine("Flags " + String.Join(",", Enum.GetNames(typeof(StatusFlagsShipType))));
                    Console.WriteLine("Flags2 " + String.Join(",", Enum.GetNames(typeof(StatusFlagsOnFoot))));
                    return;
                }
            }

            BaseUtils.QuickJSONFormatter qj = new QuickJSONFormatter();

            qj.Object().UTC("timestamp").V("event", "Status");
            qj.V("Flags", flags);

            if (flags != 0 || flags2 != 0)
            {
                qj.V("Flags2", flags2);

                if ((flags2 & (1 << (int)StatusFlagsOnFoot.OnFoot)) != 0)
                {
                    qj.V("Oxygen", oxygen);
                    qj.V("Health", health);
                    qj.V("Temperature", temperature);
                    qj.V("SelectedWeapon", SelectedWeapon);
                    if (SelectedWeaponLoc.HasChars())
                        qj.V("SelectedWeapon_Localised", SelectedWeaponLoc);
                    qj.V("Gravity", gravity);
                }
                else
                {
                    qj.V("Pips", new int[] { 2, 8, 2 });
                    qj.V("FireGroup", fg);
                    qj.V("GuiFocus", gui);
                }

                if ((flags & (1 << (int)StatusFlagsShipType.InMainShip)) != 0 || (flags & (1 << (int)StatusFlagsShipType.InSRV)) != 0)
                {
                    qj.Object("Fuel").V("FuelMain", fuel).V("FuelReservoir", 0.32).Close();
                    qj.V("Cargo", cargo);
                }

                qj.V("LegalState", legalstate);

                if ((flags & (1 << (int)StatusFlagsAll.HasLatLong)) != 0)
                {
                    qj.V("Latitude", lat);
                    qj.V("Longitude", lon);
                    qj.V("Heading", heading);

                    if (altitude >= 0)
                        qj.V("Altitude", altitude);
                }

                if (bodyname.HasChars())
                    qj.V("BodyName", bodyname);

                if (planetradius >= 0)
                    qj.V("PlanetRadius", planetradius);
            }

            qj.Close();

            string j = qj.Get();
            File.WriteAllText("Status.json", j);
            Console.Write(j);
        }

        static public void StatusRead(string file)
        {
            string user = Environment.GetEnvironmentVariable("USERNAME");

            string path = @"c:\users\" + user + @"\saved games\frontier developments\elite dangerous\";
            string watchfile = Path.Combine(path, file);

            string laststatus = "";

            while (true)
            {
                string nextstatus = null;

                Stream stream = null;
                try
                {
                    stream = File.Open(watchfile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                    StreamReader reader = new StreamReader(stream);

                    nextstatus = reader.ReadToEnd();

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

                    Console.Clear();
                    Console.CursorTop = 0;

                    if (j == null)
                    {
                        Console.WriteLine("Bad JSON" + nextstatus);
                    }
                    else
                    {
                        string s = j.ToString(true);
                        System.Diagnostics.Debug.WriteLine(s);
                        Console.WriteLine(watchfile);
                        Console.WriteLine(s);

                        ulong flags = j["Flags"].ULong();

                        foreach (var x in Enum.GetValues(typeof(StatusFlagsShip)))
                        {
                            ulong bit = (ulong)(1 << (int)x);
                            if ((flags & bit) != 0)
                            {
                                flags &= ~bit;
                                Console.WriteLine("+ " + x.ToString());
                            }
                        }

                        foreach (var x in Enum.GetValues(typeof(StatusFlagsSRV)))
                        {
                            ulong bit = (ulong)(1 << (int)x);
                            if ((flags & bit) != 0)
                            {
                                flags &= ~bit;
                                Console.WriteLine("+ " + x.ToString());
                            }
                        }

                        foreach (var x in Enum.GetValues(typeof(StatusFlagsAll)))
                        {
                            ulong bit = (ulong)(1 << (int)x);
                            if ((flags & bit) != 0)
                            {
                                flags &= ~bit;
                                Console.WriteLine("+ " + x.ToString());
                            }
                        }

                        foreach (var x in Enum.GetValues(typeof(StatusFlagsShipType)))
                        {
                            ulong bit = (ulong)(1 << (int)x);
                            if ((flags & bit) != 0)
                            {
                                flags &= ~bit;
                                Console.WriteLine("+ " + x.ToString());
                            }
                        }

                        if (flags != 0)
                            Console.WriteLine(" Remaining bits " + flags.ToString("x"));

                        ulong flags2 = j["Flags2"].ULong();

                        foreach (var x in Enum.GetValues(typeof(StatusFlagsOnFoot)))
                        {
                            ulong bit = (ulong)(1 << (int)x);
                            if ((flags2 & bit) != 0)
                            {
                                flags2 &= ~bit;
                                Console.WriteLine("+ " + x.ToString());
                            }
                        }

                        if (flags2 != 0)
                            Console.WriteLine(" F2 Remaining bits " + flags2.ToString("x"));

                        laststatus = nextstatus;
                    }
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
