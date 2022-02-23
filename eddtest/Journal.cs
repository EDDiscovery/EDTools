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
using QuickJSON;
using System;
using System.IO;

namespace EDDTest
{
    public static partial class Journal
    {
        public static void JournalEntry( CommandArgs argsentry)
        {
            int repeatdelay = 0;

            while (true) // read optional args
            {
                string opt = (argsentry.Left > 0) ? argsentry[0] : null;

                if (opt != null)
                {
                    if (opt.Equals("-keyrepeat", StringComparison.InvariantCultureIgnoreCase))
                    {
                        repeatdelay = -1;
                        argsentry.Remove();
                    }
                    else if (opt.Equals("-repeat", StringComparison.InvariantCultureIgnoreCase) && argsentry.Left >= 1)
                    {
                        argsentry.Remove();
                        if (!int.TryParse(argsentry.Next(), out repeatdelay))
                        {
                            Console.WriteLine("Bad repeat delay\n");
                            return;
                        }
                    }
                    else
                        break;
                }
                else
                    break;
            }

            string filename = argsentry.Next();
            string cmdrname = argsentry.Next();

            if (argsentry.Left == 0)
            {
                Console.WriteLine(Help(""));
                return;
            }

            int repeatcount = 0;

            while (true)
            {
                CommandArgs args = new CommandArgs(argsentry);

                string eventtype = args.Next().ToLower();

                Random rnd = new Random();

                string lineout = null;      //quick writer
                bool checkjson = true;  // check json before writing to file
                QuickJSON.JSONFormatter qj = new QuickJSON.JSONFormatter();

                if (eventtype.Equals("fsd"))
                    lineout = FSDJump(args, repeatcount);
                else if (eventtype.Equals("blamtest"))
                    Blamtest(filename, cmdrname, args);
                else if (eventtype.Equals("fsdtravel"))
                    lineout = FSDTravel(args);
                else if (eventtype.Equals("locdocked") && args.Left >= 4)
                {
                    qj.Object().UTC("timestamp").V("event", "Location");
                    qj.V("Docked", true);
                    qj.V("StationName", args.Next());
                    qj.V("StationType", "Orbis");
                    qj.V("MarketID", 12829282);
                    qj.Object("StationFaction").V("Name", args.Next()).V("FactionState", "IceCream").Close();
                    qj.Object("SystemFaction").V("Name", args.Next()).V("FactionState", "IceCream").Close();
                    qj.V("StarSystem", args.Next());
                    qj.V("SystemAddress", 6538272662);
                    //qj.Literal("\"StationGovernment\":\"$government_Prison;\", \"StationGovernment_Localised\":\"Detention Centre\", \"StationServices\":[ \"Dock\", \"Autodock\", \"Contacts\", \"Outfitting\", \"Rearm\", " +
                    //"\"Refuel\", \"Repair\", \"Shipyard\", \"Workshop\", \"FlightController\", \"StationOperations\", \"StationMenu\" ], \"StationEconomy\":\"$economy_Prison;\", " +
                    //"\"StationEconomy_Localised\":\"Prison\", \"StationEconomies\":[ { \"Name\":\"$economy_Prison;\", \"Name_Localised\":\"Prison\", \"Proportion\":1.000000 } ], " +
                    //"\"StarPos\":[-1456.81250,-84.81250,5306.93750], \"SystemAllegiance\":\"\", \"SystemEconomy\":\"$economy_None;\", \"SystemEconomy_Localised\":\"None\", " +
                    //"\"SystemSecondEconomy\":\"$economy_None;\", \"SystemSecondEconomy_Localised\":\"None\", \"SystemGovernment\":\"$government_None;\", \"SystemGovernment_Localised\":\"None\", \"SystemSecurity\":\"$GAlAXY_MAP_INFO_state_anarchy;\", \"SystemSecurity_Localised\":\"Anarchy\", " +
                    //"\"Population\":0, \"Body\":\"Rock of Isolation\", \"BodyID\":33, \"BodyType\":\"Station\", \"Factions\":[ { \"Name\":\"Independent Detention Foundation\", \"FactionState\":\"None\", \"Government\":\"Prison\", \"Influence\":0.000000, \"Allegiance\":\"Independent\", \"Happiness\":\"$Faction_HappinessBand2;\", \"Happiness_Localised\":\"Happy\", \"MyReputation\":0.000000 }, { \"Name\":\"Pilots Federation Local Branch\", \"FactionState\":\"None\", \"Government\":\"Democracy\", \"Influence\":0.000000, \"Allegiance\":\"PilotsFederation\", \"Happiness\":\"\", \"MyReputation\":0.000000 } ]"
                    //);
                }
                else if (eventtype.Equals("docked") && args.Left >= 3)
                {
                    qj.Object().UTC("timestamp").V("event", "Docked");
                    qj.V("StationName", args.Next());
                    qj.V("StationType", "Orbis");
                    qj.V("StarSystem", args.Next());
                    qj.V("SystemAddress", 65135844912);
                    qj.V("MarketID", 12829282);
                    qj.Object("StationFaction").V("Name", args.Next()).V("FactionState", "IceCream").Close();
                    //qj.Literal("\"StationGovernment\":\"$government_Prison;\", " +
                    //    "\"StationGovernment_Localised\":\"Detention Centre\", \"StationServices\":[ \"Dock\", \"Autodock\", \"Contacts\", \"Outfitting\", \"Rearm\", \"Refuel\", \"Repair\", \"Shipyard\", " +
                    //    "\"Workshop\", \"FlightController\", \"StationOperations\", \"StationMenu\" ], \"StationEconomy\":\"$economy_Prison;\", \"StationEconomy_Localised\":\"Prison\", " +
                    //    "\"StationEconomies\":[ { \"Name\":\"$economy_Prison;\", \"Name_Localised\":\"Prison\", \"Proportion\":1.000000 } ], \"DistFromStarLS\":3919.440674 ");
                    qj.Close();
                }
                else if (eventtype.Equals("undocked"))
                    qj.Object().UTC("timestamp").V("event", "Undocked").V("StationName", "Jameson Memorial").V("StationType", "Orbis");
                else if (eventtype.Equals("liftoff"))
                    qj.Object().UTC("timestamp").V("event", "Liftoff").V("Latitude", 7.141173).V("Longitude", 95.256424);
                else if (eventtype.Equals("touchdown"))
                {
                    qj.Object().UTC("timestamp").V("event", "Touchdown").V("Latitude", 7.141173).V("Longitude", 95.256424).V("PlayerControlled", true);
                }
                else if (eventtype.Equals("missionaccepted") && args.Left == 3)
                {
                    string f = args.Next();
                    string vf = args.Next();
                    int id = args.Int();

                    qj.Object().UTC("timestamp").V("event", "MissionAccepted").V("Faction", f)
                            .V("Name", "Mission_Assassinate_Legal_Corporate").V("TargetType", "$MissionUtil_FactionTag_PirateLord;")
                            .V("TargetType_Localised", "Pirate lord").V("TargetFaction", vf)
                            .V("DestinationSystem", "Quapa").V("DestinationStation", "Grabe Dock").V("Target", "mamsey")
                            .V("Expiry", DateTime.UtcNow.AddDays(1))
                            .V("Influence", "Med")
                            .V("Reputation", "Med").V("MissionID", id);
                }
                else if (eventtype.Equals("missioncompleted") && args.Left == 3)
                {
                    string f = args.Next();
                    string vf = args.Next();
                    int id = args.Int();

                    qj.Object().UTC("timestamp").V("event", "MissionCompleted").V("Faction", f)
                        .V("Name", "Mission_Assassinate_Legal_Corporate").V("TargetType", "$MissionUtil_FactionTag_PirateLord;")
                        .V("TargetType_Localised", "Pirate lord").V("TargetFaction", vf)
                        .V("MissionID", id).V("Reward", "82272")
                        .Array("CommodityReward").Object().V("Name", "CoolingHoses").V("Count", 4);
                }
                else if (eventtype.Equals("missionredirected") && args.Left == 3)
                {
                    string sysn = args.Next();
                    string stationn = args.Next();
                    int id = args.Int();
                    qj.Object().UTC("timestamp").V("event", "MissionRedirected").V("MissionID", id).V("MissionName", "Mission_Assassinate_Legal_Corporate")
                        .V("NewDestinationStation", stationn).V("OldDestinationStation", "Cuffey Orbital")
                        .V("NewDestinationSystem", sysn).V("OldDestinationSystem", "Vequess");
                }
                else if (eventtype.Equals("missions") && args.Left == 1)
                {
                    int id = args.Int();

                    qj.Object().UTC("timestamp").V("event", "Missions");

                    qj.Array("Active").Object();
                    FMission(qj, id, "Mission_Assassinate_Legal_Corporate", false, 20000);
                    qj.Close(2);

                    qj.Array("Failed").Object();
                    FMission(qj, id + 1000, "Mission_Assassinate_Legal_Corporate", false, 20000);
                    qj.Close(2);

                    qj.Array("Completed").Object();
                    FMission(qj, id + 2000, "Mission_Assassinate_Legal_Corporate", false, 20000);
                    qj.Close(2);
                }
                else if (eventtype.Equals("marketbuy") && args.Left == 3)
                {
                    string name = args.Next();
                    int count = args.Int();
                    int price = args.Int();

                    qj.Object().UTC("timestamp").V("event", "MarketBuy").V("MarketID", 29029292)
                                .V("Type", name).V("Type_Localised", name + "loc").V("Count", count).V("BuyPrice", price).V("TotalCost", price * count);
                }
                else if (eventtype.Equals("marketsell") && args.Left == 3)
                {
                    string name = args.Next();
                    int count = args.Int();
                    int price = args.Int();

                    qj.Object().UTC("timestamp").V("event", "MarketSell").V("MarketID", 29029292)
                                .V("Type", name).V("Type_Localised", name + "loc").V("Count", count).V("SellPrice", price).V("TotalSale", price * count)
                                .V("IllegalGoods", false).V("StolenGoods", false).V("BlackMarket", false);
                }
                else if (eventtype.Equals("bounty"))
                {
                    string f = args.Next();
                    int rw = args.Int();

                    qj.Object().UTC("timestamp").V("event", "Bounty").V("VictimFaction", f).V("VictimFaction_Localised", f + "_Loc")
                        .V("TotalReward", rw);
                }
                else if (eventtype.Equals("factionkillbond"))
                {
                    string f = args.Next();
                    string vf = args.Next();
                    int rw = args.Int();

                    qj.Object().UTC("timestamp").V("event", "FactionKillBond").V("VictimFaction", vf).V("VictimFaction_Localised", vf + "_Loc")
                        .V("AwardingFaction", f).V("AwardingFaction_Localised", f + "_Loc")
                        .V("Reward", rw);
                }
                else if (eventtype.Equals("capshipbond"))
                {
                    string f = args.Next();
                    string vf = args.Next();
                    int rw = args.Int();

                    qj.Object().UTC("timestamp").V("event", "CapShipBond").V("VictimFaction", vf).V("VictimFaction_Localised", vf + "_Loc")
                        .V("AwardingFaction", f).V("AwardingFaction_Localised", f + "_Loc")
                        .V("Reward", rw);
                }
                else if (eventtype.Equals("resurrect"))
                {
                    int ct = args.Int();

                    qj.Object().UTC("timestamp").V("event", "Resurrect").V("Option", "Help me").V("Cost", ct).V("Bankrupt", false);
                }
                else if (eventtype.Equals("died"))
                {
                    qj.Object().UTC("timestamp").V("event", "Died").V("KillerName", "Evil Jim McDuff").V("KillerName_Localised", "Evil Jim McDuff The great").V("KillerShip", "X-Wing").V("KillerRank", "Invincible");
                }
                else if (eventtype.Equals("miningrefined"))
                    qj.Object().UTC("timestamp").V("event", "MiningRefined").V("Type", "Gold");
                else if (eventtype.Equals("engineercraft"))
                    qj.Object().UTC("timestamp").V("event", "EngineerCraft").V("Engineer", "Robert").V("Blueprint", "FSD_LongRange")
                        .V("Level", "5").Object("Ingredients").V("magneticemittercoil", 1).V("arsenic", 1).V("chemicalmanipulators", 1).V("dataminedwake", 1);
                else if (eventtype.Equals("navbeaconscan"))
                    qj.Object().UTC("timestamp").V("event", "NavBeaconScan").V("NumBodies", "3");
                else if (eventtype.Equals("scanplanet") && args.Left >= 1)
                {
                    string name = args.Next() + (repeatcount > 0 ? "x" + repeatcount : "");
                    //qj.Object().UTC("timestamp").V("event", "Scan").V("BodyName", name).Literal("\"DistanceFromArrivalLS\":639.245483, \"TidalLock\":true, \"TerraformState\":\"\", \"PlanetClass\":\"Metal rich body\", \"Atmosphere\":\"\", \"AtmosphereType\":\"None\", \"Volcanism\":\"rocky magma volcanism\", \"MassEM\":0.010663, \"Radius\":1163226.500000, \"SurfaceGravity\":3.140944, \"SurfaceTemperature\":1068.794067, \"SurfacePressure\":0.000000, \"Landable\":true, \"Materials\":[ { \"Name\":\"iron\", \"Percent\":36.824127 }, { \"Name\":\"nickel\", \"Percent\":27.852226 }, { \"Name\":\"chromium\", \"Percent\":16.561033 }, { \"Name\":\"zinc\", \"Percent\":10.007420 }, { \"Name\":\"selenium\", \"Percent\":2.584032 }, { \"Name\":\"tin\", \"Percent\":2.449526 }, { \"Name\":\"molybdenum\", \"Percent\":2.404594 }, { \"Name\":\"technetium\", \"Percent\":1.317050 } ], \"SemiMajorAxis\":1532780800.000000, \"Eccentricity\":0.000842, \"OrbitalInclination\":-1.609496, \"Periapsis\":179.381393, \"OrbitalPeriod\":162753.062500, \"RotationPeriod\":162754.531250, \"AxialTilt\":0.033219");
                }
                else if (eventtype.Equals("scanstar"))
                {
                    string name = args.Next() + (repeatcount > 0 ? "x" + repeatcount : "");
                    //qj.Object().UTC("timestamp").V("event", "Scan").V("BodyName", name).Literal("\"DistanceFromArrivalLS\":0.000000, \"StarType\":\"B\", \"StellarMass\":8.597656, \"Radius\":2854249728.000000, \"AbsoluteMagnitude\":1.023468, \"Age_MY\":182, \"SurfaceTemperature\":23810.000000, \"Luminosity\":\"IV\", \"SemiMajorAxis\":12404761034752.000000, \"Eccentricity\":0.160601, \"OrbitalInclination\":18.126791, \"Periapsis\":49.512009, \"OrbitalPeriod\":54231617536.000000, \"RotationPeriod\":110414.359375, \"AxialTilt\":0.000000");
                }
                else if (eventtype.Equals("scanearth"))
                {
                    string name = args.Next() + (repeatcount > 0 ? "x" + repeatcount : "");
                    //qj.Object().UTC("timestamp").V("event", "Scan").V("BodyName", name).Literal("\"DistanceFromArrivalLS\":901.789856, \"TidalLock\":false, \"TerraformState\":\"Terraformed\", \"PlanetClass\":\"Earthlike body\", \"Atmosphere\":\"\", \"AtmosphereType\":\"EarthLike\", \"AtmosphereComposition\":[ { \"Name\":\"Nitrogen\", \"Percent\":92.386833 }, { \"Name\":\"Oxygen\", \"Percent\":7.265749 }, { \"Name\":\"Water\", \"Percent\":0.312345 } ], \"Volcanism\":\"\", \"MassEM\":0.840000, \"Radius\":5821451.000000, \"SurfaceGravity\":9.879300, \"SurfaceTemperature\":316.457062, \"SurfacePressure\":209183.453125, \"Landable\":false, \"SemiMajorAxis\":264788426752.000000, \"Eccentricity\":0.021031, \"OrbitalInclination\":13.604733, \"Periapsis\":73.138206, \"OrbitalPeriod\":62498732.000000, \"RotationPeriod\":58967.023438, \"AxialTilt\":-0.175809");
                }
                else if (eventtype.Equals("ring"))
                {
                    //qj.Object().UTC("timestamp").Literal("\"event\": \"Scan\",  \"ScanType\": \"AutoScan\",  \"BodyName\": \"Merope 9 Ring\",  \"DistanceFromArrivalLS\": 1883.233643,  \"SemiMajorAxis\": 70415976.0,  \"Eccentricity\": 0.0,  \"OrbitalInclination\": 0.0,  \"Periapsis\": 0.0,  \"OrbitalPeriod\": 100994.445313}");
                }
                else if (eventtype.Equals("afmurepairs"))
                    qj.Object().UTC("timestamp").V("event", "AfmuRepairs").V("Module", "$modularcargobaydoor_name;").V("Module_Localised", "Cargo Hatch").V("FullyRepaired", true)
                            .V("Health", 1.000000);

                else if (eventtype.Equals("sellshiponrebuy"))
                    qj.Object().UTC("timestamp").V("event", "SellShipOnRebuy").V("ShipType", "Dolphin").V("System", "Shinrarta Dezhra")
                        .V("SellShipId", 4).V("ShipPrice", 4110183);

                else if (eventtype.Equals("searchandrescue") && args.Left == 2)
                {
                    string name = args.Next();
                    int count = args.Int();
                    qj.Object().UTC("timestamp").V("event", "SearchAndRescue").V("MarketID", 29029292)
                                .V("Name", name).V("Name_Localised", name + "loc").V("Count", count).V("Reward", 10234);

                }
                else if (eventtype.Equals("repairdrone"))
                {
                    qj.Object().UTC("timestamp").V("event", "RepairDrone");
                    qj.V("HullRepaired", repeatcount * 0.1);
                    qj.V("CockpitRepaired", 0.1);
                    qj.V("CorrosionRepaired", 0.2);
                }
                else if (eventtype.Equals("communitygoal"))
                {
                    //qj.Object().UTC("timestamp").V("event", "CommunityGoal").Literal("\"CurrentGoals\":[ { \"CGID\":726, \"Title\":\"Alliance Research Initiative - Trade\", \"SystemName\":\"Kaushpoos\", \"MarketName\":\"Neville Horizons\", \"Expiry\":\"2017-08-17T14:58:14Z\", \"IsComplete\":false, \"CurrentTotal\":10062, \"PlayerContribution\":562, \"NumContributors\":101, \"TopRankSize\":10, \"PlayerInTopRank\":false, \"TierReached\":\"Tier 1\", \"PlayerPercentileBand\":50, \"Bonus\":200000 } ] }");
                }
                else if (eventtype.Equals("musicnormal"))
                    qj.Object().UTC("timestamp").V("event", "Music").V("MusicTrack", "NoTrack");
                else if (eventtype.Equals("musicsysmap"))
                    qj.Object().UTC("timestamp").V("event", "Music").V("MusicTrack", "SystemMap");
                else if (eventtype.Equals("musicgalmap"))
                    qj.Object().UTC("timestamp").V("event", "Music").V("MusicTrack", "GalaxyMap");
                else if (eventtype.Equals("friends") && args.Left >= 1)
                    qj.Object().UTC("timestamp").V("event", "Friends").V("Status", "Online").V("Name", args.Next());
                else if (eventtype.Equals("fuelscoop") && args.Left >= 2)
                {
                    string scoop = args.Next();
                    string total = args.Next();
                    qj.Object().UTC("timestamp").V("event", "FuelScoop").V("Scooped", scoop).V("Total", total);
                }
                else if (eventtype.Equals("jetconeboost"))
                {
                    qj.Object().UTC("timestamp").V("event", "JetConeBoost").V("BoostValue", 1.5);
                }
                else if (eventtype.Equals("fighterdestroyed"))
                    qj.Object().UTC("timestamp").V("event", "FighterDestroyed");
                else if (eventtype.Equals("fighterrebuilt"))
                    qj.Object().UTC("timestamp").V("event", "FighterRebuilt").V("Loadout", "Fred");
                else if (eventtype.Equals("npccrewpaidwage"))
                    qj.Object().UTC("timestamp").V("event", "NpcCrewPaidWage").V("NpcCrewId", 1921).V("Amount", 10292);
                else if (eventtype.Equals("npccrewrank"))
                    qj.Object().UTC("timestamp").V("event", "NpcCrewRank").V("NpcCrewId", 1921).V("RankCombat", 4);
                else if (eventtype.Equals("launchdrone"))
                    qj.Object().UTC("timestamp").V("event", "LaunchDrone").V("Type", "FuelTransfer");
                else if (eventtype.Equals("market"))
                    lineout = Market(Path.GetDirectoryName(filename), args.Next());
                else if (eventtype.Equals("moduleinfo"))
                    lineout = ModuleInfo(Path.GetDirectoryName(filename), args.Next());
                else if (eventtype.Equals("outfitting"))
                    lineout = Outfitting(Path.GetDirectoryName(filename), args.Next());
                else if (eventtype.Equals("shipyard"))
                    lineout = Shipyard(Path.GetDirectoryName(filename), args.Next());
                else if (eventtype.Equals("powerplay"))
                    qj.Object().UTC("timestamp").V("event", "PowerPlay").V("Power", "Fred").V("Rank", 10).V("Merits", 10).V("Votes", 2).V("TimePledged", 433024);
                else if (eventtype.Equals("underattack"))
                    qj.Object().UTC("timestamp").V("event", "UnderAttack").V("Target", "Fighter");
                else if (eventtype.Equals("promotion") && args.Left == 2)
                    qj.Object().UTC("timestamp").V("event", "Promotion").V(args.Next(), args.Int());
                else if (eventtype.Equals("cargodepot"))
                    lineout = CargoDepot(args);
                else if (eventtype.Equals("fsssignaldiscovered") && args.Left >= 1)
                {
                    string name = args.Next();

                    qj.Object().UTC("timestamp").V("event", "FSSSignalDiscovered");
                    qj.V("SignalName", name);

                    if (args.Left >= 2)
                    {
                        string state = args.Next();
                        qj.V("SpawingState", "$" + state);
                        qj.V("SpawingState_Localised", state);
                        string faction = args.Next();
                        qj.V("SpawingFaction", "$" + faction);
                        qj.V("SpawingFaction_Localised", faction);
                        qj.V("TimeRemaining", 60);
                        qj.V("USSThreat", 5);
                        qj.V("USSType", "Jim");
                        qj.V("USSType_Localised", "JimLoc");
                        qj.V("IsStation", false);
                    }

                    qj.Close();
                }
                else if (eventtype.Equals("modulebuyandstore") && args.Left >= 2)
                {
                    string name = args.Next();
                    int cost = args.Int();

                    qj.Object().UTC("timestamp").V("event", "ModuleBuyAndStore");
                    qj.V("BuyItem", name);
                    qj.V("BuyItem_Localised", name + "Loc");
                    qj.V("MarketID", 292929222);
                    qj.V("BuyPrice", 4144);
                    qj.V("Ship", "ferdelance");
                    qj.V("ShipID", 35);
                    qj.Close();
                }
                else if (eventtype.Equals("codexentry") && args.Left >= 4)
                {
                    string name = args.Next();
                    string subcat = args.Next();
                    string cat = args.Next();
                    string system = args.Next();

                    qj.Object().UTC("timestamp").V("event", "CodexEntry");
                    qj.V("Name", "$" + name);
                    qj.V("Name_Localised", name);
                    qj.V("SubCategory", "$" + subcat);
                    qj.V("SubCategory_Localised", subcat);
                    qj.V("Category", "$" + cat);
                    qj.V("Category_Localised", cat);
                    qj.V("System", system);
                    qj.V("Region", "Region 18");
                    qj.V("IsNewEntry", true);
                    qj.V("NewTraitsDiscovered", true);
                    qj.Array().V("T1").V("T2").V("T3").Close();
                    qj.Close();
                }
                else if (eventtype.Equals("saascancomplete") && args.Left >= 1)
                {
                    string name = args.Next();

                    qj.Object().UTC("timestamp").V("event", "SAAScanComplete");
                    qj.V("BodyName", name);
                    qj.V("BodyID", 10);
                    qj.Array("Discoverers").V("Fred").V("Jim").V("Sheila").Close();
                    qj.Array("Mappers").V("george").V("henry").Close();
                    qj.V("ProbesUsed", 10);
                    qj.V("EfficiencyTarget", 12);
                    qj.Close();
                }
                else if (eventtype.Equals("asteroidcracked") && args.Left >= 1)
                {
                    string name = args.Next();

                    qj.Object().UTC("timestamp").V("event", "AsteroidCracked");
                    qj.V("Body", name);
                    qj.Close();
                }

                else if (eventtype.Equals("multisellexplorationdata"))
                {
                    qj.Object().UTC("timestamp").V("event", "MultiSellExplorationData");
                    qj.Array("Discovered");
                    for (int i = 0; i < 5; i++)
                    {
                        qj.Object();
                        qj.V("SystemName", "Sys" + i);
                        qj.V("NumBodies", i * 2 + 1);
                        qj.Close();
                    }

                    qj.Close();
                    qj.V("BaseValue", 100);
                    qj.V("Bonus", 200);
                    qj.V("TotalEarnings", 300);
                    qj.Close();
                }
                else if (eventtype.Equals("startjump") && args.Left >= 1)
                {
                    string name = args.Next();

                    qj.Object().UTC("timestamp").V("event", "StartJump");
                    qj.V("JumpType", "Hyperspace");
                    qj.V("StarSystem", name);
                    qj.V("StarClass", "A");
                    qj.V("SystemAddress", 10);
                    qj.Close();
                }
                else if (eventtype.Equals("appliedtosquadron") && args.Left >= 1)
                    lineout = Squadron("AppliedToSquadron", args.Next());

                else if (eventtype.Equals("disbandedsquadron") && args.Left >= 1)
                    lineout = Squadron("DisbandedSquadron", args.Next());

                else if (eventtype.Equals("invitedtosquadron") && args.Left >= 1)
                    lineout = Squadron("InvitedToSquadron", args.Next());

                else if (eventtype.Equals("joinedsquadron") && args.Left >= 1)
                    lineout = Squadron("JoinedSquadron", args.Next());

                else if (eventtype.Equals("leftsquadron") && args.Left >= 1)
                    lineout = Squadron("LeftSquadron", args.Next());

                else if (eventtype.Equals("kickedfromsquadron") && args.Left >= 1)
                    lineout = Squadron("KickedFromSquadron", args.Next());

                else if (eventtype.Equals("sharedbookmarktosquadron") && args.Left >= 1)
                    lineout = Squadron("SharedBookmarkToSquadron", args.Next());

                else if (eventtype.Equals("squadroncreated") && args.Left >= 1)
                    lineout = Squadron("SquadronCreated", args.Next());

                else if (eventtype.Equals("wonatrophyforsquadron") && args.Left >= 1)
                    lineout = Squadron("WonATrophyForSquadron", args.Next());

                else if (eventtype.Equals("squadrondemotion") && args.Left >= 3)
                    lineout = Squadron("SquadronDemotion", args.Next(), args.Next(), args.Next());

                else if (eventtype.Equals("squadronpromotion") && args.Left >= 3)
                    lineout = Squadron("SquadronPromotion", args.Next(), args.Next(), args.Next());

                else if (eventtype.Equals("squadronstartup") && args.Left == 2)
                    lineout = Squadron("SquadronStartup", args.Next(), args.Next());

                else if (eventtype.Equals("fsdtarget") && args.Left >= 1)
                {
                    qj.Object().UTC("timestamp").V("event", "FSDTarget").V("Name", args.Next()).V("SystemAddress", 20);
                }
                else if (eventtype.Equals("fssallbodiesfound") && args.Left >= 1)
                {
                    qj.Object().UTC("timestamp").V("event", "FSSAllBodiesFound").V("SystemName", args.Next()).V("SystemAddress", 20);
                }
                else if (eventtype.Equals("fssdiscoveryscan"))
                {
                    qj.Object().UTC("timestamp").V("event", "FSSDiscoveryScan").V("Progress", 0.23).V("BodyCount", 20).V("NonBodyCount", 30);
                }
                else if (eventtype.Equals("commitcrime"))
                {
                    string f = args.Next();
                    int id = args.Int();
                    qj.Object().UTC("timestamp").V("event", "CommitCrime").V("CrimeType", "collidedAtSpeedInNoFireZone").V("Faction", f).V("Fine", id);
                }
                else if (eventtype.Equals("crimevictim"))
                {
                    string f = args.Next();
                    int bounty = args.Int();
                    qj.Object().UTC("timestamp").V("event", "CrimeVictim").V("CrimeType", "assault").V("Offender", f).V("Bounty", bounty);
                }
                else if (eventtype.Equals("launchsrv"))
                {
                    qj.Object().UTC("timestamp").V("event", "LaunchSRV").V("Loadout", "Normal");
                }
                else if (eventtype.Equals("docksrv"))
                {
                    qj.Object().UTC("timestamp").V("event", "DockSRV");
                }
                else if (eventtype.Equals("srvdestroyed"))
                {
                    qj.Object().UTC("timestamp").V("event", "SRVDestroyed");
                }
                else if (eventtype.Equals("receivetext") && args.Left >= 3)
                {
                    string from = args.Next();
                    string channel = args.Next();
                    string msg = args.Next();
                    qj.Object().UTC("timestamp").V("event", "ReceiveText").V("From", from).V("Message", msg).V("Channel", channel);
                }
                else if (eventtype.Equals("sendtext") && args.Left >= 2)
                {
                    string to = args.Next();
                    string msg = args.Next();
                    qj.Object().UTC("timestamp").V("event", "SendText").V("To", to).V("Message", msg);
                }
                else if (eventtype.Equals("prospectedasteroid"))
                {
                    qj.Object().UTC("timestamp").V("event", "ProspectedAsteroid")
                            .V("MotherlodeMaterial", "Serendibite")
                            .V("Content", "$AsteroidMaterialContent_High;")
                            .V("Content_Localised", "Material Content:High")
                            .V("Remaining", 100.000000);
                    string[] mats = new string[] { "Coltan", "Lepidolite", "Uraninite" };

                    qj.Array("Materials");
                    double p = 2;
                    foreach (var m in mats)
                    {
                        qj.Object().V("Name", m).V("Proportion", p).Close();
                        p *= 2.2;
                    }

                    qj.Close(99);
                }
                else if (eventtype.Equals("saasignalsfound") && args.Left >= 1)
                {
                    qj.Object().UTC("timestamp").V("event", "SAASignalsFound")
                            .V("BodyName", args.Next())
                            .V("SystemAddress", 101)
                            .V("BodyID", 11)
                            .Array("Signals")
                            .Object().V("Type", "LowTemperatureDiamond").V("Type_Localised", "Low Temperature Diamonds").V("Count", 11).Close()
                            .Object().V("Type", "FredThingies").V("Type_Localised", "Fred stuff").V("Count", 2).Close()
                            .Object().V("Type", "Widgets").V("Type_Localised", "Widgities stuff").V("Count", 20).Close()
                            .Close();
                }
                else if (eventtype.Equals("reservoirreplenished") && args.Left >= 2)
                {
                    double main = args.Double();
                    double res = args.Double();

                    qj.Object().UTC("timestamp").V("event", "ReservoirReplenished")
                            .V("FuelMain", main)
                            .V("FuelReservoir", res);
                }
                else if (eventtype.Equals("carrierdepositfuel") && args.Left >= 1)
                {
                    int amount = args.Int();

                    qj.Object().UTC("timestamp").V("event", "CarrierDepositFuel")
                            .V("CarrierID", 1234)
                            .V("Amount", amount)
                            .V("Total", amount + 5000);
                }
                else if (eventtype.Equals("screenshot") && args.Left >= 2)
                {
                    string infile = args.Next();
                    string outfolder = args.Next();
                    bool nojr = args.Left >= 1 && args.Next().Equals("NOJR");

                    if (File.Exists(infile))
                    {
                        int n = 100;
                        string outfile;
                        do
                        {
                            outfile = Path.Combine(outfolder, string.Format("Screenshot_{0}.bmp", n++));
                        } while (File.Exists(outfile));

                        File.Copy(infile, outfile);

                        Console.WriteLine("{0} -> {1}", infile, outfile);

                        if (!nojr)
                        {
                            qj.Object().UTC("timestamp").V("event", "Screenshot")
                                .V("Filename", "\\\\ED_Pictures\\\\" + Path.GetFileName(outfile))
                                .V("Width", 1920)
                                .V("Height", 1200)
                                .V("System", "Fredsys")
                                .V("Body", "Jimbody");
                        }
                    }
                    else
                        Console.WriteLine("No such file {0}", infile);


                }
                else if (eventtype.Equals("carrierfinance") && args.Left >= 1)
                {
                    int amount = args.Int();

                    qj.Object().UTC("timestamp").V("event", "CarrierFinance")
                            .V("CarrierID", 1234)
                            .V("TaxRate", 23)
                            .V("CarrierBalance", amount)
                            .V("ReserveBalance", amount + 2000)
                            .V("AvailableBalance", amount + 4000)
                            .V("ReservePercent", 42);
                }
                else if (eventtype.Equals("carriernamechanged"))
                {
                    qj.Object().UTC("timestamp").V("event", "CarrierNameChanged")
                            .V("CarrierID", 1234)
                            .V("Callsign", "sixi-20")
                            .V("Name", "Big Bertha");
                }
                else if (eventtype.Equals("cargotransfer") && args.Left >= 3)
                {
                    string type = args.Next();
                    int count = args.Int();
                    string dir = args.Next();
                    qj.Object().UTC("timestamp").V("event", "CargoTransfer")
                            .Array("Transfers")
                            .Object()
                            .V("Type", type)
                            .V("Type_Localised", type + "_loc")
                            .V("Count", count)
                            .V("Direction", dir)
                            .Close()
                            .Close();

                }
                else if (eventtype.Equals("scanorganic") && args.Left >= 4)
                {
                    string st = args.Next();
                    if (st == "Log" || st == "Sample" || st == "Analyse")
                    {
                        string genus = args.Next();
                        string species = args.Next();
                        int body = args.Int();

                        qj.Object().UTC("timestamp").V("event", "ScanOrganic");
                        qj.V("ScanType", st);
                        qj.V("Genus", genus);
                        qj.V("Genus_Localised", genus.Replace("$Codex_Ent_", "").Replace("_Genus_Name;", ""));
                        qj.V("Species", species);
                        qj.V("Species_Localised", species.Replace("$Codex_Ent_", "").Replace("_Name;", ""));
                        qj.V("SystemAddress", 1416164883666);
                        qj.V("Body", body);
                        qj.Close();
                    }
                }
                else if (eventtype.Equals("event") && args.Left >= 1)   // give it raw json from "event":"wwkwk" onwards, without } at end
                {
                    string file = args.Next();

                    var textlines = File.ReadLines(file);

                    lineout = "";
                    checkjson = false;

                    foreach (string line in textlines)
                    {
                        if (line.Length > 0)
                        {
                            JObject jo = JObject.Parse(line);
                            if (jo != null)
                            {
                                jo["timestamp"] = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'");
                                if (lineout.HasChars())
                                    lineout += Environment.NewLine;
                                lineout += jo.ToString();
                            }
                            else
                            {
                                Console.WriteLine("Bad journal line " + line);
                                break;
                            }
                        }
                    }
                }
                else if (eventtype.Equals("shiptargeted"))
                {
                    qj.Object().UTC("timestamp").V("event", "ShipTargeted").V("TargetLocked", args.Left > 0);

                    if (args.Left > 0)
                    {
                        int stage = 0;
                        string ship = args.Next();
                        qj.V("Ship", ship).V("Ship_Localised", "L:" + ship);

                        if (args.Left >= 2)
                        {
                            stage = 1;
                            string pilot = args.Next();
                            string rank = args.Next();
                            qj.V("PilotName", pilot).V("PilotName_Localised", "L:" + pilot).V("PilotRank", rank);

                            if (args.Left >= 1)
                            {
                                stage = 2;
                                double health = args.Double();
                                qj.V("ShieldHealth", health).V("HullHealth", health / 2);

                                if (args.Left >= 1)
                                {
                                    stage = 3;
                                    string faction = args.Next();
                                    qj.V("Faction", faction).V("LegalStatus", "Clean");
                                }
                            }
                        }

                        qj.V("ScanStage", stage);
                    }
                }
                else if (eventtype.Equals("continued"))
                {
                    QuickJSON.JSONFormatter qjc = new JSONFormatter();
                    qjc.Object().UTC("timestamp").V("event", "Continued").V("Part", 2);
                    WriteToLog(filename, cmdrname, qjc.Get(), checkjson);
                    filename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + ".part2" + Path.GetExtension(filename));
                    Console.WriteLine("Continued.. change to filename " + filename);
                    WriteToLog(filename, cmdrname, null, false, 2);
                }
                else if (eventtype.Equals("buysuit") && args.Left >= 1)
                {
                    qj.Object().UTC("timestamp").V("event", "BuySuit")
                            .V("Name", "TacticalSuit_Class1")
                            .V("Name_Localised", "Tactical Suit")
                            .V("SuitID", args.Int())
                            .V("CommanderId", 23)
                            .V("Price", 150000);
                }

                else
                {
                    Console.WriteLine("** Unrecognised journal event type or not enough parameters for entry" + Environment.NewLine + Help(eventtype));
                    break;
                }

                if (lineout == null && qj.Get().HasChars())
                    lineout = qj.Get();

                if (lineout != null)
                    WriteToLog(filename, cmdrname, lineout, checkjson);
                else
                    break;

                if (repeatdelay == -1)
                {
                    ConsoleKeyInfo k = Console.ReadKey();

                    if (k.Key == ConsoleKey.Escape)
                    {
                        break;
                    }
                }
                else if (repeatdelay > 0)
                {
                    System.Threading.Thread.Sleep(repeatdelay);

                    if (Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.Escape)
                        break;
                }
                else
                    break;

                repeatcount++;
            }
        }


