/*
 * Copyright 2015 - 2025 robbyxp @ github.com
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
        public bool createJournalEntry(CommandArgs args, int repeatcount)
        {
            string filenamepath = Path.GetDirectoryName(filename);

            string eventtype = args.Next().ToLower();
            string lineout = null;      //quick writer
            bool checkjson = true;

            QuickJSON.JSONFormatter qj = new QuickJSON.JSONFormatter();

            #region  Travel

            if (eventtype.Equals("fsd"))
            {
                lineout = WriteFSDJump(args, repeatcount);
            }
            else if (eventtype.Equals("fsdtravel"))
            {
                lineout = FSDTravel(args);
            }
            else if (eventtype.Equals("locdocked") && args.Left >= 4)
            {
                qj.Object().UTC("timestamp").V("event", "Location");
                qj.V("Docked", true);
                qj.V("StarSystem", args.Next());
                qj.V("StationName", args.Next());
                qj.V("StationType", "Orbis");
                qj.V("MarketID", 12829282);
                qj.Object("StationFaction").V("Name", args.Next()).V("FactionState", "IceCream").Close();
                qj.Object("SystemFaction").V("Name", args.Next()).V("FactionState", "IceCream").Close();
                qj.V("SystemAddress", 6538272662)
                .V("DistFromStarLS", 335.426033)
                .V("StationGovernment", "$government_Democracy;").V("StationGovernment_Localised", "Democracy").V("StationAllegiance", "PilotsFederation")
                .Array("StationServices").V("dock").V("autodock").V("commodities").V("contacts").V("exploration").V("missions").V("outfitting").V("crewlounge").V("rearm").V("refuel").V("repair").V("shipyard").V("tuning").V("engineer").V("missionsgenerated").V("flightcontroller").V("stationoperations").V("searchrescue").V("techBroker").V("stationMenu").V("shop").V("livery").V("socialspace").V("bartender").V("vistagenomics").V("pioneersupplies").V("apexinterstellar").V("frontlinesolutions").Close()
                .V("StationEconomy", "$economy_HighTech;").V("StationEconomy_Localised", "High Tech")
                .Array("StationEconomies")
                    .Object().V("Name", "$economy_HighTech;").V("Name_Localised", "High Tech").V("Proportion", 0.67).Close()
                    .Object().V("Name", "$economy_Industrial;").V("Name_Localised", "Industrial").V("Proportion", 0.33).Close()
                .Close()
                .V("Taxi", false).V("Multicrew", false).V("StarSystem", "Shinrarta Dezhra")
                .Array("StarPos").V(55.71875).V(17.59375).V(27.15625).Close()
                .V("SystemAllegiance", "PilotsFederation").V("SystemEconomy", "$economy_HighTech;").V("SystemEconomy_Localised", "High Tech").V("SystemSecondEconomy", "$economy_Industrial;")
                .V("SystemSecondEconomy_Localised", "Industrial").V("SystemGovernment", "$government_Democracy;").V("SystemGovernment_Localised", "Democracy")
                .V("SystemSecurity", "$SYSTEM_SECURITY_high;").V("SystemSecurity_Localised", "High Security")
                .V("Population", 85287324).V("Body", "Jameson Memorial")
                .V("BodyID", 69).V("BodyType", "Station")
                .Array("Factions")
                    .Object().V("Name", "LTT 4487 Industry").V("FactionState", "None").V("Government", "Corporate").V("Influence", 0.260302).V("Allegiance", "Federation").V("Happiness", "$Faction_HappinessBand2;").V("Happiness_Localised", "Happy").V("MyReputation", 11.88).Close()
                    .Object().V("Name", "Future of Arro Naga").V("FactionState", "CivilLiberty").V("Government", "Democracy").V("Influence", 0.232161).V("Allegiance", "Federation").V("Happiness", "$Faction_HappinessBand2;").V("Happiness_Localised", "Happy").V("MyReputation", 0.0).Array("ActiveStates").Object().V("State", "CivilLiberty").Close().Close().Close()
                    .Object().V("Name", "The Dark Wheel").V("FactionState", "Boom").V("Government", "Democracy").V("Influence", 0.309548).V("Allegiance", "Independent").V("Happiness", "$Faction_HappinessBand2;").V("Happiness_Localised", "Happy").V("MyReputation", -1.5).Array("ActiveStates").Object().V("State", "Boom").Close().Object().V("State", "Drought").Close().Close().Close()
                    .Object().V("Name", "Los Chupacabras").V("FactionState", "None").V("Government", "PrisonColony").V("Influence", 0.19799).V("Allegiance", "Independent").V("Happiness", "$Faction_HappinessBand2;").V("Happiness_Localised", "Happy").V("MyReputation", 0.0).Close()
                .Close()
                .Object("SystemFaction").V("Name", "Pilots' Federation Local Branch").Close()
                .Close();
            }
            else if (eventtype.Equals("onfootfc") && args.Left >= 1)
            {
                string starsystem = args.Next();

                qj.Object().UTC("timestamp").V("event", "Location").V("DistFromStarLS", 12.705612)
                .V("Docked", false).V("OnFoot", true)
                .V("StarSystem", starsystem)
                .V("SystemAddress", 35855326520745)
                .Array("StarPos").V(8.0).V(-11.1875).V(-2.65625).Close()
                .V("SystemAllegiance", "Independent").V("SystemEconomy", "$economy_HighTech;").V("SystemEconomy_Localised", "High Tech").V("SystemSecondEconomy", "$economy_Refinery;").V("SystemSecondEconomy_Localised", "Refinery").V("SystemGovernment", "$government_Corporate;").V("SystemGovernment_Localised", "Corporate").V("SystemSecurity", "$SYSTEM_SECURITY_high;").V("SystemSecurity_Localised", "High Security").V("Population", 49769105).V("Body", "Toolfa A 1").V("BodyID", 7).V("BodyType", "Planet")
                .Array("Factions")
                .Object().V("Name", "Union of Toolfa Progressive Party").V("FactionState", "CivilWar").V("Government", "Democracy").V("Influence", 0.07485).V("Allegiance", "Independent").V("Happiness", "$Faction_HappinessBand2;").V("Happiness_Localised", "Happy").V("MyReputation", 0.0)
                .Array("ActiveStates")
                .Object().V("State", "CivilWar").Close()
                .Close()
                .Close()
                .Object().V("Name", "Toolfa Network").V("FactionState", "None").V("Government", "Corporate").V("Influence", 0.113772).V("Allegiance", "Federation").V("Happiness", "$Faction_HappinessBand2;").V("Happiness_Localised", "Happy").V("MyReputation", 0.0)
                .Array("RecoveringStates")
                .Object().V("State", "PublicHoliday").V("Trend", 0).Close()
                .Close()
                .Close()
                .Object().V("Name", "Crimson Legal & Co").V("FactionState", "None").V("Government", "Corporate").V("Influence", 0.156687).V("Allegiance", "Federation").V("Happiness", "$Faction_HappinessBand2;").V("Happiness_Localised", "Happy").V("MyReputation", 0.0).Close()
                .Object().V("Name", "Natural Toolfa Nationalists").V("FactionState", "None").V("Government", "Dictatorship").V("Influence", 0.053892).V("Allegiance", "Independent").V("Happiness", "$Faction_HappinessBand2;").V("Happiness_Localised", "Happy").V("MyReputation", 0.0).Close()
                .Object().V("Name", "Toolfa Electronics Incorporated").V("FactionState", "CivilWar").V("Government", "Corporate").V("Influence", 0.07485).V("Allegiance", "Federation").V("Happiness", "$Faction_HappinessBand2;").V("Happiness_Localised", "Happy").V("MyReputation", 0.0)
                .Array("ActiveStates")
                .Object().V("State", "CivilWar").Close()
                .Close()
                .Close()
                .Object().V("Name", "Traditional Toolfa Movement").V("FactionState", "None").V("Government", "Dictatorship").V("Influence", 0.051896).V("Allegiance", "Independent").V("Happiness", "$Faction_HappinessBand2;").V("Happiness_Localised", "Happy").V("MyReputation", 0.0).Close()
                .Object().V("Name", "Alliance of Chinese Elite").V("FactionState", "Boom").V("Government", "Corporate").V("Influence", 0.474052).V("Allegiance", "Independent").V("Happiness", "$Faction_HappinessBand2;").V("Happiness_Localised", "Happy").V("MyReputation", 0.0)
                .Array("ActiveStates")
                .Object().V("State", "Boom").Close()
                .Close()
                .Close()
                .Close()
                .Object("SystemFaction").V("Name", "Alliance of Chinese Elite").V("FactionState", "Boom").Close()
                .Array("Conflicts")
                .Object().V("WarType", "civilwar").V("Status", "active")
                .Object("Faction1").V("Name", "Union of Toolfa Progressive Party").V("Stake", "Beauregard Industrial Facility").V("WonDays", 0).Close()
                .Object("Faction2").V("Name", "Toolfa Electronics Incorporated").V("Stake", "Henriquez Engineering Depot").V("WonDays", 0).Close()
                .Close()
                .Close()
                .Close();

            }
            else if (eventtype.Equals("docked") && args.Left >= 3)
            {
                qj.Object()
                .UTC("timestamp").V("event", "Docked")
                .V("StarSystem", args.Next()).V("SystemAddress", 3932277478106).V("MarketID", 128666762)
                .V("StationName", args.Next())
                .V("StationType", "Orbis").V("Taxi", false).V("Multicrew", false)
                .Object("StationFaction").V("Name", args.Next()).Close()
                .V("StationGovernment", "$government_Democracy;").V("StationGovernment_Localised", "Democracy").V("StationAllegiance", "PilotsFederation")
                .Array("StationServices").V("dock").V("autodock").V("commodities").V("contacts").V("exploration").V("missions").V("outfitting").V("crewlounge").V("rearm").V("refuel").V("repair").V("shipyard").V("tuning").V("engineer").V("missionsgenerated").V("flightcontroller").V("stationoperations").V("searchrescue").V("techBroker").V("stationMenu").V("shop").V("livery").V("socialspace").V("bartender").V("vistagenomics").V("pioneersupplies").V("apexinterstellar").V("frontlinesolutions").Close()
                .V("StationEconomy", "$economy_HighTech;").V("StationEconomy_Localised", "High Tech")
                .Array("StationEconomies")
                .Object().V("Name", "$economy_HighTech;").V("Name_Localised", "High Tech").V("Proportion", 0.67).Close()
                .Object().V("Name", "$economy_Industrial;").V("Name_Localised", "Industrial").V("Proportion", 0.33).Close()
                .Close()
                .V("DistFromStarLS", 335.135112)
                .Object("LandingPads").V("Small", 17).V("Medium", 18).V("Large", 9).Close()
            .Close();
            }
            else if (eventtype.Equals("dockedfc") && args.Left >= 0)
            {
                string starsystem = args.Next();

                qj.Object().UTC("timestamp").V("event", "Docked").V("StationName", "V6B-W6T").V("StationType", "FleetCarrier").V("Taxi", false).V("Multicrew", false)
                    .V("StarSystem", starsystem).V("SystemAddress", 3932277478114).V("MarketID", 3709149696)
                .Object("StationFaction").V("Name", "FleetCarrier").Close()
                .V("StationGovernment", "$government_Carrier;").V("StationGovernment_Localised", "Private Ownership")
                .Array("StationServices").V("dock").V("autodock").V("blackmarket").V("commodities").V("contacts").V("exploration").V("outfitting").V("crewlounge").V("rearm").V("refuel").V("repair").V("shipyard").V("engineer").V("flightcontroller").V("stationoperations").V("stationMenu").V("carriermanagement").V("carrierfuel").V("livery").V("voucherredemption").V("socialspace").V("bartender").V("pioneersupplies").Close()
                .V("StationEconomy", "$economy_Carrier;").V("StationEconomy_Localised", "Private Enterprise")
                .Array("StationEconomies")
                .Object().V("Name", "$economy_Carrier;").V("Name_Localised", "Private Enterprise").V("Proportion", 1.0).Close()
                .Close()
                .V("DistFromStarLS", 17.30867)
                .Object("LandingPads").V("Small", 4).V("Medium", 4).V("Large", 8).Close()
                .Close();
            }
            else if (eventtype.Equals("undocked"))
                qj.Object().UTC("timestamp").V("event", "Undocked").V("StationName", "Jameson Memorial").V("StationType", "Orbis");
            else if (eventtype.Equals("touchdown"))
            {
                qj.Object().UTC("timestamp").V("event", "Touchdown").V("Latitude", 7.141173).V("Longitude", 95.256424).V("PlayerControlled", true);
            }
            else if (eventtype.Equals("liftoff"))
                qj.Object().UTC("timestamp").V("event", "Liftoff").V("Latitude", 7.141173).V("Longitude", 95.256424);
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
            else if (eventtype.Equals("navroute") && args.Left >= 1)
            {
                DateTime t = DateTime.UtcNow;
                qj.Object().V("timestamp", t.ToStringZulu()).V("event", "NavRoute");

                JSONFormatter f = new JSONFormatter().Object().V("timestamp", t.ToStringZulu()).V("event", "NavRoute")
                    .Array("Route")
                        .Object().V("StarSystem", args.Next()).V("SystemAddress", 83785487050).Array("StarPos").V(-58.03125).V(59.5625).V(-58.78125).Close().V("StarClass", "K").Close()
                        .Object().V("StarSystem", "LHS 2610").V("SystemAddress", 5068464268697).Array("StarPos").V(-41.3125).V(40.4375).V(-27.375).Close().V("StarClass", "M").Close()
                        .Object().V("StarSystem", "44 chi Draconis").V("SystemAddress", 2278220106091).Array("StarPos").V(-22.5625).V(12.375).V(-5.40625).Close().V("StarClass", "F").Close()
                        .Object().V("StarSystem", "Sol").V("SystemAddress", 10477373803).Array("StarPos").V(0.0).V(0.0).V(0.0).Close().V("StarClass", "G").Close();

                File.WriteAllText("navRoute.json", f.Get());
            }
            else if (eventtype.Equals("navrouteclear"))
            {
                qj.Object().UTC("timestamp").V("event", "NavRouteClear");
            }
            else if (eventtype.Equals("navbeaconscan"))
                qj.Object().UTC("timestamp").V("event", "NavBeaconScan").V("NumBodies", "3");
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

            else if (eventtype.Equals("fsdtarget") && args.Left >= 3)
            {
                qj.Object().UTC("timestamp").V("event", "FSDTarget").V("Name", args.Next()).V("StarClass", args.Next()).V("SystemAddress", 2232220).V("RemainingJumpsInRoute", args.Int());
            }
            else if (eventtype.Equals("reservoirreplenished") && args.Left >= 2)
            {
                double main = args.Double();
                double res = args.Double();

                qj.Object().UTC("timestamp").V("event", "ReservoirReplenished")
                        .V("FuelMain", main)
                        .V("FuelReservoir", res);
            }
            else if (eventtype.Equals("approachbody") && args.Left >= 4)
            {
                string systemname = args.Next();
                long sysaddr = args.Long();
                string bodyname = args.Next();
                int bodyid = args.Int();

                qj.Object().UTC("timestamp").V("event", "ApproachBody")
                        .V("StarSystem", systemname)
                        .V("SystemAddress", sysaddr)
                        .V("Body", bodyname)
                        .V("BodyID", bodyid);
            }

            #endregion
            #region  Missions

            else if (eventtype.Equals("missionaccepted") && args.Left >= 3)
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
            else if (eventtype.Equals("missioncompleted") && args.Left >= 3)
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
            else if (eventtype.Equals("missionredirected") && args.Left >= 3)
            {
                string sysn = args.Next();
                string stationn = args.Next();
                int id = args.Int();
                qj.Object().UTC("timestamp").V("event", "MissionRedirected").V("MissionID", id).V("MissionName", "Mission_Assassinate_Legal_Corporate")
                    .V("NewDestinationStation", stationn).V("OldDestinationStation", "Cuffey Orbital")
                    .V("NewDestinationSystem", sysn).V("OldDestinationSystem", "Vequess");
            }
            else if (eventtype.Equals("missions") && args.Left >= 1)
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
            else if (eventtype.Equals("cargodepot") && args.Left >= 7)
            {
                int missid = args.Int();
                string type = args.Next();
                string cargotype = args.Next();
                int count = args.Int();
                int countcol = args.Int();
                int countdel = args.Int();
                int total = args.Int();

                qj.Object().UTC("timestamp").V("event", "CargoDepot")
                    .V("MissionID", missid).V("UpdateType", type)
                    .V("CargoType", cargotype)
                    .V("Count", count)
                    .V("StartMarketID", 12).V("EndMarketID", 13)
                    .V("ItemsCollected", countcol)
                    .V("ItemsDelivered", countdel)
                    .V("TotalItemsToDeliver", total)
                    .V("Progress", (double)countcol / (double)total);
            }

            #endregion
            #region  Crime/Bounties

            else if (eventtype.Equals("redeemvoucher") && args.Left >= 4)
            {
                string vtype = args.Next();
                int amount = args.Int();
                int brokerpercentage = args.Int();
                string faction = args.Next();
                qj.Object().UTC("timestamp").V("event", "RedeemVoucher")
                        .V("Type", vtype)
                        .V("Amount", amount);

                if (brokerpercentage > 0)
                    qj.V("BrokerPercentage", amount);

                if (faction.Contains(","))
                {
                    string[] factions = faction.Split(',');
                    qj.Array("Factions");
                    foreach (var x in factions)
                    {
                        qj.Object().V("Faction", x).V("Amount", 1000).Close();
                    }
                    qj.Close();
                }
                else if (faction.HasChars())
                    qj.V("Faction", faction);
            }

            else if (eventtype.Equals("bounty") && args.Left >= 2)
            {
                string f = args.Next();
                int rw = args.Int();

                qj.Object().UTC("timestamp").V("event", "Bounty").V("VictimFaction", f).V("VictimFaction_Localised", f + "_Loc")
                    .V("TotalReward", rw + repeatcount);
            }
            else if (eventtype.Equals("commitcrime") && args.Left >= 2)
            {
                string f = args.Next();
                int id = args.Int();
                qj.Object().UTC("timestamp").V("event", "CommitCrime").V("CrimeType", "collidedAtSpeedInNoFireZone").V("Faction", f).V("Fine", id);
            }
            else if (eventtype.Equals("crimevictim") && args.Left >= 2)
            {
                string f = args.Next();
                int bounty = args.Int();
                qj.Object().UTC("timestamp").V("event", "CrimeVictim").V("CrimeType", "assault").V("Offender", f).V("Bounty", bounty);
            }
            else if (eventtype.Equals("factionkillbond") && args.Left >= 3)
            {
                string f = args.Next();
                string vf = args.Next();
                int rw = args.Int();

                qj.Object().UTC("timestamp").V("event", "FactionKillBond").V("VictimFaction", vf).V("VictimFaction_Localised", vf + "_Loc")
                    .V("AwardingFaction", f).V("AwardingFaction_Localised", f + "_Loc")
                    .V("Reward", rw);
            }
            else if (eventtype.Equals("capshipbond") && args.Left >= 3)
            {
                string f = args.Next();
                string vf = args.Next();
                int rw = args.Int();

                qj.Object().UTC("timestamp").V("event", "CapShipBond").V("VictimFaction", vf).V("VictimFaction_Localised", vf + "_Loc")
                    .V("AwardingFaction", f).V("AwardingFaction_Localised", f + "_Loc")
                    .V("Reward", rw);
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
            else if (eventtype.Equals("resurrect") && args.Left >= 1)
            {
                int ct = args.Int();

                qj.Object().UTC("timestamp").V("event", "Resurrect").V("Option", "Help me").V("Cost", ct).V("Bankrupt", false);
            }
            else if (eventtype.Equals("died"))
            {
                qj.Object().UTC("timestamp").V("event", "Died").V("KillerName", "Evil Jim McDuff").V("KillerName_Localised", "Evil Jim McDuff The great").V("KillerShip", "X-Wing").V("KillerRank", "Invincible");
            }
            else if (eventtype.Equals("fighterdestroyed"))
                qj.Object().UTC("timestamp").V("event", "FighterDestroyed");
            else if (eventtype.Equals("fighterrebuilt"))
                qj.Object().UTC("timestamp").V("event", "FighterRebuilt").V("Loadout", "Fred");
            else if (eventtype.Equals("underattack"))
                qj.Object().UTC("timestamp").V("event", "UnderAttack").V("Target", "Fighter");

            #endregion
            #region  Commodities/Materials

            else if (eventtype.Equals("marketbuy") && args.Left >= 3)
            {
                string name = args.Next();
                int count = args.Int();
                int price = args.Int();

                qj.Object().UTC("timestamp").V("event", "MarketBuy").V("MarketID", 29029292)
                            .V("Type", name).V("Type_Localised", name + "loc").V("Count", count).V("BuyPrice", price).V("TotalCost", price * count);
            }
            else if (eventtype.Equals("marketsell") && args.Left >= 3)
            {
                string name = args.Next();
                int count = args.Int();
                int price = args.Int();

                qj.Object().UTC("timestamp").V("event", "MarketSell").V("MarketID", 29029292)
                            .V("Type", name).V("Type_Localised", name + "loc").V("Count", count).V("SellPrice", price).V("TotalSale", price * count)
                            .V("IllegalGoods", false).V("StolenGoods", false).V("BlackMarket", false);
            }
            else if (eventtype.Equals("market") && args.Left >= 2)
            {
                qj.Object().UTC("timestamp").V("event", "Market").V("MarketID", 12345678).V("StarSystem", args.Next()).V("StationName", args.Next());

                string market1 = qj.CurrentText + ", " + EDDTest.Properties.Resources.Market;
                string market2 = qj.CurrentText + ", " + EDDTest.Properties.Resources.Market2;
                File.WriteAllText(Path.Combine(filenamepath, "Market.json"), args.Int() == 0 ? market1 : market2);
            }
            else if (eventtype.Equals("materials") && args.Left >= 2)
            {
                qj.Object()
                    .UTC("timestamp").V("event", "Materials")
                    .Array("Raw")
                        .Object().V("Name", args.Next()).V("Count", args.Int()).Close()
                        .Object().V("Name", "iron").V("Count", 261).Close()
                        .Object().V("Name", "vanadium").V("Count", 39).Close()
                    .Close()
                .Close();
            }
            else if (eventtype.Equals("materialcollected") && args.Left >= 3)
            {
                qj.Object()
                    .UTC("timestamp").V("event", "MaterialCollected")
                    .V("Name", args.Next())
                    .V("Category", args.Next())
                    .V("Count", args.Int())
                .Close();
            }
            else if (eventtype.Equals("cargo") && args.Left >= 2)
            {
                qj.Object().UTC("timestamp").V("event", "Cargo").V("Vessel", "Ship").V("Count", 7)
                    .Array("Inventory")
                        .Object().V("Name", args.Next()).V("Count", args.Int()).V("Stolen", 0).Close()
                        .Object().V("Name", "lithiumhydroxide").V("Name_Localised", "Lithium Hydroxide").V("Count", 1).V("Stolen", 0).Close()
                        .Object().V("Name", "tritium").V("Count", 5).V("Stolen", 0).Close()
                    .Close()
                .Close();
            }
            else if (eventtype.Equals("buymicroresources") && args.Left >= 4)
            {
                string name = args.Next();
                string cat = args.Next();
                int count = args.Int();
                int price = args.Int();

                qj.Object().UTC("timestamp").V("event", "BuyMicroResources")
                            .V("Name", name).V("Name_Localised", name + "loc").V("Category", cat).V("Count", count).V("Price", price).V("MarketID", 29029292);
            }
            else if (eventtype.Equals("buymicroresources2") && args.Left >= 4)
            {
                string name = args.Next();
                string cat = args.Next();
                int count = args.Int();
                int price = args.Int();

                qj.Object().UTC("timestamp").V("event", "BuyMicroResources").V("TotalCount", count + (count + 1) + (count + 2))
                            .Array("MicroResources")
                                .Object().V("Name", name).V("Name_Localised", name + "loc").V("Category", cat).V("Count", count).Close()
                                .Object().V("Name", name + "2").V("Name_Localised", name + "loc2").V("Category", cat).V("Count", count + 1).Close()
                                .Object().V("Name", name + "3").V("Name_Localised", name + "loc3").V("Category", cat).V("Count", count + 2).Close()
                            .Close()
                            .V("Price", price).V("MarketID", 29029292);
            }
            else if (eventtype.Equals("sellmicroresources") && args.Left >= 4)
            {
                string name = args.Next();
                string cat = args.Next();
                int count = args.Int();
                int price = args.Int();

                qj.Object().UTC("timestamp").V("event", "SellMicroResources").V("TotalCount", count + (count + 1) + (count + 2))
                            .Array("MicroResources")
                                .Object().V("Name", name).V("Name_Localised", name + "loc").V("Category", cat).V("Count", count).Close()
                                .Object().V("Name", name + "2").V("Name_Localised", name + "loc2").V("Category", cat).V("Count", count + 1).Close()
                                .Object().V("Name", name + "3").V("Name_Localised", name + "loc3").V("Category", cat).V("Count", count + 2).Close()
                            .Close()
                            .V("Price", price).V("MarketID", 29029292);
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

            #endregion
            #region  Engineering

            else if (eventtype.Equals("engineercraft"))
            {
                qj.Object().UTC("timestamp").V("event", "EngineerCraft").V("Engineer", "Robert").V("Blueprint", "FSD_LongRange")
                    .V("Level", "5").Object("Ingredients").V("magneticemittercoil", 1).V("arsenic", 1).V("chemicalmanipulators", 1).V("dataminedwake", 1);
            }

            #endregion
            #region  Mining

            else if (eventtype.Equals("miningrefined") && args.Left >= 1)
            {
                string name = args.Next();
                qj.Object().UTC("timestamp").V("event", "MiningRefined").V("Type", name);
            }
            else if (eventtype.Equals("asteroidcracked") && args.Left >= 1)
            {
                string name = args.Next();

                qj.Object().UTC("timestamp").V("event", "AsteroidCracked");
                qj.V("Body", name);
                qj.Close();
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

            #endregion
            #region  Scans

            else if (eventtype.Equals("scanplanet") && args.Left >= 3)
            {
                string sysname = args.Next();
                long systemid = args.Long();
                string name = args.Next() + (repeatcount > 0 ? "x" + repeatcount : "");
                int bodyid = args.Int();
                qj.Object().UTC("timestamp")
                .V("event", "Scan").V("ScanType", "Detailed")
                .V("BodyName", name).V("BodyID", bodyid)
                .Array("Parents").Object().V("Star", 0).Close().Close()
                .V("StarSystem", sysname).V("SystemAddress", systemid)
                .V("DistanceFromArrivalLS", 2577.332838).V("TidalLock", false)
                .V("TerraformState", "").V("PlanetClass", "Icy body").V("Atmosphere", "helium atmosphere").V("AtmosphereType", "Helium")
                .Array("AtmosphereComposition")
                .Object().V("Name", "Helium").V("Percent", 91.379318).Close()
                .Object().V("Name", "Hydrogen").V("Percent", 8.620689).Close()
                .Close()
                .V("Volcanism", "major water geysers volcanism").V("MassEM", 2.148147).V("Radius", 9745602.0).V("SurfaceGravity", 9.014798).V("SurfaceTemperature", 24.18594).V("SurfacePressure", 43238.789063).V("Landable", false)
                .Object("Composition").V("Ice", 0.683679).V("Rock", 0.210949).V("Metal", 0.105372).Close()
                .V("SemiMajorAxis", 773089993000.03052).V("Eccentricity", 0.000988).V("OrbitalInclination", 0.002952).V("Periapsis", 280.393599).V("OrbitalPeriod", 792438870.668411).V("AscendingNode", -34.748208).V("MeanAnomaly", 303.857509).V("RotationPeriod", 45822.369767).V("AxialTilt", 0.380244)
                .V("WasDiscovered", false).V("WasMapped", false);
            }
            else if (eventtype.Equals("scanwaterworld") && args.Left >= 3)
            {
                string sysname = args.Next();
                long systemid = args.Long();
                string name = args.Next() + (repeatcount > 0 ? "x" + repeatcount : "");
                int bodyid = args.Int();

                qj.Object().UTC("timestamp").V("event", "Scan").V("ScanType", "Detailed").V("BodyName", name).V("BodyID", bodyid)
                .Array("Parents")
                .Object().V("Star", 0).Close()
                .Close()
                .V("StarSystem", sysname).V("SystemAddress", systemid)
                .V("DistanceFromArrivalLS", 2998.730075).V("TidalLock", false)
                .V("TerraformState", "").V("PlanetClass", "Water world").V("Atmosphere", "").V("AtmosphereType", "None")
                .V("Volcanism", "").V("MassEM", 6.011815).V("Radius", 11553491.0).V("SurfaceGravity", 17.950995).V("SurfaceTemperature", 161.07634).V("SurfacePressure", 0.0).V("Landable", false)
                .Object("Composition").V("Ice", 0.001184).V("Rock", 0.634009).V("Metal", 0.307875).Close()
                .V("SemiMajorAxis", 884778648614.88342).V("Eccentricity", 0.032729).V("OrbitalInclination", -0.303275)
                .V("Periapsis", 239.702562).V("OrbitalPeriod", 357116180.65834)
                .V("AscendingNode", 11.513124).V("MeanAnomaly", 117.771439).V("RotationPeriod", 43413.389817)
                .V("AxialTilt", 0.063492)
                .V("WasDiscovered", false).V("WasMapped", false).Close();
            }
            else if (eventtype.Equals("scanstar") && args.Left >= 2)
            {
                string sysname = args.Next();
                long systemid = args.Long();
                string name = args.Next() + (repeatcount > 0 ? "x" + repeatcount : "");
                int bodyid = args.Int();

                qj.Object().UTC("timestamp").V("event", "Scan").V("ScanType", "AutoScan")
                .V("BodyName", name).V("BodyID", bodyid)
                .V("StarSystem", sysname).V("SystemAddress", systemid)
                .V("DistanceFromArrivalLS", 0.0).V("StarType", "K").V("Subclass", 1).V("StellarMass", 0.699219)
                .V("Radius", 540175552.0).V("AbsoluteMagnitude", 6.084351).V("Age_MY", 4410).V("SurfaceTemperature", 4912.0).V("Luminosity", "Vab")
                .V("RotationPeriod", 337094.129722).V("AxialTilt", 0.0)
                .Array("Rings")
                .Object().V("Name", name + " A Belt").V("RingClass", "eRingClass_MetalRich").V("MassMT", 108980000000000.0).V("InnerRad", 943510000.0).V("OuterRad", 2141800000.0).Close()
                .Object().V("Name", name + " B Belt").V("RingClass", "eRingClass_MetalRich").V("MassMT", 7.1479E+15).V("InnerRad", 2279200000.0).V("OuterRad", 231550000000.0).Close()
                .Close()
                .V("WasDiscovered", false).V("WasMapped", false).Close();


            }
            else if (eventtype.Equals("fsssignaldiscovered") && args.Left >= 2)
            {
                string name = args.Next();
                long system = args.Long();

                qj.Object().UTC("timestamp").V("event", "FSSSignalDiscovered");
                if (name == "crazy")
                    name = @"'-II========II=#\"">";

                qj.V("SignalName", name);
                qj.V("SystemAddress", system);

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
            else if (eventtype.Equals("fssallbodiesfound") && args.Left >= 1)
            {
                qj.Object().UTC("timestamp").V("event", "FSSAllBodiesFound").V("SystemName", args.Next()).V("SystemAddress", 20);
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
                        .Close()
                        .Array("Genuses")
                        .Object().V("Genus", "$Codex_Ent_Bacterial_Genus_Name;").V("Genus_Localised", "Bacteria").Close()
                        .Object().V("Genus", "$Codex_Ent_Stratum_Genus_Name;").V("Genus_Localised", "Stratus").Close()
                        .Object().V("Genus", "$Codex_Ent_Stratum_Missing_Loc_Name;").Close()
                        .Close();
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
            else if (eventtype.Equals("scanorganic") && args.Left >= 4)
            {
                //{ "timestamp":"2023-08-31T14:13:35Z", "event":"ScanOrganic", "ScanType":"Analyse", "Genus":"$Codex_Ent_Osseus_Genus_Name;",
                //"Genus_Localised":"Osseus", "Species":"$Codex_Ent_Osseus_02_Name;", "Species_Localised":"Osseus Discus",
                //"Variant":"$Codex_Ent_Osseus_02_Tin_Name;", "Variant_Localised":"Osseus Discus - Blue", "SystemAddress":6365988787843, "Body":25 }

                string st = args.Next();
                if (st == "Log" || st == "Sample" || st == "Analyse")
                {
                    long sysid = args.Long();
                    int body = args.Int();
                    string genus = args.Next();
                    string species = args.Next();
                    string variant = args.Next();

                    var decoratedvariant = "$Codex_Ent_" + genus + "_" + species + "_" + variant + "_Name;";
                    var decoratedspecies = "$Codex_Ent_" + genus + "_" + species + "_Name;";
                    var decoratedgenus = "$Codex_Ent_" + genus + "_Genus_Name;";

                    qj.Object().UTC("timestamp").V("event", "ScanOrganic");
                    qj.V("ScanType", st);
                    qj.V("Genus", decoratedgenus);
                    qj.V("Genus_Localised", genus);
                    qj.V("Species", decoratedspecies);
                    qj.V("Species_Localised", species);
                    qj.V("Variant", decoratedvariant);
                    qj.V("Variant_Localised", variant);
                    qj.V("SystemAddress", sysid);
                    qj.V("Body", body);
                    qj.Close();
                }
                else
                    Console.WriteLine($"Need Log, Sample or Analyse");
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

            else if (eventtype.Equals("fssdiscoveryscan") && args.Left >= 1)
            {
                qj.Object().UTC("timestamp").V("event", "FSSDiscoveryScan").V("Progress", 0.23).V("BodyCount", args.Int()).V("NonBodyCount", 3);
            }


            #endregion
            #region  Ships

            else if (eventtype.Equals("sellshiponrebuy") && args.Left >= 1)
            {
                string name = args.Next();
                qj.Object().UTC("timestamp").V("event", "SellShipOnRebuy").V("ShipType", name).V("System", "Shinrarta Dezhra")
                    .V("SellShipId", 4).V("ShipPrice", 4110183);
            }
            else if (eventtype.Equals("afmurepairs") && args.Left >= 1)
            {
                string name = args.Next();
                qj.Object().UTC("timestamp").V("event", "AfmuRepairs").V("Module", name).V("Module_Localised", name + "_loc").V("FullyRepaired", true)
                        .V("Health", 1.000000);
            }
            else if (eventtype.Equals("repairdrone"))
            {
                qj.Object().UTC("timestamp").V("event", "RepairDrone");
                qj.V("HullRepaired", repeatcount * 0.1);
                qj.V("CockpitRepaired", 0.1);
                qj.V("CorrosionRepaired", 0.2);
            }
            else if (eventtype.Equals("launchdrone"))
                qj.Object().UTC("timestamp").V("event", "LaunchDrone").V("Type", "FuelTransfer");

            else if (eventtype.Equals("shipyard") && args.Left >= 2)
            {
                qj.Object().UTC("timestamp").V("event", "Shipyard").V("MarketID", 12345678).V("StarSystem", args.Next()).V("StationName", args.Next());

                string fline = qj.CurrentText + ", " + EDDTest.Properties.Resources.Shipyard;
                File.WriteAllText(Path.Combine(filenamepath, "Shipyard.json"), fline);
            }

            #endregion
            #region  Music

            else if (eventtype.Equals("musicnormal"))
                qj.Object().UTC("timestamp").V("event", "Music").V("MusicTrack", "NoTrack");
            else if (eventtype.Equals("musicsysmap"))
                qj.Object().UTC("timestamp").V("event", "Music").V("MusicTrack", "SystemMap");
            else if (eventtype.Equals("musicgalmap"))
                qj.Object().UTC("timestamp").V("event", "Music").V("MusicTrack", "GalaxyMap");


            #endregion
            #region  SRV

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

            #endregion
            #region  Modules

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
            else if (eventtype.Equals("moduleinfo") && args.Left >= 0)
            {
                qj.Object().UTC("timestamp").V("event", "ModuleInfo");
                string market = qj.CurrentText + ", " + EDDTest.Properties.Resources.ModulesInfo;
                File.WriteAllText(Path.Combine(filenamepath, "ModulesInfo.json"), market);     // note the plural
            }

            else if (eventtype.Equals("outfitting") && args.Left >= 2)
            {
                qj.Object().UTC("timestamp").V("event", "Outfitting").V("MarketID", 12345678).V("StarSystem", args.Next()).V("StationName", args.Next());

                string fline = qj.CurrentText + ", " + EDDTest.Properties.Resources.Outfitting;
                File.WriteAllText(Path.Combine(filenamepath, "Outfitting.json"), fline);
            }

            #endregion
            #region  Carrier

            else if (eventtype.Equals("carrierbuy") && args.Left >= 2)
            {
                string starsystem = args.Next();
                int price = args.Int();

                qj.Object().UTC("timestamp").V("event", "CarrierBuy").V("CarrierID", 3709149696).V("BoughtAtMarket", 3228884736)
                            .V("Location", starsystem).V("SystemAddress", 13864825529769).V("Price", price).V("Variant", "CarrierDockB").V("Callsign", "V6B-W6T").Close();
            }
            else if (eventtype.Equals("carrierdepositfuel") && args.Left >= 2)
            {
                int amount = args.Int();
                int total = args.Int();

                qj.Object().UTC("timestamp").V("event", "CarrierDepositFuel")
                        .V("CarrierID", 3709149696)
                        .V("Amount", amount)
                        .V("Total", total);
            }
            else if (eventtype.Equals("carrierfinance") && args.Left >= 1)
            {
                int amount = args.Int();

                qj.Object().UTC("timestamp").V("event", "CarrierFinance")
                        .V("CarrierID", 3709149696)
                        .V("TaxRate", 23)
                        .V("CarrierBalance", amount)
                        .V("ReserveBalance", amount + 2000)
                        .V("AvailableBalance", amount + 4000)
                        .V("ReservePercent", 42);
            }
            else if (eventtype.Equals("carriernamechanged") && args.Left >= 1)
            {
                qj.Object().UTC("timestamp").V("event", "CarrierNameChanged")
                        .V("CarrierID", 3709149696)
                        .V("Callsign", "V6B-W6T")
                        .V("Name", args.Next());
            }
            else if (eventtype.Equals("carrierjumprequest") && args.Left >= 3)
            {
                string starsystem = args.Next();
                string body = args.Next();
                int bodyid = args.Int();

                qj.Object().UTC("timestamp").V("event", "CarrierJumpRequest")
                        .V("CarrierID", 3709149696)
                        .V("SystemName", starsystem).V("Body", body).V("SystemAddress", 3657399571170).V("BodyID", bodyid).V("DepartureTime", DateTime.UtcNow.AddMinutes(15)).Close();
            }
            else if (eventtype.Equals("carrierjumpcancelled") && args.Left >= 0)
            {
                qj.Object().UTC("timestamp").V("event", "CarrierJumpCancelled")
                        .V("CarrierID", 3709149696);
            }
            else if ((eventtype.Equals("carrierjump") || eventtype.Equals("carrierlocation")) && args.Left >= 2)
            {
                string starsystem = args.Next();
                string body = args.Next();
                int bodyid = args.Int();

                bool loc = eventtype.Equals("carrierlocation");

                qj.Object().UTC("timestamp").V("event", loc ? "Location" : "CarrierJump")
                    .V("DistFromStarLS", 3111.491466).V("Docked", false).V("OnFoot", true)
                    .V("StarSystem", starsystem).V("SystemAddress", 3932277478114)
                .Array("StarPos").V(72.75).V(48.75).V(68.25).Close()
                .V("SystemAllegiance", "Alliance").V("SystemEconomy", "$economy_HighTech;").V("SystemEconomy_Localised", "High Tech").V("SystemSecondEconomy", "$economy_Industrial;")
                .V("SystemSecondEconomy_Localised", "Industrial")
                .V("SystemGovernment", "$government_Cooperative;").V("SystemGovernment_Localised", "Cooperative").V("SystemSecurity", "$SYSTEM_SECURITY_high;")
                .V("SystemSecurity_Localised", "High Security").V("Population", 5000017277).V("Body", body).V("BodyID", bodyid).V("BodyType", "Planet")

                .Array("Factions")
                    .Object().V("Name", "Sirius Corporation").V("FactionState", "None").V("Government", "Corporate").V("Influence", 0.066933).V("Allegiance", "Independent").V("Happiness", "$Faction_HappinessBand2;").V("Happiness_Localised", "Happy").V("MyReputation", 100.0).Close()
                    .Object().V("Name", "Independent Leesti for Equality").V("FactionState", "Boom").V("Government", "Democracy").V("Influence", 0.125874).V("Allegiance", "Alliance").V("Happiness", "$Faction_HappinessBand2;").V("Happiness_Localised", "Happy").V("MyReputation", 0.0)
                .Array("ActiveStates")
                    .Object().V("State", "Boom").Close()
                .Close()
                .Close()
                    .Object().V("Name", "Leesti United Steelworks").V("FactionState", "None").V("Government", "Corporate").V("Influence", 0.051948).V("Allegiance", "Alliance").V("Happiness", "$Faction_HappinessBand2;").V("Happiness_Localised", "Happy").V("MyReputation", 0.0).Close()
                    .Object().V("Name", "Orrere Energy Company").V("FactionState", "War").V("Government", "Corporate").V("Influence", 0.087912).V("Allegiance", "Federation").V("Happiness", "$Faction_HappinessBand2;").V("Happiness_Localised", "Happy").V("MyReputation", 0.0)
                .Array("ActiveStates")
                    .Object().V("State", "War").Close()
                .Close()
                .Close()
                .Object().V("Name", "Leesti Alliance Union").V("FactionState", "War").V("Government", "Patronage").V("Influence", 0.087912).V("Allegiance", "Alliance").V("Happiness", "$Faction_HappinessBand2;").V("Happiness_Localised", "Happy").V("MyReputation", 0.0)
                .Array("ActiveStates")
                    .Object().V("State", "War").Close()
                .Close()
                .Close()
                    .Object().V("Name", "Reynhardt IntelliSys").V("FactionState", "None").V("Government", "Corporate").V("Influence", 0.093906).V("Allegiance", "Independent").V("Happiness", "$Faction_HappinessBand2;").V("Happiness_Localised", "Happy").V("MyReputation", 0.0).Close()
                    .Object().V("Name", "Justice Party of Leesti").V("FactionState", "None").V("Government", "Dictatorship").V("Influence", 0.038961).V("Allegiance", "Independent").V("Happiness", "$Faction_HappinessBand2;").V("Happiness_Localised", "Happy").V("MyReputation", 0.0).Close()
                    .Object().V("Name", "Alliance Rapid-reaction Corps").V("FactionState", "Boom").V("Government", "Cooperative").V("Influence", 0.446553).V("Allegiance", "Alliance").V("Happiness", "$Faction_HappinessBand1;").V("Happiness_Localised", "Elated").V("MyReputation", 0.0)
                .Array("PendingStates")
                .Object().V("State", "Expansion").V("Trend", 0).Close()
                .Close()
                .Array("ActiveStates")
                    .Object().V("State", "CivilLiberty").Close()
                    .Object().V("State", "Boom").Close()
                .Close()
                .Close()
                .Close()
                .Object("SystemFaction").V("Name", "Alliance Rapid-reaction Corps").V("FactionState", "Boom").Close()
                .Array("Conflicts")
                    .Object().V("WarType", "war").V("Status", "active")
                    .Object("Faction1").V("Name", "Orrere Energy Company").V("Stake", "").V("WonDays", 0).Close()
                    .Object("Faction2").V("Name", "Leesti Alliance Union").V("Stake", "Cotton Engineering Foundry").V("WonDays", 1).Close()
                .Close()
                .Close()
                .Close();
            }
            else if (eventtype.Equals("carrierstats") && args.Left >= 1)
            {
                int balance = args.Int();
                qj.Object().UTC("timestamp").V("event", "CarrierStats").V("CarrierID", 3709149696).V("Callsign", "V6B-W6T").V("Name", "STANHELM007")
                .V("DockingAccess", "squadronfriends").V("AllowNotorious", true).V("FuelLevel", 463).V("JumpRangeCurr", 500.0).V("JumpRangeMax", 500.0).V("PendingDecommission", false)
                .Object("SpaceUsage").V("TotalCapacity", 25000).V("Crew", 6500).V("Cargo", 24).V("CargoSpaceReserved", 80).V("ShipPacks", 1850).V("ModulePacks", 710).V("FreeSpace", 15836).Close()
                .Object("Finance").V("CarrierBalance", balance).V("ReserveBalance", 0).V("AvailableBalance", balance).V("ReservePercent", 0)
                        .V("TaxRate_pioneersupplies", 25).V("TaxRate_shipyard", 100).V("TaxRate_rearm", 88).V("TaxRate_outfitting", 91).V("TaxRate_refuel", 90).V("TaxRate_repair", 73)
                        .Close()
                .Array("Crew")
                    .Object().V("CrewRole", "BlackMarket").V("Activated", true).V("Enabled", true).V("CrewName", "Efren Sanchez").Close()
                    .Object().V("CrewRole", "Captain").V("Activated", true).V("Enabled", true).V("CrewName", "Sid Hawkins").Close()
                    .Object().V("CrewRole", "Refuel").V("Activated", true).V("Enabled", true).V("CrewName", "Jackie Ortiz").Close()
                    .Object().V("CrewRole", "Repair").V("Activated", true).V("Enabled", true).V("CrewName", "Neal Huber").Close()
                    .Object().V("CrewRole", "Rearm").V("Activated", true).V("Enabled", true).V("CrewName", "Archie Jensen").Close()
                    .Object().V("CrewRole", "Commodities").V("Activated", true).V("Enabled", true).V("CrewName", "Cedrick Lowe").Close()
                    .Object().V("CrewRole", "VoucherRedemption").V("Activated", true).V("Enabled", true).V("CrewName", "Wallace Cooke").Close()
                    .Object().V("CrewRole", "Exploration").V("Activated", true).V("Enabled", true).V("CrewName", "Mariela Fitzgerald").Close()
                    .Object().V("CrewRole", "Shipyard").V("Activated", true).V("Enabled", true).V("CrewName", "Cesar Harrington").Close()
                    .Object().V("CrewRole", "Outfitting").V("Activated", true).V("Enabled", true).V("CrewName", "Cianna Suarez").Close()
                    .Object().V("CrewRole", "CarrierFuel").V("Activated", true).V("Enabled", true).V("CrewName", "Chauncey Griffith").Close()
                    .Object().V("CrewRole", "VistaGenomics").V("Activated", false).Close()
                    .Object().V("CrewRole", "PioneerSupplies").V("Activated", true).V("Enabled", true).V("CrewName", "Mia Leach").Close()
                    .Object().V("CrewRole", "Bartender").V("Activated", true).V("Enabled", true).V("CrewName", "Elianna Mckee").Close()
                .Close()
                .Array("ShipPacks")
                    .Object().V("PackTheme", "Zorgon Peterson - Cargo").V("PackTier", 1).Close()
                .Close()
                .Array("ModulePacks")
                    .Object().V("PackTheme", "ExplosiveWeaponry").V("PackTier", 1).Close()
                    .Object().V("PackTheme", "Mining Tools").V("PackTier", 2).Close()
                    .Object().V("PackTheme", "Storage").V("PackTier", 1).Close()
                    .Object().V("PackTheme", "VehicleSupport").V("PackTier", 1).Close()
                .Close()
                .Close();
            }


            #endregion
            #region  Crew

            else if (eventtype.Equals("crewassign") && args.Left >= 2)
            {
                qj.Object().UTC("timestamp").V("event", "CrewAssign")
                        .V("Name", args.Next())
                        .V("Role", args.Next())
                        .V("CrewID", 20);
            }
            else if (eventtype.Equals("crewlaunchfighter") && args.Left >= 2)
            {
                qj.Object().UTC("timestamp").V("event", "CrewLaunchFighter")
                        .V("Crew", args.Next())
                        .V("Telepresence", args.Int() != 0)
                        .V("CrewID", 20);
            }

            else if (eventtype.Equals("kickcrewmember") && args.Left >= 1)
            {
                qj.Object().UTC("timestamp").V("event", "KickCrewMember")
                        .V("Crew", args.Next())
                        .V("OnCrime", args.Int() != 0)
                        .V("Telepresence", args.Int() != 0);
            }

            #endregion
            #region Suits/weapons
            else if (eventtype.Equals("buysuit") && args.Left >= 1)
            {
                qj.Object().UTC("timestamp").V("event", "BuySuit")
                        .V("Name", "TacticalSuit_Class1")
                        .V("Name_Localised", "Tactical Suit")
                        .V("SuitID", args.Int())
                        .V("CommanderId", 23)
                        .V("Price", 150000);
            }

            #endregion
            #region  Squadron

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

            else if (eventtype.Equals("squadronstartup") && args.Left >= 2)
                lineout = Squadron("SquadronStartup", args.Next(), args.Next());


            #endregion
            #region  Text

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

            #endregion


            #region  Misc


            else if (eventtype.Equals("searchandrescue") && args.Left >= 2)
            {
                string name = args.Next();
                int count = args.Int();
                qj.Object().UTC("timestamp").V("event", "SearchAndRescue").V("MarketID", 29029292)
                            .V("Name", name).V("Name_Localised", name + "loc").V("Count", count).V("Reward", 10234);

            }
            else if (eventtype.Equals("communitygoal"))
            {
                //qj.Object().UTC("timestamp").V("event", "CommunityGoal").Literal("\"CurrentGoals\":[ { \"CGID\":726, \"Title\":\"Alliance Research Initiative - Trade\", \"SystemName\":\"Kaushpoos\", \"MarketName\":\"Neville Horizons\", \"Expiry\":\"2017-08-17T14:58:14Z\", \"IsComplete\":false, \"CurrentTotal\":10062, \"PlayerContribution\":562, \"NumContributors\":101, \"TopRankSize\":10, \"PlayerInTopRank\":false, \"TierReached\":\"Tier 1\", \"PlayerPercentileBand\":50, \"Bonus\":200000 } ] }");
            }

            // Misc
            else if (eventtype.Equals("friends") && args.Left >= 1)
                qj.Object().UTC("timestamp").V("event", "Friends").V("Status", "Online").V("Name", args.Next());

            else if (eventtype.Equals("npccrewrank"))
                qj.Object().UTC("timestamp").V("event", "NpcCrewRank").V("NpcCrewId", 1921).V("RankCombat", 4);
            else if (eventtype.Equals("npccrewpaidwage"))
                qj.Object().UTC("timestamp").V("event", "NpcCrewPaidWage").V("NpcCrewId", 1921).V("Amount", 10292);
            else if (eventtype.Equals("powerplay"))
                qj.Object().UTC("timestamp").V("event", "PowerPlay").V("Power", "Fred").V("Rank", 10).V("Merits", 10).V("Votes", 2).V("TimePledged", 433024);

            else if (eventtype.Equals("promotion") && args.Left >= 2)
                qj.Object().UTC("timestamp").V("event", "Promotion").V(args.Next(), args.Int());
            else if (eventtype.Equals("screenshot") && args.Left >= 2)
            {
                string infile = args.Next();
                string outfolder = args.Next();
                bool nojr = args.Left >= 1 && args.Next().Equals("NOJR");
                int times = args.Left >= 1 ? args.Int() : 1;

                if (File.Exists(infile))
                {
                    for (int count = 0; count < times; count++)
                    {
                        int n = 1000;
                        string outfile;
                        do
                        {
                            outfile = Path.Combine(outfolder, string.Format("Screenshot_{0}.bmp", n++));
                        } while (File.Exists(outfile));

                        string temp = Path.GetTempFileName();
                        File.Copy(infile, temp, true);
                        File.SetLastWriteTimeUtc(temp, DateTime.UtcNow);
                        File.Move(temp, outfile);

                        Console.WriteLine($"{infile} -> {temp} -> {outfile}");

                        if (!nojr)
                        {
                            JSONFormatter fmt = new JSONFormatter();
                            fmt.Object().UTC("timestamp").V("event", "Screenshot")
                                .V("Filename", "\\\\ED_Pictures\\\\" + Path.GetFileName(outfile))
                                .V("Width", 1920)
                                .V("Height", 1200)
                                .V("System", "Fredsys")
                                .V("Body", "Jimbody");

                            WriteToLog(fmt.Get());
                        }
                    }
                }
                else
                    Console.WriteLine("No such file {0}", infile);
            }
            else if (eventtype.Equals("event") && args.Left >= 1)   // give it a journal entry in a file {"timestamp" ... }
            {
                string file = args.Next();

                var text = File.ReadAllText(file);
                JObject jo = JObject.Parse(text);
                if (jo != null && jo.Contains("event") && jo.Contains("timestamp"))
                {
                    jo["timestamp"] = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'");     // replace timestamp
                    lineout += jo.ToString();
                }
                else
                {
                    Console.WriteLine("Bad journal line " + text);
                }
            }

            else if (eventtype.Equals("journallog") && args.Left >= 1)   // give it a line from the journal {"timestamp" ... }
            {
                string file = args.Next();

                var textlines = File.ReadLines(file);

                lineout = "";
                checkjson = false;      // don't check, as multiple lines, will confuse the decoder
                
                foreach (string line in textlines)
                {
                    if (line.Length > 0)
                    {
                        JObject jo = JObject.Parse(line);
                        if (jo != null)
                        {
                            jo["timestamp"] = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'");     // replace timestamp
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
            else if (eventtype.Equals("continued"))
            {
                QuickJSON.JSONFormatter qjc = new JSONFormatter();
                qjc.Object().UTC("timestamp").V("event", "Continued").V("Part", 2);
                WriteToLog(qjc.Get());
                filename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + ".part2" + Path.GetExtension(filename));
                Console.WriteLine("Continued.. change to filename " + filename);
                part++;
                WriteToLog(null);       // write just the header
            }
            else if (eventtype.Equals("clearimpound") && args.Left >= 1)
            {
                string sp = args.Next();
                qj.Object().UTC("timestamp").V("event", "ClearImpound").V("ShipType", sp).V("ShipType_Localised", sp + "_loc").V("ShipID", 10).V("MarketID", 12345678).V("ShipMarketID", 87654321);
            }

            #endregion


            #region Output

            else
            {
                Console.WriteLine("** Unrecognised journal event type or not enough parameters for entry" + Environment.NewLine + Help(eventtype,false));
                return false;
            }

            // we either produce lineout, or fill in qj.

            if (lineout == null && qj.Get().HasChars())
                lineout = qj.Get();

            if (lineout != null)
            {
                return WriteToLog(lineout,checkjson);
            }
            else
                return false;

            #endregion

        }
    }
}
