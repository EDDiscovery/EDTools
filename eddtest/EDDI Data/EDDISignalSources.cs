using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDDTest
{
    public partial class EDDI
    {

        public class SignalSource
        {
            public string Name;
            public string AltName;
            public SignalSource(string name)
            {
                Name = name;
                sources.Add(this);
            }
            public SignalSource(string name, string altname)
            {
                Name = name;
                AltName = altname;
                sources.Add(this);
            }
        }

        private static List<SignalSource> sources = new List<SignalSource>();


        // copied from eddidatadefinitions/signalsource.cs

        public static List<SignalSource> GetSignalSources()
        {
            var UnidentifiedSignalSource = new SignalSource("USS");
            var GenericSignalSource = new SignalSource("GenericSignalSource");

            var NavBeacon = new SignalSource("MULTIPLAYER_SCENARIO42_TITLE");
            var CompromisedNavBeacon = new SignalSource("MULTIPLAYER_SCENARIO80_TITLE");

            var ResourceExtraction = new SignalSource("MULTIPLAYER_SCENARIO14_TITLE");
            var ResourceExtractionLow = new SignalSource("MULTIPLAYER_SCENARIO77_TITLE");
            var ResourceExtractionHigh = new SignalSource("MULTIPLAYER_SCENARIO78_TITLE");
            var ResourceExtractionHazardous = new SignalSource("MULTIPLAYER_SCENARIO79_TITLE");
            var SalvageableWreckage = new SignalSource("MULTIPLAYER_SCENARIO81_TITLE");

            var CombatZoneHigh = new SignalSource("Warzone_PointRace_High");
            var CombatZoneMedium = new SignalSource("Warzone_PointRace_Med");
            var CombatZoneLow = new SignalSource("Warzone_PointRace_Low");
            var CombatZoneThargoid = new SignalSource("Warzone_TG");
            var CombatZoneThargoidHigh = new SignalSource("Warzone_TG_High");
            var CombatZoneThargoidMedium = new SignalSource("Warzone_TG_Med");
            var CombatZoneThargoidLow = new SignalSource("Warzone_TG_Low");
            var CombatZoneThargoidVeryHigh = new SignalSource("Warzone_TG_VeryHigh");

            var Aftermath = new SignalSource("USS_Type_Aftermath", "USS_SalvageHaulageWreckage");
            var Anomaly = new SignalSource("USS_Type_Anomaly");
            var Ceremonial = new SignalSource("USS_Type_Ceremonial", "USS_CeremonialComms");
            var Convoy = new SignalSource("USS_Type_Convoy", "USS_ConvoyDispersalPattern");
            var DegradedEmissions = new SignalSource("USS_Type_Salvage", "USS_DegradedEmissions");
            var Distress = new SignalSource("USS_Type_DistressSignal", "USS_DistressCall");
            var EncodedEmissions = new SignalSource("USS_Type_ValuableSalvage");
            var HighGradeEmissions = new SignalSource("USS_Type_VeryValuableSalvage", "USS_HighGradeEmissions");
            var MissionTarget = new SignalSource("USS_Type_MissionTarget");
            var NonHuman = new SignalSource("USS_Type_NonHuman", "USS_NonHumanSignalSource");
            var TradingBeacon = new SignalSource("USS_Type_TradingBeacon", "USS_TradingBeacon");
            var WeaponsFire = new SignalSource("USS_Type_WeaponsFire", "USS_WeaponsFire");

            var UnregisteredCommsBeacon = new SignalSource("NumberStation");
            var ListeningPost = new SignalSource("ListeningPost");

            var CapShip = new SignalSource("FIXED_EVENT_CAPSHIP");
            var Checkpoint = new SignalSource("FIXED_EVENT_CHECKPOINT");
            var ConvoyBeacon = new SignalSource("FIXED_EVENT_CONVOY");
            var DebrisField = new SignalSource("FIXED_EVENT_DEBRIS");
            var DistributionCenter = new SignalSource("FIXED_EVENT_DISTRIBUTIONCENTRE");
            var PirateAttackT5 = new SignalSource("FIXED_EVENT_HIGHTHREATSCENARIO_T5");
            var PirateAttackT6 = new SignalSource("FIXED_EVENT_HIGHTHREATSCENARIO_T6");
            var PirateAttackT7 = new SignalSource("FIXED_EVENT_HIGHTHREATSCENARIO_T7");
            var NotableStellarPhenomenaCloud = new SignalSource("Fixed_Event_Life_Cloud");
            var NotableStellarPhenomenaRing = new SignalSource("Fixed_Event_Life_Ring");

            var AttackAftermath = new SignalSource("AttackAftermath");
            var AftermathLarge = new SignalSource("Aftermath_Large");

            var Biological = new SignalSource("SAA_SignalType_Biological");
            var Geological = new SignalSource("SAA_SignalType_Geological");
            var Guardian = new SignalSource("SAA_SignalType_Guardian");
            var Human = new SignalSource("SAA_SignalType_Human");
            var Thargoid = new SignalSource("SAA_SignalType_Thargoid");
            var PlanetAnomaly = new SignalSource("SAA_SignalType_PlanetAnomaly");
            var Other = new SignalSource("SAA_SignalType_Other");

            var AncientGuardianRuins = new SignalSource("Ancient");
            var GuardianStructureTiny = new SignalSource("Ancient_Tiny");
            var GuardianStructureSmall = new SignalSource("Ancient_Small");
            var GuardianStructureMedium = new SignalSource("Ancient_Medium");
            var ThargoidBarnacle = new SignalSource("Settlement_Unflattened_Unknown");
            var ThargoidCrashSite = new SignalSource("Settlement_Unflattened_WreckedUnknown");

            var AbandonedBuggy = new SignalSource("Abandoned_Buggy");
            var ActivePowerSource = new SignalSource("Perimeter");
            var CrashedShip = new SignalSource("CrashedShip");
            var DamagedEagleAssassination = new SignalSource("Damaged_Eagle_Assassination");
            var DamagedSidewinderAssassination = new SignalSource("Damaged_Sidewinder_Assassination");
            var DamagedEagle = new SignalSource("Damaged_Eagle");
            var DamagedSidewinder = new SignalSource("Damaged_Sidewinder");
            var SmugglersCache = new SignalSource("Smugglers_Cache");
            var Cargo = new SignalSource("Cargo");
            var TrapCargo = new SignalSource("Trap_Cargo");
            var TrapData = new SignalSource("Trap_Data");
            var WreckageAncientProbe = new SignalSource("Wreckage_AncientProbe");
            var WreckageBuggy = new SignalSource("Wreckage_Buggy");
            var WreckageCargo = new SignalSource("Wreckage_Cargo");
            var WreckageProbe = new SignalSource("Wreckage_Probe");
            var WreckageSatellite = new SignalSource("Wreckage_Satellite");
            var WrecksEagle = new SignalSource("Wrecks_Eagle");
            var WrecksSidewinder = new SignalSource("Wrecks_Sidewinder");

            var ArmedRevolt = new SignalSource("Gro_controlScenarioTitle");

            return sources;
        }
}
}