        #region Help!

        public static string Help(string eventtype)
        {
            string s = "Usage:    Journal [-keyrepeat]|[-repeat ms] pathtologfile CMDRname eventname..\n";

            s+=he("File", "event filename - read filename with json and store in file", eventtype);
            s+=he("Travel", "FSD name x y z (x y z is position as double)", eventtype);
            s+=he("", "FSDTravel name x y z destx desty destz percentint ", eventtype);
            s+=he("", "Locdocked station stationfaction systemfaction systemname", eventtype);
            s+=he("", "Docked station starsystem faction", eventtype);
            s+=he("", "Undocked, Touchdown, Liftoff", eventtype);
            s+=he("", "FuelScoop amount total", eventtype);
            s+=he("", "JetConeBoost", eventtype);
            s+=he("Missions", "MissionAccepted/MissionCompleted faction victimfaction id", eventtype);
            s+=he("", "MissionRedirected newsystem newstation id", eventtype);
            s+=he("", "Missions activeid", eventtype);
            s+=he("C/B", "Bounty faction reward", eventtype);
            s+=he("", "CommitCrime faction amount", eventtype);
            s+=he("", "CrimeVictim offender amount", eventtype);
            s+=he("", "FactionKillBond faction victimfaction reward", eventtype);
            s+=he("", "CapShipBond faction victimfaction reward", eventtype);
            s+=he("", "Interdiction name success isplayer combatrank faction power", eventtype);
            s+=he("", "TargetShipLost", eventtype);
            s+=he("", "ShipTargeted [ship [pilot rank [health [faction]]]]", eventtype);
            s += he("Commds", "marketbuy fdname count price", eventtype);
            s += he("", "marketsell fdname count price", eventtype);
            s+=he("", "ScanPlanet, ScanStar, ScanEarth name", eventtype);
            s+=he("", "NavBeaconScan", eventtype);
            s+=he("", "Ring", eventtype);
            s+=he("Ships", "SellShipOnRebuy", eventtype);
            s += he("SRV", "LaunchSRV, DockSRV, SRVDestroyed", eventtype);
            s += he("Modules", "ModuleBuyAndStore fdname price", eventtype);
            s += he("Others", "SearchAndRescue fdname count", eventtype);
            s+=he("", "MiningRefined", eventtype);
            s+=he("", "Receivetext from channel msg", eventtype);
            s+=he("", "SentText to/channel msg", eventtype);
            s+=he("", "RepairDrone, CommunityGoal", eventtype);
            s+=he("", "MusicNormal, MusicGalMap, MusicSysMap", eventtype);
            s+=he("", "Friends name", eventtype);
            s+=he("", "Died", eventtype);
            s+=he("", "Resurrect cost", eventtype);
            s+=he("", "PowerPlay, UnderAttack", eventtype);
            s+=he("", "CargoDepot missionid updatetype(Collect,Deliver,WingUpdate) count total", eventtype);
            s+=he("", "FighterDestroyed, FigherRebuilt, NpcCrewRank, NpcCrewPaidWage, LaunchDrone", eventtype);
            s+=he("", "Market (use NOFILE after to say don't write the canned file, or 2 to write the alternate)", eventtype);
            s+=he("", "ModuleInfo, Outfitting, Shipyard (use NOFILE after to say don't write the file)", eventtype);
            s+=he("", "Promotion Combat/Trade/Explore/CQC/Federation/Empire Ranknumber", eventtype);
            s+=he("", "CodexEntry name subcat cat system", eventtype);
            s+=he("", "fsssignaldiscovered name", eventtype);
            s+=he("", "saascancomplete name", eventtype);
            s+=he("", "saasignalsfound bodyname", eventtype);
            s+=he("", "asteroidcracked name", eventtype);
            s+=he("", "multisellexplorationdata", eventtype);
            s += he("", "propectedasteroid", eventtype);
            s += he("", "scanorganic scantype (Log/Sample/Analyse) genus species bodyid" , eventtype);
            s += he("", "reservoirreplenished main reserve", eventtype);
            s+=he("", "*Squadrons* name", eventtype);
            s+=he("", "screenshot inputfile outputfolder [NOJR]", eventtype);
            return s;
        }

