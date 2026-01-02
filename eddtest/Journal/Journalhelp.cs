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
        public static string Help(string eventtype, bool header)
        {
            string s = "";

            if (header)
            {
                s = "Usage:    Journal pathtologfile CMDRname [command|event [<paras>] ]..\n"
                + "          JournalFile filename\n"
                + "command = Loop N ... EndLoop\n"
                + "          msDelay N (ms delay between events)\n"
                + "          pause N (pause for N ms)\n"
                + "          KeyDelay (pause for a key between events, NoKeyDelay to turn off)\n"
                + "          stargrid <file> (give a csv exported from the route finder or search systems panel)\n"
                + "          gameversion N (set gameversion, default 4.0.0.1050. Also use 3.8 for legacy or beta for 2.2 (Beta 2)\n"
                + "          build N (set gameversion, default 4.0.0.1050)\n"
                + "          dayoffset N (change date written by N days)\n"
                +"           end (script section)\n";
            }

            s += helpout("Travel", "FSD name sysaddr x y z (x y z is position as double)", eventtype);
            s += helpout("", "FSD name (when stargrid is present, x/y/z/system address is taken from sheet)", eventtype);
            s += helpout("", "FSDTravel name x y z destx desty destz percentint ", eventtype);
            s += helpout("", "Locdocked stasystem station stationfaction systemfaction", eventtype);
            s += helpout("", "Docked starsystem station faction", eventtype);
            s += helpout("", "dockedfc starsystem - on fleet carrier", eventtype);
            s += helpout("", "onfootfc starsystem - on fleet carrier on foot", eventtype);
            s += helpout("", "Undocked, Touchdown, Liftoff", eventtype);
            s += helpout("", "FuelScoop amount total", eventtype);
            s += helpout("", "JetConeBoost", eventtype);
            s += helpout("", "navroute system", eventtype);
            s += helpout("", "navrouteclear", eventtype);
            s += helpout("", "NavBeaconScan", eventtype);
            s += helpout("", "Startjump system", eventtype);
            s += helpout("", "fsdtarget system starclass jumpsremaining", eventtype);
            s += helpout("", "reservoirreplenished main reserve", eventtype);
            s += helpout("", "approachbody starsystem sysaddr bodyname bodyid", eventtype);
            s += helpout("", "dockinggranted landingpad stationname stationtype", eventtype);
            s += helpout("", "dockingdenied reason stationname stationtype", eventtype);
            s += helpout("", "dockingtimeout/dockingrequested/dockingcancelled stationname stationtype", eventtype);

            s += helpout("Missions", "MissionAccepted/MissionCompleted faction victimfaction id", eventtype);
            s += helpout("", "MissionRedirected newsystem newstation id", eventtype);
            s += helpout("", "Missions activeid", eventtype);
            s += helpout("", "CargoDepot missionid cargotype count updatetype(Collect,Deliver,WingUpdate) itemcsollected delivered totalitemstodeliver", eventtype);

            s += helpout("C/B", "Bounty faction reward", eventtype);
            s += helpout("", "CommitCrime faction amount", eventtype);
            s += helpout("", "CrimeVictim offender amount", eventtype);
            s += helpout("", "FactionKillBond faction victimfaction reward", eventtype);
            s += helpout("", "CapShipBond faction victimfaction reward", eventtype);
            s += helpout("", "ShipTargeted [ship [pilot rank [health [faction]]]]", eventtype);
            s += helpout("", "Resurrect cost", eventtype);
            s += helpout("", "Died", eventtype);
            s += helpout("", "FighterDestroyed, FigherRebuilt", eventtype);
            s += helpout("", "UnderAttack", eventtype);

            s += helpout("Commds", "marketbuy fdname count price", eventtype);
            s += helpout("", "marketsell fdname count price", eventtype);
            s += helpout("", "market starsystem stationname 0/1 (write different market data)", eventtype);
            s += helpout("", "materials name count ", eventtype);
            s += helpout("", "materialcollected name category count", eventtype);
            s += helpout("", "cargo name count ", eventtype);
            s += helpout("", "buymicroresources name category count price", eventtype);
            s += helpout("", "buymicroresources2 name category count price", eventtype);
            s += helpout("", "sellmicroresources name category count price", eventtype);
            s += helpout("", "cargotransfer type count direction", eventtype);

            s += helpout("Eng", "engineercraft", eventtype);

            s += helpout("Mining", "asteroidcracked name", eventtype);
            s += helpout("", "propectedasteroid", eventtype);
            s += helpout("", "MiningRefined fdname", eventtype);

            s += helpout("Scans", "ScanPlanet/Scanwaterworld/Scanstar sysname sysid name bodyid", eventtype);
            s += helpout("", "fsssignaldiscovered name systemid [spawingstate spawingfaction]", eventtype);
            s += helpout("", "fssallbodiesfound count", eventtype);
            s += helpout("", "saascancomplete name", eventtype);
            s += helpout("", "saasignalsfound bodyname", eventtype);
            s += helpout("", "multisellexplorationdata", eventtype);
            s += helpout("", "scanorganic scantype (Log/Sample/Analyse) sysid bodyid genus species variant : Ie. Log 1234 1 Osseus 02 Tin", eventtype);
            s += helpout("", "CodexEntry name subcat cat system", eventtype);
            s += helpout("", "fssdiscoveryscan bodycount", eventtype);

            s += helpout("Ships", "SellShipOnRebuy fdname", eventtype);
            s += helpout("", "afmurepairs module", eventtype);
            s += helpout("", "repairdrone/launchdrone", eventtype);
            s += helpout("", "shipyard starsystem stationname", eventtype);

            s += helpout("Music", "MusicNormal, MusicGalMap, MusicSysMap", eventtype);

            s += helpout("SRV", "LaunchSRV, DockSRV, SRVDestroyed", eventtype);

            s += helpout("Modules", "ModuleBuyAndStore fdname price", eventtype);
            s += helpout("", "ModuleInfo", eventtype);
            s += helpout("", "Outfitting starsystem stationname", eventtype);

            s += helpout("Carrier", "CarrierBuy starsystem price", eventtype);
            s += helpout("", "CarrierDepositFuel amount total", eventtype);
            s += helpout("", "CarrierFinance balance", eventtype);
            s += helpout("", "CarrierNameChanged name", eventtype);
            s += helpout("", "CarrierJumpRequest system body bodyid", eventtype);
            s += helpout("", "CarrierJumpCancelled", eventtype);
            s += helpout("", "CarrierJump system body bodyid", eventtype);
            s += helpout("", "CarrierLocation system body bodyid (used for odyssey - it does not write up to v13 carrierjump)", eventtype);

            s += helpout("", "CarrierStats balance", eventtype);

            s += helpout("Crew", "crewassign name role", eventtype);
            s += helpout("", "crewlaunchfighter crew telepresence(1/0)", eventtype);
            s += helpout("", "kickcrewmember crew oncrime(1/0) telepresence(1/0)", eventtype);

            s += helpout("Suit/Weapons", "Buysuit name", eventtype);

            s += helpout("Squadron", "*Squadrons* name [args] (* use squardon event name)", eventtype);

            s += helpout("Text", "Receivetext from channel msg", eventtype);
            s += helpout("", "SentText to/channel msg", eventtype);

            s += helpout("Misc", "SearchAndRescue fdname count", eventtype);
            s += helpout("", "CommunityGoal", eventtype);
            s += helpout("", "Friends name", eventtype);
            s += helpout("", "NpcCrewRank/NpcCrewPaidWage", eventtype);
            s += helpout("", "PowerPlay", eventtype);
            s += helpout("", "ClearImpound ship", eventtype);
            s += helpout("", "Promotion Combat/Trade/Explore/CQC/Federation/Empire Ranknumber", eventtype);
            s += helpout("", "screenshot inputfile outputfolder [NOJR [repeatcount]]", eventtype);
            s += helpout("", "continued", eventtype);
            s += helpout("", "journallog filename - copy in events from file, formatted as per journal lines", eventtype);
            s += helpout("", "event filename - a single JSON event in a file", eventtype);
            return s;
        }

        static string lastsection = "";
        static string helpout(string section, string text, string eventtype)
        {
            if (section.HasChars() && section != lastsection)
            {
                lastsection = section;
            }

            if (eventtype.HasChars())
            {
                if (text.StartsWith(eventtype, StringComparison.InvariantCultureIgnoreCase) || text.Contains(", " + eventtype, StringComparison.InvariantCultureIgnoreCase))
                    return lastsection.PadRight(10) + text + Environment.NewLine;
                else
                    return "";
            }
            else
                return section.PadRight(10) + text + Environment.NewLine;
        }
    }
}
