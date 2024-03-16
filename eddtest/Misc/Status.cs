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
 */

using BaseUtils;
using QuickJSON;
using System;
using System.IO;
using System.Threading;

namespace EDDTest
{
    public class Status
    {
        public enum StatusFlags1Ship                             // Flags
        {
            ShipDocked = 0, // (on a landing pad)
            ShipLanded = 1, // (on planet surface)
            LandingGear = 2,
            InSupercruise = 4,
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
            FsdJump = 30,
        }

        public enum StatusFlags1SRV                              // Flags
        {
            SrvHandbrake = 12,
            SrvTurret = 13,
            SrvUnderShip = 14,
            SrvDriveAssist = 15,
            SrvHighBeam = 31,
        }

        public enum StatusFlags1All                             // Flags
        {
            ShieldsUp = 3,
            Lights = 8,
            LowFuel = 19,
            HasLatLong = 21,
            IsInDanger = 22,
            NightVision = 28,             // 3.3
        }

        private enum StatusFlags1ReportedInOtherEvents       // reported via other mechs than flags 
        {
            AltitudeFromAverageRadius = 29, // 3.4, via position
        }

        // shiptype (operating mode)

        public enum StatusFlags1ShipType                        // Flags
        {
            InMainShip = 24,
            InFighter = 25,
            InSRV = 26,
            ShipMask = (1 << InMainShip) | (1 << InFighter) | (1 << InSRV),
        }

        public enum StatusFlags2ShipType                   // used to compute ship type
        {
            OnFoot = 0,
            InTaxi = 1,
            InMulticrew = 2,
            OnFootInStation = 3,
            OnFootOnPlanet = 4,
            OnFootInHangar = 13,
            OnFootInSocialSpace = 14,
            OnFootExterior = 15,
        }

        public enum StatusFlags2Events                  // these are bool flags, reported sep.
        {
            AimDownSight = 5,
            GlideMode = 12,
            BreathableAtmosphere = 16,
        }

        public enum StatusFlags2ReportedInOtherMessages     // these are states reported as part of other messages
        {
            LowOxygen = 6,
            LowHealth = 7,
            Cold = 8,
            Hot = 9,
            VeryCold = 10,
            VeryHot = 11,
            TempBits = (1 << Cold) | (1 << Hot) | (1 << VeryCold) | (1 << VeryHot),
            FSDHyperdriveCharging = 19,         // U14 nov 22
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
            double heading = -999;
            double altitude = -999;
            double planetradius = -999;
            string bodyname = "";
            string destinationname = "";
            int destinationbody = 0;
            int[] pips = new int[] { 2, 8, 2 };

            string legalstate = "Clean";