        static string lastsection = "";

        static string he(string section, string text, string eventtype)
        {
            if (section.HasChars() && section != lastsection)
            {
                lastsection = section;
            }

            if (eventtype.HasChars())
            {
                if (text.StartsWith(eventtype,StringComparison.InvariantCultureIgnoreCase) || text.Contains(", " + eventtype,StringComparison.InvariantCultureIgnoreCase))
                    return lastsection.PadRight(10) + text + Environment.NewLine;
                else
                    return "";
            }
            else
                return section.PadRight(10) + text + Environment.NewLine;
        }

        #endregion



   
        static string FSDJump(CommandArgs args, int repeatcount)
        {
            string starnameroot = null;
            double x = double.NaN, y = 0, z = 0;

            if (args.Left < 1)
            {
                Console.WriteLine("** More parameters: file cmdrname fsd x y z  or just fsd");
                return null;
            }
            else
            {
                starnameroot = args.Next();

                if ( args.Left >= 3 )
                {
                    x = args.Double();      // zero if wrong
                    y = args.Double();
                    z = args.Double();
                }
            }

            z = z + 100 * repeatcount;

            string starname = starnameroot + ((z > 0) ? "_" + z.ToStringInvariant("0") : "");

            return FSDJump(starname, x, y, z);
        }

