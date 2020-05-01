using System;
using BaseUtils;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json.Linq;

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
                Console.WriteLine(Help());
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
                BaseUtils.QuickJSONFormatter qj = new QuickJSONFormatter();

                if (eventtype.Equals("fsd"))
                    lineout = FSDJump(args, repeatcount);
                else if (eventtype.Equals("blamtest"))
                    Blamtest(filename,cmdrname,args);
                else if (eventtype.Equals("fsdtravel"))
                    lineout = FSDTravel(args);
                else if (eventtype.Equals("locdocked"))
                {
                    lineout = "{ " + TimeStamp() +
                    "\"event\":\"Location\", \"Docked\":true, \"StationName\":\"Rock of Isolation\", \"StationType\":\"MegaShip\", \"MarketID\":128928173," +
                    "\"StationFaction\":{ \"Name\":\"Independent Detention Foundation\", \"FactionState\":\"CivilWar\"}," +
                    "\"SystemFaction\":{ \"Name\":\"Fred Foundation\", \"FactionState\":\"IceCream\"}," +
                    "\"StationGovernment\":\"$government_Prison;\", \"StationGovernment_Localised\":\"Detention Centre\", \"StationServices\":[ \"Dock\", \"Autodock\", \"Contacts\", \"Outfitting\", \"Rearm\", " +
                    "\"Refuel\", \"Repair\", \"Shipyard\", \"Workshop\", \"FlightController\", \"StationOperations\", \"StationMenu\" ], \"StationEconomy\":\"$economy_Prison;\", " +
                    "\"StationEconomy_Localised\":\"Prison\", \"StationEconomies\":[ { \"Name\":\"$economy_Prison;\", \"Name_Localised\":\"Prison\", \"Proportion\":1.000000 } ], " +
                    "\"StarSystem\":\"Omega Sector OD-S b4-0\", \"SystemAddress\":651358449137, \"StarPos\":[-1456.81250,-84.81250,5306.93750], \"SystemAllegiance\":\"\", \"SystemEconomy\":\"$economy_None;\", \"SystemEconomy_Localised\":\"None\", " +
                    "\"SystemSecondEconomy\":\"$economy_None;\", \"SystemSecondEconomy_Localised\":\"None\", \"SystemGovernment\":\"$government_None;\", \"SystemGovernment_Localised\":\"None\", \"SystemSecurity\":\"$GAlAXY_MAP_INFO_state_anarchy;\", \"SystemSecurity_Localised\":\"Anarchy\", " +
                    "\"Population\":0, \"Body\":\"Rock of Isolation\", \"BodyID\":33, \"BodyType\":\"Station\", \"Factions\":[ { \"Name\":\"Independent Detention Foundation\", \"FactionState\":\"None\", \"Government\":\"Prison\", \"Influence\":0.000000, \"Allegiance\":\"Independent\", \"Happiness\":\"$Faction_HappinessBand2;\", \"Happiness_Localised\":\"Happy\", \"MyReputation\":0.000000 }, { \"Name\":\"Pilots Federation Local Branch\", \"FactionState\":\"None\", \"Government\":\"Democracy\", \"Influence\":0.000000, \"Allegiance\":\"PilotsFederation\", \"Happiness\":\"\", \"MyReputation\":0.000000 } ]" +
                    "} ";
                }
                else if (eventtype.Equals("interdiction"))
                    lineout = Interdiction(args);
                else if (eventtype.Equals("docked") && args.Left >= 2)
                {
                    lineout = "{ " + TimeStamp() +
                        "\"event\":\"Docked\", \"StationName\":\"" + args.Next() + "\", \"StationType\":\"MegaShip\", \"StarSystem\":\"Omega Sector OD-S b4-0\", " +
                        "\"SystemAddress\":651358449137, \"MarketID\":128928173, \"StationFaction\":{ \"Name\":\"" + args.Next() + "\", \"FactionState\":\"IceCream\"}," +
                        "\"StationGovernment\":\"$government_Prison;\", " +
                        "\"StationGovernment_Localised\":\"Detention Centre\", \"StationServices\":[ \"Dock\", \"Autodock\", \"Contacts\", \"Outfitting\", \"Rearm\", \"Refuel\", \"Repair\", \"Shipyard\", " +
                        "\"Workshop\", \"FlightController\", \"StationOperations\", \"StationMenu\" ], \"StationEconomy\":\"$economy_Prison;\", \"StationEconomy_Localised\":\"Prison\", " +
                        "\"StationEconomies\":[ { \"Name\":\"$economy_Prison;\", \"Name_Localised\":\"Prison\", \"Proportion\":1.000000 } ], \"DistFromStarLS\":3919.440674 " +
                        "}";
                }
                else if (eventtype.Equals("undocked"))
                    lineout = "{ " + TimeStamp() + "\"event\":\"Undocked\", " + "\"StationName\":\"Jameson Memorial\",\"StationType\":\"Orbis\" }";
                else if (eventtype.Equals("liftoff"))
                    lineout = "{ " + TimeStamp() + "\"event\":\"Liftoff\", " + "\"Latitude\":7.141173, \"Longitude\":95.256424 }";
                else if (eventtype.Equals("touchdown"))
                {
                    lineout = "{ " + TimeStamp() + F("event", "Touchdown") + F("Latitude", 7.141173) + F("Longitude", 95.256424) + FF("PlayerControlled", true) + " }";
                }
                else if (eventtype.Equals("missionaccepted") && args.Left == 3)
                {
                    string f = args.Next();
                    string vf = args.Next();
                    int id = args.Int();

                    lineout = "{ " + TimeStamp() + F("event", "MissionAccepted") + F("Faction", f) +
                            F("Name", "Mission_Assassinate_Legal_Corporate") + F("TargetType", "$MissionUtil_FactionTag_PirateLord;") + F("TargetType_Localised", "Pirate lord") + F("TargetFaction", vf)
                            + F("DestinationSystem", "Quapa") + F("DestinationStation", "Grabe Dock") + F("Target", "mamsey") + F("Expiry", DateTime.UtcNow.AddDays(1)) +
                            F("Influence", "Med") + F("Reputation", "Med") + FF("MissionID", id) + "}";
                }
                else if (eventtype.Equals("missioncompleted") && args.Left == 3)
                {
                    string f = args.Next();
                    string vf = args.Next();
                    int id = args.Int();

                    lineout = "{ " + TimeStamp() + F("event", "MissionCompleted") + F("Faction", f) +
                        F("Name", "Mission_Assassinate_Legal_Corporate") + F("TargetType", "$MissionUtil_FactionTag_PirateLord;") + F("TargetType_Localised", "Pirate lord") + F("TargetFaction", vf) +
                         F("MissionID", id) + F("Reward", "82272") + " \"CommodityReward\":[ { \"Name\": \"CoolingHoses\", \"Count\": 4 } ] }";
                }
                else if (eventtype.Equals("missionredirected") && args.Left == 3)
                {
                    string sysn = args.Next();
                    string stationn = args.Next();
                    int id = args.Int();
                    lineout = "{ " + TimeStamp() + F("event", "MissionRedirected") + F("MissionID", id) + F("MissionName", "Mission_Assassinate_Legal_Corporate") +
                        F("NewDestinationStation", stationn) + F("OldDestinationStation", "Cuffey Orbital") +
                        F("NewDestinationSystem", sysn) + FF("OldDestinationSystem", "Vequess") + " }";
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
                else if (eventtype.Equals("bounty"))
                {
                    string f = args.Next();
                    int rw = args.Int();

                    lineout = "{ " + TimeStamp() + F("event", "Bounty") + F("VictimFaction", f) + F("VictimFaction_Localised", f + "_Loc") +
                        F("TotalReward", rw, true) + "}";
                }
                else if (eventtype.Equals("factionkillbond"))
                {
                    string f = args.Next();
                    string vf = args.Next();
                    int rw = args.Int();

                    lineout = "{ " + TimeStamp() + F("event", "FactionKillBond") + F("VictimFaction", vf) + F("VictimFaction_Localised", vf + "_Loc") +
                        F("AwardingFaction", f) + F("AwardingFaction_Localised", f + "_Loc") +
                        F("Reward", rw, true) + "}";
                }
                else if (eventtype.Equals("capshipbond"))
                {
                    string f = args.Next();
                    string vf = args.Next();
                    int rw = args.Int();

                    lineout = "{ " + TimeStamp() + F("event", "CapShipBond") + F("VictimFaction", vf) + F("VictimFaction_Localised", vf + "_Loc") +
                        F("AwardingFaction", f) + F("AwardingFaction_Localised", f + "_Loc") +
                        F("Reward", rw, true) + "}";
                }
                else if (eventtype.Equals("resurrect"))
                {
                    int ct = args.Int();

                    lineout = "{ " + TimeStamp() + F("event", "Resurrect") + F("Option", "Help me") + F("Cost", ct) + FF("Bankrupt", false) + "}";
                }
                else if (eventtype.Equals("died"))
                {
                    lineout = "{ " + TimeStamp() + F("event", "Died") + F("KillerName", "Evil Jim McDuff") + F("KillerName_Localised", "Evil Jim McDuff The great") + F("KillerShip", "X-Wing") + FF("KillerRank", "Invincible") + "}";
                }
                else if (eventtype.Equals("miningrefined"))
                    lineout = "{ " + TimeStamp() + F("event", "MiningRefined") + FF("Type", "Gold") + " }";
                else if (eventtype.Equals("engineercraft"))
                    lineout = "{ " + TimeStamp() + F("event", "EngineerCraft") + F("Engineer", "Robert") + F("Blueprint", "FSD_LongRange")
                        + F("Level", "5") + "\"Ingredients\":{ \"magneticemittercoil\":1, \"arsenic\":1, \"chemicalmanipulators\":1, \"dataminedwake\":1 } }";
                else if (eventtype.Equals("navbeaconscan"))
                    lineout = "{ " + TimeStamp() + F("event", "NavBeaconScan") + FF("NumBodies", "3") + " }";
                else if (eventtype.Equals("scanplanet") && args.Left >= 1)
                {
                    lineout = "{ " + TimeStamp() + F("event", "Scan") + "\"BodyName\":\"" + args.Next() + "x" + repeatcount + "\", \"DistanceFromArrivalLS\":639.245483, \"TidalLock\":true, \"TerraformState\":\"\", \"PlanetClass\":\"Metal rich body\", \"Atmosphere\":\"\", \"AtmosphereType\":\"None\", \"Volcanism\":\"rocky magma volcanism\", \"MassEM\":0.010663, \"Radius\":1163226.500000, \"SurfaceGravity\":3.140944, \"SurfaceTemperature\":1068.794067, \"SurfacePressure\":0.000000, \"Landable\":true, \"Materials\":[ { \"Name\":\"iron\", \"Percent\":36.824127 }, { \"Name\":\"nickel\", \"Percent\":27.852226 }, { \"Name\":\"chromium\", \"Percent\":16.561033 }, { \"Name\":\"zinc\", \"Percent\":10.007420 }, { \"Name\":\"selenium\", \"Percent\":2.584032 }, { \"Name\":\"tin\", \"Percent\":2.449526 }, { \"Name\":\"molybdenum\", \"Percent\":2.404594 }, { \"Name\":\"technetium\", \"Percent\":1.317050 } ], \"SemiMajorAxis\":1532780800.000000, \"Eccentricity\":0.000842, \"OrbitalInclination\":-1.609496, \"Periapsis\":179.381393, \"OrbitalPeriod\":162753.062500, \"RotationPeriod\":162754.531250, \"AxialTilt\":0.033219 }";
                }
                else if (eventtype.Equals("scanstar"))
                {
                    lineout = "{ " + TimeStamp() + F("event", "Scan") + "\"BodyName\":\"Merope A" + repeatcount + "\", \"DistanceFromArrivalLS\":0.000000, \"StarType\":\"B\", \"StellarMass\":8.597656, \"Radius\":2854249728.000000, \"AbsoluteMagnitude\":1.023468, \"Age_MY\":182, \"SurfaceTemperature\":23810.000000, \"Luminosity\":\"IV\", \"SemiMajorAxis\":12404761034752.000000, \"Eccentricity\":0.160601, \"OrbitalInclination\":18.126791, \"Periapsis\":49.512009, \"OrbitalPeriod\":54231617536.000000, \"RotationPeriod\":110414.359375, \"AxialTilt\":0.000000 }";
                }
                else if (eventtype.Equals("scanearth"))
                {
                    int rn = rnd.Next(10);
                    lineout = "{ " + TimeStamp() + F("event", "Scan") + "\"BodyName\":\"Merope " + rn + "\", \"DistanceFromArrivalLS\":901.789856, \"TidalLock\":false, \"TerraformState\":\"Terraformed\", \"PlanetClass\":\"Earthlike body\", \"Atmosphere\":\"\", \"AtmosphereType\":\"EarthLike\", \"AtmosphereComposition\":[ { \"Name\":\"Nitrogen\", \"Percent\":92.386833 }, { \"Name\":\"Oxygen\", \"Percent\":7.265749 }, { \"Name\":\"Water\", \"Percent\":0.312345 } ], \"Volcanism\":\"\", \"MassEM\":0.840000, \"Radius\":5821451.000000, \"SurfaceGravity\":9.879300, \"SurfaceTemperature\":316.457062, \"SurfacePressure\":209183.453125, \"Landable\":false, \"SemiMajorAxis\":264788426752.000000, \"Eccentricity\":0.021031, \"OrbitalInclination\":13.604733, \"Periapsis\":73.138206, \"OrbitalPeriod\":62498732.000000, \"RotationPeriod\":58967.023438, \"AxialTilt\":-0.175809 }";
                }
                else if (eventtype.Equals("ring"))
                {
                    lineout = "{ " + TimeStamp() + "\"event\": \"Scan\",  \"ScanType\": \"AutoScan\",  \"BodyName\": \"Merope 9 Ring\",  \"DistanceFromArrivalLS\": 1883.233643,  \"SemiMajorAxis\": 70415976.0,  \"Eccentricity\": 0.0,  \"OrbitalInclination\": 0.0,  \"Periapsis\": 0.0,  \"OrbitalPeriod\": 100994.445313}";
                }
                else if (eventtype.Equals("afmurepairs"))
                    lineout = "{ " + TimeStamp() + F("event", "AfmuRepairs") + "\"Module\":\"$modularcargobaydoor_name;\", \"Module_Localised\":\"Cargo Hatch\", \"FullyRepaired\":true, \"Health\":1.000000 }";

                else if (eventtype.Equals("sellshiponrebuy"))
                    lineout = "{ " + TimeStamp() + F("event", "SellShipOnRebuy") + "\"ShipType\":\"Dolphin\", \"System\":\"Shinrarta Dezhra\", \"SellShipId\":4, \"ShipPrice\":4110183 }";

                else if (eventtype.Equals("searchandrescue") && args.Left == 2)
                {
                    string name = args.Next();
                    int count = args.Int();
                    qj.Object().UTC("timestamp").V("event", "SearchAndRescue").V("MarketID", 29029292)
                                .V("Name", name).V("Name_Localised", name + "loc").V("Count", count).V("Reward", 10234);

                }
                else if (eventtype.Equals("repairdrone"))
                    lineout = "{ " + TimeStamp() + F("event", "RepairDrone") + "\"HullRepaired\": 0.23, \"CockpitRepaired\": 0.1,  \"CorrosionRepaired\": 0.5 }";

                else if (eventtype.Equals("communitygoal"))
                    lineout = "{ " + TimeStamp() + F("event", "CommunityGoal") + "\"CurrentGoals\":[ { \"CGID\":726, \"Title\":\"Alliance Research Initiative - Trade\", \"SystemName\":\"Kaushpoos\", \"MarketName\":\"Neville Horizons\", \"Expiry\":\"2017-08-17T14:58:14Z\", \"IsComplete\":false, \"CurrentTotal\":10062, \"PlayerContribution\":562, \"NumContributors\":101, \"TopRankSize\":10, \"PlayerInTopRank\":false, \"TierReached\":\"Tier 1\", \"PlayerPercentileBand\":50, \"Bonus\":200000 } ] }";

                else if (eventtype.Equals("musicnormal"))
                    lineout = "{ " + TimeStamp() + F("event", "Music") + FF("MusicTrack", "NoTrack") + " }";
                else if (eventtype.Equals("musicsysmap"))
                    lineout = "{ " + TimeStamp() + F("event", "Music") + FF("MusicTrack", "SystemMap") + " }";
                else if (eventtype.Equals("musicgalmap"))
                    lineout = "{ " + TimeStamp() + F("event", "Music") + FF("MusicTrack", "GalaxyMap") + " }";
                else if (eventtype.Equals("friends") && args.Left >= 1)
                    lineout = "{ " + TimeStamp() + F("event", "Friends") + F("Status", "Online") + FF("Name", args.Next()) + " }";
                else if (eventtype.Equals("fuelscoop") && args.Left >= 2)
                {
                    string scoop = args.Next();
                    string total = args.Next();
                    lineout = "{ " + TimeStamp() + F("event", "FuelScoop") + F("Scooped", scoop) + FF("Total", total) + " }";
                }
                else if (eventtype.Equals("jetconeboost"))
                    lineout = "{ " + TimeStamp() + F("event", "JetConeBoost") + FF("BoostValue", "1.5") + " }";
                else if (eventtype.Equals("fighterdestroyed"))
                    lineout = "{ " + TimeStamp() + FF("event", "FighterDestroyed") + " }";
                else if (eventtype.Equals("fighterrebuilt"))
                    lineout = "{ " + TimeStamp() + F("event", "FighterRebuilt") + FF("Loadout", "Fred") + " }";
                else if (eventtype.Equals("npccrewpaidwage"))
                    lineout = "{ " + TimeStamp() + F("event", "NpcCrewPaidWage") + F("NpcCrewId", 1921) + FF("Amount", 10292) + " }";
                else if (eventtype.Equals("npccrewrank"))
                    lineout = "{ " + TimeStamp() + F("event", "NpcCrewRank") + F("NpcCrewId", 1921) + FF("RankCombat", 4) + " }";
                else if (eventtype.Equals("launchdrone"))
                    lineout = "{ " + TimeStamp() + F("event", "LaunchDrone") + FF("Type", "FuelTransfer") + " }";
                else if (eventtype.Equals("market"))
                    lineout = Market(Path.GetDirectoryName(filename), args.Next());
                else if (eventtype.Equals("moduleinfo"))
                    lineout = ModuleInfo(Path.GetDirectoryName(filename), args.Next());
                else if (eventtype.Equals("outfitting"))
                    lineout = Outfitting(Path.GetDirectoryName(filename), args.Next());
                else if (eventtype.Equals("shipyard"))
                    lineout = Shipyard(Path.GetDirectoryName(filename), args.Next());
                else if (eventtype.Equals("powerplay"))
                    lineout = "{ " + TimeStamp() + F("event", "PowerPlay") + F("Power", "Fred") + F("Rank", 10) + F("Merits", 10) + F("Votes", 2) + FF("TimePledged", 433024) + " }";
                else if (eventtype.Equals("underattack"))
                    lineout = "{ " + TimeStamp() + F("event", "UnderAttack") + FF("Target", "Fighter") + " }";
                else if (eventtype.Equals("promotion") && args.Left == 2)
                    lineout = "{ " + TimeStamp() + F("event", "Promotion") + FF(args.Next(), args.Int()) + " }";
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
                    qj.V("Traits", new string[] { "T1", "T2", "T3" });
                    qj.Close();
                }
                else if (eventtype.Equals("saascancomplete") && args.Left >= 1)
                {
                    string name = args.Next();

                    qj.Object().UTC("timestamp").V("event", "SAAScanComplete");
                    qj.V("BodyName", name);
                    qj.V("BodyID", 10);
                    qj.V("Discoverers", new string[] { "Fred", "Jim", "Sheila" });
                    qj.V("Mappers", new string[] { "george", "Henry" });
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
                    lineout = "{ " + TimeStamp() + F("event", "CrimeVictim") + F("CrimeType", "assault") + F("Offender", f) + FF("Bounty", bounty) + " }";
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
                            try
                            {
                                JObject jo = JObject.Parse(line);
                                jo["timestamp"] = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'");
                                if (lineout.HasChars())
                                    lineout += Environment.NewLine;
                                lineout += jo.ToString(Newtonsoft.Json.Formatting.None);
                            }
                            catch
                            {
                                Console.WriteLine("Bad journal line " + line);
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
                else
                {
                    Console.WriteLine("** Unrecognised journal event type or not enough parameters for entry");
                    break;
                }

                if (lineout == null && qj.Get().HasChars())
                    lineout = qj.Get();

                if (lineout != null)
                    WriteToLog(filename,cmdrname, lineout,checkjson);
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

        public static string Help()
        {
            return
            "Usage:\n" +
            "Journal [-keyrepeat]|[-repeat ms] pathtologfile CMDRname eventname..\n" +
            "File     event filename - read filename with json and store in file\n" + 
            "Travel   FSD name x y z (x y z is position as double)\n" +
            "         FSDTravel name x y z destx desty destz percentint \n" +
            "         Locdocked\n" +
            "         Docked station faction\n" +
            "         Undocked, Touchdown, Liftoff\n" +
            "         FuelScoop amount total\n" +
            "         JetConeBoost\n" +
            "Missions MissionAccepted/MissionCompleted faction victimfaction id\n" +
            "         MissionRedirected newsystem newstation id\n" +
            "         Missions activeid\n" +
            "C/B      Bounty faction reward\n" +
            "         CommitCrime faction amount\n" +
            "         CrimeVictim offender amount\n" +
            "         FactionKillBond faction victimfaction reward\n" +
            "         CapShipBond faction victimfaction reward\n" +
            "         Interdiction name success isplayer combatrank faction power\n" +
            "         TargetShipLost\n" +
            "         ShipTargeted [ship [pilot rank [health [faction]]]]\n" +
            "Commds   marketbuy fdname count price\n" +
            "Scans    ScanPlanet name\n" +
            "         ScanStar, ScanEarth\n" +
            "         NavBeaconScan\n" +
            "         Ring\n" +
            "Ships    SellShipOnRebuy\n" +
            "SRV      LaunchSRV DockSRV SRVDestroyed\n" +
            "Others   SearchAndRescue fdname count\n" +
            "         MiningRefined\n" +
            "         Receivetext from channel msg\n" +
            "         SentText to/channel msg\n" +
            "         RepairDrone, CommunityGoal\n" +
            "         MusicNormal, MusicGalMap, MusicSysMap\n" +
            "         Friends name\n" +
            "         Died\n" +
            "         Resurrect cost\n" +
            "         PowerPlay, UnderAttack\n" +
            "         CargoDepot missionid updatetype(Collect,Deliver,WingUpdate) count total\n" +
            "         FighterDestroyed, FigherRebuilt, NpcCrewRank, NpcCrewPaidWage, LaunchDrone\n" +
            "         Market (use NOFILE after to say don't write the canned file, or 2 to write the alternate)\n" +
            "         ModuleInfo, Outfitting, Shipyard (use NOFILE after to say don't write the file)\n" +
            "         Promotion Combat/Trade/Explore/CQC/Federation/Empire Ranknumber\n" +
            "         CodexEntry name subcat cat system\n" +
            "         fsssignaldiscovered name\n" +
            "         saascancomplete name\n" +
            "         saasignalsfound bodyname\n" +
            "         asteroidcracked name\n" +
            "         multisellexplorationdata\n" +
            "         propectedasteroid\n" +
            "         replenishedreservoir main reserve\n" +
            "         *Squadrons* name\n" +

            "";
        }

        #endregion



        //                                  "Options: Interdiction Loc name success isplayer combatrank faction power\n" +
        static string Interdiction(CommandArgs args)
        {
            if (args.Left < 6)
            {
                Console.WriteLine("** More parameters");
                return null;
            }

            return "{ " + TimeStamp() + "\"event\":\"Interdiction\", " +
                "\"Success\":\"" + args[1] + "\", " +
                "\"Interdicted\":\"" + args[0] + "\", " +
                "\"IsPlayer\":\"" + args[2] + "\", " +
                "\"CombatRank\":\"" + args[3] + "\", " +
                "\"Faction\":\"" + args[4] + "\", " +
                "\"Power\":\"" + args[5] + "\" }";
        }

        static string FSDJump(CommandArgs args, int repeatcount)
        {
            if (args.Left < 4)
            {
                Console.WriteLine("** More parameters: file cmdrname fsd x y z");
                return null;
            }

            double x = double.NaN, y = 0, z = 0;
            string starnameroot = args.Next();

            if (!double.TryParse(args.Next(), out x) || !double.TryParse(args.Next(), out y) || !double.TryParse(args.Next(), out z))
            {
                Console.WriteLine("** X,Y,Z must be numbers");
                return null;
            }

            z = z + 100 * repeatcount;

            string starname = starnameroot + ((z > 0) ? "_" + z.ToStringInvariant("0") : "");

            return "{ " + TimeStamp() + "\"event\":\"FSDJump\", \"StarSystem\":\"" + starname +
            "\", \"StarPos\":[" + x.ToStringInvariant("0.000000") + ", " + y.ToStringInvariant("0.000000") + ", " + z.ToStringInvariant("0.000000") +
            "], \"Allegiance\":\"\", \"Economy\":\"$economy_None;\", \"Economy_Localised\":\"None\", \"Government\":\"$government_None;\"," +
            "\"Government_Localised\":\"None\", \"Security\":\"$SYSTEM_SECURITY_low;\", \"Security_Localised\":\"Low Security\"," +
            "\"JumpDist\":10.791, \"FuelUsed\":0.790330, \"FuelLevel\":6.893371 }";
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

            return "{ " + TimeStamp() + "\"event\":\"FSDJump\", \"StarSystem\":\"" + starname +
            "\", \"StarPos\":[" + x.ToStringInvariant("0.000000") + ", " + y.ToStringInvariant("0.000000") + ", " + z.ToStringInvariant("0.000000") +
            "], \"Allegiance\":\"\", \"Economy\":\"$economy_None;\", \"Economy_Localised\":\"None\", \"Government\":\"$government_None;\"," +
            "\"Government_Localised\":\"None\", \"Security\":\"$SYSTEM_SECURITY_low;\", \"Security_Localised\":\"Low Security\"," +
            "\"JumpDist\":10.791, \"FuelUsed\":0.790330, \"FuelLevel\":6.893371 }";
        }

        static string Market(string mpath, string opt)
        {
            string mline = "{ " + TimeStamp() + F("event", "Market") + F("MarketID", 12345678) + F("StationName", "Columbus") + FF("StarSystem", "Sol");
            string market1 = mline + ", " + EDDTest.Properties.Resources.Market;
            string market2 = mline + ", " + EDDTest.Properties.Resources.Market2;

            bool writem1 = opt == null || opt.Equals("NOFILE", StringComparison.InvariantCultureIgnoreCase) == false;
            bool writem2 = opt != null && opt.Equals("2");

            if (writem1)
                File.WriteAllText(Path.Combine(mpath, "Market.json"), market1);
            if (writem2)
                File.WriteAllText(Path.Combine(mpath, "Market.json"), market2);

            return mline + " }";
        }

        static string Outfitting(string mpath, string opt)
        {
            //{ "timestamp":"2018-01-28T23:45:39Z", "event":"Outfitting", "MarketID":3229009408, "StationName":"Mourelle Gateway", "StarSystem":"G 65-9",
            string jline = "{ " + TimeStamp() + F("event", "Outfitting") + F("MarketID", 12345678) + F("StationName", "Columbus") + FF("StarSystem", "Sol");
            string fline = jline + ", " + EDDTest.Properties.Resources.Outfitting;

            if (opt == null || opt.Equals("NOFILE", StringComparison.InvariantCultureIgnoreCase) == false)
                File.WriteAllText(Path.Combine(mpath, "Outfitting.json"), fline);

            return jline + " }";
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

                return "{ " + TimeStamp() + F("event", "CargoDepot") + F("MissionID", missid) + F("UpdateType", type) +
                                F("StartMarketID", 12) + F("EndMarketID", 13) +
                                F("ItemsCollected", countcol) +
                                F("ItemsDelivered", countdel) +
                                F("TotalItemsToDeliver", total) +
                                F("Progress", (double)countcol / (double)total, true) + " }";
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

            string jline = "{ " + TimeStamp() + F("event", "Shipyard") + F("MarketID", 12345678) + F("StationName", "Columbus") + FF("StarSystem", "Sol");
            string fline = jline + ", " + EDDTest.Properties.Resources.Shipyard;

            if (opt == null || opt.Equals("NOFILE", StringComparison.InvariantCultureIgnoreCase) == false)
                File.WriteAllText(Path.Combine(mpath, "Shipyard.json"), fline);

            return jline + " }";
        }


        static string ModuleInfo(string mpath, string opt)
        {
            string mline = "{ " + TimeStamp() + FF("event", "ModuleInfo");
            string market = mline + ", " + EDDTest.Properties.Resources.ModulesInfo;

            if (opt == null || opt.Equals("NOFILE", StringComparison.InvariantCultureIgnoreCase) == false)
                File.WriteAllText(Path.Combine(mpath, "ModulesInfo.json"), market);     // note the plural

            return mline + " }";
        }

        public static void FMission(QuickJSONFormatter q, int id, string name, bool pas, int time)
        {
            q.V("MissionID", id).V("Name", name).V("PassengerMission", pas).V("Expires", time);
        }

        public static string Squadron(string ev, string name, params string[] list)
        {
            BaseUtils.QuickJSONFormatter qj = new QuickJSONFormatter();

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

                string lineout = "{ " + TimeStamp() + "\"event\":\"FSDJump\", \"StarSystem\":\"" + starname + "_" + (number) +
                "\", \"StarPos\":[" + x.ToStringInvariant("0.000000") + ", " + y.ToStringInvariant("0.000000") + ", " + z.ToStringInvariant("0.000000") +
                "], \"Allegiance\":\"\", \"Economy\":\"$economy_None;\", \"Economy_Localised\":\"None\", \"Government\":\"$government_None;\"," +
                "\"Government_Localised\":\"None\", \"Security\":\"$SYSTEM_SECURITY_low;\", \"Security_Localised\":\"Low Security\"," +
                "\"JumpDist\":10.791, \"FuelUsed\":0.790330, \"FuelLevel\":6.893371 }";

                WriteToLog(filename, cmdrname, lineout, true);
                number++;
                x += 0.5;
                System.Threading.Thread.Sleep(200);
            }
        }



    }
}