            if ( args.Left == 0 )
            {
                Console.WriteLine("Status [C:cargo] [F:fuel] [FG:Firegroup] [G:Gui] [LS:Legalstate] [0x:flag dec int]\n" +
                                  "       [GV:gravity] [H:health] [O:oxygen] [T:Temp] [S:selectedweapon] [B:bodyname] [P:W,E,S]\n" +
                                  "       [D:destname,dest-bid] [L:lat,long,alt] [HD:heading] [R:radiusm]\n" +
                                  "        normalspace | supercruise | dockedstarport | dockedinstallation | fight | fighter |\n" +
                                  "        landed | SRV | TaxiNormalSpace | TaxiSupercruise | Off\n" +
                                  "        onfootininstallation | onfootplanet |\n" +
                                  "        onfootinplanetaryporthangar | onfootinplanetaryportsocialspace |\n" +
                                  "        onfootinstarporthangar | onfootinstarportsocialspace\n"
                                );
                Console.WriteLine("       " + string.Join(",", Enum.GetNames(typeof(StatusFlags1Ship))));
                Console.WriteLine("       " + string.Join(",", Enum.GetNames(typeof(StatusFlags1SRV))));
                Console.WriteLine("       " + string.Join(",", Enum.GetNames(typeof(StatusFlags1All))));
                Console.WriteLine("       " + string.Join(",", Enum.GetNames(typeof(StatusFlags1ReportedInOtherEvents))));
                Console.WriteLine("       " + string.Join(",", Enum.GetNames(typeof(StatusFlags1ShipType))));
                Console.WriteLine("       " + string.Join(",", Enum.GetNames(typeof(StatusFlags2ShipType))));
                Console.WriteLine("       " + string.Join(",", Enum.GetNames(typeof(StatusFlags2Events))));
                Console.WriteLine("       " + string.Join(",", Enum.GetNames(typeof(StatusFlags2ReportedInOtherMessages))));
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
                    flags = (1L << (int)StatusFlags1ShipType.InMainShip) |
                                (1L << (int)StatusFlags1Ship.InSupercruise) |
                                (1L << (int)StatusFlags1All.ShieldsUp);
                }
                else if (v.Equals("NormalSpace", StringComparison.InvariantCultureIgnoreCase))          // checked alpha 4
                {
                    flags = (1L << (int)StatusFlags1ShipType.InMainShip) |
                                (1L << (int)StatusFlags1All.ShieldsUp);
                }
                else if (v.Equals("TaxiSupercruise", StringComparison.InvariantCultureIgnoreCase))               // checked alpha 4
                {
                    flags = (1L << (int)StatusFlags1ShipType.InMainShip) |
                                (1L << (int)StatusFlags1Ship.InSupercruise) |
                                (1L << (int)StatusFlags1All.ShieldsUp);
                    flags2 = (1L << (int)StatusFlags2ShipType.InTaxi);
                }
                else if (v.Equals("TaxiNormalSpace", StringComparison.InvariantCultureIgnoreCase))          // checked alpha 4
                {
                    flags = (1L << (int)StatusFlags1ShipType.InMainShip) |
                                (1L << (int)StatusFlags1All.ShieldsUp);
                    flags2 = (1L << (int)StatusFlags2ShipType.InTaxi);
                }
                else if (v.Equals("Fight", StringComparison.InvariantCultureIgnoreCase))
                {
                    flags = (1L << (int)StatusFlags1ShipType.InMainShip) |
                                (1L << (int)StatusFlags1All.ShieldsUp) |
                                (1L << (int)StatusFlags1Ship.HardpointsDeployed);
                }
                else if (v.Equals("Fighter", StringComparison.InvariantCultureIgnoreCase))
                {
                    flags = (1L << (int)StatusFlags1ShipType.InFighter) |
                                (1L << (int)StatusFlags1All.ShieldsUp);
                }
                else if (v.Equals("DockedStarPort", StringComparison.InvariantCultureIgnoreCase))             
                {
                    flags = (1L << (int)StatusFlags1Ship.ShipDocked) |
                        (1L << (int)StatusFlags1Ship.LandingGear) |
                                (1L << (int)StatusFlags1Ship.FsdMassLocked) |
                                (1L << (int)StatusFlags1All.ShieldsUp) |
                        (1L << (int)StatusFlags1ShipType.InMainShip);
                }
                else if (v.Equals("DockedInstallation", StringComparison.InvariantCultureIgnoreCase))   // TBD
                {
                    flags = (1L << (int)StatusFlags1Ship.ShipDocked) |
                           (1L << (int)StatusFlags1Ship.LandingGear) |
                            (1L << (int)StatusFlags1Ship.FsdMassLocked) |
                                (1L << (int)StatusFlags1All.ShieldsUp) |
                                (1L << (int)StatusFlags1All.HasLatLong) |
                            (1L << (int)StatusFlags1ShipType.InMainShip);

                    bodyname = "Nervi 2g";
                    altitude = 0;
                    heading = 20;
                    planetradius = 2796748.25;
                }
                else if (v.Equals("OnFootInPlanetaryPortHangar", StringComparison.InvariantCultureIgnoreCase))
                {
                    flags = (1L << (int)StatusFlags1All.HasLatLong);
                    flags2 = (1L << (int)StatusFlags2ShipType.OnFoot) |
                            (1L << (int)StatusFlags2ShipType.OnFootOnPlanet) |
                            (1L << (int)StatusFlags2ShipType.OnFootInHangar) |
                            (1L << (int)StatusFlags2ShipType.OnFootInSocialSpace) |
                            (1L << (int)StatusFlags2Events.BreathableAtmosphere);
                    bodyname = "Nervi 2g??";
                }
                else if (v.Equals("OnFootInPlanetaryPortSocialSpace", StringComparison.InvariantCultureIgnoreCase))
                {
                    flags = (1L << (int)StatusFlags1All.HasLatLong);
                    flags2 = (1L << (int)StatusFlags2ShipType.OnFoot) |
                             (1L << (int)StatusFlags2ShipType.OnFootOnPlanet) |
                             (1L << (int)StatusFlags2ShipType.OnFootInSocialSpace) |
                             (1L << (int)StatusFlags2Events.BreathableAtmosphere);
                    bodyname = "Nervi 2g";
                }
                else if (v.Equals("OnFootInStarportHangar", StringComparison.InvariantCultureIgnoreCase))
                {
                    flags = (1L << (int)StatusFlags1All.HasLatLong);
                    flags2 = (1L << (int)StatusFlags2ShipType.OnFoot) |
                        (1L << (int)StatusFlags2ShipType.OnFootInStation) |
                        (1L << (int)StatusFlags2ShipType.OnFootInHangar) |
                        (1L << (int)StatusFlags2ShipType.OnFootInSocialSpace) |
                        (1L << (int)StatusFlags2Events.BreathableAtmosphere);
                    bodyname = "Drexler Colony";
                }
                else if (v.Equals("OnFootInStarportSocialSpace", StringComparison.InvariantCultureIgnoreCase))
                {
                    flags = (1L << (int)StatusFlags1All.HasLatLong);
                    flags2 = (1L << (int)StatusFlags2ShipType.OnFoot) | 
                            (1L << (int)StatusFlags2ShipType.OnFootInStation) |
                            (1L << (int)StatusFlags2ShipType.OnFootInSocialSpace)|
                            (1L << (int)StatusFlags2Events.BreathableAtmosphere);
                    bodyname = "Starport";
                }
                else if (v.Equals("OnFootInInstallation", StringComparison.InvariantCultureIgnoreCase))    
                {
                    flags = (1L << (int)StatusFlags1All.HasLatLong);
                    flags2 = (1L << (int)StatusFlags2ShipType.OnFoot) |
                             (1L << (int)StatusFlags2ShipType.OnFootOnPlanet) |
                             (1L << (int)StatusFlags2ReportedInOtherMessages.Cold) |
                             (1L << (int)StatusFlags2ShipType.OnFootExterior);     // tbd if this is correct
                    temperature = 82;
                    SelectedWeapon = "$humanoid_fists_name;";
                    SelectedWeaponLoc = "Unarmed";
                    bodyname = "Nervi 2g";
                }
                else if (v.Equals("OnFootPlanet", StringComparison.InvariantCultureIgnoreCase))    
                {
                    flags = (1L << (int)StatusFlags1All.HasLatLong);
                    flags2 = (1L << (int)StatusFlags2ShipType.OnFoot) | (1L << (int)StatusFlags2ShipType.OnFootOnPlanet);
                    temperature = 78;
                    bodyname = "Nervi 2g";
                    SelectedWeapon = "$humanoid_fists_name;";
                    SelectedWeaponLoc = "Unarmed";
                }
                else if (v.Equals("Landed", StringComparison.InvariantCultureIgnoreCase))           // checked alpha 4
                {
                    flags = (1L << (int)StatusFlags1ShipType.InMainShip) |
                                (1L << (int)StatusFlags1Ship.ShipLanded) |
                                (1L << (int)StatusFlags1Ship.LandingGear) |
                                (1L << (int)StatusFlags1Ship.FsdMassLocked) |
                                (1L << (int)StatusFlags1All.ShieldsUp) |
                                (1L << (int)StatusFlags1All.HasLatLong) |
                                (1L << (int)StatusFlags1All.Lights);
                    bodyname = "Nervi 2g";
                    planetradius = 292892882.2;
                    altitude = 0;
                }
                else if (v.Equals("SRV", StringComparison.InvariantCultureIgnoreCase))              // checked alpha 4
                {
                    flags =     (1L << (int)StatusFlags1All.ShieldsUp) |
                                (1L << (int)StatusFlags1All.Lights) |
                                (1L << (int)StatusFlags1All.HasLatLong) |
                                (1L << (int)StatusFlags1ShipType.InSRV);
                    bodyname = "Nervi 2g";
                    planetradius = 292892882.2;
                    altitude = 0;
                    heading = 20;
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
                else if (v.StartsWith("LS:"))
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
                else if (v.StartsWith("R:"))
                {
                    planetradius = v.Mid(2).InvariantParseDouble(0);
                }
                else if (v.StartsWith("HD:"))
                {
                    heading = v.Mid(3).InvariantParseDouble(0);
                }
                else if (v.StartsWith("L:"))
                {
                    v = v.Substring(2);
                    int comma1 = v.IndexOf(",");
                    int comma2 = comma1 > 0 ? v.IndexOf(",", comma1+1) : -1;
                    if (comma2 > 0)
                    {
                        lat = v.Substring(0, comma1).InvariantParseDouble(0);
                        lon = v.Substring(comma1 + 1, comma2 - comma1 - 1).InvariantParseDouble(0);
                        altitude = v.Substring(comma2 + 1).InvariantParseDouble(0);
                        flags |= (1L << (int)StatusFlags1All.HasLatLong);
                    }
                }
                else if (v.StartsWith("P:"))
                {
                    pips = v.Mid(2).RestoreArrayFromString(0, 3);
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
                else if (v.StartsWith("D:"))
                {
                    int comma = v.IndexOf(",");
                    if ( comma >= 0)
                    {
                        destinationname = v.Substring(2, comma - 2).Trim();
                        destinationbody = v.Substring(comma + 1).InvariantParseInt(0);
                    }
                }
                else if (Enum.TryParse<StatusFlags1Ship>(v, true, out StatusFlags1Ship s))
                {
                    flags |= 1L << (int)s;
                }
                else if (Enum.TryParse<StatusFlags1SRV>(v, true, out StatusFlags1SRV sv))
                {
                    flags |= 1L << (int)sv;
                }
                else if (Enum.TryParse<StatusFlags1All>(v, true, out StatusFlags1All a))
                {
                    flags |= 1L << (int)a;
                }
                else if (Enum.TryParse<StatusFlags1ShipType>(v, true, out StatusFlags1ShipType st))
                {
                    flags |= 1L << (int)st;
                }
                else if (Enum.TryParse<StatusFlags2ShipType>(v, true, out StatusFlags2ShipType of))
                {
                    flags2 |= 1L << (int)of;
                }
                else if (Enum.TryParse<StatusFlags2Events>(v, true, out StatusFlags2Events f2e))
                {
                    flags2 |= 1L << (int)f2e;
                }
                else if (Enum.TryParse<StatusFlags2ReportedInOtherMessages>(v, true, out StatusFlags2ReportedInOtherMessages f2o))
                {
                    flags2 |= 1L << (int)f2o;
                }
                else if (Enum.TryParse<StatusFlags1ReportedInOtherEvents>(v, true, out StatusFlags1ReportedInOtherEvents f1o))
                {
                    flags |= 1L << (int)f1o;
                }
                else
                {
                    Console.WriteLine("Bad flag " + v);
                    return;
                }
            }

            JSONFormatter qj = new JSONFormatter();

            qj.Object().UTC("timestamp").V("event", "Status");
            qj.V("Flags", flags);

            if (flags != 0 || flags2 != 0)
            {
                qj.V("Flags2", flags2);

                if ((flags2 & (1 << (int)StatusFlags2ShipType.OnFoot)) != 0)
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
                    qj.Array("Pips").V(pips[0]).V(pips[1]).V(pips[2]).Close();
                    qj.V("FireGroup", fg);
                    qj.V("GuiFocus", gui);
                }

                if ((flags & (1 << (int)StatusFlags1ShipType.InMainShip)) != 0 || (flags & (1 << (int)StatusFlags1ShipType.InSRV)) != 0)
                {
                    qj.Object("Fuel").V("FuelMain", fuel).V("FuelReservoir", 0.32).Close();
                    qj.V("Cargo", cargo);
                }

                qj.V("LegalState", legalstate);

                if ((flags & (1 << (int)StatusFlags1All.HasLatLong)) != 0)
                {
                    qj.V("Latitude", lat);
                    qj.V("Longitude", lon);
                    if ( heading>=0)
                        qj.V("Heading", heading);

                    if (altitude >= 0)
                        qj.V("Altitude", altitude);
                }

                if (bodyname.HasChars())
                    qj.V("BodyName", bodyname);

                if (planetradius >= 0)
                    qj.V("PlanetRadius", planetradius);

                if ( destinationname.HasChars())
                    qj.Object("Destination").V("System", 2928282).V("Body", destinationbody).V("Name", destinationname).Close();
            }