        static string FSDJump(string starname, double x, double y, double z)
        { 
            JSONFormatter qj = new JSONFormatter();
            qj.Object().UTC("timestamp").V("event", "FSDJump");
            qj.V("StarSystem", starname);
            if ( !double.IsNaN(x))
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
            return qj.ToString();
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

            return FSDJump(starname, x, y, z);
        }

        static string Market(string mpath, string opt)
        {
            JSONFormatter js = new JSONFormatter();
            js.UTC("timestamp").V("event", "Market").V("MarketID", 12345678).V("StationName", "Columbus").V("StarSystem", "Sol");
            string mline = js.ToString();
            string market1 = mline + ", " + EDDTest.Properties.Resources.Market;
            string market2 = mline + ", " + EDDTest.Properties.Resources.Market2;

            bool writem1 = opt == null || opt.Equals("NOFILE", StringComparison.InvariantCultureIgnoreCase) == false;
            bool writem2 = opt != null && opt.Equals("2");

            if (writem1)
                File.WriteAllText(Path.Combine(mpath, "Market.json"), market1);
            if (writem2)
                File.WriteAllText(Path.Combine(mpath, "Market.json"), market2);

            return mline ;
        }

        static string Outfitting(string mpath, string opt)
        {
            //{ "timestamp":"2018-01-28T23:45:39Z", "event":"Outfitting", "MarketID":3229009408, "StationName":"Mourelle Gateway", "StarSystem":"G 65-9",

            JSONFormatter js = new JSONFormatter();
            js.UTC("timestamp").V("event", "Outfitting").V("MarketID", 12345678).V("StationName", "Columbus").V("StarSystem", "Sol");
            string jline = js.ToString();
            string fline = jline + ", " + EDDTest.Properties.Resources.Outfitting;

            if (opt == null || opt.Equals("NOFILE", StringComparison.InvariantCultureIgnoreCase) == false)
                File.WriteAllText(Path.Combine(mpath, "Outfitting.json"), fline);

            return jline ;
        }

        static string CargoDepot(CommandArgs args)
        {
            try
            {
                int missid = int.Parse(args.Next());
                string type = args.Next();
                int countcol = int.Parse(args.Next());
                int countdel = int.Parse(args.Next());
                int total = int.Parse(args.Next());

                JSONFormatter js = new JSONFormatter();
                js.UTC("timestamp").V("event", "CargoDepot").V("MissionID", missid).V("UpdateType", type)
                                .V("StartMarketID", 12) .V("EndMarketID", 13)
                                .V("ItemsCollected", countcol)
                                .V("ItemsDelivered", countdel)
                                .V("TotalItemsToDeliver", total)
                                .V("Progress", (double)countcol / (double)total) ;
                return js.ToString();
            }
            catch
            {
                Console.WriteLine("missionid type col del total");
                return null;
            }
        }

        static string Shipyard(string mpath, string opt)
        {
            // { "timestamp":"2018-01-26T03:47:33Z", "event":"Shipyard", "MarketID":128004608, "StationName":"Vonarburg Co-operative", "StarSystem":"Wyrd",
            JSONFormatter js = new JSONFormatter();
            js.UTC("timestamp").V("event", "Shipyard") .V("MarketID", 12345678) .V("StationName", "Columbus") .V("StarSystem", "Sol");
            string jline = js.ToString();
            string fline = jline + ", " + EDDTest.Properties.Resources.Shipyard;

            if (opt == null || opt.Equals("NOFILE", StringComparison.InvariantCultureIgnoreCase) == false)
                File.WriteAllText(Path.Combine(mpath, "Shipyard.json"), fline);

            return jline ;
        }