            qj.Close();

            string j = qj.Get();
            File.WriteAllText("Status.json", j);
            JToken jk = JToken.Parse(j);
            if (jk != null)
                DecodeJson("Status Write", jk);
            else
                Console.WriteLine("Bad JSON written");
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
                        DecodeJson(watchfile, j);

                        laststatus = nextstatus;
                    }
                }

                if ( !Path.GetFileName(file).Equals("Status.json",StringComparison.InvariantCultureIgnoreCase))
                    break;

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


        static public void DecodeJson(string watchfile, JToken j)
        {
            string s = j.ToString(true);
            System.Diagnostics.Debug.WriteLine(s);
            Console.WriteLine(watchfile);
            Console.WriteLine(s);

            ulong flags = j["Flags"].ULong();

            foreach (var x in Enum.GetValues(typeof(StatusFlags1Ship)))
            {
                ulong bit = (ulong)(1 << (int)x);
                if ((flags & bit) != 0)
                {
                    flags &= ~bit;
                    Console.WriteLine("+ " + x.ToString());
                }
            }

            foreach (var x in Enum.GetValues(typeof(StatusFlags1SRV)))
            {
                ulong bit = (ulong)(1 << (int)x);
                if ((flags & bit) != 0)
                {
                    flags &= ~bit;
                    Console.WriteLine("+ " + x.ToString());
                }
            }

            foreach (var x in Enum.GetValues(typeof(StatusFlags1All)))
            {
                ulong bit = (ulong)(1 << (int)x);
                if ((flags & bit) != 0)
                {
                    flags &= ~bit;
                    Console.WriteLine("+ " + x.ToString());
                }
            }

            foreach (var x in Enum.GetValues(typeof(StatusFlags1ReportedInOtherEvents)))
            {
                ulong bit = (ulong)(1 << (int)x);
                if ((flags & bit) != 0)
                {
                    flags &= ~bit;
                    Console.WriteLine("+ " + x.ToString());
                }
            }

            foreach (var x in Enum.GetValues(typeof(StatusFlags1ShipType)))
            {
                ulong bit = (ulong)(1 << (int)x);
                if ((flags & bit) != 0)
                {
                    flags &= ~bit;
                    Console.WriteLine("+ " + x.ToString());
                }
            }

            if (flags != 0)
            {
                Console.WriteLine(" F1 Remaining bits " + flags.ToString("x"));
            }

            ulong flags2 = j["Flags2"].ULong();

            foreach (var x in Enum.GetValues(typeof(StatusFlags2ShipType)))
            {
                ulong bit = (ulong)(1 << (int)x);
                if ((flags2 & bit) != 0)
                {
                    flags2 &= ~bit;
                    Console.WriteLine("+ " + x.ToString());
                }
            }

            foreach (var x in Enum.GetValues(typeof(StatusFlags2Events)))
            {
                ulong bit = (ulong)(1 << (int)x);
                if ((flags2 & bit) != 0)
                {
                    flags2 &= ~bit;
                    Console.WriteLine("+ " + x.ToString());
                }
            }

            foreach (var x in Enum.GetValues(typeof(StatusFlags2ReportedInOtherMessages)))
            {
                ulong bit = (ulong)(1 << (int)x);
                if ((flags2 & bit) != 0)
                {
                    flags2 &= ~bit;
                    Console.WriteLine("+ " + x.ToString());
                }
            }

            if (flags2 != 0)
            {
                Console.WriteLine(" F2 Remaining bits " + flags2.ToString("x"));
            }
        }

        public static void StatusMove(CommandArgs args)
        {
            long flags = (1L << (int)StatusFlags1ShipType.InSRV) |
                        (1L << (int)StatusFlags1Ship.ShipLanded) |
                        (1L << (int)StatusFlags1All.ShieldsUp) |
                        (1L << (int)StatusFlags1All.Lights);

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
                JSONFormatter qj = new JSONFormatter();

                double altitude = 404;

                qj.Object().UTC("timestamp").V("event", "Status");
                qj.V("Flags", flags);
                qj.Array("Pips").V(2).V(8).V(2).Close();
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
                if (fuelres < 0)
                {
                    fuel--;
                    fuelres = 0.99;
                }

            }
        }

    }
}