        static string ModuleInfo(string mpath, string opt)
        {
            JSONFormatter js = new JSONFormatter();
            js.UTC("timestamp").V("event", "ModuleInfo");
            string mline = js.ToString();
            string market = mline + ", " + EDDTest.Properties.Resources.ModulesInfo;

            if (opt == null || opt.Equals("NOFILE", StringComparison.InvariantCultureIgnoreCase) == false)
                File.WriteAllText(Path.Combine(mpath, "ModulesInfo.json"), market);     // note the plural

            return mline ;
        }

        public static void FMission(JSONFormatter q, int id, string name, bool pas, int time)
        {
            q.V("MissionID", id).V("Name", name).V("PassengerMission", pas).V("Expires", time);
        }

        public static string Squadron(string ev, string name, params string[] list)
        {
            QuickJSON.JSONFormatter qj = new JSONFormatter();

            qj.Object().UTC("timestamp").V("event", ev);
            qj.V("SquadronName", name);
            if (list.Length >= 2)
            {
                qj.V("OldRank", list[0]);
                qj.V("NewRank", list[1]);
            }
            else if (list.Length == 1)
            {
                qj.V("CurrentRank", list[0]);
            }

            return qj.Get();

        }


        static void Blamtest(string filename, string cmdrname, CommandArgs args)
        {
            string starname = args.Next();
            int number = 0;

            double x = double.NaN, y = 0, z = 0;

            if (!double.TryParse(args.Next(), out x) || !double.TryParse(args.Next(), out y) || !double.TryParse(args.Next(), out z))
            {
                Console.WriteLine("** X,Y,Z must be numbers");
                return;
            }

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey().Key == ConsoleKey.Escape)
                        break;
                }

                JSONFormatter js = new JSONFormatter();
                //js.Object().UTC("timestamp").V("event","FSDJump").V("StarSystem", starname + "_" + number)
                //.Array("StarPos").V(null, x.ToStringInvariant("0.000000")).V(null, y.ToStringInvariant("0.000000")).V(null, z.ToStringInvariant("0.000000")).Close()
                //.Literal("\"Allegiance\":\"\", \"Economy\":\"$economy_None;\", \"Economy_Localised\":\"None\", \"Government\":\"$government_None;\"," +
                //"\"Government_Localised\":\"None\", \"Security\":\"$SYSTEM_SECURITY_low;\", \"Security_Localised\":\"Low Security\"," +
                //"\"JumpDist\":10.791, \"FuelUsed\":0.790330, \"FuelLevel\":6.893371");
                string lineout = js.ToString();

                WriteToLog(filename, cmdrname, lineout, true);
                number++;
                x += 0.5;
                System.Threading.Thread.Sleep(200);
            }
        }
    }

    public static class QuickAssist
    {
        public static QuickJSON.JSONFormatter UTC(this QuickJSON.JSONFormatter fmt, string name)
        {
            fmt.V(name, DateTime.UtcNow.ToStringZulu());
            return fmt;
        }
    }


}
