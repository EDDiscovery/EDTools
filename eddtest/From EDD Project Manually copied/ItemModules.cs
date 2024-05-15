/*
 * Copyright 2016-2024 EDDiscovery development team
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


using System;
using System.Collections.Generic;
using System.Linq;

namespace EliteDangerousCore
{
    public partial class ItemData
    {
        static public bool TryGetShipModule(string fdid, out ShipModule m, bool synthesiseit)
        {
            m = null;
            string lowername = fdid.ToLowerInvariant();

            // try the static values first, this is thread safe
            bool state = shipmodules.TryGetValue(lowername, out m) || othershipmodules.TryGetValue(lowername, out m) ||
                        srvmodules.TryGetValue(lowername, out m) || fightermodules.TryGetValue(lowername, out m) || vanitymodules.TryGetValue(lowername, out m);

            if (state == false)    // not found, try find the synth modules. Since we can be called in journal creation thread, we need some safety.
            {
                lock (synthesisedmodules)
                {
                    state = synthesisedmodules.TryGetValue(lowername, out m);
                }
            }

            if (!state && synthesiseit)   // if not found, and we want to synthesise it
            {
                lock (synthesisedmodules)  // lock for safety
                {
                    string candidatename = fdid;
                    candidatename = candidatename.Replace("weaponcustomisation", "WeaponCustomisation").Replace("testbuggy", "SRV").
                                            Replace("enginecustomisation", "EngineCustomisation");

                    candidatename = candidatename.SplitCapsWordFull();

                    var newmodule = new ShipModule(-1, IsVanity(lowername) ? ShipModule.ModuleTypes.VanityType : ShipModule.ModuleTypes.UnknownType, 0, 0, candidatename, null);

                    System.Diagnostics.Trace.WriteLine("*** Unknown Module { \"" + lowername + "\", new ShipModule(-1,0, \"" + newmodule.EnglishModName + "\", " + (IsVanity(lowername) ? "ShipModule.ModuleTypes.VanityType" : "ShipModule.ModuleTypes.UnknownType") + " ) }, - this will not affect operation but it would be nice to report it to us so we can add it to known module lists");

                    synthesisedmodules[lowername] = m = newmodule;                   // lets cache them for completeness..
                }
            }

            return state;
        }

        // List of ship modules. Synthesised are not included
        // default is buyable modules only
        // you can include other types
        // compressarmour removes all armour entries except the ones for the sidewinder
        static public Dictionary<string, ShipModule> GetShipModules(bool includebuyable = true, bool includenonbuyable = false, bool includesrv = false,
                                                                    bool includefighter = false, bool includevanity = false, bool addunknowntype = false,
                                                                    bool compressarmourtosidewinderonly = false)
        {
            Dictionary<string, ShipModule> ml = new Dictionary<string, ShipModule>();

            if (includebuyable)
            {
                foreach (var x in shipmodules) ml[x.Key] = x.Value;
            }

            if (compressarmourtosidewinderonly)        // remove all but _grade1 armours in list
            {
                var list = shipmodules.Keys;
                foreach (var name in list)
                {
                    if (name.Contains("_armour_") && !name.Contains("sidewinder")) // only keep sidewinder - all other ones are removed
                        ml.Remove(name);
                }
            }

            if (includenonbuyable)
            {
                foreach (var x in othershipmodules) ml[x.Key] = x.Value;
            }
            if (includesrv)
            {
                foreach (var x in srvmodules) ml[x.Key] = x.Value;
            }
            if (includefighter)
            {
                foreach (var x in fightermodules) ml[x.Key] = x.Value;

            }
            if (includevanity)
            {
                foreach (var x in vanitymodules) ml[x.Key] = x.Value;

            }
            if (addunknowntype)
            {
                ml["Unknown"] = new ShipModule(-1, ShipModule.ModuleTypes.UnknownType, 0, 0, "Unknown Type", null);

            }
            return ml;
        }

        // a dictionary of module english module type vs translated module type for a set of modules
        public static Dictionary<string, string> GetModuleTypeNamesTranslations(Dictionary<string, ShipModule> modules)
        {
            var ret = new Dictionary<string, string>();
            foreach (var x in modules)
            {
                if (!ret.ContainsKey(x.Value.EnglishModTypeString))
                    ret[x.Value.EnglishModTypeString] = x.Value.TranslatedModTypeString;
            }
            return ret;
        }

        // given a module name list containing siderwinder_armour_gradeX only,
        // expand out to include all other ships armours of the same grade
        // used in spansh station to reduce list of shiptype armours shown, as if one is there for a ship, they all are there for all ships
        public static string[] ExpandArmours(string[] list)
        {
            List<string> ret = new List<string>();
            foreach (var x in list)
            {
                if (x.StartsWith("sidewinder_armour"))
                {
                    string grade = x.Substring(x.IndexOf("_"));     // its grade (_armour_grade1, _grade2 etc)

                    foreach (var kvp in shipmodules)
                    {
                        if (kvp.Key.EndsWith(grade))
                            ret.Add(kvp.Key);
                    }
                }
                else
                    ret.Add(x);
            }

            return ret.ToArray();
        }

        static public bool IsVanity(string ifd)
        {
            ifd = ifd.ToLowerInvariant();
            string[] vlist = new[] { "bobble", "decal", "enginecustomisation", "nameplate", "paintjob",
                                    "shipkit", "weaponcustomisation", "voicepack" , "lights", "spoiler" , "wings", "bumper"};
            return Array.Find(vlist, x => ifd.Contains(x)) != null;
        }

        static string TXIT(string text)
        {
            return BaseUtils.Translator.Instance.Translate(text, "ModulePartNames." + text.Replace(" ", "_"));
        }

        // called at start up to set up translation of module names
        static private void TranslateModules()
        {
            foreach (var kvp in shipmodules)
            {
                ShipModule sm = kvp.Value;

                // this logic breaks down the 

                if (kvp.Key.Contains("_armour_", StringComparison.InvariantCulture))
                {
                    string[] armourdelim = new string[] { "Lightweight", "Reinforced", "Military", "Mirrored", "Reactive" };
                    int index = sm.EnglishModName.IndexOf(armourdelim, out int anum, StringComparison.InvariantCulture);
                    string translated = TXIT(sm.EnglishModName.Substring(index));
                    sm.TranslatedModName = sm.EnglishModName.Substring(0, index) + translated;
                }
                else
                {
                    int cindex = sm.EnglishModName.IndexOf(" Class ", StringComparison.InvariantCulture);
                    int rindex = sm.EnglishModName.IndexOf(" Rating ", StringComparison.InvariantCulture);

                    if (cindex != -1 && rindex != -1)
                    {
                        string translated = TXIT(sm.EnglishModName.Substring(0, cindex));
                        string cls = TXIT(sm.EnglishModName.Substring(cindex + 1, 5));
                        string rat = TXIT(sm.EnglishModName.Substring(rindex + 1, 6));
                        sm.TranslatedModName = translated + " " + cls + " " + sm.EnglishModName.Substring(cindex + 7, 1) + " " + rat + " " + sm.EnglishModName.Substring(rindex + 8, 1);
                    }
                    else if (cindex != -1)
                    {
                        string translated = TXIT(sm.EnglishModName.Substring(0, cindex));
                        string cls = TXIT(sm.EnglishModName.Substring(cindex + 1, 5));
                        sm.TranslatedModName = translated + " " + cls + " " + sm.EnglishModName.Substring(cindex + 7, 1);
                    }
                    else if (rindex != -1)
                    {
                        string translated = TXIT(sm.EnglishModName.Substring(0, rindex));
                        string rat = TXIT(sm.EnglishModName.Substring(rindex + 1, 6));
                        sm.TranslatedModName = translated + " " + rat + " " + sm.EnglishModName.Substring(rindex + 8, 1);
                    }
                    else
                    {
                        string[] sizes = new string[] { " Small", " Medium", " Large", " Huge", " Tiny", " Standard", " Intermediate", " Advanced" };
                        int sindex = sm.EnglishModName.IndexOf(sizes, out int snum, StringComparison.InvariantCulture);

                        if (sindex >= 0)
                        {
                            string[] types = new string[] { " Gimbal ", " Fixed ", " Turret " };
                            int gindex = sm.EnglishModName.IndexOf(types, out int gnum, StringComparison.InvariantCulture);

                            if (gindex >= 0)
                            {
                                string translated = TXIT(sm.EnglishModName.Substring(0, gindex));
                                string typen = TXIT(sm.EnglishModName.Substring(gindex + 1, types[gnum].Length - 2));
                                string sizen = TXIT(sm.EnglishModName.Substring(sindex + 1, sizes[snum].Length - 1));
                                sm.TranslatedModName = translated + " " + typen + " " + sizen;
                            }
                            else
                            {
                                string translated = TXIT(sm.EnglishModName.Substring(0, sindex));
                                string sizen = TXIT(sm.EnglishModName.Substring(sindex + 1, sizes[snum].Length - 1));
                                sm.TranslatedModName = translated + " " + sizen;
                            }
                        }
                        else
                        {
                            sm.TranslatedModName = TXIT(sm.EnglishModName);
                            //System.Diagnostics.Debug.WriteLine($"?? {kvp.Key} = {sm.ModName}");
                        }
                    }
                }

                //System.Diagnostics.Debug.WriteLine($"Module {sm.ModName} : {sm.ModType} => {sm.TranslatedModName} : {sm.TranslatedModTypeString}");
            }
        }

        #region ShipModule

        public class ShipModule : IModuleInfo
        {
            public enum ModuleTypes
            {
                // Aligned with spansh, spansh is aligned with outfitting.csv on EDCD.
                // all buyable

                AXMissileRack,
                AXMulti_Cannon,
                AbrasionBlaster,
                AdvancedDockingComputer,
                AdvancedMissileRack,
                AdvancedMulti_Cannon,
                AdvancedPlanetaryApproachSuite,
                AdvancedPlasmaAccelerator,
                AutoField_MaintenanceUnit,
                BeamLaser,
                Bi_WeaveShieldGenerator,
                BurstLaser,
                BusinessClassPassengerCabin,
                Cannon,
                CargoRack,
                CargoScanner,
                CausticSinkLauncher,
                ChaffLauncher,
                CollectorLimpetController,
                CorrosionResistantCargoRack,
                CytoscramblerBurstLaser,
                DecontaminationLimpetController,
                DetailedSurfaceScanner,
                EconomyClassPassengerCabin,
                ElectronicCountermeasure,
                EnforcerCannon,
                EnhancedAXMissileRack,
                EnhancedAXMulti_Cannon,
                EnhancedPerformanceThrusters,
                EnhancedXenoScanner,
                EnzymeMissileRack,
                ExperimentalWeaponStabiliser,
                FighterHangar,
                FirstClassPassengerCabin,
                FragmentCannon,
                FrameShiftDrive,
                FrameShiftDriveInterdictor,
                FrameShiftWakeScanner,
                FuelScoop,
                FuelTank,
                FuelTransferLimpetController,
                GuardianFSDBooster,
                GuardianGaussCannon,
                GuardianHullReinforcement,
                GuardianHybridPowerDistributor,
                GuardianHybridPowerPlant,
                GuardianModuleReinforcement,
                GuardianPlasmaCharger,
                GuardianShardCannon,
                GuardianShieldReinforcement,
                HatchBreakerLimpetController,
                HeatSinkLauncher,
                HullReinforcementPackage,
                ImperialHammerRailGun,
                KillWarrantScanner,
                LifeSupport,
                LightweightAlloy,
                ////LimpetControl,
                LuxuryClassPassengerCabin,
                MetaAlloyHullReinforcement,
                MilitaryGradeComposite,
                MineLauncher,
                MiningLance,
                MiningLaser,
                MiningMultiLimpetController,
                MirroredSurfaceComposite,
                MissileRack,
                ModuleReinforcementPackage,
                Multi_Cannon,
                OperationsMultiLimpetController,
                PacifierFrag_Cannon,
                Pack_HoundMissileRack,
                PlanetaryApproachSuite,
                PlanetaryVehicleHangar,
                PlasmaAccelerator,
                PointDefence,
                PowerDistributor,
                PowerPlant,
                PrismaticShieldGenerator,
                ProspectorLimpetController,
                PulseDisruptorLaser,
                PulseLaser,
                PulseWaveAnalyser,
                RailGun,
                ReactiveSurfaceComposite,
                ReconLimpetController,
                Refinery,
                ReinforcedAlloy,
                RemoteReleaseFlakLauncher,
                RemoteReleaseFlechetteLauncher,
                RepairLimpetController,
                RescueMultiLimpetController,
                ResearchLimpetController,
                RetributorBeamLaser,
                RocketPropelledFSDDisruptor,
                SeekerMissileRack,
                SeismicChargeLauncher,
                Sensors,
                ShieldBooster,
                ShieldCellBank,
                ShieldGenerator,
                ShockCannon,
                ShockMineLauncher,
                ShutdownFieldNeutraliser,
                StandardDockingComputer,
                Sub_SurfaceDisplacementMissile,
                SupercruiseAssist,
                Thrusters,
                TorpedoPylon,
                UniversalMultiLimpetController,
                XenoMultiLimpetController,
                XenoScanner,

                // Not buyable, DiscoveryScanner marks the first non buyable
                DiscoveryScanner, PrisonCells, DataLinkScanner, SRVScanner, FighterWeapon,
                VanityType, UnknownType, CockpitType, CargoBayDoorType, WearAndTearType, Codex,
            };

            public string EnglishModName { get; set; }     // english name
            public string TranslatedModName { get; set; }     // foreign name
            public int ModuleID { get; set; }
            public ModuleTypes ModType { get; set; }

            // string should be in spansh/EDCD csv compatible format, in english, as it it fed into Spansh
            public string EnglishModTypeString { get { return ModType.ToString().Replace("AX", "AX ").Replace("_", "-").SplitCapsWordFull(); } }
            public string TranslatedModTypeString { get { return BaseUtils.Translator.Instance.Translate(EnglishModTypeString, "ModuleTypeNames." + EnglishModTypeString.Replace(" ", "_")); } }     // string should be in spansh/EDCD csv compatible format, in english

            public double Mass { get; set; }        // mass of module
            public double Power { get; set; }       // power used by module
            public int? Ammo { get; set; }
            public int? Clip { get; set; }
            public double? Damage { get; set; }
            public double? Reload { get; set; }
            public double? ThermL { get; set; }
            public double? Explosive { get; set; } //%
            public double? Kinetic { get; set; } //%
            public double? Thermal { get; set; } //%
            public double? Range { get; set; } // m
            public double? Speed { get; set; } // m/s
            public double? Repair { get; set; } // s
            public double? Protection { get; set; } // multiplier
            public double? Sys { get; set; } // power distributor rate
            public double? Eng { get; set; } // power distributor rate
            public double? Wep { get; set; } // power distributor rate
            public double? PowerGen { get; set; } // MW
            public double? OptMass { get; set; } // t
            public double? MaxMass { get; set; } // t
            public double? MinMass { get; set; } // t

            public bool IsBuyable { get { return !(ModType < ModuleTypes.DiscoveryScanner); } }

            public ShipModule(int id, ModuleTypes modtype, double mass, double power, string descr, string _)
            {
                ModuleID = id; Mass = mass; Power = power; TranslatedModName = EnglishModName = descr; ModType = modtype;
            }

            public string Info { get { return "??"; } }
            public string InfoMassPower(bool mass)
            {
                return "";
                //string i = (Info ?? "").AppendPrePad(Power > 0 ? ("Power:" + Power.ToString("0.#MW")) : "", ", ");
                //if (mass)
                //    return i.AppendPrePad(Mass > 0 ? ("Mass:" + Mass.ToString("0.#t")) : "", ", ");
                //else
                //    return i;
            }

            //public override string ToString()
            //{
            //    string i = Info == null ? "null" : $"\"{Info}\"";
            //    return $"{ModuleID},{Mass:0.##},{Power:0.##},{i},\"{EnglishModName}\",{EnglishModTypeString}";
            //    //return $"{ModuleID}, {Mass:0.##}, {Power:0.##}, {i}, \"{ModName}\", {mt}";
            //}

        };

        #endregion

        // History
        // Originally from coriolis, but now not.  Synced with Frontier data
        // Nov 1/12/23 synched with EDDI data, with outfitting.csv

        #region Ship Modules

        public static Dictionary<string, ShipModule> shipmodules = new Dictionary<string, ShipModule>
        {
 
          // Armour, in ID order

            { "sidewinder_armour_grade1", new ShipModule(128049250, ShipModule.ModuleTypes.LightweightAlloy,0,0,"Sidewinder Lightweight Armour","Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "sidewinder_armour_grade2", new ShipModule(128049251, ShipModule.ModuleTypes.ReinforcedAlloy,2,0,"Sidewinder Reinforced Armour","Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "sidewinder_armour_grade3", new ShipModule(128049252, ShipModule.ModuleTypes.MilitaryGradeComposite, 4, 0, "Sidewinder Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "sidewinder_armour_mirrored", new ShipModule(128049253, ShipModule.ModuleTypes.MirroredSurfaceComposite, 4, 0, "Sidewinder Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "sidewinder_armour_reactive", new ShipModule(128049254, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 4, 0, "Sidewinder Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "eagle_armour_grade1", new ShipModule(128049256, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Eagle Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "eagle_armour_grade2", new ShipModule(128049257, ShipModule.ModuleTypes.ReinforcedAlloy, 4, 0, "Eagle Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "eagle_armour_grade3", new ShipModule(128049258, ShipModule.ModuleTypes.MilitaryGradeComposite, 8, 0, "Eagle Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "eagle_armour_mirrored", new ShipModule(128049259, ShipModule.ModuleTypes.MirroredSurfaceComposite, 8, 0, "Eagle Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "eagle_armour_reactive", new ShipModule(128049260, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 8, 0, "Eagle Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "hauler_armour_grade1", new ShipModule(128049262, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Hauler Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "hauler_armour_grade2", new ShipModule(128049263, ShipModule.ModuleTypes.ReinforcedAlloy, 1, 0, "Hauler Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "hauler_armour_grade3", new ShipModule(128049264, ShipModule.ModuleTypes.MilitaryGradeComposite, 2, 0, "Hauler Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "hauler_armour_mirrored", new ShipModule(128049265, ShipModule.ModuleTypes.MirroredSurfaceComposite, 2, 0, "Hauler Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "hauler_armour_reactive", new ShipModule(128049266, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 2, 0, "Hauler Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "adder_armour_grade1", new ShipModule(128049268, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Adder Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "adder_armour_grade2", new ShipModule(128049269, ShipModule.ModuleTypes.ReinforcedAlloy, 3, 0, "Adder Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "adder_armour_grade3", new ShipModule(128049270, ShipModule.ModuleTypes.MilitaryGradeComposite, 5, 0, "Adder Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "adder_armour_mirrored", new ShipModule(128049271, ShipModule.ModuleTypes.MirroredSurfaceComposite, 5, 0, "Adder Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "adder_armour_reactive", new ShipModule(128049272, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 5, 0, "Adder Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "viper_armour_grade1", new ShipModule(128049274, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Viper Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "viper_armour_grade2", new ShipModule(128049275, ShipModule.ModuleTypes.ReinforcedAlloy, 5, 0, "Viper Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "viper_armour_grade3", new ShipModule(128049276, ShipModule.ModuleTypes.MilitaryGradeComposite, 9, 0, "Viper Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "viper_armour_mirrored", new ShipModule(128049277, ShipModule.ModuleTypes.MirroredSurfaceComposite, 9, 0, "Viper Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "viper_armour_reactive", new ShipModule(128049278, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 9, 0, "Viper Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "cobramkiii_armour_grade1", new ShipModule(128049280, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Cobra Mk III Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "cobramkiii_armour_grade2", new ShipModule(128049281, ShipModule.ModuleTypes.ReinforcedAlloy, 14, 0, "Cobra Mk III Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "cobramkiii_armour_grade3", new ShipModule(128049282, ShipModule.ModuleTypes.MilitaryGradeComposite, 27, 0, "Cobra Mk III Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "cobramkiii_armour_mirrored", new ShipModule(128049283, ShipModule.ModuleTypes.MirroredSurfaceComposite, 27, 0, "Cobra Mk III Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "cobramkiii_armour_reactive", new ShipModule(128049284, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 27, 0, "Cobra Mk III Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "type6_armour_grade1", new ShipModule(128049286, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Type-6 Transporter Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "type6_armour_grade2", new ShipModule(128049287, ShipModule.ModuleTypes.ReinforcedAlloy, 12, 0, "Type-6 Transporter Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "type6_armour_grade3", new ShipModule(128049288, ShipModule.ModuleTypes.MilitaryGradeComposite, 23, 0, "Type-6 Transporter Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "type6_armour_mirrored", new ShipModule(128049289, ShipModule.ModuleTypes.MirroredSurfaceComposite, 23, 0, "Type-6 Transporter Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "type6_armour_reactive", new ShipModule(128049290, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 23, 0, "Type-6 Transporter Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "dolphin_armour_grade1", new ShipModule(128049292, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Dolphin Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "dolphin_armour_grade2", new ShipModule(128049293, ShipModule.ModuleTypes.ReinforcedAlloy, 32, 0, "Dolphin Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "dolphin_armour_grade3", new ShipModule(128049294, ShipModule.ModuleTypes.MilitaryGradeComposite, 63, 0, "Dolphin Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "dolphin_armour_mirrored", new ShipModule(128049295, ShipModule.ModuleTypes.MirroredSurfaceComposite, 63, 0, "Dolphin Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "dolphin_armour_reactive", new ShipModule(128049296, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 63, 0, "Dolphin Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "type7_armour_grade1", new ShipModule(128049298, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Type-7 Transporter Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "type7_armour_grade2", new ShipModule(128049299, ShipModule.ModuleTypes.ReinforcedAlloy, 32, 0, "Type-7 Transporter Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "type7_armour_grade3", new ShipModule(128049300, ShipModule.ModuleTypes.MilitaryGradeComposite, 63, 0, "Type-7 Transporter Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "type7_armour_mirrored", new ShipModule(128049301, ShipModule.ModuleTypes.MirroredSurfaceComposite, 63, 0, "Type-7 Transporter Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "type7_armour_reactive", new ShipModule(128049302, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 63, 0, "Type-7 Transporter Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "asp_armour_grade1", new ShipModule(128049304, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Asp Explorer Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "asp_armour_grade2", new ShipModule(128049305, ShipModule.ModuleTypes.ReinforcedAlloy, 21, 0, "Asp Explorer Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "asp_armour_grade3", new ShipModule(128049306, ShipModule.ModuleTypes.MilitaryGradeComposite, 42, 0, "Asp Explorer Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "asp_armour_mirrored", new ShipModule(128049307, ShipModule.ModuleTypes.MirroredSurfaceComposite, 42, 0, "Asp Explorer Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "asp_armour_reactive", new ShipModule(128049308, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 42, 0, "Asp Explorer Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "vulture_armour_grade1", new ShipModule(128049310, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Vulture Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "vulture_armour_grade2", new ShipModule(128049311, ShipModule.ModuleTypes.ReinforcedAlloy, 17, 0, "Vulture Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "vulture_armour_grade3", new ShipModule(128049312, ShipModule.ModuleTypes.MilitaryGradeComposite, 35, 0, "Vulture Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "vulture_armour_mirrored", new ShipModule(128049313, ShipModule.ModuleTypes.MirroredSurfaceComposite, 35, 0, "Vulture Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "vulture_armour_reactive", new ShipModule(128049314, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 35, 0, "Vulture Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "empire_trader_armour_grade1", new ShipModule(128049316, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Imperial Clipper Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "empire_trader_armour_grade2", new ShipModule(128049317, ShipModule.ModuleTypes.ReinforcedAlloy, 30, 0, "Imperial Clipper Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "empire_trader_armour_grade3", new ShipModule(128049318, ShipModule.ModuleTypes.MilitaryGradeComposite, 60, 0, "Imperial Clipper Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "empire_trader_armour_mirrored", new ShipModule(128049319, ShipModule.ModuleTypes.MirroredSurfaceComposite, 60, 0, "Imperial Clipper Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "empire_trader_armour_reactive", new ShipModule(128049320, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 60, 0, "Imperial Clipper Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "federation_dropship_armour_grade1", new ShipModule(128049322, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Federal Dropship Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "federation_dropship_armour_grade2", new ShipModule(128049323, ShipModule.ModuleTypes.ReinforcedAlloy, 44, 0, "Federal Dropship Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "federation_dropship_armour_grade3", new ShipModule(128049324, ShipModule.ModuleTypes.MilitaryGradeComposite, 87, 0, "Federal Dropship Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "federation_dropship_armour_mirrored", new ShipModule(128049325, ShipModule.ModuleTypes.MirroredSurfaceComposite, 87, 0, "Federal Dropship Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "federation_dropship_armour_reactive", new ShipModule(128049326, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 87, 0, "Federal Dropship Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "orca_armour_grade1", new ShipModule(128049328, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Orca Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "orca_armour_grade2", new ShipModule(128049329, ShipModule.ModuleTypes.ReinforcedAlloy, 21, 0, "Orca Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "orca_armour_grade3", new ShipModule(128049330, ShipModule.ModuleTypes.MilitaryGradeComposite, 87, 0, "Orca Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "orca_armour_mirrored", new ShipModule(128049331, ShipModule.ModuleTypes.MirroredSurfaceComposite, 87, 0, "Orca Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "orca_armour_reactive", new ShipModule(128049332, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 87, 0, "Orca Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "type9_armour_grade1", new ShipModule(128049334, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Type-9 Heavy Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "type9_armour_grade2", new ShipModule(128049335, ShipModule.ModuleTypes.ReinforcedAlloy, 75, 0, "Type-9 Heavy Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "type9_armour_grade3", new ShipModule(128049336, ShipModule.ModuleTypes.MilitaryGradeComposite, 150, 0, "Type-9 Heavy Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "type9_armour_mirrored", new ShipModule(128049337, ShipModule.ModuleTypes.MirroredSurfaceComposite, 150, 0, "Type-9 Heavy Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "type9_armour_reactive", new ShipModule(128049338, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 150, 0, "Type-9 Heavy Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "python_armour_grade1", new ShipModule(128049340, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Python Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "python_armour_grade2", new ShipModule(128049341, ShipModule.ModuleTypes.ReinforcedAlloy, 26, 0, "Python Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "python_armour_grade3", new ShipModule(128049342, ShipModule.ModuleTypes.MilitaryGradeComposite, 53, 0, "Python Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "python_armour_mirrored", new ShipModule(128049343, ShipModule.ModuleTypes.MirroredSurfaceComposite, 53, 0, "Python Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "python_armour_reactive", new ShipModule(128049344, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 53, 0, "Python Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "belugaliner_armour_grade1", new ShipModule(128049346, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Beluga Liner Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "belugaliner_armour_grade2", new ShipModule(128049347, ShipModule.ModuleTypes.ReinforcedAlloy, 83, 0, "Beluga Liner Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "belugaliner_armour_grade3", new ShipModule(128049348, ShipModule.ModuleTypes.MilitaryGradeComposite, 165, 0, "Beluga Liner Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "belugaliner_armour_mirrored", new ShipModule(128049349, ShipModule.ModuleTypes.MirroredSurfaceComposite, 165, 0, "Beluga Liner Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "belugaliner_armour_reactive", new ShipModule(128049350, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 165, 0, "Beluga Liner Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "ferdelance_armour_grade1", new ShipModule(128049352, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Fer-de-Lance Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "ferdelance_armour_grade2", new ShipModule(128049353, ShipModule.ModuleTypes.ReinforcedAlloy, 19, 0, "Fer-de-Lance Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "ferdelance_armour_grade3", new ShipModule(128049354, ShipModule.ModuleTypes.MilitaryGradeComposite, 38, 0, "Fer-de-Lance Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "ferdelance_armour_mirrored", new ShipModule(128049355, ShipModule.ModuleTypes.MirroredSurfaceComposite, 38, 0, "Fer-de-Lance Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "ferdelance_armour_reactive", new ShipModule(128049356, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 38, 0, "Fer-de-Lance Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "anaconda_armour_grade1", new ShipModule(128049364, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Anaconda Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "anaconda_armour_grade2", new ShipModule(128049365, ShipModule.ModuleTypes.ReinforcedAlloy, 30, 0, "Anaconda Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "anaconda_armour_grade3", new ShipModule(128049366, ShipModule.ModuleTypes.MilitaryGradeComposite, 60, 0, "Anaconda Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "anaconda_armour_mirrored", new ShipModule(128049367, ShipModule.ModuleTypes.MirroredSurfaceComposite, 60, 0, "Anaconda Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "anaconda_armour_reactive", new ShipModule(128049368, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 60, 0, "Anaconda Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "federation_corvette_armour_grade1", new ShipModule(128049370, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Federal Corvette Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "federation_corvette_armour_grade2", new ShipModule(128049371, ShipModule.ModuleTypes.ReinforcedAlloy, 30, 0, "Federal Corvette Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "federation_corvette_armour_grade3", new ShipModule(128049372, ShipModule.ModuleTypes.MilitaryGradeComposite, 60, 0, "Federal Corvette Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "federation_corvette_armour_mirrored", new ShipModule(128049373, ShipModule.ModuleTypes.MirroredSurfaceComposite, 60, 0, "Federal Corvette Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "federation_corvette_armour_reactive", new ShipModule(128049374, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 60, 0, "Federal Corvette Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "cutter_armour_grade1", new ShipModule(128049376, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Imperial Cutter Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "cutter_armour_grade2", new ShipModule(128049377, ShipModule.ModuleTypes.ReinforcedAlloy, 30, 0, "Imperial Cutter Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "cutter_armour_grade3", new ShipModule(128049378, ShipModule.ModuleTypes.MilitaryGradeComposite, 60, 0, "Imperial Cutter Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "cutter_armour_mirrored", new ShipModule(128049379, ShipModule.ModuleTypes.MirroredSurfaceComposite, 60, 0, "Imperial Cutter Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "cutter_armour_reactive", new ShipModule(128049380, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 60, 0, "Imperial Cutter Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "diamondbackxl_armour_grade1", new ShipModule(128671832, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Diamondback Explorer Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "diamondbackxl_armour_grade2", new ShipModule(128671833, ShipModule.ModuleTypes.ReinforcedAlloy, 23, 0, "Diamondback Explorer Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "diamondbackxl_armour_grade3", new ShipModule(128671834, ShipModule.ModuleTypes.MilitaryGradeComposite, 47, 0, "Diamondback Explorer Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "diamondbackxl_armour_mirrored", new ShipModule(128671835, ShipModule.ModuleTypes.MirroredSurfaceComposite, 26, 0, "Diamondback Explorer Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "diamondbackxl_armour_reactive", new ShipModule(128671836, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 47, 0, "Diamondback Explorer Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },


            { "empire_eagle_armour_grade1", new ShipModule(128672140, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Imperial Eagle Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "empire_eagle_armour_grade2", new ShipModule(128672141, ShipModule.ModuleTypes.ReinforcedAlloy, 4, 0, "Imperial Eagle Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "empire_eagle_armour_grade3", new ShipModule(128672142, ShipModule.ModuleTypes.MilitaryGradeComposite, 8, 0, "Imperial Eagle Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "empire_eagle_armour_mirrored", new ShipModule(128672143, ShipModule.ModuleTypes.MirroredSurfaceComposite, 8, 0, "Imperial Eagle Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "empire_eagle_armour_reactive", new ShipModule(128672144, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 8, 0, "Imperial Eagle Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "federation_dropship_mkii_armour_grade1", new ShipModule(128672147, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Federal Assault Ship Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "federation_dropship_mkii_armour_grade2", new ShipModule(128672148, ShipModule.ModuleTypes.ReinforcedAlloy, 44, 0, "Federal Assault Ship Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "federation_dropship_mkii_armour_grade3", new ShipModule(128672149, ShipModule.ModuleTypes.MilitaryGradeComposite, 87, 0, "Federal Assault Ship Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "federation_dropship_mkii_armour_mirrored", new ShipModule(128672150, ShipModule.ModuleTypes.MirroredSurfaceComposite, 87, 0, "Federal Assault Ship Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "federation_dropship_mkii_armour_reactive", new ShipModule(128672151, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 87, 0, "Federal Assault Ship Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "federation_gunship_armour_grade1", new ShipModule(128672154, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Federal Gunship Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "federation_gunship_armour_grade2", new ShipModule(128672155, ShipModule.ModuleTypes.ReinforcedAlloy, 44, 0, "Federal Gunship Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "federation_gunship_armour_grade3", new ShipModule(128672156, ShipModule.ModuleTypes.MilitaryGradeComposite, 87, 0, "Federal Gunship Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "federation_gunship_armour_mirrored", new ShipModule(128672157, ShipModule.ModuleTypes.MirroredSurfaceComposite, 87, 0, "Federal Gunship Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "federation_gunship_armour_reactive", new ShipModule(128672158, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 87, 0, "Federal Gunship Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "viper_mkiv_armour_grade1", new ShipModule(128672257, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Viper Mk IV Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "viper_mkiv_armour_grade2", new ShipModule(128672258, ShipModule.ModuleTypes.ReinforcedAlloy, 5, 0, "Viper Mk IV Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "viper_mkiv_armour_grade3", new ShipModule(128672259, ShipModule.ModuleTypes.MilitaryGradeComposite, 9, 0, "Viper Mk IV Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "viper_mkiv_armour_mirrored", new ShipModule(128672260, ShipModule.ModuleTypes.MirroredSurfaceComposite, 9, 0, "Viper Mk IV Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "viper_mkiv_armour_reactive", new ShipModule(128672261, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 9, 0, "Viper Mk IV Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "cobramkiv_armour_grade1", new ShipModule(128672264, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Cobra Mk IV Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "cobramkiv_armour_grade2", new ShipModule(128672265, ShipModule.ModuleTypes.ReinforcedAlloy, 14, 0, "Cobra Mk IV Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "cobramkiv_armour_grade3", new ShipModule(128672266, ShipModule.ModuleTypes.MilitaryGradeComposite, 27, 0, "Cobra Mk IV Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "cobramkiv_armour_mirrored", new ShipModule(128672267, ShipModule.ModuleTypes.MirroredSurfaceComposite, 27, 0, "Cobra Mk IV Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "cobramkiv_armour_reactive", new ShipModule(128672268, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 27, 0, "Cobra Mk IV Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "independant_trader_armour_grade1", new ShipModule(128672271, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Keelback Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "independant_trader_armour_grade2", new ShipModule(128672272, ShipModule.ModuleTypes.ReinforcedAlloy, 12, 0, "Keelback Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "independant_trader_armour_grade3", new ShipModule(128672273, ShipModule.ModuleTypes.MilitaryGradeComposite, 23, 0, "Keelback Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "independant_trader_armour_mirrored", new ShipModule(128672274, ShipModule.ModuleTypes.MirroredSurfaceComposite, 23, 0, "Keelback Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "independant_trader_armour_reactive", new ShipModule(128672275, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 23, 0, "Keelback Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "asp_scout_armour_grade1", new ShipModule(128672278, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Asp Scout Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "asp_scout_armour_grade2", new ShipModule(128672279, ShipModule.ModuleTypes.ReinforcedAlloy, 21, 0, "Asp Scout Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "asp_scout_armour_grade3", new ShipModule(128672280, ShipModule.ModuleTypes.MilitaryGradeComposite, 42, 0, "Asp Scout Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "asp_scout_armour_mirrored", new ShipModule(128672281, ShipModule.ModuleTypes.MirroredSurfaceComposite, 42, 0, "Asp Scout Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "asp_scout_armour_reactive", new ShipModule(128672282, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 42, 0, "Asp Scout Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },


            { "krait_mkii_armour_grade1", new ShipModule(128816569, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Krait Mk II Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "krait_mkii_armour_grade2", new ShipModule(128816570, ShipModule.ModuleTypes.ReinforcedAlloy, 36, 0, "Krait Mk II Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "krait_mkii_armour_grade3", new ShipModule(128816571, ShipModule.ModuleTypes.MilitaryGradeComposite, 67, 0, "Krait Mk II Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "krait_mkii_armour_mirrored", new ShipModule(128816572, ShipModule.ModuleTypes.MirroredSurfaceComposite, 67, 0, "Krait Mk II Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "krait_mkii_armour_reactive", new ShipModule(128816573, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 67, 0, "Krait Mk II Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "typex_armour_grade1", new ShipModule(128816576, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Alliance Chieftain Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "typex_armour_grade2", new ShipModule(128816577, ShipModule.ModuleTypes.ReinforcedAlloy, 40, 0, "Alliance Chieftain Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "typex_armour_grade3", new ShipModule(128816578, ShipModule.ModuleTypes.MilitaryGradeComposite, 78, 0, "Alliance Chieftain Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "typex_armour_mirrored", new ShipModule(128816579, ShipModule.ModuleTypes.MirroredSurfaceComposite, 78, 0, "Alliance Chieftain Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "typex_armour_reactive", new ShipModule(128816580, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 78, 0, "Alliance Chieftain Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "typex_2_armour_grade1", new ShipModule(128816583, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Alliance Crusader Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "typex_2_armour_grade2", new ShipModule(128816584, ShipModule.ModuleTypes.ReinforcedAlloy, 40, 0, "Alliance Crusader Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "typex_2_armour_grade3", new ShipModule(128816585, ShipModule.ModuleTypes.MilitaryGradeComposite, 78, 0, "Alliance Crusader Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "typex_2_armour_mirrored", new ShipModule(128816586, ShipModule.ModuleTypes.MirroredSurfaceComposite, 78, 0, "Alliance Crusader Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "typex_2_armour_reactive", new ShipModule(128816587, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 78, 0, "Alliance Crusader Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "typex_3_armour_grade1", new ShipModule(128816590, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Alliance Challenger Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "typex_3_armour_grade2", new ShipModule(128816591, ShipModule.ModuleTypes.ReinforcedAlloy, 40, 0, "Alliance Challenger Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "typex_3_armour_grade3", new ShipModule(128816592, ShipModule.ModuleTypes.MilitaryGradeComposite, 78, 0, "Alliance Challenger Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "typex_3_armour_mirrored", new ShipModule(128816593, ShipModule.ModuleTypes.MirroredSurfaceComposite, 78, 0, "Alliance Challenger Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "typex_3_armour_reactive", new ShipModule(128816594, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 78, 0, "Alliance Challenger Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "diamondback_armour_grade1", new ShipModule(128671218, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Diamondback Scout Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "diamondback_armour_grade2", new ShipModule(128671219, ShipModule.ModuleTypes.ReinforcedAlloy, 13, 0, "Diamondback Scout Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "diamondback_armour_grade3", new ShipModule(128671220, ShipModule.ModuleTypes.MilitaryGradeComposite, 26, 0, "Diamondback Scout Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "diamondback_armour_mirrored", new ShipModule(128671221, ShipModule.ModuleTypes.MirroredSurfaceComposite, 26, 0, "Diamondback Scout Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "diamondback_armour_reactive", new ShipModule(128671222, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 26, 0, "Diamondback Scout Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "empire_courier_armour_grade1", new ShipModule(128671224, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Imperial Courier Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "empire_courier_armour_grade2", new ShipModule(128671225, ShipModule.ModuleTypes.ReinforcedAlloy, 4, 0, "Imperial Courier Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "empire_courier_armour_grade3", new ShipModule(128671226, ShipModule.ModuleTypes.MilitaryGradeComposite, 8, 0, "Imperial Courier Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "empire_courier_armour_mirrored", new ShipModule(128671227, ShipModule.ModuleTypes.MirroredSurfaceComposite, 8, 0, "Imperial Courier Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "empire_courier_armour_reactive", new ShipModule(128671228, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 8, 0, "Imperial Courier Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "type9_military_armour_grade1", new ShipModule(128785621, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Type-10 Defender Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "type9_military_armour_grade2", new ShipModule(128785622, ShipModule.ModuleTypes.ReinforcedAlloy, 75, 0, "Type-10 Defender Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "type9_military_armour_grade3", new ShipModule(128785623, ShipModule.ModuleTypes.MilitaryGradeComposite, 150, 0, "Type-10 Defender Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "type9_military_armour_mirrored", new ShipModule(128785624, ShipModule.ModuleTypes.MirroredSurfaceComposite, 150, 0, "Type-10 Defender Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "type9_military_armour_reactive", new ShipModule(128785625, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 150, 0, "Type-10 Defender Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "krait_light_armour_grade1", new ShipModule(128839283, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Krait Phantom Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "krait_light_armour_grade2", new ShipModule(128839284, ShipModule.ModuleTypes.ReinforcedAlloy, 26, 0, "Krait Phantom Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "krait_light_armour_grade3", new ShipModule(128839285, ShipModule.ModuleTypes.MilitaryGradeComposite, 53, 0, "Krait Phantom Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "krait_light_armour_mirrored", new ShipModule(128839286, ShipModule.ModuleTypes.MirroredSurfaceComposite, 53, 0, "Krait Phantom Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "krait_light_armour_reactive", new ShipModule(128839287, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 53, 0, "Krait Phantom Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "mamba_armour_grade1", new ShipModule(128915981, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Mamba Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "mamba_armour_grade2", new ShipModule(128915982, ShipModule.ModuleTypes.ReinforcedAlloy, 19, 0, "Mamba Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "mamba_armour_grade3", new ShipModule(128915983, ShipModule.ModuleTypes.MilitaryGradeComposite, 38, 0, "Mamba Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "mamba_armour_mirrored", new ShipModule(128915984, ShipModule.ModuleTypes.MirroredSurfaceComposite, 38, 0, "Mamba Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "mamba_armour_reactive", new ShipModule(128915985, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 38, 0, "Mamba Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            { "python_nx_armour_grade1", new ShipModule(-1, ShipModule.ModuleTypes.LightweightAlloy, 0, 0, "Python Mk II Lightweight Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "python_nx_armour_grade2", new ShipModule(-1, ShipModule.ModuleTypes.ReinforcedAlloy, 19, 0, "Python Mk II Reinforced Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "python_nx_armour_grade3", new ShipModule(-1, ShipModule.ModuleTypes.MilitaryGradeComposite, 38, 0, "Python Mk II Military Armour", "Explosive:-40%, Kinetic:-20%, Thermal:0%") },
            { "python_nx_armour_mirrored", new ShipModule(-1, ShipModule.ModuleTypes.MirroredSurfaceComposite, 38, 0, "Python Mk II Mirrored Surface Composite Armour", "Explosive:-50%, Kinetic:-75%, Thermal:50%") },
            { "python_nx_armour_reactive", new ShipModule(-1, ShipModule.ModuleTypes.ReactiveSurfaceComposite, 38, 0, "Python Mk II Reactive Surface Composite Armour", "Explosive:20%, Kinetic:25%, Thermal:-40%") },

            // Auto field maint

            { "int_repairer_size1_class1", new ShipModule(128667598, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 0.54, "Auto Field Maintenance Class 1 Rating E", "Ammo:1000, Repair:12") },
            { "int_repairer_size1_class2", new ShipModule(128667606, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 0.72, "Auto Field Maintenance Class 1 Rating D", "Ammo:900, Repair:14.4") },
            { "int_repairer_size1_class3", new ShipModule(128667614, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 0.9, "Auto Field Maintenance Class 1 Rating C", "Ammo:1000, Repair:20") },
            { "int_repairer_size1_class4", new ShipModule(128667622, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 1.04, "Auto Field Maintenance Class 1 Rating B", "Ammo:1200, Repair:27.6") },
            { "int_repairer_size1_class5", new ShipModule(128667630, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 1.26, "Auto Field Maintenance Class 1 Rating A", "Ammo:1100, Repair:30.8") },
            { "int_repairer_size2_class1", new ShipModule(128667599, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 0.68, "Auto Field Maintenance Class 2 Rating E", "Ammo:2300, Repair:27.6") },
            { "int_repairer_size2_class2", new ShipModule(128667607, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 0.9, "Auto Field Maintenance Class 2 Rating D", "Ammo:2100, Repair:33.6") },
            { "int_repairer_size2_class3", new ShipModule(128667615, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 1.13, "Auto Field Maintenance Class 2 Rating C", "Ammo:2300, Repair:46") },
            { "int_repairer_size2_class4", new ShipModule(128667623, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 1.29, "Auto Field Maintenance Class 2 Rating B", "Ammo:2800, Repair:64.4") },
            { "int_repairer_size2_class5", new ShipModule(128667631, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 1.58, "Auto Field Maintenance Class 2 Rating A", "Ammo:2500, Repair:70") },
            { "int_repairer_size3_class1", new ShipModule(128667600, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 0.81, "Auto Field Maintenance Class 3 Rating E", "Ammo:3600, Repair:43.2") },
            { "int_repairer_size3_class2", new ShipModule(128667608, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 1.08, "Auto Field Maintenance Class 3 Rating D", "Ammo:3200, Repair:51.2") },
            { "int_repairer_size3_class3", new ShipModule(128667616, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 1.35, "Auto Field Maintenance Class 3 Rating C", "Ammo:3600, Repair:72") },
            { "int_repairer_size3_class4", new ShipModule(128667624, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 1.55, "Auto Field Maintenance Class 3 Rating B", "Ammo:4300, Repair:98.9") },
            { "int_repairer_size3_class5", new ShipModule(128667632, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 1.89, "Auto Field Maintenance Class 3 Rating A", "Ammo:4000, Repair:112") },
            { "int_repairer_size4_class1", new ShipModule(128667601, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 0.99, "Auto Field Maintenance Class 4 Rating E", "Ammo:4900, Repair:58.8") },
            { "int_repairer_size4_class2", new ShipModule(128667609, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 1.32, "Auto Field Maintenance Class 4 Rating D", "Ammo:4400, Repair:70.4") },
            { "int_repairer_size4_class3", new ShipModule(128667617, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 1.65, "Auto Field Maintenance Class 4 Rating C", "Ammo:4900, Repair:98") },
            { "int_repairer_size4_class4", new ShipModule(128667625, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 1.9, "Auto Field Maintenance Class 4 Rating B", "Ammo:5900, Repair:135.7") },
            { "int_repairer_size4_class5", new ShipModule(128667633, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 2.31, "Auto Field Maintenance Class 4 Rating A", "Ammo:5400, Repair:151.2") },
            { "int_repairer_size5_class1", new ShipModule(128667602, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 1.17, "Auto Field Maintenance Class 5 Rating E", "Ammo:6100, Repair:73.2") },
            { "int_repairer_size5_class2", new ShipModule(128667610, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 1.56, "Auto Field Maintenance Class 5 Rating D", "Ammo:5500, Repair:88") },
            { "int_repairer_size5_class3", new ShipModule(128667618, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 1.95, "Auto Field Maintenance Class 5 Rating C", "Ammo:6100, Repair:122") },
            { "int_repairer_size5_class4", new ShipModule(128667626, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 2.24, "Auto Field Maintenance Class 5 Rating B", "Ammo:7300, Repair:167.9") },
            { "int_repairer_size5_class5", new ShipModule(128667634, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 2.73, "Auto Field Maintenance Class 5 Rating A", "Ammo:6700, Repair:187.6") },
            { "int_repairer_size6_class1", new ShipModule(128667603, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 1.4, "Auto Field Maintenance Class 6 Rating E", "Ammo:7400, Repair:88.8") },
            { "int_repairer_size6_class2", new ShipModule(128667611, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 1.86, "Auto Field Maintenance Class 6 Rating D", "Ammo:6700, Repair:107.2") },
            { "int_repairer_size6_class3", new ShipModule(128667619, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 2.33, "Auto Field Maintenance Class 6 Rating C", "Ammo:7400, Repair:148") },
            { "int_repairer_size6_class4", new ShipModule(128667627, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 2.67, "Auto Field Maintenance Class 6 Rating B", "Ammo:8900, Repair:204.7") },
            { "int_repairer_size6_class5", new ShipModule(128667635, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 3.26, "Auto Field Maintenance Class 6 Rating A", "Ammo:8100, Repair:226.8") },
            { "int_repairer_size7_class1", new ShipModule(128667604, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 1.58, "Auto Field Maintenance Class 7 Rating E", "Ammo:8700, Repair:104.4") },
            { "int_repairer_size7_class2", new ShipModule(128667612, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 2.1, "Auto Field Maintenance Class 7 Rating D", "Ammo:7800, Repair:124.8") },
            { "int_repairer_size7_class3", new ShipModule(128667620, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 2.63, "Auto Field Maintenance Class 7 Rating C", "Ammo:8700, Repair:174") },
            { "int_repairer_size7_class4", new ShipModule(128667628, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 3.02, "Auto Field Maintenance Class 7 Rating B", "Ammo:10400, Repair:239.2") },
            { "int_repairer_size7_class5", new ShipModule(128667636, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 3.68, "Auto Field Maintenance Class 7 Rating A", "Ammo:9600, Repair:268.8") },
            { "int_repairer_size8_class1", new ShipModule(128667605, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 1.8, "Auto Field Maintenance Class 8 Rating E", "Ammo:10000, Repair:120") },
            { "int_repairer_size8_class2", new ShipModule(128667613, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 2.4, "Auto Field Maintenance Class 8 Rating D", "Ammo:9000, Repair:144") },
            { "int_repairer_size8_class3", new ShipModule(128667621, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 3, "Auto Field Maintenance Class 8 Rating C", "Ammo:10000, Repair:200") },
            { "int_repairer_size8_class4", new ShipModule(128667629, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 3.45, "Auto Field Maintenance Class 8 Rating B", "Ammo:12000, Repair:276") },
            { "int_repairer_size8_class5", new ShipModule(128667637, ShipModule.ModuleTypes.AutoField_MaintenanceUnit, 0, 4.2, "Auto Field Maintenance Class 8 Rating A", "Ammo:11000, Repair:308") },

            // Beam lasers

            { "hpt_beamlaser_fixed_small", new ShipModule(128049428, ShipModule.ModuleTypes.BeamLaser, 2, 0.62, "Beam Laser Fixed Small", "Damage:9.8, Range:3000m, ThermL:3.5") },
            { "hpt_beamlaser_fixed_medium", new ShipModule(128049429, ShipModule.ModuleTypes.BeamLaser, 4, 1.01, "Beam Laser Fixed Medium", "Damage:16, Range:3000m, ThermL:5.1") },
            { "hpt_beamlaser_fixed_large", new ShipModule(128049430, ShipModule.ModuleTypes.BeamLaser, 8, 1.62, "Beam Laser Fixed Large", "Damage:25.8, Range:3000m, ThermL:7.2") },
            { "hpt_beamlaser_fixed_huge", new ShipModule(128049431, ShipModule.ModuleTypes.BeamLaser, 16, 2.61, "Beam Laser Fixed Huge", "Damage:41.4, Range:3000m, ThermL:9.9") },
            { "hpt_beamlaser_gimbal_small", new ShipModule(128049432, ShipModule.ModuleTypes.BeamLaser, 2, 0.6, "Beam Laser Gimbal Small", "Damage:7.7, Range:3000m, ThermL:3.6") },
            { "hpt_beamlaser_gimbal_medium", new ShipModule(128049433, ShipModule.ModuleTypes.BeamLaser, 4, 1, "Beam Laser Gimbal Medium", "Damage:12.5, Range:3000m, ThermL:5.3") },
            { "hpt_beamlaser_gimbal_large", new ShipModule(128049434, ShipModule.ModuleTypes.BeamLaser, 8, 1.6, "Beam Laser Gimbal Large", "Damage:20.3, Range:3000m, ThermL:7.6") },
            { "hpt_beamlaser_turret_small", new ShipModule(128049435, ShipModule.ModuleTypes.BeamLaser, 2, 0.57, "Beam Laser Turret Small", "Damage:5.4, Range:3000m, ThermL:2.4") },
            { "hpt_beamlaser_turret_medium", new ShipModule(128049436, ShipModule.ModuleTypes.BeamLaser, 4, 0.93, "Beam Laser Turret Medium", "Damage:8.8, Range:3000m, ThermL:3.5") },
            { "hpt_beamlaser_turret_large", new ShipModule(128049437, ShipModule.ModuleTypes.BeamLaser, 8, 1.51, "Beam Laser Turret Large", "Damage:14.3, Range:3000m, ThermL:5.1") },

            { "hpt_beamlaser_fixed_small_heat", new ShipModule(128671346, ShipModule.ModuleTypes.RetributorBeamLaser, 2, 0.62, "Beam Laser Fixed Small Heat", "Damage:4.9, Range:3000m, ThermL:2.7") },
            { "hpt_beamlaser_gimbal_huge", new ShipModule(128681994, ShipModule.ModuleTypes.BeamLaser, 16, 2.57, "Beam Laser Gimbal Huge", "Damage:32.7, Range:3000m, ThermL:10.6") },

            // burst laser

            { "hpt_pulselaserburst_fixed_small", new ShipModule(128049400, ShipModule.ModuleTypes.BurstLaser, 2, 0.65, "Burst Laser Fixed Small", "Damage:1.7, Range:3000m, ThermL:0.4") },
            { "hpt_pulselaserburst_fixed_medium", new ShipModule(128049401, ShipModule.ModuleTypes.BurstLaser, 4, 1.05, "Burst Laser Fixed Medium", "Damage:3.5, Range:3000m, ThermL:0.8") },
            { "hpt_pulselaserburst_fixed_large", new ShipModule(128049402, ShipModule.ModuleTypes.BurstLaser, 8, 1.66, "Burst Laser Fixed Large", "Damage:7.7, Range:3000m, ThermL:1.7") },
            { "hpt_pulselaserburst_fixed_huge", new ShipModule(128049403, ShipModule.ModuleTypes.BurstLaser, 16, 2.58, "Burst Laser Fixed Huge", "Damage:20.6, Range:3000m, ThermL:4.5") },
            { "hpt_pulselaserburst_gimbal_small", new ShipModule(128049404, ShipModule.ModuleTypes.BurstLaser, 2, 0.64, "Burst Laser Gimbal Small", "Damage:1.2, Range:3000m, ThermL:0.3") },
            { "hpt_pulselaserburst_gimbal_medium", new ShipModule(128049405, ShipModule.ModuleTypes.BurstLaser, 4, 1.04, "Burst Laser Gimbal Medium", "Damage:2.5, Range:3000m, ThermL:0.7") },
            { "hpt_pulselaserburst_gimbal_large", new ShipModule(128049406, ShipModule.ModuleTypes.BurstLaser, 8, 1.65, "Burst Laser Gimbal Large", "Damage:5.2, Range:3000m, ThermL:1.4") },
            { "hpt_pulselaserburst_turret_small", new ShipModule(128049407, ShipModule.ModuleTypes.BurstLaser, 2, 0.6, "Burst Laser Turret Small", "Damage:0.9, Range:3000m, ThermL:0.2") },
            { "hpt_pulselaserburst_turret_medium", new ShipModule(128049408, ShipModule.ModuleTypes.BurstLaser, 4, 0.98, "Burst Laser Turret Medium", "Damage:1.7, Range:3000m, ThermL:0.4") },
            { "hpt_pulselaserburst_turret_large", new ShipModule(128049409, ShipModule.ModuleTypes.BurstLaser, 8, 1.57, "Burst Laser Turret Large", "Damage:3.5, Range:3000m, ThermL:0.8") },


            { "hpt_pulselaserburst_gimbal_huge", new ShipModule(128727920, ShipModule.ModuleTypes.BurstLaser, 16, 2.59, "Burst Laser Gimbal Huge", "Damage:12.1, Range:3000m, ThermL:3.3") },

            { "hpt_pulselaserburst_fixed_small_scatter", new ShipModule(128671449, ShipModule.ModuleTypes.CytoscramblerBurstLaser, 2, 0.8, "Burst Laser Fixed Small Scatter", "Damage:3.6, Range:1000m, ThermL:0.3") },

            // Cannons

            { "hpt_cannon_fixed_small", new ShipModule(128049438, ShipModule.ModuleTypes.Cannon, 2, 0.34, "Cannon Fixed Small", "Ammo:120/6, Damage:22.5, Range:3000m, Speed:1200m/s, Reload:3s, ThermL:1.4") },
            { "hpt_cannon_fixed_medium", new ShipModule(128049439, ShipModule.ModuleTypes.Cannon, 4, 0.49, "Cannon Fixed Medium", "Ammo:120/6, Damage:36.5, Range:3500m, Speed:1051m/s, Reload:3s, ThermL:2.1") },
            { "hpt_cannon_fixed_large", new ShipModule(128049440, ShipModule.ModuleTypes.Cannon, 8, 0.67, "Cannon Fixed Large", "Ammo:120/6, Damage:54.9, Range:4000m, Speed:959m/s, Reload:3s, ThermL:3.2") },
            { "hpt_cannon_fixed_huge", new ShipModule(128049441, ShipModule.ModuleTypes.Cannon, 16, 0.92, "Cannon Fixed Huge", "Ammo:120/6, Damage:82.1, Range:4500m, Speed:900m/s, Reload:3s, ThermL:4.8") },
            { "hpt_cannon_gimbal_small", new ShipModule(128049442, ShipModule.ModuleTypes.Cannon, 2, 0.38, "Cannon Gimbal Small", "Ammo:100/5, Damage:16, Range:3000m, Speed:1000m/s, Reload:4s, ThermL:1.3") },
            { "hpt_cannon_gimbal_medium", new ShipModule(128049443, ShipModule.ModuleTypes.Cannon, 4, 0.54, "Cannon Gimbal Medium", "Ammo:100/5, Damage:24.5, Range:3500m, Speed:875m/s, Reload:4s, ThermL:1.9") },
            { "hpt_cannon_gimbal_huge", new ShipModule(128049444, ShipModule.ModuleTypes.Cannon, 16, 1.03, "Cannon Gimbal Huge", "Ammo:100/5, Damage:56.6, Range:4500m, Speed:750m/s, Reload:4s, ThermL:4.4") },
            { "hpt_cannon_turret_small", new ShipModule(128049445, ShipModule.ModuleTypes.Cannon, 2, 0.32, "Cannon Turret Small", "Ammo:100/5, Damage:12.8, Range:3000m, Speed:1000m/s, Reload:4s, ThermL:0.7") },
            { "hpt_cannon_turret_medium", new ShipModule(128049446, ShipModule.ModuleTypes.Cannon, 4, 0.45, "Cannon Turret Medium", "Ammo:100/5, Damage:19.8, Range:3500m, Speed:875m/s, Reload:4s, ThermL:1") },
            { "hpt_cannon_turret_large", new ShipModule(128049447, ShipModule.ModuleTypes.Cannon, 8, 0.64, "Cannon Turret Large", "Ammo:100/5, Damage:30.4, Range:4000m, Speed:800m/s, Reload:4s, ThermL:1.6") },

            { "hpt_cannon_gimbal_large", new ShipModule(128671120, ShipModule.ModuleTypes.Cannon, 8, 0.75, "Cannon Gimbal Large", "Ammo:100/5, Damage:37.4, Range:4000m, Speed:800m/s, Reload:4s, ThermL:2.9") },

            // Frag cannon

            { "hpt_slugshot_fixed_small", new ShipModule(128049448, ShipModule.ModuleTypes.FragmentCannon, 2, 0.45, "Fragment Cannon Fixed Small", "Ammo:180/3, Damage:1.4, Range:2000m, Speed:667m/s, Reload:5s, ThermL:0.4") },
            { "hpt_slugshot_fixed_medium", new ShipModule(128049449, ShipModule.ModuleTypes.FragmentCannon, 4, 0.74, "Fragment Cannon Fixed Medium", "Ammo:180/3, Damage:3, Range:2000m, Speed:667m/s, Reload:5s, ThermL:0.7") },
            { "hpt_slugshot_fixed_large", new ShipModule(128049450, ShipModule.ModuleTypes.FragmentCannon, 8, 1.02, "Fragment Cannon Fixed Large", "Ammo:180/3, Damage:4.6, Range:2000m, Speed:667m/s, Reload:5s, ThermL:1.1") },
            { "hpt_slugshot_gimbal_small", new ShipModule(128049451, ShipModule.ModuleTypes.FragmentCannon, 2, 0.59, "Fragment Cannon Gimbal Small", "Ammo:180/3, Damage:1, Range:2000m, Speed:667m/s, Reload:5s, ThermL:0.4") },
            { "hpt_slugshot_gimbal_medium", new ShipModule(128049452, ShipModule.ModuleTypes.FragmentCannon, 4, 1.03, "Fragment Cannon Gimbal Medium", "Ammo:180/3, Damage:2.3, Range:2000m, Speed:667m/s, Reload:5s, ThermL:0.8") },
            { "hpt_slugshot_turret_small", new ShipModule(128049453, ShipModule.ModuleTypes.FragmentCannon, 2, 0.42, "Fragment Cannon Turret Small", "Ammo:180/3, Damage:0.7, Range:2000m, Speed:667m/s, Reload:5s, ThermL:0.2") },
            { "hpt_slugshot_turret_medium", new ShipModule(128049454, ShipModule.ModuleTypes.FragmentCannon, 4, 0.79, "Fragment Cannon Turret Medium", "Ammo:180/3, Damage:1.7, Range:2000m, Speed:667m/s, Reload:5s, ThermL:0.4") },

            { "hpt_slugshot_gimbal_large", new ShipModule(128671321, ShipModule.ModuleTypes.FragmentCannon, 8, 1.55, "Fragment Cannon Gimbal Large", "Ammo:180/3, Damage:3.8, Range:2000m, Speed:667m/s, Reload:5s, ThermL:1.4") },
            { "hpt_slugshot_turret_large", new ShipModule(128671322, ShipModule.ModuleTypes.FragmentCannon, 8, 1.29, "Fragment Cannon Turret Large", "Ammo:180/3, Damage:3, Range:2000m, Speed:667m/s, Reload:5s, ThermL:0.7") },

            { "hpt_slugshot_fixed_large_range", new ShipModule(128671343, ShipModule.ModuleTypes.PacifierFrag_Cannon, 8, 1.02, "Fragment Cannon Fixed Large Range", "Ammo:180/3, Damage:4, Speed:1000m/s, Reload:5s, ThermL:1.1") },

            // Cargo racks

            { "int_cargorack_size1_class1", new ShipModule(128064338, ShipModule.ModuleTypes.CargoRack, 0, 0, "Cargo Rack Class 1 Rating E", "Size:2t") },
            { "int_cargorack_size2_class1", new ShipModule(128064339, ShipModule.ModuleTypes.CargoRack, 0, 0, "Cargo Rack Class 2 Rating E", "Size:4t") },
            { "int_cargorack_size3_class1", new ShipModule(128064340, ShipModule.ModuleTypes.CargoRack, 0, 0, "Cargo Rack Class 3 Rating E", "Size:8t") },
            { "int_cargorack_size4_class1", new ShipModule(128064341, ShipModule.ModuleTypes.CargoRack, 0, 0, "Cargo Rack Class 4 Rating E", "Size:16t") },
            { "int_cargorack_size5_class1", new ShipModule(128064342, ShipModule.ModuleTypes.CargoRack, 0, 0, "Cargo Rack Class 5 Rating E", "Size:32t") },
            { "int_cargorack_size6_class1", new ShipModule(128064343, ShipModule.ModuleTypes.CargoRack, 0, 0, "Cargo Rack Class 6 Rating E", "Size:64t") },
            { "int_cargorack_size7_class1", new ShipModule(128064344, ShipModule.ModuleTypes.CargoRack, 0, 0, "Cargo Rack Class 7 Rating E", "Size:128t") },
            { "int_cargorack_size8_class1", new ShipModule(128064345, ShipModule.ModuleTypes.CargoRack, 0, 0, "Cargo Rack Class 8 Rating E", "Size:256t") },

            { "int_cargorack_size2_class1_free", new ShipModule(128666643, ShipModule.ModuleTypes.CargoRack, 0, 0, "Cargo Rack Class 2 Rating E", "Size:4t") },

            { "int_corrosionproofcargorack_size1_class1", new ShipModule(128681641, ShipModule.ModuleTypes.CorrosionResistantCargoRack, 0, 0, "Corrosion Proof Cargo Rack Class 1 Rating E", "Size:1t") },
            { "int_corrosionproofcargorack_size1_class2", new ShipModule(128681992, ShipModule.ModuleTypes.CorrosionResistantCargoRack, 0, 0, "Corrosion Proof Cargo Rack Class 1 Rating F", "Size:2t") },

            { "int_corrosionproofcargorack_size4_class1", new ShipModule(128833944, ShipModule.ModuleTypes.CorrosionResistantCargoRack, 0, 0, "Corrosion Proof Cargo Rack Class 4 Rating E", "Size:16t") },
            { "int_corrosionproofcargorack_size5_class1", new ShipModule(128957069, ShipModule.ModuleTypes.CorrosionResistantCargoRack, 0, 0, "Corrosion Proof Cargo Rack Class 5 Rating E", "Size:32t") },
            { "int_corrosionproofcargorack_size6_class1", new ShipModule(999999906, ShipModule.ModuleTypes.CorrosionResistantCargoRack, 0, 0, "Corrosion Resistant Cargo Rack Class 6 Rating E", "Size:64t") },

            // Cargo scanner

            { "hpt_cargoscanner_size0_class1", new ShipModule(128662520, ShipModule.ModuleTypes.CargoScanner, 1.3, 0.2, "Cargo Scanner Rating E", "Range:2000m") },
            { "hpt_cargoscanner_size0_class2", new ShipModule(128662521, ShipModule.ModuleTypes.CargoScanner, 1.3, 0.4, "Cargo Scanner Rating D", "Range:2500m") },
            { "hpt_cargoscanner_size0_class3", new ShipModule(128662522, ShipModule.ModuleTypes.CargoScanner, 1.3, 0.8, "Cargo Scanner Rating C", "Range:3000m") },
            { "hpt_cargoscanner_size0_class4", new ShipModule(128662523, ShipModule.ModuleTypes.CargoScanner, 1.3, 1.6, "Cargo Scanner Rating B", "Range:3500m") },
            { "hpt_cargoscanner_size0_class5", new ShipModule(128662524, ShipModule.ModuleTypes.CargoScanner, 1.3, 3.2, "Cargo Scanner Rating A", "Range:4000m") },

            // Chaff, ECM

            { "hpt_chafflauncher_tiny", new ShipModule(128049513, ShipModule.ModuleTypes.ChaffLauncher, 1.3, 0.2, "Chaff Launcher", "Ammo:10/1, Reload:10s, ThermL:4") },
            { "hpt_electroniccountermeasure_tiny", new ShipModule(128049516, ShipModule.ModuleTypes.ElectronicCountermeasure, 1.3, 0.2, "Electronic Countermeasure Tiny", "Range:3000m, Reload:10s, ThermL:4") },
            { "hpt_heatsinklauncher_turret_tiny", new ShipModule(128049519, ShipModule.ModuleTypes.HeatSinkLauncher, 1.3, 0.2, "Heat Sink Launcher Turret Tiny", "Ammo:2/1, Reload:10s") },
            { "hpt_causticsinklauncher_turret_tiny", new ShipModule(129019262, ShipModule.ModuleTypes.CausticSinkLauncher, 1.3, 0.2, "Caustic Heat Sink Launcher Turret Tiny", "Ammo:2/1, Reload:10s") },
            { "hpt_plasmapointdefence_turret_tiny", new ShipModule(128049522, ShipModule.ModuleTypes.PointDefence, 0.5, 0.2, "Plasma Point Defence Turret Tiny", "Ammo:10000/12, Damage:0.2, Range:2500m, Speed:1000m/s, Reload:0.4s, ThermL:0.1") },

            // kill warrant

            { "hpt_crimescanner_size0_class1", new ShipModule(128662530, ShipModule.ModuleTypes.KillWarrantScanner, 1.3, 0.2, "Crime Scanner Rating E", "Range:2000m") },
            { "hpt_crimescanner_size0_class2", new ShipModule(128662531, ShipModule.ModuleTypes.KillWarrantScanner, 1.3, 0.4, "Crime Scanner Rating D", "Range:2500m") },
            { "hpt_crimescanner_size0_class3", new ShipModule(128662532, ShipModule.ModuleTypes.KillWarrantScanner, 1.3, 0.8, "Crime Scanner Rating C", "Range:3000m") },
            { "hpt_crimescanner_size0_class4", new ShipModule(128662533, ShipModule.ModuleTypes.KillWarrantScanner, 1.3, 1.6, "Crime Scanner Rating B", "Range:3500m") },
            { "hpt_crimescanner_size0_class5", new ShipModule(128662534, ShipModule.ModuleTypes.KillWarrantScanner, 1.3, 3.2, "Crime Scanner Rating A", "Range:4000m") },

            // surface scanner

            { "int_detailedsurfacescanner_tiny", new ShipModule(128666634, ShipModule.ModuleTypes.DetailedSurfaceScanner, 0, 0, "Detailed Surface Scanner", null) },

            // docking computer

            { "int_dockingcomputer_standard", new ShipModule(128049549, ShipModule.ModuleTypes.StandardDockingComputer, 0, 0.39, "Docking Computer Standard", null) },
            { "int_dockingcomputer_advanced", new ShipModule(128935155, ShipModule.ModuleTypes.AdvancedDockingComputer, 0, 0.45, "Docking Computer Advanced", null) },

            // figther bays

            { "int_fighterbay_size5_class1", new ShipModule(128727930, ShipModule.ModuleTypes.FighterHangar, 20, 0.25, "Fighter Hangar Class 5 Rating E", "Rebuilds:6t") },
            { "int_fighterbay_size6_class1", new ShipModule(128727931, ShipModule.ModuleTypes.FighterHangar, 40, 0.35, "Fighter Hangar Class 6 Rating E", "Rebuilds:8t") },
            { "int_fighterbay_size7_class1", new ShipModule(128727932, ShipModule.ModuleTypes.FighterHangar, 60, 0.35, "Fighter Hangar Class 7 Rating E", "Rebuilds:15t") },

            // flak

            { "hpt_flakmortar_fixed_medium", new ShipModule(128785626, ShipModule.ModuleTypes.RemoteReleaseFlakLauncher, 4, 1.2, "Flak Mortar Fixed Medium", "Ammo:32/1, Damage:34, Speed:550m/s, Reload:2s, ThermL:3.6") },
            { "hpt_flakmortar_turret_medium", new ShipModule(128793058, ShipModule.ModuleTypes.RemoteReleaseFlakLauncher, 4, 1.2, "Flak Mortar Turret Medium", "Ammo:32/1, Damage:34, Speed:550m/s, Reload:2s, ThermL:3.6") },

            // flechette

            { "hpt_flechettelauncher_fixed_medium", new ShipModule(128833996, ShipModule.ModuleTypes.RemoteReleaseFlechetteLauncher, 4, 1.2, "Flechette Launcher Fixed Medium", "Ammo:72/1, Damage:13, Speed:550m/s, Reload:2s, ThermL:3.6") },
            { "hpt_flechettelauncher_turret_medium", new ShipModule(128833997, ShipModule.ModuleTypes.RemoteReleaseFlechetteLauncher, 4, 1.2, "Flechette Launcher Turret Medium", "Ammo:72/1, Damage:13, Speed:550m/s, Reload:2s, ThermL:3.6") },

            // fsd interdictor

            { "int_fsdinterdictor_size1_class1", new ShipModule(128666704, ShipModule.ModuleTypes.FrameShiftDriveInterdictor, 1.3, 0.14, "FSD Interdictor Class 1 Rating E", null) },
            { "int_fsdinterdictor_size2_class1", new ShipModule(128666705, ShipModule.ModuleTypes.FrameShiftDriveInterdictor, 2.5, 0.17, "FSD Interdictor Class 2 Rating E", null) },
            { "int_fsdinterdictor_size3_class1", new ShipModule(128666706, ShipModule.ModuleTypes.FrameShiftDriveInterdictor, 5, 0.2, "FSD Interdictor Class 3 Rating E", null) },
            { "int_fsdinterdictor_size4_class1", new ShipModule(128666707, ShipModule.ModuleTypes.FrameShiftDriveInterdictor, 10, 0.25, "FSD Interdictor Class 4 Rating E", null) },
            { "int_fsdinterdictor_size1_class2", new ShipModule(128666708, ShipModule.ModuleTypes.FrameShiftDriveInterdictor, 0.5, 0.18, "FSD Interdictor Class 1 Rating D", null) },
            { "int_fsdinterdictor_size2_class2", new ShipModule(128666709, ShipModule.ModuleTypes.FrameShiftDriveInterdictor, 1, 0.22, "FSD Interdictor Class 2 Rating D", null) },
            { "int_fsdinterdictor_size3_class2", new ShipModule(128666710, ShipModule.ModuleTypes.FrameShiftDriveInterdictor, 2, 0.27, "FSD Interdictor Class 3 Rating D", null) },
            { "int_fsdinterdictor_size4_class2", new ShipModule(128666711, ShipModule.ModuleTypes.FrameShiftDriveInterdictor, 4, 0.33, "FSD Interdictor Class 4 Rating D", null) },
            { "int_fsdinterdictor_size1_class3", new ShipModule(128666712, ShipModule.ModuleTypes.FrameShiftDriveInterdictor, 1.3, 0.23, "FSD Interdictor Class 1 Rating C", null) },
            { "int_fsdinterdictor_size2_class3", new ShipModule(128666713, ShipModule.ModuleTypes.FrameShiftDriveInterdictor, 2.5, 0.28, "FSD Interdictor Class 2 Rating C", null) },
            { "int_fsdinterdictor_size3_class3", new ShipModule(128666714, ShipModule.ModuleTypes.FrameShiftDriveInterdictor, 5, 0.34, "FSD Interdictor Class 3 Rating C", null) },
            { "int_fsdinterdictor_size4_class3", new ShipModule(128666715, ShipModule.ModuleTypes.FrameShiftDriveInterdictor, 10, 0.41, "FSD Interdictor Class 4 Rating C", null) },
            { "int_fsdinterdictor_size1_class4", new ShipModule(128666716, ShipModule.ModuleTypes.FrameShiftDriveInterdictor, 2, 0.28, "FSD Interdictor Class 1 Rating B", null) },
            { "int_fsdinterdictor_size2_class4", new ShipModule(128666717, ShipModule.ModuleTypes.FrameShiftDriveInterdictor, 4, 0.34, "FSD Interdictor Class 2 Rating B", null) },
            { "int_fsdinterdictor_size3_class4", new ShipModule(128666718, ShipModule.ModuleTypes.FrameShiftDriveInterdictor, 8, 0.41, "FSD Interdictor Class 3 Rating B", null) },
            { "int_fsdinterdictor_size4_class4", new ShipModule(128666719, ShipModule.ModuleTypes.FrameShiftDriveInterdictor, 16, 0.49, "FSD Interdictor Class 4 Rating B", null) },
            { "int_fsdinterdictor_size1_class5", new ShipModule(128666720, ShipModule.ModuleTypes.FrameShiftDriveInterdictor, 1.3, 0.32, "FSD Interdictor Class 1 Rating A", null) },
            { "int_fsdinterdictor_size2_class5", new ShipModule(128666721, ShipModule.ModuleTypes.FrameShiftDriveInterdictor, 2.5, 0.39, "FSD Interdictor Class 2 Rating A", null) },
            { "int_fsdinterdictor_size3_class5", new ShipModule(128666722, ShipModule.ModuleTypes.FrameShiftDriveInterdictor, 5, 0.48, "FSD Interdictor Class 3 Rating A", null) },
            { "int_fsdinterdictor_size4_class5", new ShipModule(128666723, ShipModule.ModuleTypes.FrameShiftDriveInterdictor, 10, 0.57, "FSD Interdictor Class 4 Rating A", null) },

            // Fuel scoop

            { "int_fuelscoop_size1_class1", new ShipModule(128666644, ShipModule.ModuleTypes.FuelScoop, 0, 0.14, "Fuel Scoop Class 1 Rating E", "Rate:18") },
            { "int_fuelscoop_size2_class1", new ShipModule(128666645, ShipModule.ModuleTypes.FuelScoop, 0, 0.17, "Fuel Scoop Class 2 Rating E", "Rate:32") },
            { "int_fuelscoop_size3_class1", new ShipModule(128666646, ShipModule.ModuleTypes.FuelScoop, 0, 0.2, "Fuel Scoop Class 3 Rating E", "Rate:75") },
            { "int_fuelscoop_size4_class1", new ShipModule(128666647, ShipModule.ModuleTypes.FuelScoop, 0, 0.25, "Fuel Scoop Class 4 Rating E", "Rate:147") },
            { "int_fuelscoop_size5_class1", new ShipModule(128666648, ShipModule.ModuleTypes.FuelScoop, 0, 0.3, "Fuel Scoop Class 5 Rating E", "Rate:247") },
            { "int_fuelscoop_size6_class1", new ShipModule(128666649, ShipModule.ModuleTypes.FuelScoop, 0, 0.35, "Fuel Scoop Class 6 Rating E", "Rate:376") },
            { "int_fuelscoop_size7_class1", new ShipModule(128666650, ShipModule.ModuleTypes.FuelScoop, 0, 0.41, "Fuel Scoop Class 7 Rating E", "Rate:534") },
            { "int_fuelscoop_size8_class1", new ShipModule(128666651, ShipModule.ModuleTypes.FuelScoop, 0, 0.48, "Fuel Scoop Class 8 Rating E", "Rate:720") },
            { "int_fuelscoop_size1_class2", new ShipModule(128666652, ShipModule.ModuleTypes.FuelScoop, 0, 0.18, "Fuel Scoop Class 1 Rating D", "Rate:24") },
            { "int_fuelscoop_size2_class2", new ShipModule(128666653, ShipModule.ModuleTypes.FuelScoop, 0, 0.22, "Fuel Scoop Class 2 Rating D", "Rate:43") },
            { "int_fuelscoop_size3_class2", new ShipModule(128666654, ShipModule.ModuleTypes.FuelScoop, 0, 0.27, "Fuel Scoop Class 3 Rating D", "Rate:100") },
            { "int_fuelscoop_size4_class2", new ShipModule(128666655, ShipModule.ModuleTypes.FuelScoop, 0, 0.33, "Fuel Scoop Class 4 Rating D", "Rate:196") },
            { "int_fuelscoop_size5_class2", new ShipModule(128666656, ShipModule.ModuleTypes.FuelScoop, 0, 0.4, "Fuel Scoop Class 5 Rating D", "Rate:330") },
            { "int_fuelscoop_size6_class2", new ShipModule(128666657, ShipModule.ModuleTypes.FuelScoop, 0, 0.47, "Fuel Scoop Class 6 Rating D", "Rate:502") },
            { "int_fuelscoop_size7_class2", new ShipModule(128666658, ShipModule.ModuleTypes.FuelScoop, 0, 0.55, "Fuel Scoop Class 7 Rating D", "Rate:712") },
            { "int_fuelscoop_size8_class2", new ShipModule(128666659, ShipModule.ModuleTypes.FuelScoop, 0, 0.64, "Fuel Scoop Class 8 Rating D", "Rate:960") },
            { "int_fuelscoop_size1_class3", new ShipModule(128666660, ShipModule.ModuleTypes.FuelScoop, 0, 0.23, "Fuel Scoop Class 1 Rating C", "Rate:30") },
            { "int_fuelscoop_size2_class3", new ShipModule(128666661, ShipModule.ModuleTypes.FuelScoop, 0, 0.28, "Fuel Scoop Class 2 Rating C", "Rate:54") },
            { "int_fuelscoop_size3_class3", new ShipModule(128666662, ShipModule.ModuleTypes.FuelScoop, 0, 0.34, "Fuel Scoop Class 3 Rating C", "Rate:126") },
            { "int_fuelscoop_size4_class3", new ShipModule(128666663, ShipModule.ModuleTypes.FuelScoop, 0, 0.41, "Fuel Scoop Class 4 Rating C", "Rate:245") },
            { "int_fuelscoop_size5_class3", new ShipModule(128666664, ShipModule.ModuleTypes.FuelScoop, 0, 0.5, "Fuel Scoop Class 5 Rating C", "Rate:412") },
            { "int_fuelscoop_size6_class3", new ShipModule(128666665, ShipModule.ModuleTypes.FuelScoop, 0, 0.59, "Fuel Scoop Class 6 Rating C", "Rate:627") },
            { "int_fuelscoop_size7_class3", new ShipModule(128666666, ShipModule.ModuleTypes.FuelScoop, 0, 0.69, "Fuel Scoop Class 7 Rating C", "Rate:890") },
            { "int_fuelscoop_size8_class3", new ShipModule(128666667, ShipModule.ModuleTypes.FuelScoop, 0, 0.8, "Fuel Scoop Class 8 Rating C", "Rate:1200") },
            { "int_fuelscoop_size1_class4", new ShipModule(128666668, ShipModule.ModuleTypes.FuelScoop, 0, 0.28, "Fuel Scoop Class 1 Rating B", "Rate:36") },
            { "int_fuelscoop_size2_class4", new ShipModule(128666669, ShipModule.ModuleTypes.FuelScoop, 0, 0.34, "Fuel Scoop Class 2 Rating B", "Rate:65") },
            { "int_fuelscoop_size3_class4", new ShipModule(128666670, ShipModule.ModuleTypes.FuelScoop, 0, 0.41, "Fuel Scoop Class 3 Rating B", "Rate:151") },
            { "int_fuelscoop_size4_class4", new ShipModule(128666671, ShipModule.ModuleTypes.FuelScoop, 0, 0.49, "Fuel Scoop Class 4 Rating B", "Rate:294") },
            { "int_fuelscoop_size5_class4", new ShipModule(128666672, ShipModule.ModuleTypes.FuelScoop, 0, 0.6, "Fuel Scoop Class 5 Rating B", "Rate:494") },
            { "int_fuelscoop_size6_class4", new ShipModule(128666673, ShipModule.ModuleTypes.FuelScoop, 0, 0.71, "Fuel Scoop Class 6 Rating B", "Rate:752") },
            { "int_fuelscoop_size6_class5", new ShipModule(128666681, ShipModule.ModuleTypes.FuelScoop, 0, 0.83, "Fuel Scoop Class 6 Rating A", "Rate:878") },
            { "int_fuelscoop_size7_class4", new ShipModule(128666674, ShipModule.ModuleTypes.FuelScoop, 0, 0.83, "Fuel Scoop Class 7 Rating B", "Rate:1068") },
            { "int_fuelscoop_size8_class4", new ShipModule(128666675, ShipModule.ModuleTypes.FuelScoop, 0, 0.96, "Fuel Scoop Class 8 Rating B", "Rate:1440") },
            { "int_fuelscoop_size1_class5", new ShipModule(128666676, ShipModule.ModuleTypes.FuelScoop, 0, 0.32, "Fuel Scoop Class 1 Rating A", "Rate:42") },
            { "int_fuelscoop_size2_class5", new ShipModule(128666677, ShipModule.ModuleTypes.FuelScoop, 0, 0.39, "Fuel Scoop Class 2 Rating A", "Rate:75") },
            { "int_fuelscoop_size3_class5", new ShipModule(128666678, ShipModule.ModuleTypes.FuelScoop, 0, 0.48, "Fuel Scoop Class 3 Rating A", "Rate:176") },
            { "int_fuelscoop_size4_class5", new ShipModule(128666679, ShipModule.ModuleTypes.FuelScoop, 0, 0.57, "Fuel Scoop Class 4 Rating A", "Rate:342") },
            { "int_fuelscoop_size5_class5", new ShipModule(128666680, ShipModule.ModuleTypes.FuelScoop, 0, 0.7, "Fuel Scoop Class 5 Rating A", "Rate:577") },
            { "int_fuelscoop_size7_class5", new ShipModule(128666682, ShipModule.ModuleTypes.FuelScoop, 0, 0.97, "Fuel Scoop Class 7 Rating A", "Rate:1245") },
            { "int_fuelscoop_size8_class5", new ShipModule(128666683, ShipModule.ModuleTypes.FuelScoop, 0, 1.12, "Fuel Scoop Class 8 Rating A", "Rate:1680") },

            // fuel tank

            { "int_fueltank_size1_class3", new ShipModule(128064346, ShipModule.ModuleTypes.FuelTank, 0, 0, "Fuel Tank Class 1 Rating C", "Size:2t") },
            { "int_fueltank_size2_class3", new ShipModule(128064347, ShipModule.ModuleTypes.FuelTank, 0, 0, "Fuel Tank Class 2 Rating C", "Size:4t") },
            { "int_fueltank_size3_class3", new ShipModule(128064348, ShipModule.ModuleTypes.FuelTank, 0, 0, "Fuel Tank Class 3 Rating C", "Size:8t") },
            { "int_fueltank_size4_class3", new ShipModule(128064349, ShipModule.ModuleTypes.FuelTank, 0, 0, "Fuel Tank Class 4 Rating C", "Size:16t") },
            { "int_fueltank_size5_class3", new ShipModule(128064350, ShipModule.ModuleTypes.FuelTank, 0, 0, "Fuel Tank Class 5 Rating C", "Size:32t") },
            { "int_fueltank_size6_class3", new ShipModule(128064351, ShipModule.ModuleTypes.FuelTank, 0, 0, "Fuel Tank Class 6 Rating C", "Size:64t") },
            { "int_fueltank_size7_class3", new ShipModule(128064352, ShipModule.ModuleTypes.FuelTank, 0, 0, "Fuel Tank Class 7 Rating C", "Size:128t") },
            { "int_fueltank_size8_class3", new ShipModule(128064353, ShipModule.ModuleTypes.FuelTank, 0, 0, "Fuel Tank Class 8 Rating C", "Size:256t") },

            { "int_fueltank_size1_class3_free", new ShipModule(128667018, ShipModule.ModuleTypes.FuelTank, 0, 0, "Fuel Tank Class 1 Rating C", "Size:2t") },

            // Gardian

            { "hpt_guardian_plasmalauncher_turret_small", new ShipModule(128891606, ShipModule.ModuleTypes.GuardianPlasmaCharger, 2, 1.6, "Guardian Plasma Launcher Turret Small", "Ammo:200/15, Damage:1.1, Range:3000m, Speed:1200m/s, Reload:3s, ThermL:5") },
            { "hpt_guardian_plasmalauncher_fixed_small", new ShipModule(128891607, ShipModule.ModuleTypes.GuardianPlasmaCharger, 2, 1.4, "Guardian Plasma Launcher Fixed Small", "Ammo:200/15, Damage:1.7, Range:3000m, Speed:1200m/s, Reload:3s, ThermL:4.2") },
            { "hpt_guardian_shardcannon_turret_small", new ShipModule(128891608, ShipModule.ModuleTypes.GuardianShardCannon, 2, 0.72, "Guardian Shard Cannon Turret Small", "Ammo:180/5, Damage:1.1, Range:1700m, Speed:1133m/s, Reload:5s, ThermL:0.6") },
            { "hpt_guardian_shardcannon_fixed_small", new ShipModule(128891609, ShipModule.ModuleTypes.GuardianShardCannon, 2, 0.87, "Guardian Shard Cannon Fixed Small", "Ammo:180/5, Damage:2, Range:1700m, Speed:1133m/s, Reload:5s, ThermL:0.7") },
            { "hpt_guardian_gausscannon_fixed_small", new ShipModule(128891610, ShipModule.ModuleTypes.GuardianGaussCannon, 2, 1.91, "Guardian Gauss Cannon Fixed Small", "Ammo:80/1, Damage:22, Range:3000m, Reload:1s, ThermL:15") },

            { "hpt_guardian_gausscannon_fixed_medium", new ShipModule(128833687, ShipModule.ModuleTypes.GuardianGaussCannon, 4, 2.61, "Guardian Gauss Cannon Fixed Medium", "Ammo:80/1, Damage:38.5, Range:3000m, Reload:1s, ThermL:25") },

            { "hpt_guardian_plasmalauncher_fixed_medium", new ShipModule(128833998, ShipModule.ModuleTypes.GuardianPlasmaCharger, 4, 2.13, "Guardian Plasma Launcher Fixed Medium", "Ammo:200/15, Damage:5, Range:3500m, Speed:1200m/s, Reload:3s, ThermL:5.2") },
            { "hpt_guardian_plasmalauncher_turret_medium", new ShipModule(128833999, ShipModule.ModuleTypes.GuardianPlasmaCharger, 4, 2.01, "Guardian Plasma Launcher Turret Medium", "Ammo:200/15, Damage:4, Range:3500m, Speed:1200m/s, Reload:3s, ThermL:5.8") },
            { "hpt_guardian_shardcannon_fixed_medium", new ShipModule(128834000, ShipModule.ModuleTypes.GuardianShardCannon, 4, 1.21, "Guardian Shard Cannon Fixed Medium", "Ammo:180/5, Damage:3.7, Range:1700m, Speed:1133m/s, Reload:5s, ThermL:1.2") },
            { "hpt_guardian_shardcannon_turret_medium", new ShipModule(128834001, ShipModule.ModuleTypes.GuardianShardCannon, 4, 1.16, "Guardian Shard Cannon Turret Medium", "Ammo:180/5, Damage:2.4, Range:1700m, Speed:1133m/s, Reload:5s, ThermL:1.1") },

            { "hpt_guardian_plasmalauncher_fixed_large", new ShipModule(128834783, ShipModule.ModuleTypes.GuardianPlasmaCharger, 8, 3.1, "Guardian Plasma Launcher Fixed Large", "Ammo:200/15, Damage:3.4, Range:3000m, Speed:1200m/s, Reload:3s, ThermL:6.2") },
            { "hpt_guardian_plasmalauncher_turret_large", new ShipModule(128834784, ShipModule.ModuleTypes.GuardianPlasmaCharger, 8, 2.53, "Guardian Plasma Launcher Turret Large", "Ammo:200/15, Damage:3.3, Range:3000m, Speed:1200m/s, Reload:3s, ThermL:6.4") },

            { "hpt_guardian_shardcannon_fixed_large", new ShipModule(128834778, ShipModule.ModuleTypes.GuardianShardCannon, 8, 1.68, "Guardian Shard Cannon Fixed Large", "Ammo:180/5, Damage:5.2, Range:1700m, Speed:1133m/s, Reload:5s, ThermL:2.2") },
            { "hpt_guardian_shardcannon_turret_large", new ShipModule(128834779, ShipModule.ModuleTypes.GuardianShardCannon, 8, 1.39, "Guardian Shard Cannon Turret Large", "Ammo:180/5, Damage:3.4, Range:1700m, Speed:1133m/s, Reload:5s, ThermL:2") },

            { "int_guardianhullreinforcement_size1_class2", new ShipModule(128833946, ShipModule.ModuleTypes.GuardianHullReinforcement, 1, 0.56, "Guardian Hull Reinforcement Class 1 Rating D", "Explosive:0%, Kinetic:0%, Thermal:2%") },
            { "int_guardianhullreinforcement_size1_class1", new ShipModule(128833945, ShipModule.ModuleTypes.GuardianHullReinforcement, 2, 0.45, "Guardian Hull Reinforcement Class 1 Rating E", "Explosive:0%, Kinetic:0%, Thermal:2%") },
            { "int_guardianhullreinforcement_size2_class1", new ShipModule(128833947, ShipModule.ModuleTypes.GuardianHullReinforcement, 4, 0.68, "Guardian Hull Reinforcement Class 2 Rating E", "Explosive:0%, Kinetic:0%, Thermal:2%") },
            { "int_guardianhullreinforcement_size2_class2", new ShipModule(128833948, ShipModule.ModuleTypes.GuardianHullReinforcement, 2, 0.79, "Guardian Hull Reinforcement Class 2 Rating D", "Explosive:0%, Kinetic:0%, Thermal:2%") },
            { "int_guardianhullreinforcement_size3_class1", new ShipModule(128833949, ShipModule.ModuleTypes.GuardianHullReinforcement, 8, 0.9, "Guardian Hull Reinforcement Class 3 Rating E", "Explosive:0%, Kinetic:0%, Thermal:2%") },
            { "int_guardianhullreinforcement_size3_class2", new ShipModule(128833950, ShipModule.ModuleTypes.GuardianHullReinforcement, 4, 1.01, "Guardian Hull Reinforcement Class 3 Rating D", "Explosive:0%, Kinetic:0%, Thermal:2%") },
            { "int_guardianhullreinforcement_size4_class1", new ShipModule(128833951, ShipModule.ModuleTypes.GuardianHullReinforcement, 16, 1.13, "Guardian Hull Reinforcement Class 4 Rating E", "Explosive:0%, Kinetic:0%, Thermal:2%") },
            { "int_guardianhullreinforcement_size4_class2", new ShipModule(128833952, ShipModule.ModuleTypes.GuardianHullReinforcement, 8, 1.24, "Guardian Hull Reinforcement Class 4 Rating D", "Explosive:0%, Kinetic:0%, Thermal:2%") },
            { "int_guardianhullreinforcement_size5_class1", new ShipModule(128833953, ShipModule.ModuleTypes.GuardianHullReinforcement, 32, 1.35, "Guardian Hull Reinforcement Class 5 Rating E", "Explosive:0%, Kinetic:0%, Thermal:2%") },
            { "int_guardianhullreinforcement_size5_class2", new ShipModule(128833954, ShipModule.ModuleTypes.GuardianHullReinforcement, 16, 1.46, "Guardian Hull Reinforcement Class 5 Rating D", "Explosive:0%, Kinetic:0%, Thermal:2%") },

            { "int_guardianmodulereinforcement_size1_class1", new ShipModule(128833955, ShipModule.ModuleTypes.GuardianModuleReinforcement, 2, 0.27, "Guardian Module Reinforcement Class 1 Rating E", "Protection:0.3") },
            { "int_guardianmodulereinforcement_size1_class2", new ShipModule(128833956, ShipModule.ModuleTypes.GuardianModuleReinforcement, 1, 0.34, "Guardian Module Reinforcement Class 1 Rating D", "Protection:0.6") },
            { "int_guardianmodulereinforcement_size2_class1", new ShipModule(128833957, ShipModule.ModuleTypes.GuardianModuleReinforcement, 4, 0.41, "Guardian Module Reinforcement Class 2 Rating E", "Protection:0.3") },
            { "int_guardianmodulereinforcement_size2_class2", new ShipModule(128833958, ShipModule.ModuleTypes.GuardianModuleReinforcement, 2, 0.47, "Guardian Module Reinforcement Class 2 Rating D", "Protection:0.6") },
            { "int_guardianmodulereinforcement_size3_class1", new ShipModule(128833959, ShipModule.ModuleTypes.GuardianModuleReinforcement, 8, 0.54, "Guardian Module Reinforcement Class 3 Rating E", "Protection:0.3") },
            { "int_guardianmodulereinforcement_size3_class2", new ShipModule(128833960, ShipModule.ModuleTypes.GuardianModuleReinforcement, 4, 0.61, "Guardian Module Reinforcement Class 3 Rating D", "Protection:0.6") },
            { "int_guardianmodulereinforcement_size4_class1", new ShipModule(128833961, ShipModule.ModuleTypes.GuardianModuleReinforcement, 16, 0.68, "Guardian Module Reinforcement Class 4 Rating E", "Protection:0.3") },
            { "int_guardianmodulereinforcement_size4_class2", new ShipModule(128833962, ShipModule.ModuleTypes.GuardianModuleReinforcement, 8, 0.74, "Guardian Module Reinforcement Class 4 Rating D", "Protection:0.6") },
            { "int_guardianmodulereinforcement_size5_class1", new ShipModule(128833963, ShipModule.ModuleTypes.GuardianModuleReinforcement, 32, 0.81, "Guardian Module Reinforcement Class 5 Rating E", "Protection:0.3") },
            { "int_guardianmodulereinforcement_size5_class2", new ShipModule(128833964, ShipModule.ModuleTypes.GuardianModuleReinforcement, 16, 0.88, "Guardian Module Reinforcement Class 5 Rating D", "Protection:0.6") },

            { "int_guardianshieldreinforcement_size1_class1", new ShipModule(128833965, ShipModule.ModuleTypes.GuardianShieldReinforcement, 2, 0.35, "Guardian Shield Reinforcement Class 1 Rating E", null) },
            { "int_guardianshieldreinforcement_size1_class2", new ShipModule(128833966, ShipModule.ModuleTypes.GuardianShieldReinforcement, 1, 0.46, "Guardian Shield Reinforcement Class 1 Rating D", null) },
            { "int_guardianshieldreinforcement_size2_class1", new ShipModule(128833967, ShipModule.ModuleTypes.GuardianShieldReinforcement, 4, 0.56, "Guardian Shield Reinforcement Class 2 Rating E", null) },
            { "int_guardianshieldreinforcement_size2_class2", new ShipModule(128833968, ShipModule.ModuleTypes.GuardianShieldReinforcement, 2, 0.67, "Guardian Shield Reinforcement Class 2 Rating D", null) },
            { "int_guardianshieldreinforcement_size3_class1", new ShipModule(128833969, ShipModule.ModuleTypes.GuardianShieldReinforcement, 8, 0.74, "Guardian Shield Reinforcement Class 3 Rating E", null) },
            { "int_guardianshieldreinforcement_size3_class2", new ShipModule(128833970, ShipModule.ModuleTypes.GuardianShieldReinforcement, 4, 0.84, "Guardian Shield Reinforcement Class 3 Rating D", null) },
            { "int_guardianshieldreinforcement_size4_class1", new ShipModule(128833971, ShipModule.ModuleTypes.GuardianShieldReinforcement, 16, 0.95, "Guardian Shield Reinforcement Class 4 Rating E", null) },
            { "int_guardianshieldreinforcement_size4_class2", new ShipModule(128833972, ShipModule.ModuleTypes.GuardianShieldReinforcement, 8, 1.05, "Guardian Shield Reinforcement Class 4 Rating D", null) },
            { "int_guardianshieldreinforcement_size5_class1", new ShipModule(128833973, ShipModule.ModuleTypes.GuardianShieldReinforcement, 32, 1.16, "Guardian Shield Reinforcement Class 5 Rating E", null) },
            { "int_guardianshieldreinforcement_size5_class2", new ShipModule(128833974, ShipModule.ModuleTypes.GuardianShieldReinforcement, 16, 1.26, "Guardian Shield Reinforcement Class 5 Rating D", null) },

            { "int_guardianfsdbooster_size1", new ShipModule(128833975, ShipModule.ModuleTypes.GuardianFSDBooster, 1.3, 0.75, "Guardian FSD Booster Class 1", null) },
            { "int_guardianfsdbooster_size2", new ShipModule(128833976, ShipModule.ModuleTypes.GuardianFSDBooster, 1.3, 0.98, "Guardian FSD Booster Class 2", null) },
            { "int_guardianfsdbooster_size3", new ShipModule(128833977, ShipModule.ModuleTypes.GuardianFSDBooster, 1.3, 1.27, "Guardian FSD Booster Class 3", null) },
            { "int_guardianfsdbooster_size4", new ShipModule(128833978, ShipModule.ModuleTypes.GuardianFSDBooster, 1.3, 1.65, "Guardian FSD Booster Class 4", null) },
            { "int_guardianfsdbooster_size5", new ShipModule(128833979, ShipModule.ModuleTypes.GuardianFSDBooster, 1.3, 2.14, "Guardian FSD Booster Class 5", null) },

            { "int_guardianpowerdistributor_size1", new ShipModule(128833980, ShipModule.ModuleTypes.GuardianHybridPowerDistributor, 1.4, 0.62, "Guardian Power Distributor Class 1", "Sys:0.8MW, Eng:0.8MW, Wep:2.5MW") },
            { "int_guardianpowerdistributor_size2", new ShipModule(128833981, ShipModule.ModuleTypes.GuardianHybridPowerDistributor, 2.6, 0.73, "Guardian Power Distributor Class 2", "Sys:0.8MW, Eng:0.8MW, Wep:2.5MW") },
            { "int_guardianpowerdistributor_size3", new ShipModule(128833982, ShipModule.ModuleTypes.GuardianHybridPowerDistributor, 5.25, 0.78, "Guardian Power Distributor Class 3", "Sys:1.7MW, Eng:1.7MW, Wep:3.1MW") },
            { "int_guardianpowerdistributor_size4", new ShipModule(128833983, ShipModule.ModuleTypes.GuardianHybridPowerDistributor, 10.5, 0.87, "Guardian Power Distributor Class 4", "Sys:1.7MW, Eng:2.5MW, Wep:4.9MW") },
            { "int_guardianpowerdistributor_size5", new ShipModule(128833984, ShipModule.ModuleTypes.GuardianHybridPowerDistributor, 21, 0.96, "Guardian Power Distributor Class 5", "Sys:3.3MW, Eng:3.3MW, Wep:6MW") },
            { "int_guardianpowerdistributor_size6", new ShipModule(128833985, ShipModule.ModuleTypes.GuardianHybridPowerDistributor, 42, 1.07, "Guardian Power Distributor Class 6", "Sys:4.2MW, Eng:4.2MW, Wep:7.3MW") },
            { "int_guardianpowerdistributor_size7", new ShipModule(128833986, ShipModule.ModuleTypes.GuardianHybridPowerDistributor, 84, 1.16, "Guardian Power Distributor Class 7", "Sys:5.2MW, Eng:5.2MW, Wep:8.5MW") },
            { "int_guardianpowerdistributor_size8", new ShipModule(128833987, ShipModule.ModuleTypes.GuardianHybridPowerDistributor, 168, 1.25, "Guardian Power Distributor Class 8", "Sys:6.2MW, Eng:6.2MW, Wep:10.1MW") },

            { "int_guardianpowerplant_size2", new ShipModule(128833988, ShipModule.ModuleTypes.GuardianHybridPowerPlant, 1.5, 0, "Guardian Powerplant Class 2", "Power:12.7MW") },
            { "int_guardianpowerplant_size3", new ShipModule(128833989, ShipModule.ModuleTypes.GuardianHybridPowerPlant, 2.9, 0, "Guardian Powerplant Class 3", "Power:15.8MW") },
            { "int_guardianpowerplant_size4", new ShipModule(128833990, ShipModule.ModuleTypes.GuardianHybridPowerPlant, 5.9, 0, "Guardian Powerplant Class 4", "Power:20.6MW") },
            { "int_guardianpowerplant_size5", new ShipModule(128833991, ShipModule.ModuleTypes.GuardianHybridPowerPlant, 11.7, 0, "Guardian Powerplant Class 5", "Power:26.9MW") },
            { "int_guardianpowerplant_size6", new ShipModule(128833992, ShipModule.ModuleTypes.GuardianHybridPowerPlant, 23.4, 0, "Guardian Powerplant Class 6", "Power:33.3MW") },
            { "int_guardianpowerplant_size7", new ShipModule(128833993, ShipModule.ModuleTypes.GuardianHybridPowerPlant, 46.8, 0, "Guardian Powerplant Class 7", "Power:39.6MW") },
            { "int_guardianpowerplant_size8", new ShipModule(128833994, ShipModule.ModuleTypes.GuardianHybridPowerPlant, 93.6, 0, "Guardian Powerplant Class 8", "Power:47.5MW") },

            // hull reinforcements 

            { "int_hullreinforcement_size1_class1", new ShipModule(128668537, ShipModule.ModuleTypes.HullReinforcementPackage, 2, 0, "Hull Reinforcement Class 1 Rating E", "Explosive:0.5%, Kinetic:0.5%, Thermal:0.5%") },
            { "int_hullreinforcement_size1_class2", new ShipModule(128668538, ShipModule.ModuleTypes.HullReinforcementPackage, 1, 0, "Hull Reinforcement Class 1 Rating D", "Explosive:0.5%, Kinetic:0.5%, Thermal:0.5%") },
            { "int_hullreinforcement_size2_class1", new ShipModule(128668539, ShipModule.ModuleTypes.HullReinforcementPackage, 4, 0, "Hull Reinforcement Class 2 Rating E", "Explosive:1%, Kinetic:1%, Thermal:1%") },
            { "int_hullreinforcement_size2_class2", new ShipModule(128668540, ShipModule.ModuleTypes.HullReinforcementPackage, 2, 0, "Hull Reinforcement Class 2 Rating D", "Explosive:1%, Kinetic:1%, Thermal:1%") },
            { "int_hullreinforcement_size3_class1", new ShipModule(128668541, ShipModule.ModuleTypes.HullReinforcementPackage, 8, 0, "Hull Reinforcement Class 3 Rating E", "Explosive:1.5%, Kinetic:1.5%, Thermal:1.5%") },
            { "int_hullreinforcement_size3_class2", new ShipModule(128668542, ShipModule.ModuleTypes.HullReinforcementPackage, 4, 0, "Hull Reinforcement Class 3 Rating D", "Explosive:1.5%, Kinetic:1.5%, Thermal:1.5%") },
            { "int_hullreinforcement_size4_class1", new ShipModule(128668543, ShipModule.ModuleTypes.HullReinforcementPackage, 16, 0, "Hull Reinforcement Class 4 Rating E", "Explosive:2%, Kinetic:2%, Thermal:2%") },
            { "int_hullreinforcement_size4_class2", new ShipModule(128668544, ShipModule.ModuleTypes.HullReinforcementPackage, 8, 0, "Hull Reinforcement Class 4 Rating D", "Explosive:2%, Kinetic:2%, Thermal:2%") },
            { "int_hullreinforcement_size5_class1", new ShipModule(128668545, ShipModule.ModuleTypes.HullReinforcementPackage, 32, 0, "Hull Reinforcement Class 5 Rating E", "Explosive:2.5%, Kinetic:2.5%, Thermal:2.5%") },
            { "int_hullreinforcement_size5_class2", new ShipModule(128668546, ShipModule.ModuleTypes.HullReinforcementPackage, 16, 0, "Hull Reinforcement Class 5 Rating D", "Explosive:2.5%, Kinetic:2.5%, Thermal:2.5%") },

            // Frame ship drive

            { "int_hyperdrive_size2_class1", new ShipModule(128064103, ShipModule.ModuleTypes.FrameShiftDrive, 2.5, 0.16, "Hyperdrive Class 2 Rating E", "OptMass:48t") },
            { "int_hyperdrive_size2_class2", new ShipModule(128064104, ShipModule.ModuleTypes.FrameShiftDrive, 1, 0.18, "Hyperdrive Class 2 Rating D", "OptMass:54t") },
            { "int_hyperdrive_size2_class3", new ShipModule(128064105, ShipModule.ModuleTypes.FrameShiftDrive, 2.5, 0.2, "Hyperdrive Class 2 Rating C", "OptMass:60t") },
            { "int_hyperdrive_size2_class4", new ShipModule(128064106, ShipModule.ModuleTypes.FrameShiftDrive, 4, 0.25, "Hyperdrive Class 2 Rating B", "OptMass:75t") },
            { "int_hyperdrive_size2_class5", new ShipModule(128064107, ShipModule.ModuleTypes.FrameShiftDrive, 2.5, 0.3, "Hyperdrive Class 2 Rating A", "OptMass:90t") },
            { "int_hyperdrive_size3_class1", new ShipModule(128064108, ShipModule.ModuleTypes.FrameShiftDrive, 5, 0.24, "Hyperdrive Class 3 Rating E", "OptMass:80t") },
            { "int_hyperdrive_size3_class2", new ShipModule(128064109, ShipModule.ModuleTypes.FrameShiftDrive, 2, 0.27, "Hyperdrive Class 3 Rating D", "OptMass:90t") },
            { "int_hyperdrive_size3_class3", new ShipModule(128064110, ShipModule.ModuleTypes.FrameShiftDrive, 5, 0.3, "Hyperdrive Class 3 Rating C", "OptMass:100t") },
            { "int_hyperdrive_size3_class4", new ShipModule(128064111, ShipModule.ModuleTypes.FrameShiftDrive, 8, 0.38, "Hyperdrive Class 3 Rating B", "OptMass:125t") },
            { "int_hyperdrive_size3_class5", new ShipModule(128064112, ShipModule.ModuleTypes.FrameShiftDrive, 5, 0.45, "Hyperdrive Class 3 Rating A", "OptMass:150t") },
            { "int_hyperdrive_size4_class1", new ShipModule(128064113, ShipModule.ModuleTypes.FrameShiftDrive, 10, 0.24, "Hyperdrive Class 4 Rating E", "OptMass:280t") },
            { "int_hyperdrive_size4_class2", new ShipModule(128064114, ShipModule.ModuleTypes.FrameShiftDrive, 4, 0.27, "Hyperdrive Class 4 Rating D", "OptMass:315t") },
            { "int_hyperdrive_size4_class3", new ShipModule(128064115, ShipModule.ModuleTypes.FrameShiftDrive, 10, 0.3, "Hyperdrive Class 4 Rating C", "OptMass:350t") },
            { "int_hyperdrive_size4_class4", new ShipModule(128064116, ShipModule.ModuleTypes.FrameShiftDrive, 16, 0.38, "Hyperdrive Class 4 Rating B", "OptMass:438t") },
            { "int_hyperdrive_size4_class5", new ShipModule(128064117, ShipModule.ModuleTypes.FrameShiftDrive, 10, 0.45, "Hyperdrive Class 4 Rating A", "OptMass:525t") },
            { "int_hyperdrive_size5_class1", new ShipModule(128064118, ShipModule.ModuleTypes.FrameShiftDrive, 20, 0.32, "Hyperdrive Class 5 Rating E", "OptMass:560t") },
            { "int_hyperdrive_size5_class2", new ShipModule(128064119, ShipModule.ModuleTypes.FrameShiftDrive, 8, 0.36, "Hyperdrive Class 5 Rating D", "OptMass:630t") },
            { "int_hyperdrive_size5_class3", new ShipModule(128064120, ShipModule.ModuleTypes.FrameShiftDrive, 20, 0.4, "Hyperdrive Class 5 Rating C", "OptMass:700t") },
            { "int_hyperdrive_size5_class4", new ShipModule(128064121, ShipModule.ModuleTypes.FrameShiftDrive, 32, 0.5, "Hyperdrive Class 5 Rating B", "OptMass:875t") },
            { "int_hyperdrive_size5_class5", new ShipModule(128064122, ShipModule.ModuleTypes.FrameShiftDrive, 20, 0.6, "Hyperdrive Class 5 Rating A", "OptMass:1050t") },
            { "int_hyperdrive_size6_class1", new ShipModule(128064123, ShipModule.ModuleTypes.FrameShiftDrive, 40, 0.4, "Hyperdrive Class 6 Rating E", "OptMass:960t") },
            { "int_hyperdrive_size6_class2", new ShipModule(128064124, ShipModule.ModuleTypes.FrameShiftDrive, 16, 0.45, "Hyperdrive Class 6 Rating D", "OptMass:1080t") },
            { "int_hyperdrive_size6_class3", new ShipModule(128064125, ShipModule.ModuleTypes.FrameShiftDrive, 40, 0.5, "Hyperdrive Class 6 Rating C", "OptMass:1200t") },
            { "int_hyperdrive_size6_class4", new ShipModule(128064126, ShipModule.ModuleTypes.FrameShiftDrive, 64, 0.63, "Hyperdrive Class 6 Rating B", "OptMass:1500t") },
            { "int_hyperdrive_size6_class5", new ShipModule(128064127, ShipModule.ModuleTypes.FrameShiftDrive, 40, 0.75, "Hyperdrive Class 6 Rating A", "OptMass:1800t") },
            { "int_hyperdrive_size7_class1", new ShipModule(128064128, ShipModule.ModuleTypes.FrameShiftDrive, 80, 0.48, "Hyperdrive Class 7 Rating E", "OptMass:1440t") },
            { "int_hyperdrive_size7_class2", new ShipModule(128064129, ShipModule.ModuleTypes.FrameShiftDrive, 32, 0.54, "Hyperdrive Class 7 Rating D", "OptMass:1620t") },
            { "int_hyperdrive_size7_class3", new ShipModule(128064130, ShipModule.ModuleTypes.FrameShiftDrive, 80, 0.6, "Hyperdrive Class 7 Rating C", "OptMass:1800t") },
            { "int_hyperdrive_size7_class4", new ShipModule(128064131, ShipModule.ModuleTypes.FrameShiftDrive, 128, 0.75, "Hyperdrive Class 7 Rating B", "OptMass:2250t") },
            { "int_hyperdrive_size7_class5", new ShipModule(128064132, ShipModule.ModuleTypes.FrameShiftDrive, 80, 0.9, "Hyperdrive Class 7 Rating A", "OptMass:2700t") },
            { "int_hyperdrive_size8_class1", new ShipModule(128064133, ShipModule.ModuleTypes.FrameShiftDrive, 160, 0.56, "Hyperdrive Class 8 Rating E", "OptMass:0t") },
            { "int_hyperdrive_size8_class2", new ShipModule(128064134, ShipModule.ModuleTypes.FrameShiftDrive, 64, 0.63, "Hyperdrive Class 8 Rating D", "OptMass:0t") },
            { "int_hyperdrive_size8_class3", new ShipModule(128064135, ShipModule.ModuleTypes.FrameShiftDrive, 160, 0.7, "Hyperdrive Class 8 Rating C", "OptMass:0t") },
            { "int_hyperdrive_size8_class4", new ShipModule(128064136, ShipModule.ModuleTypes.FrameShiftDrive, 256, 0.88, "Hyperdrive Class 8 Rating B", "OptMass:0t") },
            { "int_hyperdrive_size8_class5", new ShipModule(128064137, ShipModule.ModuleTypes.FrameShiftDrive, 160, 1.05, "Hyperdrive Class 8 Rating A", "OptMass:0t") },
            { "int_hyperdrive_size2_class1_free", new ShipModule(128666637, ShipModule.ModuleTypes.FrameShiftDrive, 2.5, 0.16, "Hyperdrive Class 2 Rating E", "OptMass:48t") },


            { "int_hyperdrive_overcharge_size2_class1", new ShipModule(129030577, ShipModule.ModuleTypes.FrameShiftDrive, 2.5, 0.2, "Hyperdrive Overcharge Class 2 Rating E", "OptMass:60t, SpeedIncrease:25%, AccelerationRate:0.08 , HeatGenerationRate:-1 , ControlInterference:0.25") },
            { "int_hyperdrive_overcharge_size2_class2", new ShipModule(129030578, ShipModule.ModuleTypes.FrameShiftDrive, 2.5, 0.25, "Hyperdrive Overcharge Class 2 Rating D", "OptMass:90t, SpeedIncrease:142%, AccelerationRate:0.09 , HeatGenerationRate:-1 , ControlInterference:0.24") },
            { "int_hyperdrive_overcharge_size2_class3", new ShipModule(129030487, ShipModule.ModuleTypes.FrameShiftDrive, 2.5, 0.25, "Hyperdrive Overcharge Class 2 Rating C", "OptMass:90t, SpeedIncrease:142%, AccelerationRate: 0.09, HeatGenerationRate:-1 0.41, ControlInterference:0.24") },
            { "int_hyperdrive_overcharge_size2_class4", new ShipModule(129030579, ShipModule.ModuleTypes.FrameShiftDrive, 2.5, 0.25, "Hyperdrive Overcharge Class 2 Rating B", "OptMass:100t, SpeedIncrease:142%, AccelerationRate:0.09 , HeatGenerationRate:-1 , ControlInterference:0.24") },
            { "int_hyperdrive_overcharge_size2_class5", new ShipModule(129030580, ShipModule.ModuleTypes.FrameShiftDrive, 2.5, 0.3, "Hyperdrive Overcharge Class 2 Rating A", "OptMass:100t, SpeedIncrease:160%, AccelerationRate:0.09 , HeatGenerationRate:-1 , ControlInterference:0.23") },

            { "int_hyperdrive_overcharge_size3_class1", new ShipModule(129030581, ShipModule.ModuleTypes.FrameShiftDrive, 5, 0.3, "Hyperdrive Overcharge Class 3 Rating E", "OptMass:100t, SpeedIncrease:20%, AccelerationRate:0.06 , HeatGenerationRate:-1 , ControlInterference:0.3") },
            { "int_hyperdrive_overcharge_size3_class2", new ShipModule(129030582, ShipModule.ModuleTypes.FrameShiftDrive, 2, 0.38, "Hyperdrive Overcharge Class 3 Rating D", "OptMass:150t, SpeedIncrease:120%, AccelerationRate:0.07 , HeatGenerationRate:-1 , ControlInterference:0.29") },
            { "int_hyperdrive_overcharge_size3_class4", new ShipModule(129030583, ShipModule.ModuleTypes.FrameShiftDrive, 5, 0.38, "Hyperdrive Overcharge Class 3 Rating B", "OptMass:150t, SpeedIncrease:120%, AccelerationRate:0.07 , HeatGenerationRate:-1 , ControlInterference:0.29") },
            { "int_hyperdrive_overcharge_size3_class3", new ShipModule(129030486, ShipModule.ModuleTypes.FrameShiftDrive, 5, 0.38, "Hyperdrive Overcharge Class 3 Rating C", "OptMass:150t, SpeedIncrease:120%, AccelerationRate:0.07, HeatGenerationRate:-1 0.49, ControlInterference:0.29") },
            { "int_hyperdrive_overcharge_size3_class5", new ShipModule(129030584, ShipModule.ModuleTypes.FrameShiftDrive, 5, 0.45, "Hyperdrive Overcharge Class 3 Rating A", "OptMass:167t, SpeedIncrease:138%, AccelerationRate:0.07 , HeatGenerationRate:-1 , ControlInterference:0.28") },

            { "int_hyperdrive_overcharge_size4_class1", new ShipModule(129030585, ShipModule.ModuleTypes.FrameShiftDrive, 10, 0.3, "Hyperdrive Overcharge Class 4 Rating E", "OptMass:350t, SpeedIncrease:15%, AccelerationRate:0.05 , HeatGenerationRate:-1 , ControlInterference:0.37") },
            { "int_hyperdrive_overcharge_size4_class2", new ShipModule(129030586, ShipModule.ModuleTypes.FrameShiftDrive, 4, 0.38, "Hyperdrive Overcharge Class 4 Rating D", "OptMass:525t, SpeedIncrease:100%, AccelerationRate:0.06 , HeatGenerationRate:-1 , ControlInterference:0.35") },
            { "int_hyperdrive_overcharge_size4_class3", new ShipModule(129030485, ShipModule.ModuleTypes.FrameShiftDrive, 10, 0.38, "Hyperdrive Overcharge Class 4 Rating C", "OptMass:525t, SpeedIncrease:100%, AccelerationRate: 0.06, HeatGenerationRate:-1 1.23, ControlInterference:0.35") },
            { "int_hyperdrive_overcharge_size4_class4", new ShipModule(129030587, ShipModule.ModuleTypes.FrameShiftDrive, 10, 0.38, "Hyperdrive Overcharge Class 4 Rating B", "OptMass:525t, SpeedIncrease:100%, AccelerationRate:0.06 , HeatGenerationRate:-1 , ControlInterference:0.35") },
            { "int_hyperdrive_overcharge_size4_class5", new ShipModule(129030588, ShipModule.ModuleTypes.FrameShiftDrive, 10, 0.45, "Hyperdrive Overcharge Class 4 Rating A", "OptMass:585t, SpeedIncrease:117%, AccelerationRate:0.06 , HeatGenerationRate:-1 , ControlInterference:0.34") },

            { "int_hyperdrive_overcharge_size5_class1", new ShipModule(129030589, ShipModule.ModuleTypes.FrameShiftDrive, 20, 0.45, "Hyperdrive Overcharge Class 5 Rating E", "OptMass:700t, SpeedIncrease:-1%, AccelerationRate:0.04 , HeatGenerationRate:-1 , ControlInterference:0.42") },
            { "int_hyperdrive_overcharge_size5_class2", new ShipModule(129030590, ShipModule.ModuleTypes.FrameShiftDrive, 8, 0.5, "Hyperdrive Overcharge Class 5 Rating D", "OptMass:1050t, SpeedIncrease:80%, AccelerationRate:0.055 , HeatGenerationRate:-1 , ControlInterference:0.4") },
            { "int_hyperdrive_overcharge_size5_class3", new ShipModule(129030474, ShipModule.ModuleTypes.FrameShiftDrive, 20, 0.5, "Hyperdrive Overcharge Class 5 Rating C", "OptMass:1050t, SpeedIncrease:80%, AccelerationRate: 0.055, HeatGenerationRate:-1 1.4, ControlInterference:0.4") },
            { "int_hyperdrive_overcharge_size5_class4", new ShipModule(129030591, ShipModule.ModuleTypes.FrameShiftDrive, 20, 0.5, "Hyperdrive Overcharge Class 5 Rating B", "OptMass:1050t, SpeedIncrease:80%, AccelerationRate:0.055 , HeatGenerationRate:-1 , ControlInterference:0.4") },
            { "int_hyperdrive_overcharge_size5_class5", new ShipModule(129030592, ShipModule.ModuleTypes.FrameShiftDrive, 20, 0.6, "Hyperdrive Overcharge Class 5 Rating A", "OptMass:1175t, SpeedIncrease:95%, AccelerationRate:0.055 , HeatGenerationRate:-1 , ControlInterference:0.39") },

            { "int_hyperdrive_overcharge_size6_class1", new ShipModule(129030593, ShipModule.ModuleTypes.FrameShiftDrive, 40, 0.5, "Hyperdrive Overcharge Class 6 Rating E", "OptMass:1200t, SpeedIncrease:-1%, AccelerationRate:0.045 , HeatGenerationRate:-1 , ControlInterference:0.67") },
            { "int_hyperdrive_overcharge_size6_class2", new ShipModule(129030594, ShipModule.ModuleTypes.FrameShiftDrive, 16, 0.63, "Hyperdrive Overcharge Class 6 Rating D", "OptMass:1800t, SpeedIncrease:62%, AccelerationRate:0.05 , HeatGenerationRate:-1 , ControlInterference:0.64") },
            { "int_hyperdrive_overcharge_size6_class3", new ShipModule(129030484, ShipModule.ModuleTypes.FrameShiftDrive, 40, 0.63, "Hyperdrive Overcharge Class 6 Rating C", "OptMass:1800t, SpeedIncrease:62%, AccelerationRate:0.05, HeatGenerationRate:-1 1.8, ControlInterference:0.64") },
            { "int_hyperdrive_overcharge_size6_class4", new ShipModule(129030595, ShipModule.ModuleTypes.FrameShiftDrive, 40, 0.63, "Hyperdrive Overcharge Class 6 Rating B", "OptMass:1800t, SpeedIncrease:62%, AccelerationRate:0.05 , HeatGenerationRate:-1 , ControlInterference:0.64") },
            { "int_hyperdrive_overcharge_size6_class5", new ShipModule(129030596, ShipModule.ModuleTypes.FrameShiftDrive, 40, 0.75, "Hyperdrive Overcharge Class 6 Rating A", "OptMass:2000t, SpeedIncrease:76%, AccelerationRate:0.05 , HeatGenerationRate:-1 , ControlInterference:0.62") },

            { "int_hyperdrive_overcharge_size7_class1", new ShipModule(129030597, ShipModule.ModuleTypes.FrameShiftDrive, 80, 0.6, "Hyperdrive Overcharge Class 7 Rating E", "OptMass:1800t, SpeedIncrease:-1%, AccelerationRate:0.03 , HeatGenerationRate:-1 , ControlInterference:0.67 ") },
            { "int_hyperdrive_overcharge_size7_class2", new ShipModule(129030598, ShipModule.ModuleTypes.FrameShiftDrive, 32, 0.75, "Hyperdrive Overcharge Class 7 Rating D", "OptMass:2700t, SpeedIncrease:46%, AccelerationRate:0.04 , HeatGenerationRate:-1 , ControlInterference:0.64 ") },
            { "int_hyperdrive_overcharge_size7_class3", new ShipModule(129030483, ShipModule.ModuleTypes.FrameShiftDrive, 80, 0.75, "Hyperdrive Overcharge Class 7 Rating C", "OptMass:2700t, SpeedIncrease:46%, AccelerationRate:0.04, HeatGenerationRate:-1 2, ControlInterference:0.64") },
            { "int_hyperdrive_overcharge_size7_class4", new ShipModule(129030599, ShipModule.ModuleTypes.FrameShiftDrive, 80, 0.75, "Hyperdrive Overcharge Class 7 Rating B", "OptMass:2700t, SpeedIncrease:46%, AccelerationRate:0.04 , HeatGenerationRate:-1 , ControlInterference:0.64") },
            { "int_hyperdrive_overcharge_size7_class5", new ShipModule(129030600, ShipModule.ModuleTypes.FrameShiftDrive, 80, 0.9, "Hyperdrive Overcharge Class 7 Rating A", "OptMass:3000t, SpeedIncrease:58%, AccelerationRate:0.04 , HeatGenerationRate:-1 , ControlInterference:0.62") },



            // wake scanner

            { "hpt_cloudscanner_size0_class1", new ShipModule(128662525, ShipModule.ModuleTypes.FrameShiftWakeScanner, 1.3, 0.2, "Cloud Scanner Rating E", "Range:2000m") },
            { "hpt_cloudscanner_size0_class2", new ShipModule(128662526, ShipModule.ModuleTypes.FrameShiftWakeScanner, 1.3, 0.4, "Cloud Scanner Rating D", "Range:2500m") },
            { "hpt_cloudscanner_size0_class3", new ShipModule(128662527, ShipModule.ModuleTypes.FrameShiftWakeScanner, 1.3, 0.8, "Cloud Scanner Rating C", "Range:3000m") },
            { "hpt_cloudscanner_size0_class4", new ShipModule(128662528, ShipModule.ModuleTypes.FrameShiftWakeScanner, 1.3, 1.6, "Cloud Scanner Rating B", "Range:3500m") },
            { "hpt_cloudscanner_size0_class5", new ShipModule(128662529, ShipModule.ModuleTypes.FrameShiftWakeScanner, 1.3, 3.2, "Cloud Scanner Rating A", "Range:4000m") },

            // life support

            { "int_lifesupport_size1_class1", new ShipModule(128064138, ShipModule.ModuleTypes.LifeSupport, 1.3, 0.32, "Life Support Class 1 Rating E", "Time:300s") },
            { "int_lifesupport_size1_class2", new ShipModule(128064139, ShipModule.ModuleTypes.LifeSupport, 0.5, 0.36, "Life Support Class 1 Rating D", "Time:450s") },
            { "int_lifesupport_size1_class3", new ShipModule(128064140, ShipModule.ModuleTypes.LifeSupport, 1.3, 0.4, "Life Support Class 1 Rating C", "Time:600s") },
            { "int_lifesupport_size1_class4", new ShipModule(128064141, ShipModule.ModuleTypes.LifeSupport, 2, 0.44, "Life Support Class 1 Rating B", "Time:900s") },
            { "int_lifesupport_size1_class5", new ShipModule(128064142, ShipModule.ModuleTypes.LifeSupport, 1.3, 0.48, "Life Support Class 1 Rating A", "Time:1500s") },
            { "int_lifesupport_size2_class1", new ShipModule(128064143, ShipModule.ModuleTypes.LifeSupport, 2.5, 0.37, "Life Support Class 2 Rating E", "Time:300s") },
            { "int_lifesupport_size2_class2", new ShipModule(128064144, ShipModule.ModuleTypes.LifeSupport, 1, 0.41, "Life Support Class 2 Rating D", "Time:450s") },
            { "int_lifesupport_size2_class3", new ShipModule(128064145, ShipModule.ModuleTypes.LifeSupport, 2.5, 0.46, "Life Support Class 2 Rating C", "Time:600s") },
            { "int_lifesupport_size2_class4", new ShipModule(128064146, ShipModule.ModuleTypes.LifeSupport, 4, 0.51, "Life Support Class 2 Rating B", "Time:900s") },
            { "int_lifesupport_size2_class5", new ShipModule(128064147, ShipModule.ModuleTypes.LifeSupport, 2.5, 0.55, "Life Support Class 2 Rating A", "Time:1500s") },
            { "int_lifesupport_size3_class1", new ShipModule(128064148, ShipModule.ModuleTypes.LifeSupport, 5, 0.42, "Life Support Class 3 Rating E", "Time:300s") },
            { "int_lifesupport_size3_class2", new ShipModule(128064149, ShipModule.ModuleTypes.LifeSupport, 2, 0.48, "Life Support Class 3 Rating D", "Time:450s") },
            { "int_lifesupport_size3_class3", new ShipModule(128064150, ShipModule.ModuleTypes.LifeSupport, 5, 0.53, "Life Support Class 3 Rating C", "Time:600s") },
            { "int_lifesupport_size3_class4", new ShipModule(128064151, ShipModule.ModuleTypes.LifeSupport, 8, 0.58, "Life Support Class 3 Rating B", "Time:900s") },
            { "int_lifesupport_size3_class5", new ShipModule(128064152, ShipModule.ModuleTypes.LifeSupport, 5, 0.64, "Life Support Class 3 Rating A", "Time:1500s") },
            { "int_lifesupport_size4_class1", new ShipModule(128064153, ShipModule.ModuleTypes.LifeSupport, 10, 0.5, "Life Support Class 4 Rating E", "Time:300s") },
            { "int_lifesupport_size4_class2", new ShipModule(128064154, ShipModule.ModuleTypes.LifeSupport, 4, 0.56, "Life Support Class 4 Rating D", "Time:450s") },
            { "int_lifesupport_size4_class3", new ShipModule(128064155, ShipModule.ModuleTypes.LifeSupport, 10, 0.62, "Life Support Class 4 Rating C", "Time:600s") },
            { "int_lifesupport_size4_class4", new ShipModule(128064156, ShipModule.ModuleTypes.LifeSupport, 16, 0.68, "Life Support Class 4 Rating B", "Time:900s") },
            { "int_lifesupport_size4_class5", new ShipModule(128064157, ShipModule.ModuleTypes.LifeSupport, 10, 0.74, "Life Support Class 4 Rating A", "Time:1500s") },
            { "int_lifesupport_size5_class1", new ShipModule(128064158, ShipModule.ModuleTypes.LifeSupport, 20, 0.57, "Life Support Class 5 Rating E", "Time:300s") },
            { "int_lifesupport_size5_class2", new ShipModule(128064159, ShipModule.ModuleTypes.LifeSupport, 8, 0.64, "Life Support Class 5 Rating D", "Time:450s") },
            { "int_lifesupport_size5_class3", new ShipModule(128064160, ShipModule.ModuleTypes.LifeSupport, 20, 0.71, "Life Support Class 5 Rating C", "Time:600s") },
            { "int_lifesupport_size5_class4", new ShipModule(128064161, ShipModule.ModuleTypes.LifeSupport, 32, 0.78, "Life Support Class 5 Rating B", "Time:900s") },
            { "int_lifesupport_size5_class5", new ShipModule(128064162, ShipModule.ModuleTypes.LifeSupport, 20, 0.85, "Life Support Class 5 Rating A", "Time:1500s") },
            { "int_lifesupport_size6_class1", new ShipModule(128064163, ShipModule.ModuleTypes.LifeSupport, 40, 0.64, "Life Support Class 6 Rating E", "Time:300s") },
            { "int_lifesupport_size6_class2", new ShipModule(128064164, ShipModule.ModuleTypes.LifeSupport, 16, 0.72, "Life Support Class 6 Rating D", "Time:450s") },
            { "int_lifesupport_size6_class3", new ShipModule(128064165, ShipModule.ModuleTypes.LifeSupport, 40, 0.8, "Life Support Class 6 Rating C", "Time:600s") },
            { "int_lifesupport_size6_class4", new ShipModule(128064166, ShipModule.ModuleTypes.LifeSupport, 64, 0.88, "Life Support Class 6 Rating B", "Time:900s") },
            { "int_lifesupport_size6_class5", new ShipModule(128064167, ShipModule.ModuleTypes.LifeSupport, 40, 0.96, "Life Support Class 6 Rating A", "Time:1500s") },
            { "int_lifesupport_size7_class1", new ShipModule(128064168, ShipModule.ModuleTypes.LifeSupport, 80, 0.72, "Life Support Class 7 Rating E", "Time:300s") },
            { "int_lifesupport_size7_class2", new ShipModule(128064169, ShipModule.ModuleTypes.LifeSupport, 32, 0.81, "Life Support Class 7 Rating D", "Time:450s") },
            { "int_lifesupport_size7_class3", new ShipModule(128064170, ShipModule.ModuleTypes.LifeSupport, 80, 0.9, "Life Support Class 7 Rating C", "Time:600s") },
            { "int_lifesupport_size7_class4", new ShipModule(128064171, ShipModule.ModuleTypes.LifeSupport, 128, 0.99, "Life Support Class 7 Rating B", "Time:900s") },
            { "int_lifesupport_size7_class5", new ShipModule(128064172, ShipModule.ModuleTypes.LifeSupport, 80, 1.08, "Life Support Class 7 Rating A", "Time:1500s") },
            { "int_lifesupport_size8_class1", new ShipModule(128064173, ShipModule.ModuleTypes.LifeSupport, 160, 0.8, "Life Support Class 8 Rating E", "Time:300s") },
            { "int_lifesupport_size8_class2", new ShipModule(128064174, ShipModule.ModuleTypes.LifeSupport, 64, 0.9, "Life Support Class 8 Rating D", "Time:450s") },
            { "int_lifesupport_size8_class3", new ShipModule(128064175, ShipModule.ModuleTypes.LifeSupport, 160, 1, "Life Support Class 8 Rating C", "Time:600s") },
            { "int_lifesupport_size8_class4", new ShipModule(128064176, ShipModule.ModuleTypes.LifeSupport, 256, 1.1, "Life Support Class 8 Rating B", "Time:900s") },
            { "int_lifesupport_size8_class5", new ShipModule(128064177, ShipModule.ModuleTypes.LifeSupport, 160, 1.2, "Life Support Class 8 Rating A", "Time:1500s") },

            { "int_lifesupport_size1_class1_free", new ShipModule(128666638, ShipModule.ModuleTypes.LifeSupport, 1.3, 0.32, "Life Support Class 1 Rating E", "Time:300s") },

            // Limpet control

            { "int_dronecontrol_collection_size1_class1", new ShipModule(128671229, ShipModule.ModuleTypes.CollectorLimpetController, 0.5, 0.14, "Collection Drone Controller Class 1 Rating E", "Time:300s, Range:0.8km") },
            { "int_dronecontrol_collection_size1_class2", new ShipModule(128671230, ShipModule.ModuleTypes.CollectorLimpetController, 0.5, 0.18, "Collection Drone Controller Class 1 Rating D", "Time:600s, Range:0.6km") },
            { "int_dronecontrol_collection_size1_class3", new ShipModule(128671231, ShipModule.ModuleTypes.CollectorLimpetController, 1.3, 0.23, "Collection Drone Controller Class 1 Rating C", "Time:510s, Range:1km") },
            { "int_dronecontrol_collection_size1_class4", new ShipModule(128671232, ShipModule.ModuleTypes.CollectorLimpetController, 2, 0.28, "Collection Drone Controller Class 1 Rating B", "Time:420s, Range:1.4km") },
            { "int_dronecontrol_collection_size1_class5", new ShipModule(128671233, ShipModule.ModuleTypes.CollectorLimpetController, 2, 0.32, "Collection Drone Controller Class 1 Rating A", "Time:720s, Range:1.2km") },
            { "int_dronecontrol_collection_size3_class1", new ShipModule(128671234, ShipModule.ModuleTypes.CollectorLimpetController, 2, 0.2, "Collection Drone Controller Class 3 Rating E", "Time:300s, Range:0.9km") },
            { "int_dronecontrol_collection_size3_class2", new ShipModule(128671235, ShipModule.ModuleTypes.CollectorLimpetController, 2, 0.27, "Collection Drone Controller Class 3 Rating D", "Time:600s, Range:0.7km") },
            { "int_dronecontrol_collection_size3_class3", new ShipModule(128671236, ShipModule.ModuleTypes.CollectorLimpetController, 5, 0.34, "Collection Drone Controller Class 3 Rating C", "Time:510s, Range:1.1km") },
            { "int_dronecontrol_collection_size3_class4", new ShipModule(128671237, ShipModule.ModuleTypes.CollectorLimpetController, 8, 0.41, "Collection Drone Controller Class 3 Rating B", "Time:420s, Range:1.5km") },
            { "int_dronecontrol_collection_size3_class5", new ShipModule(128671238, ShipModule.ModuleTypes.CollectorLimpetController, 8, 0.48, "Collection Drone Controller Class 3 Rating A", "Time:720s, Range:1.3km") },
            { "int_dronecontrol_collection_size5_class1", new ShipModule(128671239, ShipModule.ModuleTypes.CollectorLimpetController, 8, 0.3, "Collection Drone Controller Class 5 Rating E", "Time:300s, Range:1km") },
            { "int_dronecontrol_collection_size5_class2", new ShipModule(128671240, ShipModule.ModuleTypes.CollectorLimpetController, 8, 0.4, "Collection Drone Controller Class 5 Rating D", "Time:600s, Range:0.8km") },
            { "int_dronecontrol_collection_size5_class3", new ShipModule(128671241, ShipModule.ModuleTypes.CollectorLimpetController, 20, 0.5, "Collection Drone Controller Class 5 Rating C", "Time:510s, Range:1.3km") },
            { "int_dronecontrol_collection_size5_class4", new ShipModule(128671242, ShipModule.ModuleTypes.CollectorLimpetController, 32, 0.6, "Collection Drone Controller Class 5 Rating B", "Time:420s, Range:1.8km") },
            { "int_dronecontrol_collection_size5_class5", new ShipModule(128671243, ShipModule.ModuleTypes.CollectorLimpetController, 32, 0.7, "Collection Drone Controller Class 5 Rating A", "Time:720s, Range:1.6km") },
            { "int_dronecontrol_collection_size7_class1", new ShipModule(128671244, ShipModule.ModuleTypes.CollectorLimpetController, 32, 0.41, "Collection Drone Controller Class 7 Rating E", "Time:300s, Range:1.4km") },
            { "int_dronecontrol_collection_size7_class2", new ShipModule(128671245, ShipModule.ModuleTypes.CollectorLimpetController, 32, 0.55, "Collection Drone Controller Class 7 Rating D", "Time:600s, Range:1km") },
            { "int_dronecontrol_collection_size7_class3", new ShipModule(128671246, ShipModule.ModuleTypes.CollectorLimpetController, 80, 0.69, "Collection Drone Controller Class 7 Rating C", "Time:510s, Range:1.7km") },
            { "int_dronecontrol_collection_size7_class4", new ShipModule(128671247, ShipModule.ModuleTypes.CollectorLimpetController, 128, 0.83, "Collection Drone Controller Class 7 Rating B", "Time:420s, Range:2.4km") },
            { "int_dronecontrol_collection_size7_class5", new ShipModule(128671248, ShipModule.ModuleTypes.CollectorLimpetController, 128, 0.97, "Collection Drone Controller Class 7 Rating A", "Time:720s, Range:2km") },

            { "int_dronecontrol_fueltransfer_size1_class1", new ShipModule(128671249, ShipModule.ModuleTypes.FuelTransferLimpetController, 1.3, 0.18, "Fuel Transfer Drone Controller Class 1 Rating E", "Range:0.6km") },
            { "int_dronecontrol_fueltransfer_size1_class2", new ShipModule(128671250, ShipModule.ModuleTypes.FuelTransferLimpetController, 0.5, 0.14, "Fuel Transfer Drone Controller Class 1 Rating D", "Range:0.8km") },
            { "int_dronecontrol_fueltransfer_size1_class3", new ShipModule(128671251, ShipModule.ModuleTypes.FuelTransferLimpetController, 1.3, 0.23, "Fuel Transfer Drone Controller Class 1 Rating C", "Range:1km") },
            { "int_dronecontrol_fueltransfer_size1_class4", new ShipModule(128671252, ShipModule.ModuleTypes.FuelTransferLimpetController, 2, 0.32, "Fuel Transfer Drone Controller Class 1 Rating B", "Range:1.2km") },
            { "int_dronecontrol_fueltransfer_size1_class5", new ShipModule(128671253, ShipModule.ModuleTypes.FuelTransferLimpetController, 1.3, 0.28, "Fuel Transfer Drone Controller Class 1 Rating A", "Range:1.4km") },
            { "int_dronecontrol_fueltransfer_size3_class1", new ShipModule(128671254, ShipModule.ModuleTypes.FuelTransferLimpetController, 5, 0.27, "Fuel Transfer Drone Controller Class 3 Rating E", "Range:0.7km") },
            { "int_dronecontrol_fueltransfer_size3_class2", new ShipModule(128671255, ShipModule.ModuleTypes.FuelTransferLimpetController, 2, 0.2, "Fuel Transfer Drone Controller Class 3 Rating D", "Range:0.9km") },
            { "int_dronecontrol_fueltransfer_size3_class3", new ShipModule(128671256, ShipModule.ModuleTypes.FuelTransferLimpetController, 5, 0.34, "Fuel Transfer Drone Controller Class 3 Rating C", "Range:1.1km") },
            { "int_dronecontrol_fueltransfer_size3_class4", new ShipModule(128671257, ShipModule.ModuleTypes.FuelTransferLimpetController, 8, 0.48, "Fuel Transfer Drone Controller Class 3 Rating B", "Range:1.3km") },
            { "int_dronecontrol_fueltransfer_size3_class5", new ShipModule(128671258, ShipModule.ModuleTypes.FuelTransferLimpetController, 5, 0.41, "Fuel Transfer Drone Controller Class 3 Rating A", "Range:1.5km") },
            { "int_dronecontrol_fueltransfer_size5_class1", new ShipModule(128671259, ShipModule.ModuleTypes.FuelTransferLimpetController, 20, 0.4, "Fuel Transfer Drone Controller Class 5 Rating E", "Range:0.8km") },
            { "int_dronecontrol_fueltransfer_size5_class2", new ShipModule(128671260, ShipModule.ModuleTypes.FuelTransferLimpetController, 8, 0.3, "Fuel Transfer Drone Controller Class 5 Rating D", "Range:1km") },
            { "int_dronecontrol_fueltransfer_size5_class3", new ShipModule(128671261, ShipModule.ModuleTypes.FuelTransferLimpetController, 20, 0.5, "Fuel Transfer Drone Controller Class 5 Rating C", "Range:1.3km") },
            { "int_dronecontrol_fueltransfer_size5_class4", new ShipModule(128671262, ShipModule.ModuleTypes.FuelTransferLimpetController, 32, 0.97, "Fuel Transfer Drone Controller Class 5 Rating B", "Range:1.6km") },
            { "int_dronecontrol_fueltransfer_size5_class5", new ShipModule(128671263, ShipModule.ModuleTypes.FuelTransferLimpetController, 20, 0.6, "Fuel Transfer Drone Controller Class 5 Rating A", "Range:1.8km") },
            { "int_dronecontrol_fueltransfer_size7_class1", new ShipModule(128671264, ShipModule.ModuleTypes.FuelTransferLimpetController, 80, 0.55, "Fuel Transfer Drone Controller Class 7 Rating E", "Range:1km") },
            { "int_dronecontrol_fueltransfer_size7_class2", new ShipModule(128671265, ShipModule.ModuleTypes.FuelTransferLimpetController, 32, 0.41, "Fuel Transfer Drone Controller Class 7 Rating D", "Range:1.4km") },
            { "int_dronecontrol_fueltransfer_size7_class3", new ShipModule(128671266, ShipModule.ModuleTypes.FuelTransferLimpetController, 80, 0.69, "Fuel Transfer Drone Controller Class 7 Rating C", "Range:1.7km") },
            { "int_dronecontrol_fueltransfer_size7_class4", new ShipModule(128671267, ShipModule.ModuleTypes.FuelTransferLimpetController, 128, 0.97, "Fuel Transfer Drone Controller Class 7 Rating B", "Range:2km") },
            { "int_dronecontrol_fueltransfer_size7_class5", new ShipModule(128671268, ShipModule.ModuleTypes.FuelTransferLimpetController, 80, 0.83, "Fuel Transfer Drone Controller Class 7 Rating A", "Range:2.4km") },

            { "int_dronecontrol_resourcesiphon_size1_class1", new ShipModule(128066532, ShipModule.ModuleTypes.HatchBreakerLimpetController, 1.3, 0.12, "Hatch Breaker Drone Controller Class 1 Rating E", "Time:42s, Range:1.5km") },
            { "int_dronecontrol_resourcesiphon_size1_class2", new ShipModule(128066533, ShipModule.ModuleTypes.HatchBreakerLimpetController, 0.5, 0.16, "Hatch Breaker Drone Controller Class 1 Rating D", "Time:36s, Range:2km") },
            { "int_dronecontrol_resourcesiphon_size1_class3", new ShipModule(128066534, ShipModule.ModuleTypes.HatchBreakerLimpetController, 1.3, 0.2, "Hatch Breaker Drone Controller Class 1 Rating C", "Time:30s, Range:2.5km") },
            { "int_dronecontrol_resourcesiphon_size1_class4", new ShipModule(128066535, ShipModule.ModuleTypes.HatchBreakerLimpetController, 2, 0.24, "Hatch Breaker Drone Controller Class 1 Rating B", "Time:24s, Range:3km") },
            { "int_dronecontrol_resourcesiphon_size1_class5", new ShipModule(128066536, ShipModule.ModuleTypes.HatchBreakerLimpetController, 1.3, 0.28, "Hatch Breaker Drone Controller Class 1 Rating A", "Time:18s, Range:3.5km") },
            { "int_dronecontrol_resourcesiphon_size3_class1", new ShipModule(128066537, ShipModule.ModuleTypes.HatchBreakerLimpetController, 5, 0.18, "Hatch Breaker Drone Controller Class 3 Rating E", "Time:36s, Range:1.6km") },
            { "int_dronecontrol_resourcesiphon_size3_class2", new ShipModule(128066538, ShipModule.ModuleTypes.HatchBreakerLimpetController, 2, 0.24, "Hatch Breaker Drone Controller Class 3 Rating D", "Time:31s, Range:2.2km") },
            { "int_dronecontrol_resourcesiphon_size3_class3", new ShipModule(128066539, ShipModule.ModuleTypes.HatchBreakerLimpetController, 5, 0.3, "Hatch Breaker Drone Controller Class 3 Rating C", "Time:26s, Range:2.7km") },
            { "int_dronecontrol_resourcesiphon_size3_class4", new ShipModule(128066540, ShipModule.ModuleTypes.HatchBreakerLimpetController, 8, 0.36, "Hatch Breaker Drone Controller Class 3 Rating B", "Time:21s, Range:3.2km") },
            { "int_dronecontrol_resourcesiphon_size3_class5", new ShipModule(128066541, ShipModule.ModuleTypes.HatchBreakerLimpetController, 5, 0.42, "Hatch Breaker Drone Controller Class 3 Rating A", "Time:16s, Range:3.8km") },
            { "int_dronecontrol_resourcesiphon_size5_class1", new ShipModule(128066542, ShipModule.ModuleTypes.HatchBreakerLimpetController, 20, 0.3, "Hatch Breaker Drone Controller Class 5 Rating E", "Time:31s, Range:2km") },
            { "int_dronecontrol_resourcesiphon_size5_class2", new ShipModule(128066543, ShipModule.ModuleTypes.HatchBreakerLimpetController, 8, 0.4, "Hatch Breaker Drone Controller Class 5 Rating D", "Time:26s, Range:2.6km") },
            { "int_dronecontrol_resourcesiphon_size5_class3", new ShipModule(128066544, ShipModule.ModuleTypes.HatchBreakerLimpetController, 20, 0.5, "Hatch Breaker Drone Controller Class 5 Rating C", "Time:22s, Range:3.3km") },
            { "int_dronecontrol_resourcesiphon_size5_class4", new ShipModule(128066545, ShipModule.ModuleTypes.HatchBreakerLimpetController, 32, 0.6, "Hatch Breaker Drone Controller Class 5 Rating B", "Time:18s, Range:4km") },
            { "int_dronecontrol_resourcesiphon_size5_class5", new ShipModule(128066546, ShipModule.ModuleTypes.HatchBreakerLimpetController, 20, 0.7, "Hatch Breaker Drone Controller Class 5 Rating A", "Time:13s, Range:4.6km") },
            { "int_dronecontrol_resourcesiphon_size7_class1", new ShipModule(128066547, ShipModule.ModuleTypes.HatchBreakerLimpetController, 80, 0.42, "Hatch Breaker Drone Controller Class 7 Rating E", "Time:25s, Range:2.6km") },
            { "int_dronecontrol_resourcesiphon_size7_class2", new ShipModule(128066548, ShipModule.ModuleTypes.HatchBreakerLimpetController, 32, 0.56, "Hatch Breaker Drone Controller Class 7 Rating D", "Time:22s, Range:3.4km") },
            { "int_dronecontrol_resourcesiphon_size7_class3", new ShipModule(128066549, ShipModule.ModuleTypes.HatchBreakerLimpetController, 80, 0.7, "Hatch Breaker Drone Controller Class 7 Rating C", "Time:18s, Range:4.3km") },
            { "int_dronecontrol_resourcesiphon_size7_class4", new ShipModule(128066550, ShipModule.ModuleTypes.HatchBreakerLimpetController, 128, 0.84, "Hatch Breaker Drone Controller Class 7 Rating B", "Time:14s, Range:5.2km") },
            { "int_dronecontrol_resourcesiphon_size7_class5", new ShipModule(128066551, ShipModule.ModuleTypes.HatchBreakerLimpetController, 80, 0.98, "Hatch Breaker Drone Controller Class 7 Rating A", "Time:11s, Range:6km") },
            { "int_dronecontrol_resourcesiphon", new ShipModule(128066402, ShipModule.ModuleTypes.HatchBreakerLimpetController, 0, 0, "Hatch Breaker Limpet Controller", null) },

            { "int_dronecontrol_prospector_size1_class1", new ShipModule(128671269, ShipModule.ModuleTypes.ProspectorLimpetController, 1.3, 0.18, "Prospector Drone Controller Class 1 Rating E", "Range:3km") },
            { "int_dronecontrol_prospector_size1_class2", new ShipModule(128671270, ShipModule.ModuleTypes.ProspectorLimpetController, 0.5, 0.14, "Prospector Drone Controller Class 1 Rating D", "Range:4km") },
            { "int_dronecontrol_prospector_size1_class3", new ShipModule(128671271, ShipModule.ModuleTypes.ProspectorLimpetController, 1.3, 0.23, "Prospector Drone Controller Class 1 Rating C", "Range:5km") },
            { "int_dronecontrol_prospector_size1_class4", new ShipModule(128671272, ShipModule.ModuleTypes.ProspectorLimpetController, 2, 0.32, "Prospector Drone Controller Class 1 Rating B", "Range:6km") },
            { "int_dronecontrol_prospector_size1_class5", new ShipModule(128671273, ShipModule.ModuleTypes.ProspectorLimpetController, 1.3, 0.28, "Prospector Drone Controller Class 1 Rating A", "Range:7km") },
            { "int_dronecontrol_prospector_size3_class1", new ShipModule(128671274, ShipModule.ModuleTypes.ProspectorLimpetController, 5, 0.27, "Prospector Drone Controller Class 3 Rating E", "Range:3.3km") },
            { "int_dronecontrol_prospector_size3_class2", new ShipModule(128671275, ShipModule.ModuleTypes.ProspectorLimpetController, 2, 0.2, "Prospector Drone Controller Class 3 Rating D", "Range:4.4km") },
            { "int_dronecontrol_prospector_size3_class3", new ShipModule(128671276, ShipModule.ModuleTypes.ProspectorLimpetController, 5, 0.34, "Prospector Drone Controller Class 3 Rating C", "Range:5.5km") },
            { "int_dronecontrol_prospector_size3_class4", new ShipModule(128671277, ShipModule.ModuleTypes.ProspectorLimpetController, 8, 0.48, "Prospector Drone Controller Class 3 Rating B", "Range:6.6km") },
            { "int_dronecontrol_prospector_size3_class5", new ShipModule(128671278, ShipModule.ModuleTypes.ProspectorLimpetController, 5, 0.41, "Prospector Drone Controller Class 3 Rating A", "Range:7.7km") },
            { "int_dronecontrol_prospector_size5_class1", new ShipModule(128671279, ShipModule.ModuleTypes.ProspectorLimpetController, 20, 0.4, "Prospector Drone Controller Class 5 Rating E", "Range:3.9km") },
            { "int_dronecontrol_prospector_size5_class2", new ShipModule(128671280, ShipModule.ModuleTypes.ProspectorLimpetController, 8, 0.3, "Prospector Drone Controller Class 5 Rating D", "Range:5.2km") },
            { "int_dronecontrol_prospector_size5_class3", new ShipModule(128671281, ShipModule.ModuleTypes.ProspectorLimpetController, 20, 0.5, "Prospector Drone Controller Class 5 Rating C", "Range:6.5km") },
            { "int_dronecontrol_prospector_size5_class4", new ShipModule(128671282, ShipModule.ModuleTypes.ProspectorLimpetController, 32, 0.97, "Prospector Drone Controller Class 5 Rating B", "Range:7.8km") },
            { "int_dronecontrol_prospector_size5_class5", new ShipModule(128671283, ShipModule.ModuleTypes.ProspectorLimpetController, 20, 0.6, "Prospector Drone Controller Class 5 Rating A", "Range:9.1km") },
            { "int_dronecontrol_prospector_size7_class1", new ShipModule(128671284, ShipModule.ModuleTypes.ProspectorLimpetController, 80, 0.55, "Prospector Drone Controller Class 7 Rating E", "Range:5.1km") },
            { "int_dronecontrol_prospector_size7_class2", new ShipModule(128671285, ShipModule.ModuleTypes.ProspectorLimpetController, 32, 0.41, "Prospector Drone Controller Class 7 Rating D", "Range:6.8km") },
            { "int_dronecontrol_prospector_size7_class3", new ShipModule(128671286, ShipModule.ModuleTypes.ProspectorLimpetController, 80, 0.69, "Prospector Drone Controller Class 7 Rating C", "Range:8.5km") },
            { "int_dronecontrol_prospector_size7_class4", new ShipModule(128671287, ShipModule.ModuleTypes.ProspectorLimpetController, 128, 0.97, "Prospector Drone Controller Class 7 Rating B", "Range:10.2km") },
            { "int_dronecontrol_prospector_size7_class5", new ShipModule(128671288, ShipModule.ModuleTypes.ProspectorLimpetController, 80, 0.83, "Prospector Drone Controller Class 7 Rating A", "Range:11.9km") },

            { "int_dronecontrol_repair_size1_class1", new ShipModule(128777327, ShipModule.ModuleTypes.RepairLimpetController, 1.3, 0.18, "Repair Drone Controller Class 1 Rating E", "Range:0.6km") },
            { "int_dronecontrol_repair_size1_class2", new ShipModule(128777328, ShipModule.ModuleTypes.RepairLimpetController, 0.5, 0.14, "Repair Drone Controller Class 1 Rating D", "Range:0.8km") },
            { "int_dronecontrol_repair_size1_class3", new ShipModule(128777329, ShipModule.ModuleTypes.RepairLimpetController, 1.3, 0.23, "Repair Drone Controller Class 1 Rating C", "Range:1km") },
            { "int_dronecontrol_repair_size1_class4", new ShipModule(128777330, ShipModule.ModuleTypes.RepairLimpetController, 2, 0.32, "Repair Drone Controller Class 1 Rating B", "Range:1.2km") },
            { "int_dronecontrol_repair_size1_class5", new ShipModule(128777331, ShipModule.ModuleTypes.RepairLimpetController, 1.3, 0.28, "Repair Drone Controller Class 1 Rating A", "Range:1.4km") },
            { "int_dronecontrol_repair_size3_class1", new ShipModule(128777332, ShipModule.ModuleTypes.RepairLimpetController, 5, 0.27, "Repair Drone Controller Class 3 Rating E", "Range:0.7km") },
            { "int_dronecontrol_repair_size3_class2", new ShipModule(128777333, ShipModule.ModuleTypes.RepairLimpetController, 2, 0.2, "Repair Drone Controller Class 3 Rating D", "Range:0.9km") },
            { "int_dronecontrol_repair_size3_class3", new ShipModule(128777334, ShipModule.ModuleTypes.RepairLimpetController, 5, 0.34, "Repair Drone Controller Class 3 Rating C", "Range:1.1km") },
            { "int_dronecontrol_repair_size3_class4", new ShipModule(128777335, ShipModule.ModuleTypes.RepairLimpetController, 8, 0.48, "Repair Drone Controller Class 3 Rating B", "Range:1.3km") },
            { "int_dronecontrol_repair_size3_class5", new ShipModule(128777336, ShipModule.ModuleTypes.RepairLimpetController, 5, 0.41, "Repair Drone Controller Class 3 Rating A", "Range:1.5km") },
            { "int_dronecontrol_repair_size5_class1", new ShipModule(128777337, ShipModule.ModuleTypes.RepairLimpetController, 20, 0.4, "Repair Drone Controller Class 5 Rating E", "Range:0.8km") },
            { "int_dronecontrol_repair_size5_class2", new ShipModule(128777338, ShipModule.ModuleTypes.RepairLimpetController, 8, 0.3, "Repair Drone Controller Class 5 Rating D", "Range:1km") },
            { "int_dronecontrol_repair_size5_class3", new ShipModule(128777339, ShipModule.ModuleTypes.RepairLimpetController, 20, 0.5, "Repair Drone Controller Class 5 Rating C", "Range:1.3km") },
            { "int_dronecontrol_repair_size5_class4", new ShipModule(128777340, ShipModule.ModuleTypes.RepairLimpetController, 32, 0.97, "Repair Drone Controller Class 5 Rating B", "Range:1.6km") },
            { "int_dronecontrol_repair_size5_class5", new ShipModule(128777341, ShipModule.ModuleTypes.RepairLimpetController, 20, 0.6, "Repair Drone Controller Class 5 Rating A", "Range:1.8km") },
            { "int_dronecontrol_repair_size7_class1", new ShipModule(128777342, ShipModule.ModuleTypes.RepairLimpetController, 80, 0.55, "Repair Drone Controller Class 7 Rating E", "Range:1km") },
            { "int_dronecontrol_repair_size7_class2", new ShipModule(128777343, ShipModule.ModuleTypes.RepairLimpetController, 32, 0.41, "Repair Drone Controller Class 7 Rating D", "Range:1.4km") },
            { "int_dronecontrol_repair_size7_class3", new ShipModule(128777344, ShipModule.ModuleTypes.RepairLimpetController, 80, 0.69, "Repair Drone Controller Class 7 Rating C", "Range:1.7km") },
            { "int_dronecontrol_repair_size7_class4", new ShipModule(128777345, ShipModule.ModuleTypes.RepairLimpetController, 128, 0.97, "Repair Drone Controller Class 7 Rating B", "Range:2km") },
            { "int_dronecontrol_repair_size7_class5", new ShipModule(128777346, ShipModule.ModuleTypes.RepairLimpetController, 80, 0.83, "Repair Drone Controller Class 7 Rating A", "Range:2.4km") },

            { "int_dronecontrol_unkvesselresearch", new ShipModule(128793116, ShipModule.ModuleTypes.ResearchLimpetController, 1.3, 0.4, "Drone Controller Vessel Research", "Time:300s, Range:2km") },

            // More limpets

            { "int_dronecontrol_decontamination_size1_class1", new ShipModule(128793941, ShipModule.ModuleTypes.DecontaminationLimpetController, 1.3, 0.18, "Decontamination Drone Controller Class 1 Rating E", "Range:0.6km") },
            { "int_dronecontrol_decontamination_size3_class1", new ShipModule(128793942, ShipModule.ModuleTypes.DecontaminationLimpetController, 2, 0.2, "Decontamination Drone Controller Class 3 Rating E", "Range:0.9km") },
            { "int_dronecontrol_decontamination_size5_class1", new ShipModule(128793943, ShipModule.ModuleTypes.DecontaminationLimpetController, 20, 0.5, "Decontamination Drone Controller Class 5 Rating E", "Range:1.3km") },
            { "int_dronecontrol_decontamination_size7_class1", new ShipModule(128793944, ShipModule.ModuleTypes.DecontaminationLimpetController, 128, 0.97, "Decontamination Drone Controller Class 7 Rating E", "Range:2km") },

            { "int_dronecontrol_recon_size1_class1", new ShipModule(128837858, ShipModule.ModuleTypes.ReconLimpetController, 1.3, 0.18, "Recon Drone Controller Class 1 Rating E", "Range:1.2km") },

            { "int_dronecontrol_recon_size3_class1", new ShipModule(128841592, ShipModule.ModuleTypes.ReconLimpetController, 2, 0.2, "Recon Drone Controller Class 3 Rating E", "Range:1.4km") },
            { "int_dronecontrol_recon_size5_class1", new ShipModule(128841593, ShipModule.ModuleTypes.ReconLimpetController, 20, 0.5, "Recon Drone Controller Class 5 Rating E", "Range:1.7km") },
            { "int_dronecontrol_recon_size7_class1", new ShipModule(128841594, ShipModule.ModuleTypes.ReconLimpetController, 128, 0.97, "Recon Drone Controller Class 7 Rating E", "Range:2km") },

            { "int_multidronecontrol_mining_size3_class1", new ShipModule(129001921, ShipModule.ModuleTypes.MiningMultiLimpetController, 12, 0.5, "Multi Purpose Mining Drone Controller Class 3 Rating E", "Range:3.3km") },
            { "int_multidronecontrol_mining_size3_class3", new ShipModule(129001922, ShipModule.ModuleTypes.MiningMultiLimpetController, 10, 0.35, "Multi Purpose Mining Drone Controller Class 3 Rating C", "Range:5km") },
            { "int_multidronecontrol_operations_size3_class3", new ShipModule(129001923, ShipModule.ModuleTypes.OperationsMultiLimpetController, 10, 0.35, "Multi Purpose Operations Drone Controller Class 3 Rating C", "Range:5km") },
            { "int_multidronecontrol_operations_size3_class4", new ShipModule(129001924, ShipModule.ModuleTypes.OperationsMultiLimpetController, 15, 0.3, "Multi Purpose Operations Drone Controller Class 3 Rating B", "Range:3.1km") },
            { "int_multidronecontrol_rescue_size3_class2", new ShipModule(129001925, ShipModule.ModuleTypes.RescueMultiLimpetController, 8, 0.4, "Multi Purpose Operations Drone Controller Class 3 Rating D", "Range:2.1km") },
            { "int_multidronecontrol_rescue_size3_class3", new ShipModule(129001926, ShipModule.ModuleTypes.RescueMultiLimpetController, 10, 0.35, "Multi Purpose Operations Drone Controller Class 3 Rating C", "Range:2.6km") },
            { "int_multidronecontrol_xeno_size3_class3", new ShipModule(129001927, ShipModule.ModuleTypes.XenoMultiLimpetController, 10, 0.35, "Multi Purpose Xeno Drone Controller Class 3 Rating C", "Range:5km") },
            { "int_multidronecontrol_xeno_size3_class4", new ShipModule(129001928, ShipModule.ModuleTypes.XenoMultiLimpetController, 15, 0.3, "Multi Purpose Xeno Drone Controller Class 3 Rating B", "Range:5km") },
            { "int_multidronecontrol_universal_size7_class3", new ShipModule(129001929, ShipModule.ModuleTypes.UniversalMultiLimpetController, 126, 0.8, "Multi Purpose Universal Drone Controller Class 7 Rating C", "Range:6.5km") },
            { "int_multidronecontrol_universal_size7_class5", new ShipModule(129001930, ShipModule.ModuleTypes.UniversalMultiLimpetController, 140, 1.1, "Multi Purpose Universal Drone Controller Class 7 Rating A", "Range:9.1km") },

            // Meta hull reinforcements

            { "int_metaalloyhullreinforcement_size1_class1", new ShipModule(128793117, ShipModule.ModuleTypes.MetaAlloyHullReinforcement, 2, 0, "Meta Alloy Hull Reinforcement Class 1 Rating E", "Explosive:0%, Kinetic:0%, Thermal:0%") },
            { "int_metaalloyhullreinforcement_size1_class2", new ShipModule(128793118, ShipModule.ModuleTypes.MetaAlloyHullReinforcement, 2, 0, "Meta Alloy Hull Reinforcement Class 1 Rating D", "Explosive:0%, Kinetic:0%, Thermal:0%") },
            { "int_metaalloyhullreinforcement_size2_class1", new ShipModule(128793119, ShipModule.ModuleTypes.MetaAlloyHullReinforcement, 4, 0, "Meta Alloy Hull Reinforcement Class 2 Rating E", "Explosive:0%, Kinetic:0%, Thermal:0%") },
            { "int_metaalloyhullreinforcement_size2_class2", new ShipModule(128793120, ShipModule.ModuleTypes.MetaAlloyHullReinforcement, 2, 0, "Meta Alloy Hull Reinforcement Class 2 Rating D", "Explosive:0%, Kinetic:0%, Thermal:0%") },
            { "int_metaalloyhullreinforcement_size3_class1", new ShipModule(128793121, ShipModule.ModuleTypes.MetaAlloyHullReinforcement, 8, 0, "Meta Alloy Hull Reinforcement Class 3 Rating E", "Explosive:0%, Kinetic:0%, Thermal:0%") },
            { "int_metaalloyhullreinforcement_size3_class2", new ShipModule(128793122, ShipModule.ModuleTypes.MetaAlloyHullReinforcement, 4, 0, "Meta Alloy Hull Reinforcement Class 3 Rating D", "Explosive:0%, Kinetic:0%, Thermal:0%") },
            { "int_metaalloyhullreinforcement_size4_class1", new ShipModule(128793123, ShipModule.ModuleTypes.MetaAlloyHullReinforcement, 16, 0, "Meta Alloy Hull Reinforcement Class 4 Rating E", "Explosive:0%, Kinetic:0%, Thermal:0%") },
            { "int_metaalloyhullreinforcement_size4_class2", new ShipModule(128793124, ShipModule.ModuleTypes.MetaAlloyHullReinforcement, 8, 0, "Meta Alloy Hull Reinforcement Class 4 Rating D", "Explosive:0%, Kinetic:0%, Thermal:0%") },
            { "int_metaalloyhullreinforcement_size5_class1", new ShipModule(128793125, ShipModule.ModuleTypes.MetaAlloyHullReinforcement, 32, 0, "Meta Alloy Hull Reinforcement Class 5 Rating E", "Explosive:0%, Kinetic:0%, Thermal:0%") },
            { "int_metaalloyhullreinforcement_size5_class2", new ShipModule(128793126, ShipModule.ModuleTypes.MetaAlloyHullReinforcement, 16, 0, "Meta Alloy Hull Reinforcement Class 5 Rating D", "Explosive:0%, Kinetic:0%, Thermal:0%") },

            // Mine launches charges

            { "hpt_minelauncher_fixed_small", new ShipModule(128049500, ShipModule.ModuleTypes.MineLauncher, 2, 0.4, "Mine Launcher Fixed Small", "Ammo:36/1, Damage:44, Reload:2s, ThermL:5") },
            { "hpt_minelauncher_fixed_medium", new ShipModule(128049501, ShipModule.ModuleTypes.MineLauncher, 4, 0.4, "Mine Launcher Fixed Medium", "Ammo:72/3, Damage:44, Reload:6.6s, ThermL:7.5") },
            { "hpt_minelauncher_fixed_small_impulse", new ShipModule(128671448, ShipModule.ModuleTypes.ShockMineLauncher, 2, 0.4, "Mine Launcher Fixed Small Impulse", "Ammo:36/1, Damage:32, Reload:2s, ThermL:5") },

            { "hpt_mining_abrblstr_fixed_small", new ShipModule(128915458, ShipModule.ModuleTypes.AbrasionBlaster, 2, 0.34, "Mining Abrasion Blaster Fixed Small", "Damage:4, Range:1000m, Speed:667m/s, Reload:2s, ThermL:1.8") },
            { "hpt_mining_abrblstr_turret_small", new ShipModule(128915459, ShipModule.ModuleTypes.AbrasionBlaster, 2, 0.47, "Mining Abrasion Blaster Turret Small", "Damage:4, Range:1000m, Speed:667m/s, Reload:2s, ThermL:1.8") },

            { "hpt_mining_seismchrgwarhd_fixed_medium", new ShipModule(128915460, ShipModule.ModuleTypes.SeismicChargeLauncher, 4, 1.2, "Mining Seismic Charge Warhead Fixed Medium", "Ammo:72/1, Damage:15, Range:3000m, Speed:350m/s, ThermL:3.6") },
            { "hpt_mining_seismchrgwarhd_turret_medium", new ShipModule(128915461, ShipModule.ModuleTypes.SeismicChargeLauncher, 4, 1.2, "Mining Seismic Charge Warhead Turret Medium", "Ammo:72/1, Damage:15, Range:3000m, Speed:350m/s, ThermL:3.6") },

            { "hpt_mining_subsurfdispmisle_fixed_small", new ShipModule(128915454, ShipModule.ModuleTypes.Sub_SurfaceDisplacementMissile, 2, 0.42, "Mining Subsurface Displacement Missile Fixed Small", "Ammo:32/1, Damage:5, Range:3000m, Speed:550m/s, Reload:2s, ThermL:2.2") },
            { "hpt_mining_subsurfdispmisle_turret_small", new ShipModule(128915455, ShipModule.ModuleTypes.Sub_SurfaceDisplacementMissile, 2, 0.53, "Mining Subsurface Displacement Missile Turret Small", "Ammo:32/1, Damage:5, Range:3000m, Speed:550m/s, Reload:2s, ThermL:2.2") },
            { "hpt_mining_subsurfdispmisle_fixed_medium", new ShipModule(128915456, ShipModule.ModuleTypes.Sub_SurfaceDisplacementMissile, 4, 1.01, "Mining Subsurface Displacement Missile Fixed Medium", "Ammo:96/1, Damage:5, Range:3000m, Speed:550m/s, Reload:2s, ThermL:2.9") },
            { "hpt_mining_subsurfdispmisle_turret_medium", new ShipModule(128915457, ShipModule.ModuleTypes.Sub_SurfaceDisplacementMissile, 4, 0.93, "Mining Subsurface Displacement Missile Turret Medium", "Ammo:96/1, Damage:5, Range:3000m, Speed:550m/s, Reload:2s, ThermL:2.9") },

            // Mining lasers

            { "hpt_mininglaser_fixed_small", new ShipModule(128049525, ShipModule.ModuleTypes.MiningLaser, 2, 0.5, "Mining Laser Fixed Small", "Damage:2, Range:500m, ThermL:2") },
            { "hpt_mininglaser_fixed_medium", new ShipModule(128049526, ShipModule.ModuleTypes.MiningLaser, 2, 0.75, "Mining Laser Fixed Medium", "Damage:4, Range:500m, ThermL:4") },
            { "hpt_mininglaser_turret_small", new ShipModule(128740819, ShipModule.ModuleTypes.MiningLaser, 2, 0.5, "Mining Laser Turret Small", "Damage:2, Range:500m, ThermL:2") },
            { "hpt_mininglaser_turret_medium", new ShipModule(128740820, ShipModule.ModuleTypes.MiningLaser, 2, 0.75, "Mining Laser Turret Medium", "Damage:4, Range:500m, ThermL:4") },
            { "hpt_mininglaser_fixed_small_advanced", new ShipModule(128671340, ShipModule.ModuleTypes.MiningLance, 2, 0.7, "Mining Laser Fixed Small Advanced", "Damage:8, Range:2000m, ThermL:6") },
            
            // Missiles

            { "hpt_atdumbfiremissile_fixed_medium", new ShipModule(128788699, ShipModule.ModuleTypes.AXMissileRack, 4, 1.2, "AX Dumbfire Missile Fixed Medium", "Ammo:64/8, Damage:64, Speed:750m/s, Reload:5s, ThermL:2.4") },
            { "hpt_atdumbfiremissile_fixed_large", new ShipModule(128788700, ShipModule.ModuleTypes.AXMissileRack, 8, 1.62, "AX Dumbfire Missile Fixed Large", "Ammo:128/12, Damage:64, Speed:750m/s, Reload:5s, ThermL:3.6") },
            { "hpt_atdumbfiremissile_turret_medium", new ShipModule(128788704, ShipModule.ModuleTypes.AXMissileRack, 4, 1.2, "AX Dumbfire Missile Turret Medium", "Ammo:64/8, Damage:50, Speed:750m/s, Reload:5s, ThermL:1.5") },
            { "hpt_atdumbfiremissile_turret_large", new ShipModule(128788705, ShipModule.ModuleTypes.AXMissileRack, 8, 1.75, "AX Dumbfire Missile Turret Large", "Ammo:128/12, Damage:64, Speed:750m/s, Reload:5s, ThermL:1.9") },

            { "hpt_atdumbfiremissile_fixed_medium_v2", new ShipModule(129022081, ShipModule.ModuleTypes.EnhancedAXMissileRack, 4, 1.3, "Enhanced AX Missile Rack Medium", "Damage: 16.0/S") },
            { "hpt_atdumbfiremissile_fixed_large_v2", new ShipModule(129022079, ShipModule.ModuleTypes.EnhancedAXMissileRack, 8, 1.72, "Enhanced AX Missile Rack Large", "Damage: 16.0/S") },
            { "hpt_atdumbfiremissile_turret_medium_v2", new ShipModule(129022083, ShipModule.ModuleTypes.EnhancedAXMissileRack, 4, 1.3, "Enhanced AX Missile Rack Medium", "Damage: 12.2/S") },
            { "hpt_atdumbfiremissile_turret_large_v2", new ShipModule(129022082, ShipModule.ModuleTypes.EnhancedAXMissileRack, 8, 1.85, "Enhanced AX Missile Rack Large", "Damage: 12.2/S") },

            { "hpt_atventdisruptorpylon_fixed_medium", new ShipModule(129030049, ShipModule.ModuleTypes.TorpedoPylon, 0, 0, "Guardian Nanite Torpedo Pylon Medium", "") },
            { "hpt_atventdisruptorpylon_fixed_large", new ShipModule(129030050, ShipModule.ModuleTypes.TorpedoPylon, 0, 0, "Guardian Nanite Torpedo Pylon Large", "") },

            { "hpt_basicmissilerack_fixed_small", new ShipModule(128049492, ShipModule.ModuleTypes.SeekerMissileRack, 2, 0.6, "Seeker Missile Rack Fixed Small", "Ammo:6/6, Damage:40, Speed:625m/s, Reload:12s, ThermL:3.6") },
            { "hpt_basicmissilerack_fixed_medium", new ShipModule(128049493, ShipModule.ModuleTypes.SeekerMissileRack, 4, 1.2, "Seeker Missile Rack Fixed Medium", "Ammo:18/6, Damage:40, Speed:625m/s, Reload:12s, ThermL:3.6") },
            { "hpt_basicmissilerack_fixed_large", new ShipModule(128049494, ShipModule.ModuleTypes.SeekerMissileRack, 8, 1.62, "Seeker Missile Rack Fixed Large", "Ammo:36/6, Damage:40, Speed:625m/s, Reload:12s, ThermL:3.6") },

            { "hpt_dumbfiremissilerack_fixed_small", new ShipModule(128666724, ShipModule.ModuleTypes.MissileRack, 2, 0.4, "Dumbfire Missile Rack Fixed Small", "Ammo:16/8, Damage:50, Speed:750m/s, Reload:5s, ThermL:3.6") },
            { "hpt_dumbfiremissilerack_fixed_medium", new ShipModule(128666725, ShipModule.ModuleTypes.MissileRack, 4, 1.2, "Dumbfire Missile Rack Fixed Medium", "Ammo:48/12, Damage:50, Speed:750m/s, Reload:5s, ThermL:3.6") },
            { "hpt_dumbfiremissilerack_fixed_large", new ShipModule(128891602, ShipModule.ModuleTypes.MissileRack, 8, 1.62, "Dumbfire Missile Rack Fixed Large", "Ammo:96/12, Damage:50, Speed:750m/s, Reload:5s, ThermL:3.6") },

            { "hpt_dumbfiremissilerack_fixed_medium_lasso", new ShipModule(128732552, ShipModule.ModuleTypes.RocketPropelledFSDDisruptor, 4, 1.2, "Dumbfire Missile Rack Fixed Medium Lasso", "Ammo:48/12, Damage:40, Speed:750m/s, Reload:5s, ThermL:3.6") },
            { "hpt_drunkmissilerack_fixed_medium", new ShipModule(128671344, ShipModule.ModuleTypes.Pack_HoundMissileRack, 4, 1.2, "Pack Hound Missile Rack Fixed Medium", "Ammo:120/12, Damage:7.5, Speed:600m/s, Reload:5s, ThermL:3.6") },

            { "hpt_advancedtorppylon_fixed_small", new ShipModule(128049509, ShipModule.ModuleTypes.TorpedoPylon, 2, 0.4, "Advanced Torp Pylon Fixed Small", "Ammo:1/1, Damage:120, Speed:250m/s, Reload:5s, ThermL:45") },
            { "hpt_advancedtorppylon_fixed_medium", new ShipModule(128049510, ShipModule.ModuleTypes.TorpedoPylon, 4, 0.4, "Advanced Torp Pylon Fixed Medium", "Ammo:2/1, Damage:120, Speed:250m/s, Reload:5s, ThermL:50") },
            { "hpt_advancedtorppylon_fixed_large", new ShipModule(128049511, ShipModule.ModuleTypes.TorpedoPylon, 8, 0.6, "Advanced Torp Pylon Fixed Large", "Ammo:4/4, Damage:120, Speed:250m/s, Reload:5s, ThermL:55") },

            { "hpt_dumbfiremissilerack_fixed_small_advanced", new ShipModule(128935982, ShipModule.ModuleTypes.AdvancedMissileRack, 1, 0.4, "Dumbfire Missile Rack Fixed Small Advanced", null) },
            { "hpt_dumbfiremissilerack_fixed_medium_advanced", new ShipModule(128935983, ShipModule.ModuleTypes.AdvancedMissileRack, 1, 1.2, "Dumbfire Missile Rack Fixed Medium Advanced", null) },

            { "hpt_human_extraction_fixed_medium", new ShipModule(129028577, ShipModule.ModuleTypes.MissileRack, 1, 1.2, "Human Extraction Missile Medium", null) },

            { "hpt_causticmissile_fixed_medium", new ShipModule(128833995, ShipModule.ModuleTypes.EnzymeMissileRack, 4, 1.2, "Caustic Missile Fixed Medium", "Ammo:64/8, Damage:5, Speed:750m/s, Reload:5s, ThermL:1.5") },

            // Module Reinforcements

            { "int_modulereinforcement_size1_class1", new ShipModule(128737270, ShipModule.ModuleTypes.ModuleReinforcementPackage, 2, 0, "Module Reinforcement Class 1 Rating E", "Protection:0.3") },
            { "int_modulereinforcement_size1_class2", new ShipModule(128737271, ShipModule.ModuleTypes.ModuleReinforcementPackage, 1, 0, "Module Reinforcement Class 1 Rating D", "Protection:0.6") },
            { "int_modulereinforcement_size2_class1", new ShipModule(128737272, ShipModule.ModuleTypes.ModuleReinforcementPackage, 4, 0, "Module Reinforcement Class 2 Rating E", "Protection:0.3") },
            { "int_modulereinforcement_size2_class2", new ShipModule(128737273, ShipModule.ModuleTypes.ModuleReinforcementPackage, 2, 0, "Module Reinforcement Class 2 Rating D", "Protection:0.6") },
            { "int_modulereinforcement_size3_class1", new ShipModule(128737274, ShipModule.ModuleTypes.ModuleReinforcementPackage, 8, 0, "Module Reinforcement Class 3 Rating E", "Protection:0.3") },
            { "int_modulereinforcement_size3_class2", new ShipModule(128737275, ShipModule.ModuleTypes.ModuleReinforcementPackage, 4, 0, "Module Reinforcement Class 3 Rating D", "Protection:0.6") },
            { "int_modulereinforcement_size4_class1", new ShipModule(128737276, ShipModule.ModuleTypes.ModuleReinforcementPackage, 16, 0, "Module Reinforcement Class 4 Rating E", "Protection:0.3") },
            { "int_modulereinforcement_size4_class2", new ShipModule(128737277, ShipModule.ModuleTypes.ModuleReinforcementPackage, 8, 0, "Module Reinforcement Class 4 Rating D", "Protection:0.6") },
            { "int_modulereinforcement_size5_class1", new ShipModule(128737278, ShipModule.ModuleTypes.ModuleReinforcementPackage, 32, 0, "Module Reinforcement Class 5 Rating E", "Protection:0.3") },
            { "int_modulereinforcement_size5_class2", new ShipModule(128737279, ShipModule.ModuleTypes.ModuleReinforcementPackage, 16, 0, "Module Reinforcement Class 5 Rating D", "Protection:0.6") },

            // Multicannons

            { "hpt_atmulticannon_fixed_medium", new ShipModule(128788701, ShipModule.ModuleTypes.AXMulti_Cannon, 4, 0.46, "AX Multi Cannon Fixed Medium", "Ammo:2100/100, Damage:3.3, Range:4000m, Speed:1600m/s, Reload:4s, ThermL:0.2") },
            { "hpt_atmulticannon_fixed_large", new ShipModule(128788702, ShipModule.ModuleTypes.AXMulti_Cannon, 8, 0.64, "AX Multi Cannon Fixed Large", "Ammo:2100/100, Damage:6.1, Range:4000m, Speed:1600m/s, Reload:4s, ThermL:0.3") },

            { "hpt_atmulticannon_turret_medium", new ShipModule(128793059, ShipModule.ModuleTypes.AXMulti_Cannon, 4, 0.5, "AX Multi Cannon Turret Medium", "Ammo:2100/90, Damage:1.7, Range:4000m, Speed:1600m/s, Reload:4s, ThermL:0.1") },
            { "hpt_atmulticannon_turret_large", new ShipModule(128793060, ShipModule.ModuleTypes.AXMulti_Cannon, 8, 0.64, "AX Multi Cannon Turret Large", "Ammo:2100/90, Damage:3.3, Range:4000m, Speed:1600m/s, Reload:4s, ThermL:0.1") },

            { "hpt_atmulticannon_fixed_medium_v2", new ShipModule(129022080, ShipModule.ModuleTypes.EnhancedAXMulti_Cannon, 4, 0.48, "Enhanced AX Multi Cannon Fixed Medium", "Damage: 10/S") },
            { "hpt_atmulticannon_fixed_large_v2", new ShipModule(129022084, ShipModule.ModuleTypes.EnhancedAXMulti_Cannon, 8, 0.69, "Enhanced AX Multi Cannon Fixed Large", "Damage: 15.6/S") },

            { "hpt_atmulticannon_turret_medium_v2", new ShipModule(129022086, ShipModule.ModuleTypes.EnhancedAXMulti_Cannon, 4, 0.52, "Enhanced AX Multi Cannon Turret Medium", "") },
            { "hpt_atmulticannon_turret_large_v2", new ShipModule(129022085, ShipModule.ModuleTypes.EnhancedAXMulti_Cannon, 8, 0.69, "Enhanced AX Multi Cannon Turret Large", "") },

            { "hpt_atmulticannon_gimbal_medium", new ShipModule(129022089, ShipModule.ModuleTypes.EnhancedAXMulti_Cannon, 4, 0.46, "Enhanced AX Multi Cannon Gimbal Medium", "Damage: 9.6/S") },
            { "hpt_atmulticannon_gimbal_large", new ShipModule(129022088, ShipModule.ModuleTypes.EnhancedAXMulti_Cannon, 8, 0.64, "Enhanced AX Multi Cannon Gimbal Large", "Damage: 15.2/S") },

            { "hpt_multicannon_fixed_small", new ShipModule(128049455, ShipModule.ModuleTypes.Multi_Cannon, 2, 0.28, "Multi Cannon Fixed Small", "Ammo:2100/100, Damage:1.1, Range:4000m, Speed:1600m/s, Reload:4s, ThermL:0.1") },
            { "hpt_multicannon_fixed_medium", new ShipModule(128049456, ShipModule.ModuleTypes.Multi_Cannon, 4, 0.46, "Multi Cannon Fixed Medium", "Ammo:2100/100, Damage:2.2, Range:4000m, Speed:1600m/s, Reload:4s, ThermL:0.2") },
            { "hpt_multicannon_fixed_large", new ShipModule(128049457, ShipModule.ModuleTypes.Multi_Cannon, 4, 0.46, "Multi Cannon Fixed Medium", "Ammo:2100/100, Damage:2.2, Range:4000m, Speed:1600m/s, Reload:4s, ThermL:0.2") },
            { "hpt_multicannon_fixed_huge", new ShipModule(128049458, ShipModule.ModuleTypes.Multi_Cannon, 16, 0.73, "Multi Cannon Fixed Huge", "Ammo:2100/100, Damage:4.6, Range:4000m, Speed:1600m/s, Reload:4s, ThermL:0.4") },

            { "hpt_multicannon_gimbal_small", new ShipModule(128049459, ShipModule.ModuleTypes.Multi_Cannon, 2, 0.37, "Multi Cannon Gimbal Small", "Ammo:2100/90, Damage:0.8, Range:4000m, Speed:1600m/s, Reload:5s, ThermL:0.1") },
            { "hpt_multicannon_gimbal_medium", new ShipModule(128049460, ShipModule.ModuleTypes.Multi_Cannon, 4, 0.64, "Multi Cannon Gimbal Medium", "Ammo:2100/90, Damage:1.6, Range:4000m, Speed:1600m/s, Reload:5s, ThermL:0.2") },
            { "hpt_multicannon_gimbal_large", new ShipModule(128049461, ShipModule.ModuleTypes.Multi_Cannon, 8, 0.97, "Multi Cannon Gimbal Large", "Ammo:2100/90, Damage:2.8, Range:4000m, Speed:1600m/s, Reload:5s, ThermL:0.3") },

            { "hpt_multicannon_turret_small", new ShipModule(128049462, ShipModule.ModuleTypes.Multi_Cannon, 2, 0.26, "Multi Cannon Turret Small", "Ammo:2100/90, Damage:0.6, Range:4000m, Speed:1600m/s, Reload:4s, ThermL:0") },
            { "hpt_multicannon_turret_medium", new ShipModule(128049463, ShipModule.ModuleTypes.Multi_Cannon, 4, 0.5, "Multi Cannon Turret Medium", "Ammo:2100/90, Damage:1.2, Range:4000m, Speed:1600m/s, Reload:4s, ThermL:0.1") },
            { "hpt_multicannon_turret_large", new ShipModule(128049464, ShipModule.ModuleTypes.Multi_Cannon, 8, 0.86, "Multi Cannon Turret Large", "Ammo:2100/90, Damage:2.2, Range:4000m, Speed:1600m/s, Reload:4s, ThermL:0.2") },

            { "hpt_multicannon_gimbal_huge", new ShipModule(128681996, ShipModule.ModuleTypes.Multi_Cannon, 16, 1.22, "Multi Cannon Gimbal Huge", "Ammo:2100/90, Damage:3.5, Range:4000m, Speed:1600m/s, Reload:5s, ThermL:0.5") },

            { "hpt_multicannon_fixed_small_strong", new ShipModule(128671345, ShipModule.ModuleTypes.EnforcerCannon, 2, 0.28, "Multi Cannon Fixed Small Strong", "Ammo:1000/60, Damage:2.9, Range:4500m, Speed:1800m/s, Reload:4s, ThermL:0.2") },

            { "hpt_multicannon_fixed_medium_advanced", new ShipModule(128935980, ShipModule.ModuleTypes.AdvancedMulti_Cannon, 1, 0.5, "Multi Cannon Fixed Medium Advanced", null) },
            { "hpt_multicannon_fixed_small_advanced", new ShipModule(128935981, ShipModule.ModuleTypes.AdvancedMulti_Cannon, 1, 0.3, "Multi Cannon Fixed Small Advanced", null) },

            // Passenger cabins

            { "int_passengercabin_size4_class1", new ShipModule(128727922, ShipModule.ModuleTypes.EconomyClassPassengerCabin, 10, 0, "Economy Passenger Cabin Class 4 Rating E", "Passengers:8") },
            { "int_passengercabin_size4_class2", new ShipModule(128727923, ShipModule.ModuleTypes.BusinessClassPassengerCabin, 10, 0, "Business Class Passenger Cabin Class 4 Rating D", "Passengers:6") },
            { "int_passengercabin_size4_class3", new ShipModule(128727924, ShipModule.ModuleTypes.FirstClassPassengerCabin, 10, 0, "First Class Passenger Cabin Class 4 Rating C", "Passengers:3") },
            { "int_passengercabin_size5_class4", new ShipModule(128727925, ShipModule.ModuleTypes.LuxuryClassPassengerCabin, 20, 0, "Luxury Passenger Cabin Class 5 Rating B", "Passengers:4") },
            { "int_passengercabin_size6_class1", new ShipModule(128727926, ShipModule.ModuleTypes.EconomyClassPassengerCabin, 40, 0, "Economy Passenger Cabin Class 6 Rating E", "Passengers:32") },
            { "int_passengercabin_size6_class2", new ShipModule(128727927, ShipModule.ModuleTypes.BusinessClassPassengerCabin, 40, 0, "Business Class Passenger Cabin Class 6 Rating D", "Passengers:16") },
            { "int_passengercabin_size6_class3", new ShipModule(128727928, ShipModule.ModuleTypes.FirstClassPassengerCabin, 40, 0, "First Class Passenger Cabin Class 6 Rating C", "Passengers:12") },
            { "int_passengercabin_size6_class4", new ShipModule(128727929, ShipModule.ModuleTypes.LuxuryClassPassengerCabin, 40, 0, "Luxury Passenger Cabin Class 6 Rating B", "Passengers:8") },

            { "int_passengercabin_size2_class1", new ShipModule(128734690, ShipModule.ModuleTypes.EconomyClassPassengerCabin, 2.5, 0, "Economy Passenger Cabin Class 2 Rating E", "Passengers:2") },
            { "int_passengercabin_size3_class1", new ShipModule(128734691, ShipModule.ModuleTypes.EconomyClassPassengerCabin, 5, 0, "Economy Passenger Cabin Class 3 Rating E", "Passengers:4") },
            { "int_passengercabin_size3_class2", new ShipModule(128734692, ShipModule.ModuleTypes.BusinessClassPassengerCabin, 5, 0, "Business Class Passenger Cabin Class 3 Rating D", "Passengers:3") },
            { "int_passengercabin_size5_class1", new ShipModule(128734693, ShipModule.ModuleTypes.EconomyClassPassengerCabin, 20, 0, "Economy Passenger Cabin Class 5 Rating E", "Passengers:16") },
            { "int_passengercabin_size5_class2", new ShipModule(128734694, ShipModule.ModuleTypes.BusinessClassPassengerCabin, 20, 0, "Business Class Passenger Cabin Class 5 Rating D", "Passengers:10") },
            { "int_passengercabin_size5_class3", new ShipModule(128734695, ShipModule.ModuleTypes.FirstClassPassengerCabin, 20, 0, "First Class Passenger Cabin Class 5 Rating C", "Passengers:6") },

            // Planetary approach

            { "int_planetapproachsuite_advanced", new ShipModule(128975719, ShipModule.ModuleTypes.AdvancedPlanetaryApproachSuite, 0, 0, "Advanced Planet Approach Suite", null) },
            { "int_planetapproachsuite", new ShipModule(128672317, ShipModule.ModuleTypes.PlanetaryApproachSuite, 0, 0, "Planet Approach Suite", null) },

            // planetary hangar

            { "int_buggybay_size2_class1", new ShipModule(128672288, ShipModule.ModuleTypes.PlanetaryVehicleHangar, 12, 0.25, "Planetary Vehicle Hangar Class 2 Rating H", null) },
            { "int_buggybay_size2_class2", new ShipModule(128672289, ShipModule.ModuleTypes.PlanetaryVehicleHangar, 6, 0.75, "Planetary Vehicle Hangar Class 2 Rating G", null) },
            { "int_buggybay_size4_class1", new ShipModule(128672290, ShipModule.ModuleTypes.PlanetaryVehicleHangar, 20, 0.4, "Planetary Vehicle Hangar Class 4 Rating H", null) },
            { "int_buggybay_size4_class2", new ShipModule(128672291, ShipModule.ModuleTypes.PlanetaryVehicleHangar, 10, 1.2, "Planetary Vehicle Hangar Class 4 Rating G", null) },
            { "int_buggybay_size6_class1", new ShipModule(128672292, ShipModule.ModuleTypes.PlanetaryVehicleHangar, 34, 0.6, "Planetary Vehicle Hangar Class 6 Rating H", null) },
            { "int_buggybay_size6_class2", new ShipModule(128672293, ShipModule.ModuleTypes.PlanetaryVehicleHangar, 17, 1.8, "Planetary Vehicle Hangar Class 6 Rating G", null) },

            // Plasmas

            { "hpt_plasmaaccelerator_fixed_medium", new ShipModule(128049465, ShipModule.ModuleTypes.PlasmaAccelerator, 4, 1.43, "Plasma Accelerator Fixed Medium", "Ammo:100/5, Damage:54.3, Range:3500m, Speed:875m/s, Reload:6s, ThermL:15.6") },
            { "hpt_plasmaaccelerator_fixed_large", new ShipModule(128049466, ShipModule.ModuleTypes.PlasmaAccelerator, 8, 1.97, "Plasma Accelerator Fixed Large", "Ammo:100/5, Damage:83.4, Range:3500m, Speed:875m/s, Reload:6s, ThermL:21.8") },
            { "hpt_plasmaaccelerator_fixed_huge", new ShipModule(128049467, ShipModule.ModuleTypes.PlasmaAccelerator, 16, 2.63, "Plasma Accelerator Fixed Huge", "Ammo:100/5, Damage:125.2, Range:3500m, Speed:875m/s, Reload:6s, ThermL:29.5") },
            { "hpt_plasmaaccelerator_fixed_large_advanced", new ShipModule(128671339, ShipModule.ModuleTypes.AdvancedPlasmaAccelerator, 8, 1.97, "Plasma Accelerator Fixed Large Advanced", "Ammo:300/20, Damage:34.5, Range:3500m, Speed:875m/s, Reload:6s, ThermL:11") },

            { "hpt_plasmashockcannon_fixed_large", new ShipModule(128834780, ShipModule.ModuleTypes.ShockCannon, 8, 0.89, "Plasma Shock Cannon Fixed Large", "Ammo:240/16, Damage:18.1, Range:3000m, Speed:1200m/s, Reload:6s, ThermL:2.7") },
            { "hpt_plasmashockcannon_gimbal_large", new ShipModule(128834781, ShipModule.ModuleTypes.ShockCannon, 8, 0.89, "Plasma Shock Cannon Gimbal Large", "Ammo:240/16, Damage:14.9, Range:3000m, Speed:1200m/s, Reload:6s, ThermL:3.1") },
            { "hpt_plasmashockcannon_turret_large", new ShipModule(128834782, ShipModule.ModuleTypes.ShockCannon, 8, 0.64, "Plasma Shock Cannon Turret Large", "Ammo:240/16, Damage:12.3, Range:3000m, Speed:1200m/s, Reload:6s, ThermL:2.2") },

            { "hpt_plasmashockcannon_fixed_medium", new ShipModule(128834002, ShipModule.ModuleTypes.ShockCannon, 4, 0.57, "Plasma Shock Cannon Fixed Medium", "Ammo:240/16, Damage:13, Range:3000m, Speed:1200m/s, Reload:6s, ThermL:1.8") },
            { "hpt_plasmashockcannon_gimbal_medium", new ShipModule(128834003, ShipModule.ModuleTypes.ShockCannon, 4, 0.61, "Plasma Shock Cannon Gimbal Medium", "Ammo:240/16, Damage:10.2, Range:3000m, Speed:1200m/s, Reload:6s, ThermL:2.1") },
            { "hpt_plasmashockcannon_turret_medium", new ShipModule(128834004, ShipModule.ModuleTypes.ShockCannon, 4, 0.5, "Plasma Shock Cannon Turret Medium", "Ammo:240/16, Damage:9, Range:3000m, Speed:1200m/s, Reload:6s, ThermL:1.2") },

            { "hpt_plasmashockcannon_turret_small", new ShipModule(128891603, ShipModule.ModuleTypes.ShockCannon, 2, 0.54, "Plasma Shock Cannon Turret Small", "Ammo:240/16, Damage:4.5, Range:3000m, Speed:1200m/s, Reload:6s, ThermL:0.7") },
            { "hpt_plasmashockcannon_gimbal_small", new ShipModule(128891604, ShipModule.ModuleTypes.ShockCannon, 2, 0.47, "Plasma Shock Cannon Gimbal Small", "Ammo:240/16, Damage:6.9, Range:3000m, Speed:1200m/s, Reload:6s, ThermL:1.5") },
            { "hpt_plasmashockcannon_fixed_small", new ShipModule(128891605, ShipModule.ModuleTypes.ShockCannon, 2, 0.41, "Plasma Shock Cannon Fixed Small", "Ammo:240/16, Damage:8.6, Range:3000m, Speed:1200m/s, Reload:6s, ThermL:1.1") },

            // power distributor

            { "int_powerdistributor_size1_class1", new ShipModule(128064178, ShipModule.ModuleTypes.PowerDistributor, 1.3, 0.32, "Power Distributor Class 1 Rating E", "Sys:0.4MW, Eng:0.4MW, Wep:1.2MW") },
            { "int_powerdistributor_size1_class2", new ShipModule(128064179, ShipModule.ModuleTypes.PowerDistributor, 0.5, 0.36, "Power Distributor Class 1 Rating D", "Sys:0.5MW, Eng:0.5MW, Wep:1.4MW") },
            { "int_powerdistributor_size1_class3", new ShipModule(128064180, ShipModule.ModuleTypes.PowerDistributor, 1.3, 0.4, "Power Distributor Class 1 Rating C", "Sys:0.5MW, Eng:0.5MW, Wep:1.5MW") },
            { "int_powerdistributor_size1_class4", new ShipModule(128064181, ShipModule.ModuleTypes.PowerDistributor, 2, 0.44, "Power Distributor Class 1 Rating B", "Sys:0.6MW, Eng:0.6MW, Wep:1.7MW") },
            { "int_powerdistributor_size1_class5", new ShipModule(128064182, ShipModule.ModuleTypes.PowerDistributor, 1.3, 0.48, "Power Distributor Class 1 Rating A", "Sys:0.6MW, Eng:0.6MW, Wep:1.8MW") },
            { "int_powerdistributor_size2_class1", new ShipModule(128064183, ShipModule.ModuleTypes.PowerDistributor, 2.5, 0.36, "Power Distributor Class 2 Rating E", "Sys:0.6MW, Eng:0.6MW, Wep:1.4MW") },
            { "int_powerdistributor_size2_class2", new ShipModule(128064184, ShipModule.ModuleTypes.PowerDistributor, 1, 0.41, "Power Distributor Class 2 Rating D", "Sys:0.6MW, Eng:0.6MW, Wep:1.6MW") },
            { "int_powerdistributor_size2_class3", new ShipModule(128064185, ShipModule.ModuleTypes.PowerDistributor, 2.5, 0.45, "Power Distributor Class 2 Rating C", "Sys:0.7MW, Eng:0.7MW, Wep:1.8MW") },
            { "int_powerdistributor_size2_class4", new ShipModule(128064186, ShipModule.ModuleTypes.PowerDistributor, 4, 0.5, "Power Distributor Class 2 Rating B", "Sys:0.8MW, Eng:0.8MW, Wep:2MW") },
            { "int_powerdistributor_size2_class5", new ShipModule(128064187, ShipModule.ModuleTypes.PowerDistributor, 2.5, 0.54, "Power Distributor Class 2 Rating A", "Sys:0.8MW, Eng:0.8MW, Wep:2.2MW") },
            { "int_powerdistributor_size3_class1", new ShipModule(128064188, ShipModule.ModuleTypes.PowerDistributor, 5, 0.4, "Power Distributor Class 3 Rating E", "Sys:0.9MW, Eng:0.9MW, Wep:1.8MW") },
            { "int_powerdistributor_size3_class2", new ShipModule(128064189, ShipModule.ModuleTypes.PowerDistributor, 2, 0.45, "Power Distributor Class 3 Rating D", "Sys:1MW, Eng:1MW, Wep:2.1MW") },
            { "int_powerdistributor_size3_class3", new ShipModule(128064190, ShipModule.ModuleTypes.PowerDistributor, 5, 0.5, "Power Distributor Class 3 Rating C", "Sys:1.1MW, Eng:1.1MW, Wep:2.3MW") },
            { "int_powerdistributor_size3_class4", new ShipModule(128064191, ShipModule.ModuleTypes.PowerDistributor, 8, 0.55, "Power Distributor Class 3 Rating B", "Sys:1.2MW, Eng:1.2MW, Wep:2.5MW") },
            { "int_powerdistributor_size3_class5", new ShipModule(128064192, ShipModule.ModuleTypes.PowerDistributor, 5, 0.6, "Power Distributor Class 3 Rating A", "Sys:1.3MW, Eng:1.3MW, Wep:2.8MW") },
            { "int_powerdistributor_size4_class1", new ShipModule(128064193, ShipModule.ModuleTypes.PowerDistributor, 10, 0.45, "Power Distributor Class 4 Rating E", "Sys:1.3MW, Eng:1.3MW, Wep:2.3MW") },
            { "int_powerdistributor_size4_class2", new ShipModule(128064194, ShipModule.ModuleTypes.PowerDistributor, 4, 0.5, "Power Distributor Class 4 Rating D", "Sys:1.4MW, Eng:1.4MW, Wep:2.6MW") },
            { "int_powerdistributor_size4_class3", new ShipModule(128064195, ShipModule.ModuleTypes.PowerDistributor, 10, 0.56, "Power Distributor Class 4 Rating C", "Sys:1.6MW, Eng:1.6MW, Wep:2.9MW") },
            { "int_powerdistributor_size4_class4", new ShipModule(128064196, ShipModule.ModuleTypes.PowerDistributor, 16, 0.62, "Power Distributor Class 4 Rating B", "Sys:1.8MW, Eng:1.8MW, Wep:3.2MW") },
            { "int_powerdistributor_size4_class5", new ShipModule(128064197, ShipModule.ModuleTypes.PowerDistributor, 10, 0.67, "Power Distributor Class 4 Rating A", "Sys:1.9MW, Eng:1.9MW, Wep:3.5MW") },
            { "int_powerdistributor_size5_class1", new ShipModule(128064198, ShipModule.ModuleTypes.PowerDistributor, 20, 0.5, "Power Distributor Class 5 Rating E", "Sys:1.7MW, Eng:1.7MW, Wep:2.9MW") },
            { "int_powerdistributor_size5_class2", new ShipModule(128064199, ShipModule.ModuleTypes.PowerDistributor, 8, 0.56, "Power Distributor Class 5 Rating D", "Sys:1.9MW, Eng:1.9MW, Wep:3.2MW") },
            { "int_powerdistributor_size5_class3", new ShipModule(128064200, ShipModule.ModuleTypes.PowerDistributor, 20, 0.62, "Power Distributor Class 5 Rating C", "Sys:2.1MW, Eng:2.1MW, Wep:3.6MW") },
            { "int_powerdistributor_size5_class4", new ShipModule(128064201, ShipModule.ModuleTypes.PowerDistributor, 32, 0.68, "Power Distributor Class 5 Rating B", "Sys:2.3MW, Eng:2.3MW, Wep:4MW") },
            { "int_powerdistributor_size5_class5", new ShipModule(128064202, ShipModule.ModuleTypes.PowerDistributor, 20, 0.74, "Power Distributor Class 5 Rating A", "Sys:2.5MW, Eng:2.5MW, Wep:4.3MW") },
            { "int_powerdistributor_size6_class1", new ShipModule(128064203, ShipModule.ModuleTypes.PowerDistributor, 40, 0.54, "Power Distributor Class 6 Rating E", "Sys:2.2MW, Eng:2.2MW, Wep:3.4MW") },
            { "int_powerdistributor_size6_class2", new ShipModule(128064204, ShipModule.ModuleTypes.PowerDistributor, 16, 0.61, "Power Distributor Class 6 Rating D", "Sys:2.4MW, Eng:2.4MW, Wep:3.9MW") },
            { "int_powerdistributor_size6_class3", new ShipModule(128064205, ShipModule.ModuleTypes.PowerDistributor, 40, 0.68, "Power Distributor Class 6 Rating C", "Sys:2.7MW, Eng:2.7MW, Wep:4.3MW") },
            { "int_powerdistributor_size6_class4", new ShipModule(128064206, ShipModule.ModuleTypes.PowerDistributor, 64, 0.75, "Power Distributor Class 6 Rating B", "Sys:3MW, Eng:3MW, Wep:4.7MW") },
            { "int_powerdistributor_size6_class5", new ShipModule(128064207, ShipModule.ModuleTypes.PowerDistributor, 40, 0.82, "Power Distributor Class 6 Rating A", "Sys:3.2MW, Eng:3.2MW, Wep:5.2MW") },
            { "int_powerdistributor_size7_class1", new ShipModule(128064208, ShipModule.ModuleTypes.PowerDistributor, 80, 0.59, "Power Distributor Class 7 Rating E", "Sys:2.6MW, Eng:2.6MW, Wep:4.1MW") },
            { "int_powerdistributor_size7_class2", new ShipModule(128064209, ShipModule.ModuleTypes.PowerDistributor, 32, 0.67, "Power Distributor Class 7 Rating D", "Sys:3MW, Eng:3MW, Wep:4.6MW") },
            { "int_powerdistributor_size7_class3", new ShipModule(128064210, ShipModule.ModuleTypes.PowerDistributor, 80, 0.74, "Power Distributor Class 7 Rating C", "Sys:3.3MW, Eng:3.3MW, Wep:5.1MW") },
            { "int_powerdistributor_size7_class4", new ShipModule(128064211, ShipModule.ModuleTypes.PowerDistributor, 128, 0.81, "Power Distributor Class 7 Rating B", "Sys:3.6MW, Eng:3.6MW, Wep:5.6MW") },
            { "int_powerdistributor_size7_class5", new ShipModule(128064212, ShipModule.ModuleTypes.PowerDistributor, 80, 0.89, "Power Distributor Class 7 Rating A", "Sys:4MW, Eng:4MW, Wep:6.1MW") },
            { "int_powerdistributor_size8_class1", new ShipModule(128064213, ShipModule.ModuleTypes.PowerDistributor, 160, 0.64, "Power Distributor Class 8 Rating E", "Sys:3.2MW, Eng:3.2MW, Wep:4.8MW") },
            { "int_powerdistributor_size8_class2", new ShipModule(128064214, ShipModule.ModuleTypes.PowerDistributor, 64, 0.72, "Power Distributor Class 8 Rating D", "Sys:3.6MW, Eng:3.6MW, Wep:5.4MW") },
            { "int_powerdistributor_size8_class3", new ShipModule(128064215, ShipModule.ModuleTypes.PowerDistributor, 160, 0.8, "Power Distributor Class 8 Rating C", "Sys:4MW, Eng:4MW, Wep:6MW") },
            { "int_powerdistributor_size8_class4", new ShipModule(128064216, ShipModule.ModuleTypes.PowerDistributor, 256, 0.88, "Power Distributor Class 8 Rating B", "Sys:4.4MW, Eng:4.4MW, Wep:6.6MW") },
            { "int_powerdistributor_size8_class5", new ShipModule(128064217, ShipModule.ModuleTypes.PowerDistributor, 160, 0.96, "Power Distributor Class 8 Rating A", "Sys:4.8MW, Eng:4.8MW, Wep:7.2MW") },

            { "int_powerdistributor_size1_class1_free", new ShipModule(128666639, ShipModule.ModuleTypes.PowerDistributor, 1.3, 0.32, "Power Distributor Class 1 Rating E", "Sys:0.4MW, Eng:0.4MW, Wep:1.2MW") },

            // Power plant

            { "int_powerplant_size2_class1", new ShipModule(128064033, ShipModule.ModuleTypes.PowerPlant, 2.5, 0, "Powerplant Class 2 Rating E", "Power:6.4MW") },
            { "int_powerplant_size2_class2", new ShipModule(128064034, ShipModule.ModuleTypes.PowerPlant, 1, 0, "Powerplant Class 2 Rating D", "Power:7.2MW") },
            { "int_powerplant_size2_class3", new ShipModule(128064035, ShipModule.ModuleTypes.PowerPlant, 1.3, 0, "Powerplant Class 2 Rating C", "Power:8MW") },
            { "int_powerplant_size2_class4", new ShipModule(128064036, ShipModule.ModuleTypes.PowerPlant, 2, 0, "Powerplant Class 2 Rating B", "Power:8.8MW") },
            { "int_powerplant_size2_class5", new ShipModule(128064037, ShipModule.ModuleTypes.PowerPlant, 1.3, 0, "Powerplant Class 2 Rating A", "Power:9.6MW") },
            { "int_powerplant_size3_class1", new ShipModule(128064038, ShipModule.ModuleTypes.PowerPlant, 5, 0, "Powerplant Class 3 Rating E", "Power:8MW") },
            { "int_powerplant_size3_class2", new ShipModule(128064039, ShipModule.ModuleTypes.PowerPlant, 2, 0, "Powerplant Class 3 Rating D", "Power:9MW") },
            { "int_powerplant_size3_class3", new ShipModule(128064040, ShipModule.ModuleTypes.PowerPlant, 2.5, 0, "Powerplant Class 3 Rating C", "Power:10MW") },
            { "int_powerplant_size3_class4", new ShipModule(128064041, ShipModule.ModuleTypes.PowerPlant, 4, 0, "Powerplant Class 3 Rating B", "Power:11MW") },
            { "int_powerplant_size3_class5", new ShipModule(128064042, ShipModule.ModuleTypes.PowerPlant, 2.5, 0, "Powerplant Class 3 Rating A", "Power:12MW") },
            { "int_powerplant_size4_class1", new ShipModule(128064043, ShipModule.ModuleTypes.PowerPlant, 10, 0, "Powerplant Class 4 Rating E", "Power:10.4MW") },
            { "int_powerplant_size4_class2", new ShipModule(128064044, ShipModule.ModuleTypes.PowerPlant, 4, 0, "Powerplant Class 4 Rating D", "Power:11.7MW") },
            { "int_powerplant_size4_class3", new ShipModule(128064045, ShipModule.ModuleTypes.PowerPlant, 5, 0, "Powerplant Class 4 Rating C", "Power:13MW") },
            { "int_powerplant_size4_class4", new ShipModule(128064046, ShipModule.ModuleTypes.PowerPlant, 8, 0, "Powerplant Class 4 Rating B", "Power:14.3MW") },
            { "int_powerplant_size4_class5", new ShipModule(128064047, ShipModule.ModuleTypes.PowerPlant, 5, 0, "Powerplant Class 4 Rating A", "Power:15.6MW") },
            { "int_powerplant_size5_class1", new ShipModule(128064048, ShipModule.ModuleTypes.PowerPlant, 20, 0, "Powerplant Class 5 Rating E", "Power:13.6MW") },
            { "int_powerplant_size5_class2", new ShipModule(128064049, ShipModule.ModuleTypes.PowerPlant, 8, 0, "Powerplant Class 5 Rating D", "Power:15.3MW") },
            { "int_powerplant_size5_class3", new ShipModule(128064050, ShipModule.ModuleTypes.PowerPlant, 10, 0, "Powerplant Class 5 Rating C", "Power:17MW") },
            { "int_powerplant_size5_class4", new ShipModule(128064051, ShipModule.ModuleTypes.PowerPlant, 16, 0, "Powerplant Class 5 Rating B", "Power:18.7MW") },
            { "int_powerplant_size5_class5", new ShipModule(128064052, ShipModule.ModuleTypes.PowerPlant, 10, 0, "Powerplant Class 5 Rating A", "Power:20.4MW") },
            { "int_powerplant_size6_class1", new ShipModule(128064053, ShipModule.ModuleTypes.PowerPlant, 40, 0, "Powerplant Class 6 Rating E", "Power:16.8MW") },
            { "int_powerplant_size6_class2", new ShipModule(128064054, ShipModule.ModuleTypes.PowerPlant, 16, 0, "Powerplant Class 6 Rating D", "Power:18.9MW") },
            { "int_powerplant_size6_class3", new ShipModule(128064055, ShipModule.ModuleTypes.PowerPlant, 20, 0, "Powerplant Class 6 Rating C", "Power:21MW") },
            { "int_powerplant_size6_class4", new ShipModule(128064056, ShipModule.ModuleTypes.PowerPlant, 32, 0, "Powerplant Class 6 Rating B", "Powter:23.1MW") },
            { "int_powerplant_size6_class5", new ShipModule(128064057, ShipModule.ModuleTypes.PowerPlant, 20, 0, "Powerplant Class 6 Rating A", "Power:25.2MW") },
            { "int_powerplant_size7_class1", new ShipModule(128064058, ShipModule.ModuleTypes.PowerPlant, 80, 0, "Powerplant Class 7 Rating E", "Power:20MW") },
            { "int_powerplant_size7_class2", new ShipModule(128064059, ShipModule.ModuleTypes.PowerPlant, 32, 0, "Powerplant Class 7 Rating D", "Power:22.5MW") },
            { "int_powerplant_size7_class3", new ShipModule(128064060, ShipModule.ModuleTypes.PowerPlant, 40, 0, "Powerplant Class 7 Rating C", "Power:25MW") },
            { "int_powerplant_size7_class4", new ShipModule(128064061, ShipModule.ModuleTypes.PowerPlant, 64, 0, "Powerplant Class 7 Rating B", "Power:27.5MW") },
            { "int_powerplant_size7_class5", new ShipModule(128064062, ShipModule.ModuleTypes.PowerPlant, 40, 0, "Powerplant Class 7 Rating A", "Power:30MW") },
            { "int_powerplant_size8_class1", new ShipModule(128064063, ShipModule.ModuleTypes.PowerPlant, 160, 0, "Powerplant Class 8 Rating E", "Power:24MW") },
            { "int_powerplant_size8_class2", new ShipModule(128064064, ShipModule.ModuleTypes.PowerPlant, 64, 0, "Powerplant Class 8 Rating D", "Power:27MW") },
            { "int_powerplant_size8_class3", new ShipModule(128064065, ShipModule.ModuleTypes.PowerPlant, 80, 0, "Powerplant Class 8 Rating C", "Power:30MW") },
            { "int_powerplant_size8_class4", new ShipModule(128064066, ShipModule.ModuleTypes.PowerPlant, 128, 0, "Powerplant Class 8 Rating B", "Power:33MW") },
            { "int_powerplant_size8_class5", new ShipModule(128064067, ShipModule.ModuleTypes.PowerPlant, 80, 0, "Powerplant Class 8 Rating A", "Power:36MW") },
            { "int_powerplant_size2_class1_free", new ShipModule(128666635, ShipModule.ModuleTypes.PowerPlant, 2.5, 0, "Powerplant Class 2 Rating E", "Power:6.4MW") },

            // Pulse laser

            { "hpt_pulselaser_fixed_small", new ShipModule(128049381, ShipModule.ModuleTypes.PulseLaser, 2, 0.39, "Pulse Laser Fixed Small", "Damage:2.1, Range:3000m, ThermL:0.3") },
            { "hpt_pulselaser_fixed_medium", new ShipModule(128049382, ShipModule.ModuleTypes.PulseLaser, 4, 0.6, "Pulse Laser Fixed Medium", "Damage:3.5, Range:3000m, ThermL:0.6") },
            { "hpt_pulselaser_fixed_large", new ShipModule(128049383, ShipModule.ModuleTypes.PulseLaser, 8, 0.9, "Pulse Laser Fixed Large", "Damage:6, Range:3000m, ThermL:1") },
            { "hpt_pulselaser_fixed_huge", new ShipModule(128049384, ShipModule.ModuleTypes.PulseLaser, 16, 1.33, "Pulse Laser Fixed Huge", "Damage:10.2, Range:3000m, ThermL:1.6") },
            { "hpt_pulselaser_gimbal_small", new ShipModule(128049385, ShipModule.ModuleTypes.PulseLaser, 2, 0.39, "Pulse Laser Gimbal Small", "Damage:1.6, Range:3000m, ThermL:0.3") },
            { "hpt_pulselaser_gimbal_medium", new ShipModule(128049386, ShipModule.ModuleTypes.PulseLaser, 4, 0.6, "Pulse Laser Gimbal Medium", "Damage:2.7, Range:3000m, ThermL:0.5") },
            { "hpt_pulselaser_gimbal_large", new ShipModule(128049387, ShipModule.ModuleTypes.PulseLaser, 8, 0.92, "Pulse Laser Gimbal Large", "Damage:4.6, Range:3000m, ThermL:0.9") },
            { "hpt_pulselaser_turret_small", new ShipModule(128049388, ShipModule.ModuleTypes.PulseLaser, 2, 0.38, "Pulse Laser Turret Small", "Damage:1.2, Range:3000m, ThermL:0.2") },
            { "hpt_pulselaser_turret_medium", new ShipModule(128049389, ShipModule.ModuleTypes.PulseLaser, 4, 0.58, "Pulse Laser Turret Medium", "Damage:2.1, Range:3000m, ThermL:0.3") },
            { "hpt_pulselaser_turret_large", new ShipModule(128049390, ShipModule.ModuleTypes.PulseLaser, 8, 0.89, "Pulse Laser Turret Large", "Damage:3.5, Range:3000m, ThermL:0.6") },
            { "hpt_pulselaser_gimbal_huge", new ShipModule(128681995, ShipModule.ModuleTypes.PulseLaser, 16, 1.37, "Pulse Laser Gimbal Huge", "Damage:7.8, Range:3000m, ThermL:1.6") },

            { "hpt_pulselaser_fixed_smallfree", new ShipModule(128049673, ShipModule.ModuleTypes.PulseLaser, 1, 0.4, "Pulse Laser Fixed Small Free", null) },
            { "hpt_pulselaser_fixed_medium_disruptor", new ShipModule(128671342, ShipModule.ModuleTypes.PulseDisruptorLaser, 4, 0.7, "Pulse Laser Fixed Medium Disruptor", "Damage:2.8, ThermL:1") },

            // Pulse wave Scanner

            { "hpt_mrascanner_size0_class1", new ShipModule(128915718, ShipModule.ModuleTypes.PulseWaveAnalyser, 1.3, 0.2, "Pulse Wave scanner Rating E", null) },
            { "hpt_mrascanner_size0_class2", new ShipModule(128915719, ShipModule.ModuleTypes.PulseWaveAnalyser, 1.3, 0.4, "Pulse Wave scanner Rating D", null) },
            { "hpt_mrascanner_size0_class3", new ShipModule(128915720, ShipModule.ModuleTypes.PulseWaveAnalyser, 1.3, 0.8, "Pulse Wave scanner Rating C", null) },
            { "hpt_mrascanner_size0_class4", new ShipModule(128915721, ShipModule.ModuleTypes.PulseWaveAnalyser, 1.3, 1.6, "Pulse Wave scanner Rating B", null) },
            { "hpt_mrascanner_size0_class5", new ShipModule(128915722, ShipModule.ModuleTypes.PulseWaveAnalyser, 1.3, 3.2, "Pulse Wave scanner Rating A", null) },

            // Rail guns

            { "hpt_railgun_fixed_small", new ShipModule(128049488, ShipModule.ModuleTypes.RailGun, 2, 1.15, "Railgun Fixed Small", "Ammo:80/1, Damage:23.3, Range:3000m, Reload:1s, ThermL:12") },
            { "hpt_railgun_fixed_medium", new ShipModule(128049489, ShipModule.ModuleTypes.RailGun, 4, 1.63, "Railgun Fixed Medium", "Ammo:80/1, Damage:41.5, Range:3000m, Reload:1s, ThermL:20") },
            { "hpt_railgun_fixed_medium_burst", new ShipModule(128671341, ShipModule.ModuleTypes.ImperialHammerRailGun, 4, 1.63, "Railgun Fixed Medium Burst", "Ammo:240/3, Damage:15, Range:3000m, Reload:1s, ThermL:11") },

            // Refineries

            { "int_refinery_size1_class1", new ShipModule(128666684, ShipModule.ModuleTypes.Refinery, 0, 0.14, "Refinery Class 1 Rating E", null) },
            { "int_refinery_size2_class1", new ShipModule(128666685, ShipModule.ModuleTypes.Refinery, 0, 0.17, "Refinery Class 2 Rating E", null) },
            { "int_refinery_size3_class1", new ShipModule(128666686, ShipModule.ModuleTypes.Refinery, 0, 0.2, "Refinery Class 3 Rating E", null) },
            { "int_refinery_size4_class1", new ShipModule(128666687, ShipModule.ModuleTypes.Refinery, 0, 0.25, "Refinery Class 4 Rating E", null) },
            { "int_refinery_size1_class2", new ShipModule(128666688, ShipModule.ModuleTypes.Refinery, 0, 0.18, "Refinery Class 1 Rating D", null) },
            { "int_refinery_size2_class2", new ShipModule(128666689, ShipModule.ModuleTypes.Refinery, 0, 0.22, "Refinery Class 2 Rating D", null) },
            { "int_refinery_size3_class2", new ShipModule(128666690, ShipModule.ModuleTypes.Refinery, 0, 0.27, "Refinery Class 3 Rating D", null) },
            { "int_refinery_size4_class2", new ShipModule(128666691, ShipModule.ModuleTypes.Refinery, 0, 0.33, "Refinery Class 4 Rating D", null) },
            { "int_refinery_size1_class3", new ShipModule(128666692, ShipModule.ModuleTypes.Refinery, 0, 0.23, "Refinery Class 1 Rating C", null) },
            { "int_refinery_size2_class3", new ShipModule(128666693, ShipModule.ModuleTypes.Refinery, 0, 0.28, "Refinery Class 2 Rating C", null) },
            { "int_refinery_size3_class3", new ShipModule(128666694, ShipModule.ModuleTypes.Refinery, 0, 0.34, "Refinery Class 3 Rating C", null) },
            { "int_refinery_size4_class3", new ShipModule(128666695, ShipModule.ModuleTypes.Refinery, 0, 0.41, "Refinery Class 4 Rating C", null) },
            { "int_refinery_size1_class4", new ShipModule(128666696, ShipModule.ModuleTypes.Refinery, 0, 0.28, "Refinery Class 1 Rating B", null) },
            { "int_refinery_size2_class4", new ShipModule(128666697, ShipModule.ModuleTypes.Refinery, 0, 0.34, "Refinery Class 2 Rating B", null) },
            { "int_refinery_size3_class4", new ShipModule(128666698, ShipModule.ModuleTypes.Refinery, 0, 0.41, "Refinery Class 3 Rating B", null) },
            { "int_refinery_size4_class4", new ShipModule(128666699, ShipModule.ModuleTypes.Refinery, 0, 0.49, "Refinery Class 4 Rating B", null) },
            { "int_refinery_size1_class5", new ShipModule(128666700, ShipModule.ModuleTypes.Refinery, 0, 0.32, "Refinery Class 1 Rating A", null) },
            { "int_refinery_size2_class5", new ShipModule(128666701, ShipModule.ModuleTypes.Refinery, 0, 0.39, "Refinery Class 2 Rating A", null) },
            { "int_refinery_size3_class5", new ShipModule(128666702, ShipModule.ModuleTypes.Refinery, 0, 0.48, "Refinery Class 3 Rating A", null) },
            { "int_refinery_size4_class5", new ShipModule(128666703, ShipModule.ModuleTypes.Refinery, 0, 0.57, "Refinery Class 4 Rating A", null) },

            // Sensors

            { "int_sensors_size1_class1", new ShipModule(128064218, ShipModule.ModuleTypes.Sensors, 1.3, 0.16, "Sensors Class 1 Rating E", "Range:4km") },
            { "int_sensors_size1_class2", new ShipModule(128064219, ShipModule.ModuleTypes.Sensors, 0.5, 0.18, "Sensors Class 1 Rating D", "Range:4.5km") },
            { "int_sensors_size1_class3", new ShipModule(128064220, ShipModule.ModuleTypes.Sensors, 1.3, 0.2, "Sensors Class 1 Rating C", "Range:5km") },
            { "int_sensors_size1_class4", new ShipModule(128064221, ShipModule.ModuleTypes.Sensors, 2, 0.33, "Sensors Class 1 Rating B", "Range:5.5km") },
            { "int_sensors_size1_class5", new ShipModule(128064222, ShipModule.ModuleTypes.Sensors, 1.3, 0.6, "Sensors Class 1 Rating A", "Range:6km") },
            { "int_sensors_size2_class1", new ShipModule(128064223, ShipModule.ModuleTypes.Sensors, 2.5, 0.18, "Sensors Class 2 Rating E", "Range:4.2km") },
            { "int_sensors_size2_class2", new ShipModule(128064224, ShipModule.ModuleTypes.Sensors, 1, 0.21, "Sensors Class 2 Rating D", "Range:4.7km") },
            { "int_sensors_size2_class3", new ShipModule(128064225, ShipModule.ModuleTypes.Sensors, 2.5, 0.23, "Sensors Class 2 Rating C", "Range:5.2km") },
            { "int_sensors_size2_class4", new ShipModule(128064226, ShipModule.ModuleTypes.Sensors, 4, 0.38, "Sensors Class 2 Rating B", "Range:5.7km") },
            { "int_sensors_size2_class5", new ShipModule(128064227, ShipModule.ModuleTypes.Sensors, 2.5, 0.69, "Sensors Class 2 Rating A", "Range:6.2km") },
            { "int_sensors_size3_class1", new ShipModule(128064228, ShipModule.ModuleTypes.Sensors, 5, 0.22, "Sensors Class 3 Rating E", "Range:4.3km") },
            { "int_sensors_size3_class2", new ShipModule(128064229, ShipModule.ModuleTypes.Sensors, 2, 0.25, "Sensors Class 3 Rating D", "Range:4.9km") },
            { "int_sensors_size3_class3", new ShipModule(128064230, ShipModule.ModuleTypes.Sensors, 5, 0.28, "Sensors Class 3 Rating C", "Range:5.4km") },
            { "int_sensors_size3_class4", new ShipModule(128064231, ShipModule.ModuleTypes.Sensors, 8, 0.46, "Sensors Class 3 Rating B", "Range:5.9km") },
            { "int_sensors_size3_class5", new ShipModule(128064232, ShipModule.ModuleTypes.Sensors, 5, 0.84, "Sensors Class 3 Rating A", "Range:6.5km") },
            { "int_sensors_size4_class1", new ShipModule(128064233, ShipModule.ModuleTypes.Sensors, 10, 0.27, "Sensors Class 4 Rating E", "Range:4.5km") },
            { "int_sensors_size4_class2", new ShipModule(128064234, ShipModule.ModuleTypes.Sensors, 4, 0.31, "Sensors Class 4 Rating D", "Range:5km") },
            { "int_sensors_size4_class3", new ShipModule(128064235, ShipModule.ModuleTypes.Sensors, 10, 0.34, "Sensors Class 4 Rating C", "Range:5.6km") },
            { "int_sensors_size4_class4", new ShipModule(128064236, ShipModule.ModuleTypes.Sensors, 16, 0.56, "Sensors Class 4 Rating B", "Range:6.2km") },
            { "int_sensors_size4_class5", new ShipModule(128064237, ShipModule.ModuleTypes.Sensors, 10, 1.02, "Sensors Class 4 Rating A", "Range:6.7km") },
            { "int_sensors_size5_class1", new ShipModule(128064238, ShipModule.ModuleTypes.Sensors, 20, 0.33, "Sensors Class 5 Rating E", "Range:4.6km") },
            { "int_sensors_size5_class2", new ShipModule(128064239, ShipModule.ModuleTypes.Sensors, 8, 0.37, "Sensors Class 5 Rating D", "Range:5.2km") },
            { "int_sensors_size5_class3", new ShipModule(128064240, ShipModule.ModuleTypes.Sensors, 20, 0.41, "Sensors Class 5 Rating C", "Range:5.8km") },
            { "int_sensors_size5_class4", new ShipModule(128064241, ShipModule.ModuleTypes.Sensors, 32, 0.68, "Sensors Class 5 Rating B", "Range:6.4km") },
            { "int_sensors_size5_class5", new ShipModule(128064242, ShipModule.ModuleTypes.Sensors, 20, 1.23, "Sensors Class 5 Rating A", "Range:7km") },
            { "int_sensors_size6_class1", new ShipModule(128064243, ShipModule.ModuleTypes.Sensors, 40, 0.4, "Sensors Class 6 Rating E", "Range:4.8km") },
            { "int_sensors_size6_class2", new ShipModule(128064244, ShipModule.ModuleTypes.Sensors, 16, 0.45, "Sensors Class 6 Rating D", "Range:5.4km") },
            { "int_sensors_size6_class3", new ShipModule(128064245, ShipModule.ModuleTypes.Sensors, 40, 0.5, "Sensors Class 6 Rating C", "Range:6km") },
            { "int_sensors_size6_class4", new ShipModule(128064246, ShipModule.ModuleTypes.Sensors, 64, 0.83, "Sensors Class 6 Rating B", "Range:6.6km") },
            { "int_sensors_size6_class5", new ShipModule(128064247, ShipModule.ModuleTypes.Sensors, 40, 1.5, "Sensors Class 6 Rating A", "Range:7.2km") },
            { "int_sensors_size7_class1", new ShipModule(128064248, ShipModule.ModuleTypes.Sensors, 80, 0.47, "Sensors Class 7 Rating E", "Range:5km") },
            { "int_sensors_size7_class2", new ShipModule(128064249, ShipModule.ModuleTypes.Sensors, 32, 0.53, "Sensors Class 7 Rating D", "Range:5.6km") },
            { "int_sensors_size7_class3", new ShipModule(128064250, ShipModule.ModuleTypes.Sensors, 80, 0.59, "Sensors Class 7 Rating C", "Range:6.2km") },
            { "int_sensors_size7_class4", new ShipModule(128064251, ShipModule.ModuleTypes.Sensors, 128, 0.97, "Sensors Class 7 Rating B", "Range:6.8km") },
            { "int_sensors_size7_class5", new ShipModule(128064252, ShipModule.ModuleTypes.Sensors, 80, 1.77, "Sensors Class 7 Rating A", "Range:7.4km") },
            { "int_sensors_size8_class1", new ShipModule(128064253, ShipModule.ModuleTypes.Sensors, 160, 0.55, "Sensors Class 8 Rating E", "Range:5.1km") },
            { "int_sensors_size8_class2", new ShipModule(128064254, ShipModule.ModuleTypes.Sensors, 64, 0.62, "Sensors Class 8 Rating D", "Range:5.8km") },
            { "int_sensors_size8_class3", new ShipModule(128064255, ShipModule.ModuleTypes.Sensors, 160, 0.69, "Sensors Class 8 Rating C", "Range:6.4km") },
            { "int_sensors_size8_class4", new ShipModule(128064256, ShipModule.ModuleTypes.Sensors, 256, 1.14, "Sensors Class 8 Rating B", "Range:7km") },
            { "int_sensors_size8_class5", new ShipModule(128064257, ShipModule.ModuleTypes.Sensors, 160, 2.07, "Sensors Class 8 Rating A", "Range:7.7km") },
            { "int_sensors_size1_class1_free", new ShipModule(128666640, ShipModule.ModuleTypes.Sensors, 1.3, 0.16, "Sensors Class 1 Rating E", "Range:4km") },

            // Shield Boosters

            { "hpt_shieldbooster_size0_class1", new ShipModule(128668532, ShipModule.ModuleTypes.ShieldBooster, 0.5, 0.2, "Shield Booster Rating E", "Boost:4.0%, Explosive:0%, Kinetic:0%, Thermal:0%") },
            { "hpt_shieldbooster_size0_class2", new ShipModule(128668533, ShipModule.ModuleTypes.ShieldBooster, 1, 0.5, "Shield Booster Rating D", "Boost:8.0%, Explosive:0%, Kinetic:0%, Thermal:0%") },
            { "hpt_shieldbooster_size0_class3", new ShipModule(128668534, ShipModule.ModuleTypes.ShieldBooster, 2, 0.7, "Shield Booster Rating C", "Boost:12.0%, Explosive:0%, Kinetic:0%, Thermal:0%") },
            { "hpt_shieldbooster_size0_class4", new ShipModule(128668535, ShipModule.ModuleTypes.ShieldBooster, 3, 1, "Shield Booster Rating B", "Boost:16.0%, Explosive:0%, Kinetic:0%, Thermal:0%") },
            { "hpt_shieldbooster_size0_class5", new ShipModule(128668536, ShipModule.ModuleTypes.ShieldBooster, 3.5, 1.2, "Shield Booster Rating A", "Boost:20.0%, Explosive:0%, Kinetic:0%, Thermal:0%") },

            // cell banks

            { "int_shieldcellbank_size1_class1", new ShipModule(128064298, ShipModule.ModuleTypes.ShieldCellBank, 1.3, 0.41, "Shield Cell Bank Class 1 Rating E", "Ammo:3/1, ThermL:170") },
            { "int_shieldcellbank_size1_class2", new ShipModule(128064299, ShipModule.ModuleTypes.ShieldCellBank, 0.5, 0.55, "Shield Cell Bank Class 1 Rating D", "Ammo:0/1, ThermL:170") },
            { "int_shieldcellbank_size1_class3", new ShipModule(128064300, ShipModule.ModuleTypes.ShieldCellBank, 1.3, 0.69, "Shield Cell Bank Class 1 Rating C", "Ammo:2/1, ThermL:170") },
            { "int_shieldcellbank_size1_class4", new ShipModule(128064301, ShipModule.ModuleTypes.ShieldCellBank, 2, 0.83, "Shield Cell Bank Class 1 Rating B", "Ammo:3/1, ThermL:170") },
            { "int_shieldcellbank_size1_class5", new ShipModule(128064302, ShipModule.ModuleTypes.ShieldCellBank, 1.3, 0.97, "Shield Cell Bank Class 1 Rating A", "Ammo:2/1, ThermL:170") },
            { "int_shieldcellbank_size2_class1", new ShipModule(128064303, ShipModule.ModuleTypes.ShieldCellBank, 2.5, 0.5, "Shield Cell Bank Class 2 Rating E", "Ammo:4/1, ThermL:240") },
            { "int_shieldcellbank_size2_class2", new ShipModule(128064304, ShipModule.ModuleTypes.ShieldCellBank, 1, 0.67, "Shield Cell Bank Class 2 Rating D", "Ammo:2/1, ThermL:240") },
            { "int_shieldcellbank_size2_class3", new ShipModule(128064305, ShipModule.ModuleTypes.ShieldCellBank, 2.5, 0.84, "Shield Cell Bank Class 2 Rating C", "Ammo:3/1, ThermL:240") },
            { "int_shieldcellbank_size2_class4", new ShipModule(128064306, ShipModule.ModuleTypes.ShieldCellBank, 4, 1.01, "Shield Cell Bank Class 2 Rating B", "Ammo:4/1, ThermL:240") },
            { "int_shieldcellbank_size2_class5", new ShipModule(128064307, ShipModule.ModuleTypes.ShieldCellBank, 2.5, 1.18, "Shield Cell Bank Class 2 Rating A", "Ammo:3/1, ThermL:240") },
            { "int_shieldcellbank_size3_class1", new ShipModule(128064308, ShipModule.ModuleTypes.ShieldCellBank, 5, 0.61, "Shield Cell Bank Class 3 Rating E", "Ammo:4/1, ThermL:340") },
            { "int_shieldcellbank_size3_class2", new ShipModule(128064309, ShipModule.ModuleTypes.ShieldCellBank, 2, 0.82, "Shield Cell Bank Class 3 Rating D", "Ammo:2/1, ThermL:340") },
            { "int_shieldcellbank_size3_class3", new ShipModule(128064310, ShipModule.ModuleTypes.ShieldCellBank, 5, 1.02, "Shield Cell Bank Class 3 Rating C", "Ammo:3/1, ThermL:340") },
            { "int_shieldcellbank_size3_class4", new ShipModule(128064311, ShipModule.ModuleTypes.ShieldCellBank, 8, 1.22, "Shield Cell Bank Class 3 Rating B", "Ammo:4/1, ThermL:340") },
            { "int_shieldcellbank_size3_class5", new ShipModule(128064312, ShipModule.ModuleTypes.ShieldCellBank, 5, 1.43, "Shield Cell Bank Class 3 Rating A", "Ammo:3/1, ThermL:340") },
            { "int_shieldcellbank_size4_class1", new ShipModule(128064313, ShipModule.ModuleTypes.ShieldCellBank, 10, 0.74, "Shield Cell Bank Class 4 Rating E", "Ammo:4/1, ThermL:410") },
            { "int_shieldcellbank_size4_class2", new ShipModule(128064314, ShipModule.ModuleTypes.ShieldCellBank, 4, 0.98, "Shield Cell Bank Class 4 Rating D", "Ammo:2/1, ThermL:410") },
            { "int_shieldcellbank_size4_class3", new ShipModule(128064315, ShipModule.ModuleTypes.ShieldCellBank, 10, 1.23, "Shield Cell Bank Class 4 Rating C", "Ammo:3/1, ThermL:410") },
            { "int_shieldcellbank_size4_class4", new ShipModule(128064316, ShipModule.ModuleTypes.ShieldCellBank, 16, 1.48, "Shield Cell Bank Class 4 Rating B", "Ammo:4/1, ThermL:410") },
            { "int_shieldcellbank_size4_class5", new ShipModule(128064317, ShipModule.ModuleTypes.ShieldCellBank, 10, 1.72, "Shield Cell Bank Class 4 Rating A", "Ammo:3/1, ThermL:410") },
            { "int_shieldcellbank_size5_class1", new ShipModule(128064318, ShipModule.ModuleTypes.ShieldCellBank, 20, 0.9, "Shield Cell Bank Class 5 Rating E", "Ammo:4/1, ThermL:540") },
            { "int_shieldcellbank_size5_class2", new ShipModule(128064319, ShipModule.ModuleTypes.ShieldCellBank, 8, 1.2, "Shield Cell Bank Class 5 Rating D", "Ammo:2/1, ThermL:540") },
            { "int_shieldcellbank_size5_class3", new ShipModule(128064320, ShipModule.ModuleTypes.ShieldCellBank, 20, 1.5, "Shield Cell Bank Class 5 Rating C", "Ammo:3/1, ThermL:540") },
            { "int_shieldcellbank_size5_class4", new ShipModule(128064321, ShipModule.ModuleTypes.ShieldCellBank, 32, 1.8, "Shield Cell Bank Class 5 Rating B", "Ammo:4/1, ThermL:540") },
            { "int_shieldcellbank_size5_class5", new ShipModule(128064322, ShipModule.ModuleTypes.ShieldCellBank, 20, 2.1, "Shield Cell Bank Class 5 Rating A", "Ammo:3/1, ThermL:540") },
            { "int_shieldcellbank_size6_class1", new ShipModule(128064323, ShipModule.ModuleTypes.ShieldCellBank, 40, 1.06, "Shield Cell Bank Class 6 Rating E", "Ammo:5/1, ThermL:640") },
            { "int_shieldcellbank_size6_class2", new ShipModule(128064324, ShipModule.ModuleTypes.ShieldCellBank, 16, 1.42, "Shield Cell Bank Class 6 Rating D", "Ammo:3/1, ThermL:640") },
            { "int_shieldcellbank_size6_class3", new ShipModule(128064325, ShipModule.ModuleTypes.ShieldCellBank, 40, 1.77, "Shield Cell Bank Class 6 Rating C", "Ammo:4/1, ThermL:640") },
            { "int_shieldcellbank_size6_class4", new ShipModule(128064326, ShipModule.ModuleTypes.ShieldCellBank, 64, 2.12, "Shield Cell Bank Class 6 Rating B", "Ammo:5/1, ThermL:640") },
            { "int_shieldcellbank_size6_class5", new ShipModule(128064327, ShipModule.ModuleTypes.ShieldCellBank, 40, 2.48, "Shield Cell Bank Class 6 Rating A", "Ammo:4/1, ThermL:640") },
            { "int_shieldcellbank_size7_class1", new ShipModule(128064328, ShipModule.ModuleTypes.ShieldCellBank, 80, 1.24, "Shield Cell Bank Class 7 Rating E", "Ammo:5/1, ThermL:720") },
            { "int_shieldcellbank_size7_class2", new ShipModule(128064329, ShipModule.ModuleTypes.ShieldCellBank, 32, 1.66, "Shield Cell Bank Class 7 Rating D", "Ammo:3/1, ThermL:720") },
            { "int_shieldcellbank_size7_class3", new ShipModule(128064330, ShipModule.ModuleTypes.ShieldCellBank, 80, 2.07, "Shield Cell Bank Class 7 Rating C", "Ammo:4/1, ThermL:720") },
            { "int_shieldcellbank_size7_class4", new ShipModule(128064331, ShipModule.ModuleTypes.ShieldCellBank, 128, 2.48, "Shield Cell Bank Class 7 Rating B", "Ammo:5/1, ThermL:720") },
            { "int_shieldcellbank_size7_class5", new ShipModule(128064332, ShipModule.ModuleTypes.ShieldCellBank, 80, 2.9, "Shield Cell Bank Class 7 Rating A", "Ammo:4/1, ThermL:720") },
            { "int_shieldcellbank_size8_class1", new ShipModule(128064333, ShipModule.ModuleTypes.ShieldCellBank, 160, 1.44, "Shield Cell Bank Class 8 Rating E", "Ammo:5/1, ThermL:800") },
            { "int_shieldcellbank_size8_class2", new ShipModule(128064334, ShipModule.ModuleTypes.ShieldCellBank, 64, 1.92, "Shield Cell Bank Class 8 Rating D", "Ammo:3/1, ThermL:800") },
            { "int_shieldcellbank_size8_class3", new ShipModule(128064335, ShipModule.ModuleTypes.ShieldCellBank, 160, 2.4, "Shield Cell Bank Class 8 Rating C", "Ammo:4/1, ThermL:800") },
            { "int_shieldcellbank_size8_class4", new ShipModule(128064336, ShipModule.ModuleTypes.ShieldCellBank, 256, 2.88, "Shield Cell Bank Class 8 Rating B", "Ammo:5/1, ThermL:800") },
            { "int_shieldcellbank_size8_class5", new ShipModule(128064337, ShipModule.ModuleTypes.ShieldCellBank, 160, 3.36, "Shield Cell Bank Class 8 Rating A", "Ammo:4/1, ThermL:800") },

            // Shield Generators

            { "int_shieldgenerator_size1_class1", new ShipModule(128064258, ShipModule.ModuleTypes.ShieldGenerator, 1.3, 0.72, "Shield Generator Class 1 Rating E", null) },
            { "int_shieldgenerator_size1_class2", new ShipModule(128064259, ShipModule.ModuleTypes.ShieldGenerator, 0.5, 0.96, "Shield Generator Class 1 Rating E", null) },
            { "int_shieldgenerator_size1_class3", new ShipModule(128064260, ShipModule.ModuleTypes.ShieldGenerator, 1.3, 1.2, "Shield Generator Class 1 Rating E", null) },
            { "int_shieldgenerator_size1_class5", new ShipModule(128064262, ShipModule.ModuleTypes.ShieldGenerator, 1.3, 1.68, "Shield Generator Class 1 Rating A", "OptMass:25t, MaxMass:63t, MinMass:13t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size2_class1", new ShipModule(128064263, ShipModule.ModuleTypes.ShieldGenerator, 2.5, 0.9, "Shield Generator Class 2 Rating E", "OptMass:55t, MaxMass:138t, MinMass:28t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size2_class2", new ShipModule(128064264, ShipModule.ModuleTypes.ShieldGenerator, 1, 1.2, "Shield Generator Class 2 Rating D", "OptMass:55t, MaxMass:138t, MinMass:28t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size2_class3", new ShipModule(128064265, ShipModule.ModuleTypes.ShieldGenerator, 2.5, 1.5, "Shield Generator Class 2 Rating C", "OptMass:55t, MaxMass:138t, MinMass:28t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size2_class4", new ShipModule(128064266, ShipModule.ModuleTypes.ShieldGenerator, 4, 1.8, "Shield Generator Class 2 Rating B", "OptMass:55t, MaxMass:138t, MinMass:28t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size2_class5", new ShipModule(128064267, ShipModule.ModuleTypes.ShieldGenerator, 2.5, 2.1, "Shield Generator Class 2 Rating A", "OptMass:55t, MaxMass:138t, MinMass:28t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size3_class1", new ShipModule(128064268, ShipModule.ModuleTypes.ShieldGenerator, 5, 1.08, "Shield Generator Class 3 Rating E", "OptMass:165t, MaxMass:413t, MinMass:83t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size3_class2", new ShipModule(128064269, ShipModule.ModuleTypes.ShieldGenerator, 2, 1.44, "Shield Generator Class 3 Rating D", "OptMass:165t, MaxMass:413t, MinMass:83t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size3_class3", new ShipModule(128064270, ShipModule.ModuleTypes.ShieldGenerator, 5, 1.8, "Shield Generator Class 3 Rating C", "OptMass:165t, MaxMass:413t, MinMass:83t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size3_class4", new ShipModule(128064271, ShipModule.ModuleTypes.ShieldGenerator, 8, 2.16, "Shield Generator Class 3 Rating B", "OptMass:165t, MaxMass:413t, MinMass:83t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size3_class5", new ShipModule(128064272, ShipModule.ModuleTypes.ShieldGenerator, 5, 2.52, "Shield Generator Class 3 Rating A", "OptMass:165t, MaxMass:413t, MinMass:83t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size4_class1", new ShipModule(128064273, ShipModule.ModuleTypes.ShieldGenerator, 10, 1.32, "Shield Generator Class 4 Rating E", "OptMass:285t, MaxMass:713t, MinMass:143t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size4_class2", new ShipModule(128064274, ShipModule.ModuleTypes.ShieldGenerator, 4, 1.76, "Shield Generator Class 4 Rating D", "OptMass:285t, MaxMass:713t, MinMass:143t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size4_class3", new ShipModule(128064275, ShipModule.ModuleTypes.ShieldGenerator, 10, 2.2, "Shield Generator Class 4 Rating C", "OptMass:285t, MaxMass:713t, MinMass:143t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size4_class4", new ShipModule(128064276, ShipModule.ModuleTypes.ShieldGenerator, 16, 2.64, "Shield Generator Class 4 Rating B", "OptMass:285t, MaxMass:713t, MinMass:143t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size4_class5", new ShipModule(128064277, ShipModule.ModuleTypes.ShieldGenerator, 10, 3.08, "Shield Generator Class 4 Rating A", "OptMass:285t, MaxMass:713t, MinMass:143t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size5_class1", new ShipModule(128064278, ShipModule.ModuleTypes.ShieldGenerator, 20, 1.56, "Shield Generator Class 5 Rating E", "OptMass:405t, MaxMass:1013t, MinMass:203t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size5_class2", new ShipModule(128064279, ShipModule.ModuleTypes.ShieldGenerator, 8, 2.08, "Shield Generator Class 5 Rating D", "OptMass:405t, MaxMass:1013t, MinMass:203t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size5_class3", new ShipModule(128064280, ShipModule.ModuleTypes.ShieldGenerator, 20, 2.6, "Shield Generator Class 5 Rating C", "OptMass:405t, MaxMass:1013t, MinMass:203t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size5_class4", new ShipModule(128064281, ShipModule.ModuleTypes.ShieldGenerator, 32, 3.12, "Shield Generator Class 5 Rating B", "OptMass:405t, MaxMass:1013t, MinMass:203t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size5_class5", new ShipModule(128064282, ShipModule.ModuleTypes.ShieldGenerator, 20, 3.64, "Shield Generator Class 5 Rating A", "OptMass:405t, MaxMass:1013t, MinMass:203t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size6_class1", new ShipModule(128064283, ShipModule.ModuleTypes.ShieldGenerator, 40, 1.86, "Shield Generator Class 6 Rating E", "OptMass:540t, MaxMass:1350t, MinMass:270t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size6_class2", new ShipModule(128064284, ShipModule.ModuleTypes.ShieldGenerator, 16, 2.48, "Shield Generator Class 6 Rating D", "OptMass:540t, MaxMass:1350t, MinMass:270t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size6_class3", new ShipModule(128064285, ShipModule.ModuleTypes.ShieldGenerator, 40, 3.1, "Shield Generator Class 6 Rating C", "OptMass:540t, MaxMass:1350t, MinMass:270t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size6_class4", new ShipModule(128064286, ShipModule.ModuleTypes.ShieldGenerator, 64, 3.72, "Shield Generator Class 6 Rating B", "OptMass:540t, MaxMass:1350t, MinMass:270t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size6_class5", new ShipModule(128064287, ShipModule.ModuleTypes.ShieldGenerator, 40, 4.34, "Shield Generator Class 6 Rating A", "OptMass:540t, MaxMass:1350t, MinMass:270t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size7_class1", new ShipModule(128064288, ShipModule.ModuleTypes.ShieldGenerator, 80, 2.1, "Shield Generator Class 7 Rating E", "OptMass:1060t, MaxMass:2650t, MinMass:530t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size7_class2", new ShipModule(128064289, ShipModule.ModuleTypes.ShieldGenerator, 32, 2.8, "Shield Generator Class 7 Rating D", "OptMass:1060t, MaxMass:2650t, MinMass:530t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size7_class3", new ShipModule(128064290, ShipModule.ModuleTypes.ShieldGenerator, 80, 3.5, "Shield Generator Class 7 Rating C", "OptMass:1060t, MaxMass:2650t, MinMass:530t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size7_class4", new ShipModule(128064291, ShipModule.ModuleTypes.ShieldGenerator, 128, 4.2, "Shield Generator Class 7 Rating B", "OptMass:1060t, MaxMass:2650t, MinMass:530t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size7_class5", new ShipModule(128064292, ShipModule.ModuleTypes.ShieldGenerator, 80, 4.9, "Shield Generator Class 7 Rating A", "OptMass:1060t, MaxMass:2650t, MinMass:530t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size8_class1", new ShipModule(128064293, ShipModule.ModuleTypes.ShieldGenerator, 160, 2.4, "Shield Generator Class 8 Rating E", "OptMass:1800t, MaxMass:4500t, MinMass:900t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size8_class2", new ShipModule(128064294, ShipModule.ModuleTypes.ShieldGenerator, 64, 3.2, "Shield Generator Class 8 Rating D", "OptMass:1800t, MaxMass:4500t, MinMass:900t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size8_class3", new ShipModule(128064295, ShipModule.ModuleTypes.ShieldGenerator, 160, 4, "Shield Generator Class 8 Rating C", "OptMass:1800t, MaxMass:4500t, MinMass:900t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size8_class4", new ShipModule(128064296, ShipModule.ModuleTypes.ShieldGenerator, 256, 4.8, "Shield Generator Class 8 Rating B", "OptMass:1800t, MaxMass:4500t, MinMass:900t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size8_class5", new ShipModule(128064297, ShipModule.ModuleTypes.ShieldGenerator, 160, 5.6, "Shield Generator Class 8 Rating A", "OptMass:1800t, MaxMass:4500t, MinMass:900t, Explosive:50%, Kinetic:40%, Thermal:-20%") },

            { "int_shieldgenerator_size2_class1_free", new ShipModule(128666641, ShipModule.ModuleTypes.ShieldGenerator, 2.5, 0.9, "Shield Generator Class 2 Rating E", "OptMass:55t, MaxMass:138t, MinMass:28t, Explosive:50%, Kinetic:40%, Thermal:-20%") },

            { "int_shieldgenerator_size1_class5_strong", new ShipModule(128671323, ShipModule.ModuleTypes.PrismaticShieldGenerator, 2.6, 2.52, "Shield Generator Class 1 Rating A Strong", "OptMass:25t, MaxMass:63t, MinMass:13t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size2_class5_strong", new ShipModule(128671324, ShipModule.ModuleTypes.PrismaticShieldGenerator, 5, 3.15, "Shield Generator Class 2 Rating A Strong", "OptMass:55t, MaxMass:138t, MinMass:23t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size3_class5_strong", new ShipModule(128671325, ShipModule.ModuleTypes.PrismaticShieldGenerator, 10, 3.78, "Shield Generator Class 3 Rating A Strong", "OptMass:165t, MaxMass:413t, MinMass:83t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size4_class5_strong", new ShipModule(128671326, ShipModule.ModuleTypes.PrismaticShieldGenerator, 20, 4.62, "Shield Generator Class 4 Rating A Strong", "OptMass:285t, MaxMass:713t, MinMass:143t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size5_class5_strong", new ShipModule(128671327, ShipModule.ModuleTypes.PrismaticShieldGenerator, 40, 5.46, "Shield Generator Class 5 Rating A Strong", "OptMass:405t, MaxMass:1013t, MinMass:203t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size6_class5_strong", new ShipModule(128671328, ShipModule.ModuleTypes.PrismaticShieldGenerator, 80, 6.51, "Shield Generator Class 6 Rating A Strong", "OptMass:540t, MaxMass:1350t, MinMass:270t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size7_class5_strong", new ShipModule(128671329, ShipModule.ModuleTypes.PrismaticShieldGenerator, 160, 7.35, "Shield Generator Class 7 Rating A Strong", "OptMass:1060t, MaxMass:2650t, MinMass:530t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size8_class5_strong", new ShipModule(128671330, ShipModule.ModuleTypes.PrismaticShieldGenerator, 320, 8.4, "Shield Generator Class 8 Rating A Strong", "OptMass:1800t, MaxMass:4500t, MinMass:900t, Explosive:50%, Kinetic:40%, Thermal:-20%") },

            { "int_shieldgenerator_size1_class3_fast", new ShipModule(128671331, ShipModule.ModuleTypes.Bi_WeaveShieldGenerator, 1.3, 1.2, "Shield Generator Class 1 Rating C Fast", "OptMass:25t, MaxMass:63t, MinMass:13t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size2_class3_fast", new ShipModule(128671332, ShipModule.ModuleTypes.Bi_WeaveShieldGenerator, 2.5, 1.5, "Shield Generator Class 2 Rating C Fast", "OptMass:55t, MaxMass:138t, MinMass:28t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size3_class3_fast", new ShipModule(128671333, ShipModule.ModuleTypes.Bi_WeaveShieldGenerator, 5, 1.8, "Shield Generator Class 3 Rating C Fast", "OptMass:165t, MaxMass:413t, MinMass:83t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size4_class3_fast", new ShipModule(128671334, ShipModule.ModuleTypes.Bi_WeaveShieldGenerator, 10, 2.2, "Shield Generator Class 4 Rating C Fast", "OptMass:285t, MaxMass:713t, MinMass:143t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size5_class3_fast", new ShipModule(128671335, ShipModule.ModuleTypes.Bi_WeaveShieldGenerator, 20, 2.6, "Shield Generator Class 5 Rating C Fast", "OptMass:405t, MaxMass:1013t, MinMass:203t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size6_class3_fast", new ShipModule(128671336, ShipModule.ModuleTypes.Bi_WeaveShieldGenerator, 40, 3.1, "Shield Generator Class 6 Rating C Fast", "OptMass:540t, MaxMass:1350t, MinMass:270t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size7_class3_fast", new ShipModule(128671337, ShipModule.ModuleTypes.Bi_WeaveShieldGenerator, 80, 3.5, "Shield Generator Class 7 Rating C Fast", "OptMass:1060t, MaxMass:2650t, MinMass:530t, Explosive:50%, Kinetic:40%, Thermal:-20%") },
            { "int_shieldgenerator_size8_class3_fast", new ShipModule(128671338, ShipModule.ModuleTypes.Bi_WeaveShieldGenerator, 160, 4, "Shield Generator Class 8 Rating C Fast", "OptMass:1800t, MaxMass:4500t, MinMass:900t, Explosive:50%, Kinetic:40%, Thermal:-20%") },

            // shield shutdown neutraliser

            { "hpt_antiunknownshutdown_tiny", new ShipModule(128771884, ShipModule.ModuleTypes.ShutdownFieldNeutraliser, 1.3, 0.2, "Shutdown Field Neutraliser", "Range:3000m") },
            { "hpt_antiunknownshutdown_tiny_v2", new ShipModule(129022663, ShipModule.ModuleTypes.ShutdownFieldNeutraliser, 1.3, 0.2, "Enhanced Shutdown Field Neutraliser", "Range:3000m") },

            // weapon stabliser
            { "int_expmodulestabiliser_size3_class3", new ShipModule(129019260, ShipModule.ModuleTypes.ExperimentalWeaponStabiliser, 8, 1.5, "Exp Module Weapon Stabiliser Class 3 Rating F", null) },
            { "int_expmodulestabiliser_size5_class3", new ShipModule(129019261, ShipModule.ModuleTypes.ExperimentalWeaponStabiliser, 20, 3, "Exp Module Weapon Stabiliser Class 5 Rating F", null) },

            // supercruise
            { "int_supercruiseassist", new ShipModule(128932273, ShipModule.ModuleTypes.SupercruiseAssist, 0, 0.3, "Supercruise Assist", null) },

            // stellar scanners

            { "int_stellarbodydiscoveryscanner_standard_free", new ShipModule(128666642, ShipModule.ModuleTypes.DiscoveryScanner, 2, 0, "Stellar Body Discovery Scanner Standard", "Range:500ls") },
            { "int_stellarbodydiscoveryscanner_standard", new ShipModule(128662535, ShipModule.ModuleTypes.DiscoveryScanner, 2, 0, "Stellar Body Discovery Scanner Standard", "Range:500ls") },
            { "int_stellarbodydiscoveryscanner_intermediate", new ShipModule(128663560, ShipModule.ModuleTypes.DiscoveryScanner, 2, 0, "Stellar Body Discovery Scanner Intermediate", "Range:1000ls") },
            { "int_stellarbodydiscoveryscanner_advanced", new ShipModule(128663561, ShipModule.ModuleTypes.DiscoveryScanner, 2, 0, "Stellar Body Discovery Scanner Advanced", null) },

            // thrusters

            { "int_engine_size2_class1", new ShipModule(128064068, ShipModule.ModuleTypes.Thrusters, 2.5, 2, "Thrusters Class 2 Rating E", "OptMass:48t, MaxMass:72t, MinMass:24t") },
            { "int_engine_size2_class2", new ShipModule(128064069, ShipModule.ModuleTypes.Thrusters, 1, 2.25, "Thrusters Class 2 Rating D", "OptMass:54t, MaxMass:81t, MinMass:27t") },
            { "int_engine_size2_class3", new ShipModule(128064070, ShipModule.ModuleTypes.Thrusters, 2.5, 2.5, "Thrusters Class 2 Rating C", "OptMass:60t, MaxMass:90t, MinMass:30t") },
            { "int_engine_size2_class4", new ShipModule(128064071, ShipModule.ModuleTypes.Thrusters, 4, 2.75, "Thrusters Class 2 Rating B", "OptMass:66t, MaxMass:99t, MinMass:33t") },
            { "int_engine_size2_class5", new ShipModule(128064072, ShipModule.ModuleTypes.Thrusters, 2.5, 3, "Thrusters Class 2 Rating A", "OptMass:72t, MaxMass:108t, MinMass:36t") },
            { "int_engine_size3_class1", new ShipModule(128064073, ShipModule.ModuleTypes.Thrusters, 5, 2.48, "Thrusters Class 3 Rating E", "OptMass:80t, MaxMass:120t, MinMass:40t") },
            { "int_engine_size3_class2", new ShipModule(128064074, ShipModule.ModuleTypes.Thrusters, 2, 2.79, "Thrusters Class 3 Rating D", "OptMass:90t, MaxMass:135t, MinMass:45t") },
            { "int_engine_size3_class3", new ShipModule(128064075, ShipModule.ModuleTypes.Thrusters, 5, 3.1, "Thrusters Class 3 Rating C", "OptMass:100t, MaxMass:150t, MinMass:50t") },
            { "int_engine_size3_class4", new ShipModule(128064076, ShipModule.ModuleTypes.Thrusters, 8, 3.41, "Thrusters Class 3 Rating B", "OptMass:110t, MaxMass:165t, MinMass:55t") },
            { "int_engine_size3_class5", new ShipModule(128064077, ShipModule.ModuleTypes.Thrusters, 5, 3.72, "Thrusters Class 3 Rating A", "OptMass:120t, MaxMass:180t, MinMass:60t") },
            { "int_engine_size4_class1", new ShipModule(128064078, ShipModule.ModuleTypes.Thrusters, 10, 3.28, "Thrusters Class 4 Rating E", "OptMass:280t, MaxMass:420t, MinMass:140t") },
            { "int_engine_size4_class2", new ShipModule(128064079, ShipModule.ModuleTypes.Thrusters, 4, 3.69, "Thrusters Class 4 Rating D", "OptMass:315t, MaxMass:472t, MinMass:158t") },
            { "int_engine_size4_class3", new ShipModule(128064080, ShipModule.ModuleTypes.Thrusters, 10, 4.1, "Thrusters Class 4 Rating C", "OptMass:350t, MaxMass:525t, MinMass:175t") },
            { "int_engine_size4_class4", new ShipModule(128064081, ShipModule.ModuleTypes.Thrusters, 16, 4.51, "Thrusters Class 4 Rating B", "OptMass:385t, MaxMass:578t, MinMass:192t") },
            { "int_engine_size4_class5", new ShipModule(128064082, ShipModule.ModuleTypes.Thrusters, 10, 4.92, "Thrusters Class 4 Rating A", "OptMass:420t, MaxMass:630t, MinMass:210t") },
            { "int_engine_size5_class1", new ShipModule(128064083, ShipModule.ModuleTypes.Thrusters, 20, 4.08, "Thrusters Class 5 Rating E", "OptMass:560t, MaxMass:840t, MinMass:280t") },
            { "int_engine_size5_class2", new ShipModule(128064084, ShipModule.ModuleTypes.Thrusters, 8, 4.59, "Thrusters Class 5 Rating D", "OptMass:630t, MaxMass:945t, MinMass:315t") },
            { "int_engine_size5_class3", new ShipModule(128064085, ShipModule.ModuleTypes.Thrusters, 20, 5.1, "Thrusters Class 5 Rating C", "OptMass:700t, MaxMass:1050t, MinMass:350t") },
            { "int_engine_size5_class4", new ShipModule(128064086, ShipModule.ModuleTypes.Thrusters, 32, 5.61, "Thrusters Class 5 Rating B", "OptMass:770t, MaxMass:1155t, MinMass:385t") },
            { "int_engine_size5_class5", new ShipModule(128064087, ShipModule.ModuleTypes.Thrusters, 20, 6.12, "Thrusters Class 5 Rating A", "OptMass:840t, MaxMass:1260t, MinMass:420t") },
            { "int_engine_size6_class1", new ShipModule(128064088, ShipModule.ModuleTypes.Thrusters, 40, 5.04, "Thrusters Class 6 Rating E", "OptMass:960t, MaxMass:1440t, MinMass:480t") },
            { "int_engine_size6_class2", new ShipModule(128064089, ShipModule.ModuleTypes.Thrusters, 16, 5.67, "Thrusters Class 6 Rating D", "OptMass:1080t, MaxMass:1620t, MinMass:540t") },
            { "int_engine_size6_class3", new ShipModule(128064090, ShipModule.ModuleTypes.Thrusters, 40, 6.3, "Thrusters Class 6 Rating C", "OptMass:1200t, MaxMass:1800t, MinMass:600t") },
            { "int_engine_size6_class4", new ShipModule(128064091, ShipModule.ModuleTypes.Thrusters, 64, 6.93, "Thrusters Class 6 Rating B", "OptMass:1320t, MaxMass:1980t, MinMass:660t") },
            { "int_engine_size6_class5", new ShipModule(128064092, ShipModule.ModuleTypes.Thrusters, 40, 7.56, "Thrusters Class 6 Rating A", "OptMass:1440t, MaxMass:2160t, MinMass:720t") },
            { "int_engine_size7_class1", new ShipModule(128064093, ShipModule.ModuleTypes.Thrusters, 80, 6.08, "Thrusters Class 7 Rating E", "OptMass:1440t, MaxMass:2160t, MinMass:720t") },
            { "int_engine_size7_class2", new ShipModule(128064094, ShipModule.ModuleTypes.Thrusters, 32, 6.84, "Thrusters Class 7 Rating D", "OptMass:1620t, MaxMass:2430t, MinMass:810t") },
            { "int_engine_size7_class3", new ShipModule(128064095, ShipModule.ModuleTypes.Thrusters, 80, 7.6, "Thrusters Class 7 Rating C", "OptMass:1800t, MaxMass:2700t, MinMass:900t") },
            { "int_engine_size7_class4", new ShipModule(128064096, ShipModule.ModuleTypes.Thrusters, 128, 8.36, "Thrusters Class 7 Rating B", "OptMass:1980t, MaxMass:2970t, MinMass:990t") },
            { "int_engine_size7_class5", new ShipModule(128064097, ShipModule.ModuleTypes.Thrusters, 80, 9.12, "Thrusters Class 7 Rating A", "OptMass:2160t, MaxMass:3240t, MinMass:1080t") },
            { "int_engine_size8_class1", new ShipModule(128064098, ShipModule.ModuleTypes.Thrusters, 160, 7.2, "Thrusters Class 8 Rating E", "OptMass:2240t, MaxMass:3360t, MinMass:1120t") },
            { "int_engine_size8_class2", new ShipModule(128064099, ShipModule.ModuleTypes.Thrusters, 64, 8.1, "Thrusters Class 8 Rating D", "OptMass:2520t, MaxMass:3780t, MinMass:1260t") },
            { "int_engine_size8_class3", new ShipModule(128064100, ShipModule.ModuleTypes.Thrusters, 160, 9, "Thrusters Class 8 Rating C", "OptMass:2800t, MaxMass:4200t, MinMass:1400t") },
            { "int_engine_size8_class4", new ShipModule(128064101, ShipModule.ModuleTypes.Thrusters, 256, 9.9, "Thrusters Class 8 Rating B", "OptMass:3080t, MaxMass:4620t, MinMass:1540t") },
            { "int_engine_size8_class5", new ShipModule(128064102, ShipModule.ModuleTypes.Thrusters, 160, 10.8, "Thrusters Class 8 Rating A", "OptMass:3360t, MaxMass:5040t, MinMass:1680t") },

            { "int_engine_size2_class1_free", new ShipModule(128666636, ShipModule.ModuleTypes.Thrusters, 2.5, 2, "Thrusters Class 2 Rating E", "OptMass:48t, MaxMass:72t, MinMass:24t") },

            { "int_engine_size3_class5_fast", new ShipModule(128682013, ShipModule.ModuleTypes.EnhancedPerformanceThrusters, 5, 5, "Thrusters Class 3 Rating A Fast", "OptMass:90t, MaxMass:200t, MinMass:70t") },
            { "int_engine_size2_class5_fast", new ShipModule(128682014, ShipModule.ModuleTypes.EnhancedPerformanceThrusters, 2.5, 4, "Thrusters Class 2 Rating A Fast", "OptMass:60t, MaxMass:120t, MinMass:50t") },

            // XENO Scanners

            { "hpt_xenoscanner_basic_tiny", new ShipModule(128793115, ShipModule.ModuleTypes.XenoScanner, 1.3, 0.2, "Xeno Scanner", "Range:500m") },
            { "hpt_xenoscannermk2_basic_tiny", new ShipModule(128808878, ShipModule.ModuleTypes.EnhancedXenoScanner, 1.3, 0.8, "Xeno Scanner MK 2", "Range:2000m") },
            { "hpt_xenoscanner_advanced_tiny", new ShipModule(129022952, ShipModule.ModuleTypes.EnhancedXenoScanner, 1.3, 0.8, "Advanced Xeno Scanner", "Range:2000m") },

        };

        // non buyable

        public static Dictionary<string, ShipModule> othershipmodules = new Dictionary<string, ShipModule>
        {
            { "adder_cockpit", new ShipModule(999999913,ShipModule.ModuleTypes.CockpitType,0,0,"Adder Cockpit",null) },
            { "typex_3_cockpit", new ShipModule(999999945,ShipModule.ModuleTypes.CockpitType,0,0,"Alliance Challenger Cockpit",null) },
            { "typex_cockpit", new ShipModule(999999943,ShipModule.ModuleTypes.CockpitType,0,0,"Alliance Chieftain Cockpit",null) },
            { "anaconda_cockpit", new ShipModule(999999926,ShipModule.ModuleTypes.CockpitType,0,0,"Anaconda Cockpit",null) },
            { "asp_cockpit", new ShipModule(999999918,ShipModule.ModuleTypes.CockpitType,0,0,"Asp Cockpit",null) },
            { "asp_scout_cockpit", new ShipModule(999999934,ShipModule.ModuleTypes.CockpitType,0,0,"Asp Scout Cockpit",null) },
            { "belugaliner_cockpit", new ShipModule(999999938,ShipModule.ModuleTypes.CockpitType,0,0,"Beluga Cockpit",null) },
            { "cobramkiii_cockpit", new ShipModule(999999915,ShipModule.ModuleTypes.CockpitType,0,0,"Cobra Mk III Cockpit",null) },
            { "cobramkiv_cockpit", new ShipModule(999999937,ShipModule.ModuleTypes.CockpitType,0,0,"Cobra Mk IV Cockpit",null) },
            { "cutter_cockpit", new ShipModule(999999932,ShipModule.ModuleTypes.CockpitType,0,0,"Cutter Cockpit",null) },
            { "diamondbackxl_cockpit", new ShipModule(999999928,ShipModule.ModuleTypes.CockpitType,0,0,"Diamondback Explorer Cockpit",null) },
            { "diamondback_cockpit", new ShipModule(999999927,ShipModule.ModuleTypes.CockpitType,0,0,"Diamondback Scout Cockpit",null) },
            { "dolphin_cockpit", new ShipModule(999999939,ShipModule.ModuleTypes.CockpitType,0,0,"Dolphin Cockpit",null) },
            { "eagle_cockpit", new ShipModule(999999911,ShipModule.ModuleTypes.CockpitType,0,0,"Eagle Cockpit",null) },
            { "empire_courier_cockpit", new ShipModule(999999909,ShipModule.ModuleTypes.CockpitType,0,0,"Empire Courier Cockpit",null) },
            { "empire_eagle_cockpit", new ShipModule(999999929,ShipModule.ModuleTypes.CockpitType,0,0,"Empire Eagle Cockpit",null) },
            { "empire_fighter_cockpit", new ShipModule(899990000,ShipModule.ModuleTypes.CockpitType,0,0,"Empire Fighter Cockpit",null) },
            { "empire_trader_cockpit", new ShipModule(999999920,ShipModule.ModuleTypes.CockpitType,0,0,"Empire Trader Cockpit",null) },
            { "federation_corvette_cockpit", new ShipModule(999999933,ShipModule.ModuleTypes.CockpitType,0,0,"Federal Corvette Cockpit",null) },
            { "federation_dropship_mkii_cockpit", new ShipModule(999999930,ShipModule.ModuleTypes.CockpitType,0,0,"Federal Dropship Cockpit",null) },
            { "federation_dropship_cockpit", new ShipModule(999999921,ShipModule.ModuleTypes.CockpitType,0,0,"Federal Gunship Cockpit",null) },
            { "federation_gunship_cockpit", new ShipModule(999999931,ShipModule.ModuleTypes.CockpitType,0,0,"Federal Gunship Cockpit",null) },
            { "federation_fighter_cockpit", new ShipModule(899990001,ShipModule.ModuleTypes.CockpitType,0,0,"Federation Fighter Cockpit",null) },
            { "ferdelance_cockpit", new ShipModule(999999925,ShipModule.ModuleTypes.CockpitType,0,0,"Fer De Lance Cockpit",null) },
            { "hauler_cockpit", new ShipModule(999999912,ShipModule.ModuleTypes.CockpitType,0,0,"Hauler Cockpit",null) },
            { "independant_trader_cockpit", new ShipModule(999999936,ShipModule.ModuleTypes.CockpitType,0,0,"Independant Trader Cockpit",null) },
            { "independent_fighter_cockpit", new ShipModule(899990002,ShipModule.ModuleTypes.CockpitType,0,0,"Independent Fighter Cockpit",null) },
            { "krait_light_cockpit", new ShipModule(999999948,ShipModule.ModuleTypes.CockpitType,0,0,"Krait Light Cockpit",null) },
            { "krait_mkii_cockpit", new ShipModule(999999946,ShipModule.ModuleTypes.CockpitType,0,0,"Krait MkII Cockpit",null) },
            { "mamba_cockpit", new ShipModule(999999949,ShipModule.ModuleTypes.CockpitType,0,0,"Mamba Cockpit",null) },
            { "orca_cockpit", new ShipModule(999999922,ShipModule.ModuleTypes.CockpitType,0,0,"Orca Cockpit",null) },
            { "python_cockpit", new ShipModule(999999924,ShipModule.ModuleTypes.CockpitType,0,0,"Python Cockpit",null) },
            { "python_nx_cockpit", new ShipModule(-1,ShipModule.ModuleTypes.CockpitType,0,0,"Python Nx Cockpit",null) },
            { "sidewinder_cockpit", new ShipModule(999999910,ShipModule.ModuleTypes.CockpitType,0,0,"Sidewinder Cockpit",null) },
            { "type6_cockpit", new ShipModule(999999916,ShipModule.ModuleTypes.CockpitType,0,0,"Type 6 Cockpit",null) },
            { "type7_cockpit", new ShipModule(999999917,ShipModule.ModuleTypes.CockpitType,0,0,"Type 7 Cockpit",null) },
            { "type9_cockpit", new ShipModule(999999923,ShipModule.ModuleTypes.CockpitType,0,0,"Type 9 Cockpit",null) },
            { "type9_military_cockpit", new ShipModule(999999942,ShipModule.ModuleTypes.CockpitType,0,0,"Type 9 Military Cockpit",null) },
            { "typex_2_cockpit", new ShipModule(999999950,ShipModule.ModuleTypes.CockpitType,0,0,"Typex 2 Cockpit",null) },
            { "viper_cockpit", new ShipModule(999999914,ShipModule.ModuleTypes.CockpitType,0,0,"Viper Cockpit",null) },
            { "viper_mkiv_cockpit", new ShipModule(999999935,ShipModule.ModuleTypes.CockpitType,0,0,"Viper Mk IV Cockpit",null) },
            { "vulture_cockpit", new ShipModule(999999919,ShipModule.ModuleTypes.CockpitType,0,0,"Vulture Cockpit",null) },

            { "int_codexscanner", new ShipModule(999999947,ShipModule.ModuleTypes.Codex,0,0,"Codex Scanner",null) },
            { "hpt_shipdatalinkscanner", new ShipModule(999999940,ShipModule.ModuleTypes.DataLinkScanner,0,0,"Hpt Shipdatalinkscanner",null) },

            { "int_passengercabin_size2_class0", new ShipModule(-1,ShipModule.ModuleTypes.PrisonCells,2.5,0,"Prison Cell","Prisoners:2") },
            { "int_passengercabin_size3_class0", new ShipModule(-1,ShipModule.ModuleTypes.PrisonCells,5,0,"Prison Cell","Prisoners:4") },
            { "int_passengercabin_size4_class0", new ShipModule(-1,ShipModule.ModuleTypes.PrisonCells,10,0,"Prison Cell","Prisoners:8") },
            { "int_passengercabin_size5_class0", new ShipModule(-1,ShipModule.ModuleTypes.PrisonCells,20,0,"Prison Cell","Prisoners:16") },
            { "int_passengercabin_size6_class0", new ShipModule(-1,ShipModule.ModuleTypes.PrisonCells,40,0,"Prison Cell","Prisoners:32") },

            { "hpt_cannon_turret_huge", new ShipModule(-1,ShipModule.ModuleTypes.Cannon,1,0.9,"Cannon Turret Huge",null) },

            { "modularcargobaydoorfdl", new ShipModule(999999907,ShipModule.ModuleTypes.CargoBayDoorType,0,0,"FDL Cargo Bay Door",null) },
            { "modularcargobaydoor", new ShipModule(999999908,ShipModule.ModuleTypes.CargoBayDoorType,0,0,"Modular Cargo Bay Door",null) },

            { "hpt_cargoscanner_basic_tiny", new ShipModule(-1,ShipModule.ModuleTypes.CargoScanner,0,0,"Cargo Scanner Basic Tiny",null) },

           // { "int_corrosionproofcargorack_size2_class1", new ShipModule(-1,0,0,null,"Corrosion Resistant Cargo Rack",ShipModule.ModuleTypes.CargoRack) },
           // { "hpt_plasmaburstcannon_fixed_medium", new ShipModule(-1,1,1.4,null,"Plasma Burst Cannon Fixed Medium","Plasma Accelerator") },      // no evidence
           // { "hpt_pulselaserstealth_fixed_small", new ShipModule(-1,1,0.2,null,"Pulse Laser Stealth Fixed Small",ShipModule.ModuleTypes.PulseLaser) },
            ///{ "int_shieldgenerator_size1_class4", new ShipModule(-1,2,1.44,null,"Shield Generator Class 1 Rating E",ShipModule.ModuleTypes.ShieldGenerator) },
        };

        #endregion

        #region Fighters

        public static Dictionary<string, ShipModule> fightermodules = new Dictionary<string, ShipModule>
        {
            { "hpt_guardiangauss_fixed_gdn_fighter", new ShipModule(899990050,ShipModule.ModuleTypes.FighterWeapon,1,1,"Guardian Gauss Fixed GDN Fighter",null) },
            { "hpt_guardianplasma_fixed_gdn_fighter", new ShipModule(899990050,ShipModule.ModuleTypes.FighterWeapon,1,1,"Guardian Plasma Fixed GDN Fighter",null) },
            { "hpt_guardianshard_fixed_gdn_fighter", new ShipModule(899990050,ShipModule.ModuleTypes.FighterWeapon,1,1,"Guardian Shard Fixed GDN Fighter",null) },

            { "empire_fighter_armour_standard", new ShipModule(899990059,ShipModule.ModuleTypes.LightweightAlloy,0,0,"Empire Fighter Armour Standard",null) },
            { "federation_fighter_armour_standard", new ShipModule(899990060,ShipModule.ModuleTypes.LightweightAlloy,0,0,"Federation Fighter Armour Standard",null) },
            { "independent_fighter_armour_standard", new ShipModule(899990070,ShipModule.ModuleTypes.LightweightAlloy,0,0,"Independent Fighter Armour Standard",null) },
            { "gdn_hybrid_fighter_v1_armour_standard", new ShipModule(899990060,ShipModule.ModuleTypes.LightweightAlloy,0,0,"GDN Hybrid Fighter V 1 Armour Standard",null) },
            { "gdn_hybrid_fighter_v2_armour_standard", new ShipModule(899990060,ShipModule.ModuleTypes.LightweightAlloy,0,0,"GDN Hybrid Fighter V 2 Armour Standard",null) },
            { "gdn_hybrid_fighter_v3_armour_standard", new ShipModule(899990060,ShipModule.ModuleTypes.LightweightAlloy,0,0,"GDN Hybrid Fighter V 3 Armour Standard",null) },

            { "hpt_beamlaser_fixed_empire_fighter", new ShipModule(899990018,ShipModule.ModuleTypes.BeamLaser,0,1,"Beam Laser Fixed Empire Fighter",null) },
            { "hpt_beamlaser_fixed_fed_fighter", new ShipModule(899990019,ShipModule.ModuleTypes.BeamLaser,0,1,"Beam Laser Fixed Federation Fighter",null) },
            { "hpt_beamlaser_fixed_indie_fighter", new ShipModule(899990020,ShipModule.ModuleTypes.BeamLaser,0,1,"Beam Laser Fixed Indie Fighter",null) },
            { "hpt_beamlaser_gimbal_empire_fighter", new ShipModule(899990023,ShipModule.ModuleTypes.BeamLaser,0,1,"Beam Laser Gimbal Empire Fighter",null) },
            { "hpt_beamlaser_gimbal_fed_fighter", new ShipModule(899990024,ShipModule.ModuleTypes.BeamLaser,0,1,"Beam Laser Gimbal Federation Fighter",null) },
            { "hpt_beamlaser_gimbal_indie_fighter", new ShipModule(899990025,ShipModule.ModuleTypes.BeamLaser,0,1,"Beam Laser Gimbal Indie Fighter",null) },
            { "hpt_plasmarepeater_fixed_empire_fighter", new ShipModule(899990026,ShipModule.ModuleTypes.PlasmaAccelerator,0,1,"Plasma Repeater Fixed Empire Fighter",null) },
            { "hpt_plasmarepeater_fixed_fed_fighter", new ShipModule(899990027,ShipModule.ModuleTypes.PlasmaAccelerator,0,1,"Plasma Repeater Fixed Fed Fighter",null) },
            { "hpt_plasmarepeater_fixed_indie_fighter", new ShipModule(899990028,ShipModule.ModuleTypes.PlasmaAccelerator,0,1,"Plasma Repeater Fixed Indie Fighter",null) },
            { "hpt_pulselaser_fixed_empire_fighter", new ShipModule(899990029,ShipModule.ModuleTypes.PulseLaser,0,1,"Pulse Laser Fixed Empire Fighter",null) },
            { "hpt_pulselaser_fixed_fed_fighter", new ShipModule(899990030,ShipModule.ModuleTypes.PulseLaser,0,1,"Pulse Laser Fixed Federation Fighter",null) },
            { "hpt_pulselaser_fixed_indie_fighter", new ShipModule(899990031,ShipModule.ModuleTypes.PulseLaser,0,1,"Pulse Laser Fixed Indie Fighter",null) },
            { "hpt_pulselaser_gimbal_empire_fighter", new ShipModule(899990032,ShipModule.ModuleTypes.PulseLaser,0,1,"Pulse Laser Gimbal Empire Fighter",null) },
            { "hpt_pulselaser_gimbal_fed_fighter", new ShipModule(899990033,ShipModule.ModuleTypes.PulseLaser,0,1,"Pulse Laser Gimbal Federation Fighter",null) },
            { "hpt_pulselaser_gimbal_indie_fighter", new ShipModule(899990034,ShipModule.ModuleTypes.PulseLaser,0,1,"Pulse Laser Gimbal Indie Fighter",null) },

            { "int_engine_fighter_class1", new ShipModule(-1,ShipModule.ModuleTypes.Thrusters,1,1,"Fighter Engine Class 1",null) },

            { "gdn_hybrid_fighter_v1_cockpit", new ShipModule(899990101,ShipModule.ModuleTypes.CockpitType,0,0,"GDN Hybrid Fighter V 1 Cockpit",null) },
            { "gdn_hybrid_fighter_v2_cockpit", new ShipModule(899990102,ShipModule.ModuleTypes.CockpitType,0,0,"GDN Hybrid Fighter V 2 Cockpit",null) },
            { "gdn_hybrid_fighter_v3_cockpit", new ShipModule(899990103,ShipModule.ModuleTypes.CockpitType,0,0,"GDN Hybrid Fighter V 3 Cockpit",null) },

            { "hpt_atmulticannon_fixed_indie_fighter", new ShipModule(899990040,ShipModule.ModuleTypes.AXMulti_Cannon,0,1,"AX Multicannon Fixed Indie Fighter",null) },
            { "hpt_multicannon_fixed_empire_fighter", new ShipModule(899990050,ShipModule.ModuleTypes.Multi_Cannon,0,1,"Multicannon Fixed Empire Fighter",null) },
            { "hpt_multicannon_fixed_fed_fighter", new ShipModule(899990051,ShipModule.ModuleTypes.Multi_Cannon,0,1,"Multicannon Fixed Fed Fighter",null) },
            { "hpt_multicannon_fixed_indie_fighter", new ShipModule(899990052,ShipModule.ModuleTypes.Multi_Cannon,0,1,"Multicannon Fixed Indie Fighter",null) },

            { "int_powerdistributor_fighter_class1", new ShipModule(-1,ShipModule.ModuleTypes.PowerDistributor,0,0,"Int Powerdistributor Fighter Class 1",null) },

            { "int_powerplant_fighter_class1", new ShipModule(-1,ShipModule.ModuleTypes.PowerPlant,0,0,"Int Powerplant Fighter Class 1",null) },

            { "int_sensors_fighter_class1", new ShipModule(-1,ShipModule.ModuleTypes.Sensors,0,0,"Int Sensors Fighter Class 1",null) },
            { "int_shieldgenerator_fighter_class1", new ShipModule(899990080,ShipModule.ModuleTypes.ShieldGenerator,0,0,"Shield Generator Fighter Class 1",null) },
            { "ext_emitter_guardian", new ShipModule(899990190,ShipModule.ModuleTypes.Sensors,0,0,"Ext Emitter Guardian",null) },
            { "ext_emitter_standard", new ShipModule(899990090,ShipModule.ModuleTypes.Sensors,0,0,"Ext Emitter Standard",null) },

        };

        #endregion

        #region SRV

        public static Dictionary<string, ShipModule> srvmodules = new Dictionary<string, ShipModule>
        {
            { "buggycargobaydoor", new ShipModule(-1,ShipModule.ModuleTypes.CargoBayDoorType,0,0,"SRV Cargo Bay Door",null) },
            { "int_fueltank_size0_class3", new ShipModule(-1,ShipModule.ModuleTypes.FuelTank,0,0,"SRV Scarab Fuel Tank",null) },
            { "vehicle_scorpion_missilerack_lockon", new ShipModule(-1,ShipModule.ModuleTypes.MissileRack,0,0,"SRV Scorpion Missile Rack",null) },
            { "int_powerdistributor_size0_class1", new ShipModule(-1,ShipModule.ModuleTypes.PowerDistributor,0,0,"SRV Scarab Power Distributor",null) },
            { "int_powerplant_size0_class1", new ShipModule(-1,ShipModule.ModuleTypes.PowerPlant,0,0,"SRV Scarab Powerplant",null) },
            { "vehicle_plasmaminigun_turretgun", new ShipModule(-1,ShipModule.ModuleTypes.PulseLaser,0,0,"SRV Scorpion Plasma Turret Gun",null) },

            { "testbuggy_cockpit", new ShipModule(-1,ShipModule.ModuleTypes.CockpitType,0,0,"SRV Scarab Cockpit",null) },
            { "scarab_armour_grade1", new ShipModule(-1,ShipModule.ModuleTypes.LightweightAlloy,0,0,"SRV Scarab Armour",null) },
            { "int_fueltank_size0_class2", new ShipModule(-1,ShipModule.ModuleTypes.FuelTank,0,0,"SRV Scopion Fuel tank Size 0 Class 2",null) },
            { "combat_multicrew_srv_01_cockpit", new ShipModule(-1,ShipModule.ModuleTypes.CockpitType,0,0,"SRV Scorpion Cockpit",null) },
            { "int_powerdistributor_size0_class1_cms", new ShipModule(-1,ShipModule.ModuleTypes.PowerDistributor,0,0,"SRV Scorpion Power Distributor Size 0 Class 1 Cms",null) },
            { "int_powerplant_size0_class1_cms", new ShipModule(-1,ShipModule.ModuleTypes.PowerPlant,0,0,"SRV Scorpion Powerplant Size 0 Class 1 Cms",null) },
            { "vehicle_turretgun", new ShipModule(-1,ShipModule.ModuleTypes.PulseLaser,0,0,"SRV Scarab Turret",null) },

            { "hpt_datalinkscanner", new ShipModule(-1,ShipModule.ModuleTypes.Sensors,0,0,"SRV Data Link Scanner",null) },
            { "int_sinewavescanner_size1_class1", new ShipModule(-1,ShipModule.ModuleTypes.Sensors,0,0,"SRV Scarab Scanner",null) },
            { "int_sensors_surface_size1_class1", new ShipModule(-1,ShipModule.ModuleTypes.Sensors,0,0,"SRV Sensors",null) },

            { "int_lifesupport_size0_class1", new ShipModule(-1,ShipModule.ModuleTypes.LifeSupport,0,0,"SRV Life Support",null) },
            { "int_shieldgenerator_size0_class3", new ShipModule(-1,ShipModule.ModuleTypes.ShieldGenerator,0,0,"SRV Shields",null) },
        };

        #endregion

        #region Vanity Modules

        public static Dictionary<string, ShipModule> vanitymodules = new Dictionary<string, ShipModule>   // DO NOT USE DIRECTLY - public is for checking only
        {
            { "null", new ShipModule(-1,ShipModule.ModuleTypes.UnknownType,0,0,"Error in frontier journal - Null module",null) },

            { "typex_shipkit1_bumper3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Alliance Chieftain Shipkit 1 Bumper 3",null) },
            { "typex_shipkit1_spoiler3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Alliance Chieftain Shipkit 1 Spoiler 3",null) },
            { "typex_shipkit1_wings1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Alliance Chieftain Shipkit 1 Wings 1",null) },
            { "anaconda_shipkit1_bumper1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Anaconda Shipkit 1 Bumper 1",null) },
            { "anaconda_shipkit1_bumper2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Anaconda Shipkit 1 Bumper 2",null) },
            { "anaconda_shipkit1_bumper3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Anaconda Shipkit 1 Bumper 3",null) },
            { "anaconda_shipkit1_bumper4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Anaconda Shipkit 1 Bumper 4",null) },
            { "anaconda_shipkit1_spoiler1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Anaconda Shipkit 1 Spoiler 1",null) },
            { "anaconda_shipkit1_spoiler2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Anaconda Shipkit 1 Spoiler 2",null) },
            { "anaconda_shipkit1_spoiler3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Anaconda Shipkit 1 Spoiler 3",null) },
            { "anaconda_shipkit1_spoiler4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Anaconda Shipkit 1 Spoiler 4",null) },
            { "anaconda_shipkit1_tail1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Anaconda Shipkit 1 Tail 1",null) },
            { "anaconda_shipkit1_tail2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Anaconda Shipkit 1 Tail 2",null) },
            { "anaconda_shipkit1_tail3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Anaconda Shipkit 1 Tail 3",null) },
            { "anaconda_shipkit1_tail4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Anaconda Shipkit 1 Tail 4",null) },
            { "anaconda_shipkit1_wings1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Anaconda Shipkit 1 Wings 1",null) },
            { "anaconda_shipkit1_wings2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Anaconda Shipkit 1 Wings 2",null) },
            { "anaconda_shipkit1_wings3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Anaconda Shipkit 1 Wings 3",null) },
            { "anaconda_shipkit1_wings4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Anaconda Shipkit 1 Wings 4",null) },
            { "anaconda_shipkit2raider_bumper1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Anaconda Shipkit 2 Raider Bumper 1",null) },
            { "anaconda_shipkit2raider_bumper2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Anaconda Shipkit 2 Raider Bumper 2",null) },
            { "anaconda_shipkit2raider_bumper3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Anaconda Shipkit 2 Raider Bumper 3",null) },
            { "anaconda_shipkit2raider_spoiler1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Anaconda Shipkit 2 Raider Spoiler 1",null) },
            { "anaconda_shipkit2raider_spoiler2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Anaconda Shipkit 2 Raider Spoiler 2",null) },
            { "anaconda_shipkit2raider_spoiler3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Anaconda Shipkit 2 Raider Spoiler 3",null) },
            { "anaconda_shipkit2raider_tail2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Anaconda Shipkit 2 Raider Tail 2",null) },
            { "anaconda_shipkit2raider_tail3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Anaconda Shipkit 2 Raider Tail 3",null) },
            { "anaconda_shipkit2raider_wings2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Anaconda Shipkit 2 Raider Wings 2",null) },
            { "anaconda_shipkit2raider_wings3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Anaconda Shipkit 2 Raider Wings 3",null) },
            { "asp_industrial1_bumper1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Asp Industrial 1 Bumper 1",null) },
            { "asp_industrial1_spoiler1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Asp Industrial 1 Spoiler 1",null) },
            { "asp_industrial1_wings1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Asp Industrial 1 Wings 1",null) },
            { "asp_shipkit1_bumper1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Asp Shipkit 1 Bumper 1",null) },
            { "asp_shipkit1_bumper2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Asp Shipkit 1 Bumper 2",null) },
            { "asp_shipkit1_bumper3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Asp Shipkit 1 Bumper 3",null) },
            { "asp_shipkit1_bumper4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Asp Shipkit 1 Bumper 4",null) },
            { "asp_shipkit1_spoiler1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Asp Shipkit 1 Spoiler 1",null) },
            { "asp_shipkit1_spoiler2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Asp Shipkit 1 Spoiler 2",null) },
            { "asp_shipkit1_spoiler3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Asp Shipkit 1 Spoiler 3",null) },
            { "asp_shipkit1_spoiler4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Asp Shipkit 1 Spoiler 4",null) },
            { "asp_shipkit1_wings1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Asp Shipkit 1 Wings 1",null) },
            { "asp_shipkit1_wings2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Asp Shipkit 1 Wings 2",null) },
            { "asp_shipkit1_wings3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Asp Shipkit 1 Wings 3",null) },
            { "asp_shipkit1_wings4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Asp Shipkit 1 Wings 4",null) },
            { "asp_shipkit2raider_bumper2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Asp Shipkit 2 Raider Bumper 2",null) },
            { "asp_shipkit2raider_bumper3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Asp Shipkit 2 Raider Bumper 3",null) },
            { "asp_shipkit2raider_tail2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Asp Shipkit 2 Raider Tail 2",null) },
            { "asp_shipkit2raider_wings2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Asp Shipkit 2 Raider Wings 2",null) },
            { "asp_science1_spoiler1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Asp Science 1 Spoiler 1",null) },
            { "asp_science1_wings1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Asp Science 1 Wings 1",null) },
            { "asp_science1_bumper1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Asp Science 1 Bumper 1",null) },
            { "bobble_ap2_textexclam", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Ap 2 Text !",null) },
            { "bobble_ap2_texte", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Ap 2 Text e",null) },
            { "bobble_ap2_textl", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Ap 2 Text l",null) },
            { "bobble_ap2_textn", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Ap 2 Text n",null) },
            { "bobble_ap2_texto", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Ap 2 Text o",null) },
            { "bobble_ap2_textr", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Ap 2 Text r",null) },
            { "bobble_ap2_texts", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Ap 2 Text s",null) },
            { "bobble_ap2_textasterisk", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Ap 2 Textasterisk",null) },
            { "bobble_ap2_textg", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Ap 2 Textg",null) },
            { "bobble_ap2_textj", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Ap 2 Textj",null) },
            { "bobble_ap2_textu", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Ap 2 Textu",null) },
            { "bobble_ap2_texty", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Ap 2 Texty",null) },
            { "bobble_christmastree", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Christmas Tree",null) },
            { "bobble_davidbraben", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble David Braben",null) },
            { "bobble_dotd_blueskull", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Dotd Blueskull",null) },
            { "bobble_nav_beacon", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Nav Beacon",null) },
            { "bobble_oldskool_anaconda", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Oldskool Anaconda",null) },
            { "bobble_oldskool_aspmkii", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Oldskool Asp Mk II",null) },
            { "bobble_oldskool_cobramkiii", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Oldskool Cobram Mk III",null) },
            { "bobble_oldskool_python", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Oldskool Python",null) },
            { "bobble_oldskool_thargoid", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Oldskool Thargoid",null) },
            { "bobble_pilot_dave_expo_flight_suit", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Pilot Dave Expo Flight Suit",null) },
            { "bobble_pilotfemale", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Pilot Female",null) },
            { "bobble_pilotmale", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Pilot Male",null) },
            { "bobble_pilotmale_expo_flight_suit", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Pilot Male Expo Flight Suit",null) },
            { "bobble_planet_earth", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Planet Earth",null) },
            { "bobble_planet_jupiter", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Planet Jupiter",null) },
            { "bobble_planet_mars", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Planet Mars",null) },
            { "bobble_planet_mercury", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Planet Mercury",null) },
            { "bobble_planet_neptune", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Planet Neptune",null) },
            { "bobble_planet_saturn", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Planet Saturn",null) },
            { "bobble_planet_uranus", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Planet Uranus",null) },
            { "bobble_planet_venus", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Planet Venus",null) },
            { "bobble_plant_aloe", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Plant Aloe",null) },
            { "bobble_plant_braintree", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Plant Braintree",null) },
            { "bobble_plant_rosequartz", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Plant Rosequartz",null) },
            { "bobble_pumpkin", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Pumpkin",null) },
            { "bobble_santa", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Santa",null) },
            { "bobble_ship_anaconda", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Ship Anaconda",null) },
            { "bobble_ship_cobramkiii", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Ship Cobra Mk III",null) },
            { "bobble_ship_cobramkiii_ffe", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Ship Cobra Mk III FFE",null) },
            { "bobble_ship_thargoid", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Ship Thargoid",null) },
            { "bobble_ship_viper", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Ship Viper",null) },
            { "bobble_snowflake", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Snowflake",null) },
            { "bobble_snowman", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Snowman",null) },
            { "bobble_station_coriolis", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Station Coriolis",null) },
            { "bobble_station_coriolis_wire", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Station Coriolis Wire",null) },
            { "bobble_textexclam", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text !",null) },
            { "bobble_textpercent", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text %",null) },
            { "bobble_textquest", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text ?",null) },
            { "bobble_text0", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text 0",null) },
            { "bobble_text1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text 1",null) },
            { "bobble_text2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text 2",null) },
            { "bobble_text3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text 3",null) },
            { "bobble_text4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text 4",null) },
            { "bobble_text5", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text 5",null) },
            { "bobble_text6", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text 6",null) },
            { "bobble_text7", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text 7",null) },
            { "bobble_text8", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text 8",null) },
            { "bobble_text9", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text 9",null) },
            { "bobble_texta", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text A",null) },
            { "bobble_textb", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text B",null) },
            { "bobble_textbracket01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text Bracket 1",null) },
            { "bobble_textbracket02", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text Bracket 2",null) },
            { "bobble_textcaret", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text Caret",null) },
            { "bobble_textd", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text d",null) },
            { "bobble_textdollar", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text Dollar",null) },
            { "bobble_texte", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text e",null) },
            { "bobble_texte04", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text E 4",null) },
            { "bobble_textexclam01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text Exclam 1",null) },
            { "bobble_textf", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text f",null) },
            { "bobble_textg", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text G",null) },
            { "bobble_texth", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text H",null) },
            { "bobble_texthash", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text Hash",null) },
            { "bobble_texti", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text I",null) },
            { "bobble_texti02", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text I 2",null) },
            { "bobble_textm", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text m",null) },
            { "bobble_textn", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text n",null) },
            { "bobble_texto02", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text O 2",null) },
            { "bobble_texto03", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text O 3",null) },
            { "bobble_textp", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text P",null) },
            { "bobble_textplus", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text Plus",null) },
            { "bobble_textr", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text r",null) },
            { "bobble_textt", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text t",null) },
            { "bobble_textu", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text U",null) },
            { "bobble_textu01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text U 1",null) },
            { "bobble_textv", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text V",null) },
            { "bobble_textx", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text X",null) },
            { "bobble_texty", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text Y",null) },
            { "bobble_textz", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Text Z",null) },
            { "bobble_textasterisk", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Textasterisk",null) },
            { "bobble_texte01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Texte 1",null) },
            { "bobble_texti01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Texti 1",null) },
            { "bobble_textk", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Textk",null) },
            { "bobble_textl", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Textl",null) },
            { "bobble_textminus", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Textminus",null) },
            { "bobble_texto", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Texto",null) },
            { "bobble_texts", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Texts",null) },
            { "bobble_textunderscore", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Textunderscore",null) },
            { "bobble_trophy_anti_thargoid_s", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Trophy Anti Thargoid S",null) },
            { "bobble_trophy_combat", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Trophy Combat",null) },
            { "bobble_trophy_combat_s", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Trophy Combat S",null) },
            { "bobble_trophy_community", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Trophy Community",null) },
            { "bobble_trophy_exploration", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Trophy Exploration",null) },
            { "bobble_trophy_exploration_b", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Trophy Exploration B",null) },
            { "bobble_trophy_exploration_s", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Trophy Exploration S",null) },
            { "bobble_trophy_powerplay_b", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Bobble Trophy Powerplay B",null) },
            { "cobramkiii_shipkit1_wings3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Co)bra MK III Shipkit 1 Wings 3",null) },
            { "cobramkiii_shipkit1_bumper1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Cobra MK III Shipkit 1 Bumper 1",null) },
            { "cobramkiii_shipkit1_spoiler2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Cobra MK III Shipkit 1 Spoiler 2",null) },
            { "cobramkiii_shipkit1_spoiler4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Cobra Mk III Shipkit 1 Spoiler 4",null) },
            { "cobramkiii_shipkit1_tail1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Cobra MK III Shipkit 1 Tail 1",null) },
            { "cobramkiii_shipkit1_tail3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Cobra Mk III Shipkit 1 Tail 3",null) },
            { "cobramkiii_shipkit1_wings1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Cobra Mk III Shipkit 1 Wings 1",null) },
            { "cobramkiii_shipkit1_wings2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Cobra MK III Shipkit 1 Wings 2",null) },
            { "cobramkiii_shipkitraider1_bumper2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Cobra Mk III Shipkit Raider 1 Bumper 2",null) },
            { "cobramkiii_shipkitraider1_spoiler3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Cobra Mk III Shipkit Raider 1 Spoiler 3",null) },
            { "cobramkiii_shipkitraider1_tail2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Cobra Mk III Shipkit Raider 1 Tail 2",null) },
            { "cobramkiii_shipkitraider1_wings1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Cobra Mk III Shipkit Raider 1 Wings 1",null) },
            { "cobramkiii_shipkit1_bumper4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Cobramkiii Shipkit 1 Bumper 4",null) },
            { "cobramkiii_shipkit1_tail4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Cobramkiii Shipkit 1 Tail 4",null) },
            { "cutter_shipkit1_bumper2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Cutter Shipkit 1 Bumper 2",null) },
            { "cutter_shipkit1_bumper3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Cutter Shipkit 1 Bumper 3",null) },
            { "cutter_shipkit1_bumper4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Cutter Shipkit 1 Bumper 4",null) },
            { "cutter_shipkit1_spoiler2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Cutter Shipkit 1 Spoiler 2",null) },
            { "cutter_shipkit1_spoiler3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Cutter Shipkit 1 Spoiler 3",null) },
            { "cutter_shipkit1_spoiler4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Cutter Shipkit 1 Spoiler 4",null) },
            { "cutter_shipkit1_wings2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Cutter Shipkit 1 Wings 2",null) },
            { "cutter_shipkit1_wings3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Cutter Shipkit 1 Wings 3",null) },
            { "decal_explorer_elite02", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Explorer Elite 2",null) },
            { "decal_explorer_elite03", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Explorer Elite 3",null) },
            { "decal_skull9", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Skull 9",null) },
            { "decal_skull8", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Skull 8",null) },
            { "decal_alien_hunter2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Alien Hunter 2",null) },
            { "decal_alien_hunter6", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Alien Hunter 6",null) },
            { "decal_alien_sympathiser_b", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Alien Sympathiser B",null) },
            { "decal_anti_thargoid", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Anti Thargoid",null) },
            { "decal_bat2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Bat 2",null) },
            { "decal_beta_tester", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Beta Tester",null) },
            { "decal_bounty_hunter", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Bounty Hunter",null) },
            { "decal_bridgingthegap", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Bridgingthegap",null) },
            { "decal_cannon", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Cannon",null) },
            { "decal_combat_competent", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Combat Competent",null) },
            { "decal_combat_dangerous", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Combat Dangerous",null) },
            { "decal_combat_deadly", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Combat Deadly",null) },
            { "decal_combat_elite", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Combat Elite",null) },
            { "decal_combat_expert", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Combat Expert",null) },
            { "decal_combat_master", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Combat Master",null) },
            { "decal_combat_mostly_harmless", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Combat Mostly Harmless",null) },
            { "decal_combat_novice", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Combat Novice",null) },
            { "decal_community", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Community",null) },
            { "decal_distantworlds", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Distant Worlds",null) },
            { "decal_distantworlds2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Distantworlds 2",null) },
            { "decal_egx", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Egx",null) },
            { "decal_espionage", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Espionage",null) },
            { "decal_exploration_emisswhite", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Exploration Emisswhite",null) },
            { "decal_explorer_elite", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Explorer Elite",null) },
            { "decal_explorer_elite05", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Explorer Elite 5",null) },
            { "decal_explorer_mostly_aimless", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Explorer Mostly Aimless",null) },
            { "decal_explorer_pathfinder", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Explorer Pathfinder",null) },
            { "decal_explorer_ranger", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Explorer Ranger",null) },
            { "decal_explorer_scout", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Explorer Scout",null) },
            { "decal_explorer_starblazer", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Explorer Starblazer",null) },
            { "decal_explorer_surveyor", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Explorer Surveyor",null) },
            { "decal_explorer_trailblazer", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Explorer Trailblazer",null) },
            { "decal_expo", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Expo",null) },
            { "decal_founders_reversed", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Founders Reversed",null) },
            { "decal_fuelrats", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Fuel Rats",null) },
            { "decal_galnet", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Galnet",null) },
            { "decal_lavecon", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Lave Con",null) },
            { "decal_met_constructshipemp_gold", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Met Constructshipemp Gold",null) },
            { "decal_met_espionage_gold", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Met Espionage Gold",null) },
            { "decal_met_espionage_silver", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Met Espionage Silver",null) },
            { "decal_met_exploration_gold", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Met Exploration Gold",null) },
            { "decal_met_mining_bronze", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Met Mining Bronze",null) },
            { "decal_met_mining_gold", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Met Mining Gold",null) },
            { "decal_met_mining_silver", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Met Mining Silver",null) },
            { "decal_met_salvage_gold", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Met Salvage Gold",null) },
            { "decal_mining", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Mining",null) },
            { "decal_networktesters", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Network Testers",null) },
            { "decal_onionhead1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Onionhead 1",null) },
            { "decal_onionhead2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Onionhead 2",null) },
            { "decal_onionhead3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Onionhead 3",null) },
            { "decal_passenger_e", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Passenger E",null) },
            { "decal_passenger_g", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Passenger G",null) },
            { "decal_passenger_l", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Passenger L",null) },
            { "decal_paxprime", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Pax Prime",null) },
            { "decal_pilot_fed1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Pilot Fed 1",null) },
            { "decal_planet2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Planet 2",null) },
            { "decal_playergroup_wolves_of_jonai", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Player Group Wolves Of Jonai",null) },
            { "decal_playergroup_ugc", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Playergroup Ugc",null) },
            { "decal_powerplay_hudson", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Power Play Hudson",null) },
            { "decal_powerplay_mahon", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Power Play Mahon",null) },
            { "decal_powerplay_utopia", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Power Play Utopia",null) },
            { "decal_powerplay_aislingduval", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Powerplay Aislingduval",null) },
            { "decal_powerplay_halsey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Powerplay Halsey",null) },
            { "decal_powerplay_kumocrew", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Powerplay Kumocrew",null) },
            { "decal_powerplay_sirius", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Powerplay Sirius",null) },
            { "decal_pumpkin", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Pumpkin",null) },
            { "decal_shark1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Shark 1",null) },
            { "decal_skull3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Skull 3",null) },
            { "decal_skull5", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Skull 5",null) },
            { "decal_specialeffect", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Special Effect",null) },
            { "decal_spider", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Spider",null) },
            { "decal_thegolconda", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Thegolconda",null) },
            { "decal_trade_broker", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Trade Broker",null) },
            { "decal_trade_dealer", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Trade Dealer",null) },
            { "decal_trade_elite", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Trade Elite",null) },
            { "decal_trade_elite05", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Trade Elite 5",null) },
            { "decal_trade_entrepeneur", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Trade Entrepeneur",null) },
            { "decal_trade_merchant", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Trade Merchant",null) },
            { "decal_trade_mostly_penniless", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Trade Mostly Penniless",null) },
            { "decal_trade_peddler", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Trade Peddler",null) },
            { "decal_trade_tycoon", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Trade Tycoon",null) },
            { "decal_triple_elite", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Decal Triple Elite",null) },
            { "diamondbackxl_shipkit1_bumper1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Diamond Back XL Shipkit 1 Bumper 1",null) },
            { "diamondbackxl_shipkit1_spoiler2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Diamond Back XL Shipkit 1 Spoiler 2",null) },
            { "diamondbackxl_shipkit1_wings2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Diamond Back XL Shipkit 1 Wings 2",null) },
            { "dolphin_shipkit1_bumper2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Dolphin Shipkit 1 Bumper 2",null) },
            { "dolphin_shipkit1_bumper3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Dolphin Shipkit 1 Bumper 3",null) },
            { "dolphin_shipkit1_spoiler2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Dolphin Shipkit 1 Spoiler 2",null) },
            { "dolphin_shipkit1_tail4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Dolphin Shipkit 1 Tail 4",null) },
            { "dolphin_shipkit1_wings2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Dolphin Shipkit 1 Wings 2",null) },
            { "dolphin_shipkit1_wings3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Dolphin Shipkit 1 Wings 3",null) },
            { "eagle_shipkit1_bumper2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Eagle Shipkit 1 Bumper 2",null) },
            { "eagle_shipkit1_spoiler1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Eagle Shipkit 1 Spoiler 1",null) },
            { "eagle_shipkit1_wings1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Eagle Shipkit 1 Wings 1",null) },
            { "empire_courier_shipkit1_bumper2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Empire Courier Shipkit 1 Bumper 2",null) },
            { "empire_courier_shipkit1_bumper3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Empire Courier Shipkit 1 Bumper 3",null) },
            { "empire_courier_shipkit1_spoiler2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Empire Courier Shipkit 1 Spoiler 2",null) },
            { "empire_courier_shipkit1_spoiler3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Empire Courier Shipkit 1 Spoiler 3",null) },
            { "empire_courier_shipkit1_wings1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Empire Courier Shipkit 1 Wings 1",null) },
            { "empire_courier_shipkit1_wings2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Empire Courier Shipkit 1 Wings 2",null) },
            { "empire_courier_shipkit1_wings3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Empire Courier Shipkit 1 Wings 3",null) },
            { "empire_trader_shipkit1_bumper3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Empire Trader Shipkit 1 Bumper 3",null) },
            { "empire_trader_shipkit1_spoiler1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Empire Trader Shipkit 1 Spoiler 1",null) },
            { "empire_trader_shipkit1_spoiler3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Empire Trader Shipkit 1 Spoiler 3",null) },
            { "empire_trader_shipkit1_spoiler4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Empire Trader Shipkit 1 Spoiler 4",null) },
            { "empire_trader_shipkit1_tail1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Empire Trader Shipkit 1 Tail 1",null) },
            { "empire_trader_shipkit1_tail2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Empire Trader Shipkit 1 Tail 2",null) },
            { "empire_trader_shipkit1_tail3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Empire Trader Shipkit 1 Tail 3",null) },
            { "empire_trader_shipkit1_tail4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Empire Trader Shipkit 1 Tail 4",null) },
            { "empire_trader_shipkit1_wings1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Empire Trader Shipkit 1 Wings 1",null) },
            { "enginecustomisation_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Engine Customisation Blue",null) },
            { "enginecustomisation_cyan", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Engine Customisation Cyan",null) },
            { "enginecustomisation_green", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Engine Customisation Green",null) },
            { "enginecustomisation_orange", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Engine Customisation Orange",null) },
            { "enginecustomisation_pink", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Engine Customisation Pink",null) },
            { "enginecustomisation_purple", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Engine Customisation Purple",null) },
            { "enginecustomisation_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Engine Customisation Red",null) },
            { "enginecustomisation_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Engine Customisation White",null) },
            { "enginecustomisation_yellow", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Engine Customisation Yellow",null) },
            { "federation_corvette_shipkit1_bumper2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Federation Corvette Shipkit 1 Bumper 2",null) },
            { "federation_corvette_shipkit1_bumper3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Federation Corvette Shipkit 1 Bumper 3",null) },
            { "federation_corvette_shipkit1_bumper4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Federation Corvette Shipkit 1 Bumper 4",null) },
            { "federation_corvette_shipkit1_spoiler1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Federation Corvette Shipkit 1 Spoiler 1",null) },
            { "federation_corvette_shipkit1_spoiler2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Federation Corvette Shipkit 1 Spoiler 2",null) },
            { "federation_corvette_shipkit1_spoiler3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Federation Corvette Shipkit 1 Spoiler 3",null) },
            { "federation_corvette_shipkit1_spoiler4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Federation Corvette Shipkit 1 Spoiler 4",null) },
            { "federation_corvette_shipkit1_tail1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Federation Corvette Shipkit 1 Tail 1",null) },
            { "federation_corvette_shipkit1_tail2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Federation Corvette Shipkit 1 Tail 2",null) },
            { "federation_corvette_shipkit1_tail3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Federation Corvette Shipkit 1 Tail 3",null) },
            { "federation_corvette_shipkit1_tail4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Federation Corvette Shipkit 1 Tail 4",null) },
            { "federation_corvette_shipkit1_wings3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Federation Corvette Shipkit 1 Wings 3",null) },
            { "federation_corvette_shipkit1_wings4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Federation Corvette Shipkit 1 Wings 4",null) },
            { "federation_gunship_shipkit1_bumper1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Federation Gunship Shipkit 1 Bumper 1",null) },
            { "ferdelance_shipkit1_bumper4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Fer De Lance Shipkit 1 Bumper 4",null) },
            { "ferdelance_shipkit1_tail1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Fer De Lance Shipkit 1 Tail 1",null) },
            { "ferdelance_shipkit1_wings2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Fer De Lance Shipkit 1 Wings 2",null) },
            { "ferdelance_shipkit1_bumper1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Ferdelance Shipkit 1 Bumper 1",null) },
            { "ferdelance_shipkit1_bumper3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Ferdelance Shipkit 1 Bumper 3",null) },
            { "ferdelance_shipkit1_spoiler3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Ferdelance Shipkit 1 Spoiler 3",null) },
            { "ferdelance_shipkit1_tail3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Ferdelance Shipkit 1 Tail 3",null) },
            { "ferdelance_shipkit1_wings1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Ferdelance Shipkit 1 Wings 1",null) },
            { "krait_light_shipkit1_bumper1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Light Shipkit 1 Bumper 1",null) },
            { "krait_light_shipkit1_bumper2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Light Shipkit 1 Bumper 2",null) },
            { "krait_light_shipkit1_bumper4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Light Shipkit 1 Bumper 4",null) },
            { "krait_light_shipkit1_spoiler1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Light Shipkit 1 Spoiler 1",null) },
            { "krait_light_shipkit1_spoiler2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Light Shipkit 1 Spoiler 2",null) },
            { "krait_light_shipkit1_spoiler3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Light Shipkit 1 Spoiler 3",null) },
            { "krait_light_shipkit1_spoiler4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Light Shipkit 1 Spoiler 4",null) },
            { "krait_light_shipkit1_tail3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Light Shipkit 1 Tail 3",null) },
            { "krait_light_shipkit1_tail4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Light Shipkit 1 Tail 4",null) },
            { "krait_light_shipkit1_wings1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Light Shipkit 1 Wings 1",null) },
            { "krait_light_shipkit1_wings2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Light Shipkit 1 Wings 2",null) },
            { "krait_light_shipkit1_wings3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Light Shipkit 1 Wings 3",null) },
            { "krait_light_shipkit1_wings4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Light Shipkit 1 Wings 4",null) },
            { "krait_mkii_shipkit1_bumper1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Mkii Shipkit 1 Bumper 1",null) },
            { "krait_mkii_shipkit1_bumper2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Mkii Shipkit 1 Bumper 2",null) },
            { "krait_mkii_shipkit1_bumper3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Mkii Shipkit 1 Bumper 3",null) },
            { "krait_mkii_shipkit1_spoiler1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Mkii Shipkit 1 Spoiler 1",null) },
            { "krait_mkii_shipkit1_spoiler2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Mkii Shipkit 1 Spoiler 2",null) },
            { "krait_mkii_shipkit1_spoiler4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Mkii Shipkit 1 Spoiler 4",null) },
            { "krait_mkii_shipkit1_tail1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Mkii Shipkit 1 Tail 1",null) },
            { "krait_mkii_shipkit1_tail2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Mkii Shipkit 1 Tail 2",null) },
            { "krait_mkii_shipkit1_tail3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Mkii Shipkit 1 Tail 3",null) },
            { "krait_mkii_shipkit1_wings2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Mkii Shipkit 1 Wings 2",null) },
            { "krait_mkii_shipkit1_wings3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Mkii Shipkit 1 Wings 3",null) },
            { "krait_mkii_shipkit1_wings4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Mkii Shipkit 1 Wings 4",null) },
            { "krait_mkii_shipkitraider1_spoiler3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Mkii Shipkit raider 1 Spoiler 3",null) },
            { "krait_mkii_shipkitraider1_wings2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Mkii Shipkit raider 1 Wings 2",null) },
            { "nameplate_combat01_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Combat 1 White",null) },
            { "nameplate_combat02_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Combat 2 White",null) },
            { "nameplate_combat03_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Combat 3 Black",null) },
            { "nameplate_combat03_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Combat 3 White",null) },
            { "nameplate_empire01_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Empire 1 White",null) },
            { "nameplate_empire02_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Empire 2 Black",null) },
            { "nameplate_empire03_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Empire 3 Black",null) },
            { "nameplate_empire03_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Empire 3 White",null) },
            { "nameplate_expedition01_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Expedition 1 Black",null) },
            { "nameplate_expedition01_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Expedition 1 White",null) },
            { "nameplate_expedition02_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Expedition 2 Black",null) },
            { "nameplate_expedition02_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Expedition 2 White",null) },
            { "nameplate_expedition03_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Expedition 3 Black",null) },
            { "nameplate_expedition03_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Expedition 3 White",null) },
            { "nameplate_explorer01_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Explorer 1 Black",null) },
            { "nameplate_explorer01_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Explorer 1 Grey",null) },
            { "nameplate_explorer01_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Explorer 1 White",null) },
            { "nameplate_explorer02_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Explorer 2 Black",null) },
            { "nameplate_explorer02_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Explorer 2 Grey",null) },
            { "nameplate_explorer02_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Explorer 2 White",null) },
            { "nameplate_explorer03_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Explorer 3 Black",null) },
            { "nameplate_explorer03_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Explorer 3 White",null) },
            { "nameplate_hunter01_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Hunter 1 White",null) },
            { "nameplate_passenger01_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Passenger 1 Black",null) },
            { "nameplate_passenger01_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Passenger 1 White",null) },
            { "nameplate_passenger02_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Passenger 2 Black",null) },
            { "nameplate_passenger03_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Passenger 3 White",null) },
            { "nameplate_pirate03_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Pirate 3 White",null) },
            { "nameplate_practical01_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Practical 1 Black",null) },
            { "nameplate_practical01_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Practical 1 Grey",null) },
            { "nameplate_practical01_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Practical 1 White",null) },
            { "nameplate_practical02_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Practical 2 Black",null) },
            { "nameplate_practical02_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Practical 2 Grey",null) },
            { "nameplate_practical02_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Practical 2 White",null) },
            { "nameplate_practical03_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Practical 3 Black",null) },
            { "nameplate_practical03_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Practical 3 White",null) },
            { "nameplate_raider03_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Raider 3 Black",null) },
            { "nameplate_shipid_doubleline_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Ship ID Double Line Black",null) },
            { "nameplate_shipid_doubleline_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Ship ID Double Line Grey",null) },
            { "nameplate_shipid_doubleline_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Ship ID Double Line White",null) },
            { "nameplate_shipid_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Ship ID Grey",null) },
            { "nameplate_shipid_singleline_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Ship ID Single Line Black",null) },
            { "nameplate_shipid_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Ship ID White",null) },
            { "nameplate_shipname_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Ship Name White",null) },
            { "nameplate_shipid_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Shipid Black",null) },
            { "nameplate_shipid_singleline_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Shipid Singleline Grey",null) },
            { "nameplate_shipid_singleline_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Shipid Singleline White",null) },
            { "nameplate_shipname_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Shipname Black",null) },
            { "nameplate_shipname_distressed_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Shipname Distressed Black",null) },
            { "nameplate_shipname_distressed_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Shipname Distressed Grey",null) },
            { "nameplate_shipname_distressed_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Shipname Distressed White",null) },
            { "nameplate_shipname_worn_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Shipname Worn Black",null) },
            { "nameplate_shipname_worn_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Shipname Worn White",null) },
            { "nameplate_skulls01_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Skulls 1 White",null) },
            { "nameplate_skulls03_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Skulls 3 Black",null) },
            { "nameplate_skulls03_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Skulls 3 White",null) },
            { "nameplate_sympathiser03_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Sympathiser 3 White",null) },
            { "nameplate_trader01_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Trader 1 Black",null) },
            { "nameplate_trader01_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Trader 1 White",null) },
            { "nameplate_trader02_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Trader 2 Black",null) },
            { "nameplate_trader02_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Trader 2 Grey",null) },
            { "nameplate_trader02_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Trader 2 White",null) },
            { "nameplate_trader03_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Trader 3 Black",null) },
            { "nameplate_trader03_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Trader 3 White",null) },
            { "nameplate_victory02_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Victory 2 White",null) },
            { "nameplate_victory03_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Victory 3 White",null) },
            { "nameplate_wings01_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Wings 1 Black",null) },
            { "nameplate_wings01_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Wings 1 White",null) },
            { "nameplate_wings02_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Wings 2 Black",null) },
            { "nameplate_wings02_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Wings 2 White",null) },
            { "nameplate_wings03_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Wings 3 Black",null) },
            { "nameplate_wings03_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Wings 3 Grey",null) },
            { "nameplate_wings03_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Wings 3 White",null) },
            { "paintjob_adder_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Adder Black Friday 1",null) },
            { "paintjob_anaconda_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Anaconda Blackfriday 1",null) },
            { "paintjob_anaconda_corrosive_04", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Anaconda Corrosive 4",null) },
            { "paintjob_anaconda_eliteexpo_eliteexpo", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Anaconda Elite Expo Elite Expo",null) },
            { "paintjob_anaconda_faction1_04", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Anaconda Faction 1 4",null) },
            { "paintjob_anaconda_gold_wireframe_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Anaconda Gold Wireframe 1",null) },
            { "paintjob_anaconda_horus2_03", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Anaconda Horus 2 3",null) },
            { "paintjob_anaconda_iridescenthighcolour_02", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Anaconda Iridescent High Colour 2",null) },
            { "paintjob_anaconda_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Anaconda Lrpo Azure",null) },
            { "paintjob_anaconda_luminous_stripe_03", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Anaconda Luminous Stripe 3",null) },
            { "paintjob_anaconda_luminous_stripe_04", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Anaconda Luminous Stripe 4",null) },
            { "paintjob_anaconda_luminous_stripe_06", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Anaconda Luminous Stripe 6",null) },
            { "paintjob_anaconda_metallic2_chrome", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Anaconda Metallic 2 Chrome",null) },
            { "paintjob_anaconda_metallic_gold", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Anaconda Metallic Gold",null) },
            { "paintjob_anaconda_militaire_earth_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Anaconda Militaire Earth Red",null) },
            { "paintjob_anaconda_militaire_earth_yellow", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Anaconda Militaire Earth Yellow",null) },
            { "paintjob_anaconda_pulse2_purple", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Anaconda Pulse 2 Purple",null) },
            { "paintjob_anaconda_strife_strife", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Anaconda Strife Strife",null) },
            { "paintjob_anaconda_tactical_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Anaconda Tactical Blue",null) },
            { "paintjob_anaconda_vibrant_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Anaconda Vibrant Blue",null) },
            { "paintjob_anaconda_vibrant_green", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Anaconda Vibrant Green",null) },
            { "paintjob_anaconda_vibrant_orange", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Anaconda Vibrant Orange",null) },
            { "paintjob_anaconda_vibrant_purple", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Anaconda Vibrant Purple",null) },
            { "paintjob_anaconda_vibrant_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Anaconda Vibrant Red",null) },
            { "paintjob_anaconda_vibrant_yellow", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Anaconda Vibrant Yellow",null) },
            { "paintjob_anaconda_wireframe_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Anaconda Wireframe 1",null) },
            { "paintjob_asp_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Asp Blackfriday 1",null) },
            { "paintjob_asp_gamescom_gamescom", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Asp Games Com GamesCom",null) },
            { "paintjob_asp_gold_wireframe_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Asp Gold Wireframe 1",null) },
            { "paintjob_asp_iridescenthighcolour_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Asp Iridescent High Colour 1",null) },
            { "paintjob_asp_largelogometallic_05", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Asp Largelogometallic 5",null) },
            { "paintjob_asp_metallic_gold", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Asp Metallic Gold",null) },
            { "paintjob_asp_blackfriday2_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Asp Blackfriday 2 1",null) },
            { "paintjob_asp_salvage_03", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Asp Salvage 3",null) },
            { "paintjob_asp_salvage_06", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Asp Salvage 6",null) },
            { "paintjob_asp_scout_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Asp Scout Black Friday 1",null) },
            { "paintjob_asp_squadron_green", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Asp Squadron Green",null) },
            { "paintjob_asp_squadron_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Asp Squadron Red",null) },
            { "paintjob_asp_stripe1_03", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Asp Stripe 1 3",null) },
            { "paintjob_asp_tactical_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Asp Tactical Grey",null) },
            { "paintjob_asp_tactical_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Asp Tactical White",null) },
            { "paintjob_asp_trespasser_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Asp Trespasser 1",null) },
            { "paintjob_asp_vibrant_purple", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Asp Vibrant Purple",null) },
            { "paintjob_asp_vibrant_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Asp Vibrant Red",null) },
            { "paintjob_asp_wireframe_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Asp Wireframe 1",null) },
            { "paintjob_belugaliner_metallic2_gold", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Beluga Liner Metallic 2 Gold",null) },
            { "paintjob_cobramkiii_25thanniversary_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cobra Mk III 25 Thanniversary 1",null) },
            { "paintjob_cobramkiii_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cobra Mk III Black Friday 1",null) },
            { "paintjob_cobramkiii_flag_canada_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cobra Mk III Flag Canada 1",null) },
            { "paintjob_cobramkiii_flag_uk_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cobra Mk III Flag UK 1",null) },
            { "paintjob_cobramkiii_militaire_earth_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cobra Mk III Militaire Earth Red",null) },
            { "paintjob_cobramkiii_militaire_forest_green", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cobra Mk III Militaire Forest Green",null) },
            { "paintjob_cobramkiii_militaire_sand", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cobra Mk III Militaire Sand",null) },
            { "paintjob_cobramkiii_onionhead1_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cobra Mk III Onionhead 1 1",null) },
            { "paintjob_cobramkiii_stripe2_02", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cobra Mk III Stripe 2 2",null) },
            { "paintjob_cobramkiii_vibrant_yellow", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cobra Mk III Vibrant Yellow",null) },
            { "paintjob_cobramkiii_wireframe_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cobra Mk III Wireframe 1",null) },
            { "paintjob_cobramkiv_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cobra Mk IV Black Friday 1",null) },
            { "paintjob_cobramkiv_gradient2_06", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cobra Mk IV Gradient 2 6",null) },
            { "paintjob_cobramkiii_corrosive_05", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cobra MKIII Corrosive 5",null) },
            { "paintjob_cobramkiii_default_52", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cobra Mkiii Default 52",null) },
            { "paintjob_cutter_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cutter Black Friday 1",null) },
            { "paintjob_cutter_fullmetal_cobalt", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cutter Full Metal Cobalt",null) },
            { "paintjob_cutter_fullmetal_paladium", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cutter Fullmetal Paladium",null) },
            { "paintjob_cutter_iridescenthighcolour_02", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cutter Iridescent High Colour 2",null) },
            { "paintjob_cutter_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cutter Lrpo Azure",null) },
            { "paintjob_cutter_luminous_stripe_ver2_02", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cutter Luminous Stripe Ver 2 2",null) },
            { "paintjob_cutter_luminous_stripe_ver2_04", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cutter Luminous Stripe Ver 2 4",null) },
            { "paintjob_cutter_metallic2_chrome", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cutter Metallic 2 Chrome",null) },
            { "paintjob_cutter_metallic2_gold", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cutter Metallic 2 Gold",null) },
            { "paintjob_cutter_metallic_chrome", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cutter Metallic Chrome",null) },
            { "paintjob_cutter_militaire_forest_green", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cutter Militaire Forest Green",null) },
            { "paintjob_cutter_smartfancy_2_06", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cutter Smartfancy 2 6",null) },
            { "paintjob_cutter_smartfancy_04", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cutter Smartfancy 4",null) },
            { "paintjob_cutter_tactical_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cutter Tactical Grey",null) },
            { "paintjob_cutter_vibrant_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cutter Vibrant Blue",null) },
            { "paintjob_cutter_vibrant_purple", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cutter Vibrant Purple",null) },
            { "paintjob_cutter_vibrant_yellow", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Cutter Vibrant Yellow",null) },
            { "paintjob_diamondbackxl_metallic2_chrome", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Diamond Back XL Metallic 2 Chrome",null) },
            { "paintjob_diamondbackxl_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Diamondbackxl Lrpo Azure",null) },
            { "paintjob_dolphin_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Dolphin Blackfriday 1",null) },
            { "paintjob_dolphin_iridescentblack_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Dolphin Iridescentblack 1",null) },
            { "paintjob_dolphin_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Dolphin Lrpo Azure",null) },
            { "paintjob_dolphin_metallic2_gold", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Dolphin Metallic 2 Gold",null) },
            { "paintjob_eagle_crimson", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Eagle Crimson",null) },
            { "paintjob_eagle_tactical_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Eagle Tactical Grey",null) },
            { "paintjob_empire_courier_aerial_display_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Empire Courier Aerial Display Blue",null) },
            { "paintjob_empire_courier_aerial_display_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Empire Courier Aerial Display Red",null) },
            { "paintjob_empire_courier_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Empire Courier Lrpo Azure",null) },
            { "paintjob_empire_courier_smartfancy_04", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Empire Courier Smartfancy 4",null) },
            { "paintjob_empire_courier_tactical_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Empire Courier Tactical Grey",null) },
            { "paintjob_empire_courier_vibrant_yellow", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Empire Courier Vibrant Yellow",null) },
            { "paintjob_empire_eagle_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Empire Eagle Black Friday 1",null) },
            { "paintjob_empire_eagle_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Empire Eagle Lrpo Azure",null) },
            { "paintjob_empiretrader_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Empire Trader Black Friday 1",null) },
            { "paintjob_empire_trader_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Empire Trader Lrpo Azure",null) },
            { "paintjob_empiretrader_smartfancy_2_06", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Empiretrader Smartfancy 2 6",null) },
            { "paintjob_empiretrader_smartfancy_04", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Empiretrader Smartfancy 4",null) },
            { "paintjob_empiretrader_tactical_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Empiretrader Tactical Blue",null) },
            { "paintjob_empiretrader_tactical_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Empiretrader Tactical Grey",null) },
            { "paintjob_empiretrader_vibrant_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Empiretrader Vibrant Blue",null) },
            { "paintjob_empiretrader_vibrant_purple", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Empiretrader Vibrant Purple",null) },
            { "paintjob_feddropship_mkii_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Fed Dropship Mk II Black Friday 1",null) },
            { "paintjob_feddropship_mkii_tactical_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Fed Dropship Mk II Tactical Blue",null) },
            { "paintjob_feddropship_mkii_vibrant_purple", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Fed Dropship Mk II Vibrant Purple",null) },
            { "paintjob_feddropship_tactical_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Fed Dropship Tactical Blue",null) },
            { "paintjob_feddropship_mkii_vibrant_yellow", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Feddropship Mkii Vibrant Yellow",null) },
            { "paintjob_feddropship_vibrant_orange", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Feddropship Vibrant Orange",null) },
            { "paintjob_federation_corvette_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Federation Corvette Blackfriday 1",null) },
            { "paintjob_federation_corvette_colourgeo2_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Federation Corvette Colour Geo 2 Blue",null) },
            { "paintjob_federation_corvette_colourgeo_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Federation Corvette Colour Geo Blue",null) },
            { "paintjob_federation_corvette_iridescenthighcolour_02", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Federation Corvette Iridescent High Colour 2",null) },
            { "paintjob_federation_corvette_iridescentblack_02", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Federation Corvette Iridescentblack 2",null) },
            { "paintjob_federation_corvette_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Federation Corvette Lrpo Azure",null) },
            { "paintjob_federation_corvette_metallic2_chrome", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Federation Corvette Metallic 2 Chrome",null) },
            { "paintjob_federation_corvette_metallic2_gold", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Federation Corvette Metallic 2 Gold",null) },
            { "paintjob_federation_corvette_metallic_chrome", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Federation Corvette Metallic Chrome",null) },
            { "paintjob_federation_corvette_predator_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Federation Corvette Predator Red",null) },
            { "paintjob_federation_corvette_vibrant_purple", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Federation Corvette Vibrant Purple",null) },
            { "paintjob_federation_gunship_metallic_chrome", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Federation Gunship Metallic Chrome",null) },
            { "paintjob_federation_gunship_tactical_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Federation Gunship Tactical Blue",null) },
            { "paintjob_federation_gunship_tactical_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Federation Gunship Tactical Grey",null) },
            { "paintjob_ferdelance_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Fer De Lance Black Friday 1",null) },
            { "paintjob_ferdelance_metallic2_chrome", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Fer De Lance Metallic 2 Chrome",null) },
            { "paintjob_ferdelance_metallic2_gold", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Fer De Lance Metallic 2 Gold",null) },
            { "paintjob_ferdelance_wireframe_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Fer De Lance Wireframe 1",null) },
            { "paintjob_ferdelance_gradient2_crimson", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Ferdelance Gradient 2 Crimson",null) },
            { "paintjob_ferdelance_vibrant_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Ferdelance Vibrant Red",null) },
            { "paintjob_hauler_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Hauler Blackfriday 1",null) },
            { "paintjob_hauler_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Hauler Lrpo Azure",null) },
            { "paintjob_indfighter_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Ind Fighter Black Friday 1",null) },
            { "paintjob_indfighter_vibrant_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Ind Fighter Vibrant Blue",null) },
            { "paintjob_indfighter_vibrant_green", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Ind Fighter Vibrant Green",null) },
            { "paintjob_indfighter_vibrant_yellow", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Ind Fighter Vibrant Yellow",null) },
            { "paintjob_independant_trader_tactical_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Independant Trader Tactical White",null) },
            { "paintjob_independant_trader_vibrant_purple", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Independant Trader Vibrant Purple",null) },
            { "paintjob_indfighter_vibrant_purple", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Indfighter Vibrant Purple",null) },
            { "paintjob_krait_light_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Krait Light Blackfriday 1",null) },
            { "paintjob_krait_light_gradient2_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Krait Light Gradient 2 Blue",null) },
            { "paintjob_krait_light_horus1_03", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Krait Light Horus 1 3",null) },
            { "paintjob_krait_light_horus2_03", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Krait Light Horus 2 3",null) },
            { "paintjob_krait_light_iridescentblack_02", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Krait Light Iridescentblack 2",null) },
            { "paintjob_krait_light_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Krait Light Lrpo Azure",null) },
            { "paintjob_krait_light_salvage_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Krait Light Salvage 1",null) },
            { "paintjob_krait_light_salvage_03", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Krait Light Salvage 3",null) },
            { "paintjob_krait_light_salvage_04", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Krait Light Salvage 4",null) },
            { "paintjob_krait_light_salvage_06", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Krait Light Salvage 6",null) },
            { "paintjob_krait_light_spring_05", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Krait Light Spring 5",null) },
            { "paintjob_krait_light_tactical_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Krait Light Tactical White",null) },
            { "paintjob_krait_mkii_iridescenthighcolour_05", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Krait Mk II Iridescent High Colour 5",null) },
            { "paintjob_krait_mkii_specialeffectchristmas_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Krait Mk II Special Effect Christmas 1",null) },
            { "paintjob_krait_mkii_festive_silver", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Krait Mkii Festive Silver",null) },
            { "paintjob_krait_mkii_horus2_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Krait Mkii Horus 2 1",null) },
            { "paintjob_krait_mkii_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Krait Mkii Lrpo Azure",null) },
            { "paintjob_krait_mkii_militaire_forest_green", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Krait Mkii Militaire Forest Green",null) },
            { "paintjob_krait_mkii_salvage_03", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Krait Mkii Salvage 3",null) },
            { "paintjob_krait_mkii_tactical_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Krait Mkii Tactical Red",null) },
            { "paintjob_krait_mkii_trims_blackmagenta", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Krait Mkii Trims Blackmagenta",null) },
            { "paintjob_krait_mkii_turbulence_02", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Krait Mkii Turbulence 2",null) },
            { "paintjob_mamba_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Mamba Black Friday 1",null) },
            { "paintjob_orca_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Orca Black Friday 1",null) },
            { "paintjob_orca_corporate2_corporate2e", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Orca Corporate 2 Corporate 2 E",null) },
            { "paintjob_orca_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Orca Lrpo Azure",null) },
            { "paintjob_python_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Black Friday 1",null) },
            { "paintjob_python_corrosive_05", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Corrosive 5",null) },
            { "paintjob_python_eliteexpo_eliteexpo", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Elite Expo Elite Expo",null) },
            { "paintjob_python_gold_wireframe_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Gold Wireframe 1",null) },
            { "paintjob_python_gradient2_02", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Gradient 2 2",null) },
            { "paintjob_python_gradient2_06", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Gradient 2 6",null) },
            { "paintjob_python_horus1_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Horus 1 1",null) },
            { "paintjob_python_iridescentblack_06", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Iridescentblack 6",null) },
            { "paintjob_python_luminous_stripe_03", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Luminous Stripe 3",null) },
            { "paintjob_python_metallic2_chrome", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Metallic 2 Chrome",null) },
            { "paintjob_python_metallic2_gold", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Metallic 2 Gold",null) },
            { "paintjob_python_militaire_dark_green", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Militaire Dark Green",null) },
            { "paintjob_python_militaire_desert_sand", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Militaire Desert Sand",null) },
            { "paintjob_python_militaire_earth_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Militaire Earth Red",null) },
            { "paintjob_python_militaire_earth_yellow", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Militaire Earth Yellow",null) },
            { "paintjob_python_militaire_forest_green", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Militaire Forest Green",null) },
            { "paintjob_python_militaire_sand", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Militaire Sand",null) },
            { "paintjob_python_militarystripe_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Military Stripe Blue",null) },
            { "paintjob_python_salvage_03", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Salvage 3",null) },
            { "paintjob_python_squadron_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Squadron Black",null) },
            { "paintjob_python_vibrant_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Vibrant Blue",null) },
            { "paintjob_python_vibrant_green", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Vibrant Green",null) },
            { "paintjob_python_vibrant_orange", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Vibrant Orange",null) },
            { "paintjob_python_vibrant_purple", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Vibrant Purple",null) },
            { "paintjob_python_vibrant_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Vibrant Red",null) },
            { "paintjob_python_vibrant_yellow", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Vibrant Yellow",null) },
            { "paintjob_python_wireframe_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Python Wireframe 1",null) },
            { "paintjob_python_nx_venom_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Python Nx Venom 1",null) },
            { "paintjob_sidewinder_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Sidewinder Blackfriday 1",null) },
            { "paintjob_sidewinder_doublestripe_08", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Sidewinder Doublestripe 8",null) },
            { "paintjob_sidewinder_festive_silver", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Sidewinder Festive Silver",null) },
            { "paintjob_sidewinder_hotrod_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Sidewinder Hotrod 1",null) },
            { "paintjob_sidewinder_metallic_chrome", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Sidewinder Metallic Chrome",null) },
            { "paintjob_sidewinder_pax_east_pax_east", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Sidewinder Pax East",null) },
            { "paintjob_sidewinder_pilotreward_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Sidewinder Pilotreward 1",null) },
            { "paintjob_sidewinder_vibrant_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Sidewinder Vibrant Blue",null) },
            { "paintjob_sidewinder_vibrant_orange", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Sidewinder Vibrant Orange",null) },
            { "paintjob_testbuggy_chase_04", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Testbuggy Chase 4",null) },
            { "paintjob_testbuggy_chase_05", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Testbuggy Chase 5",null) },
            { "paintjob_testbuggy_militaire_desert_sand", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Testbuggy Militaire Desert Sand",null) },
            { "paintjob_testbuggy_tactical_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Testbuggy Tactical Grey",null) },
            { "paintjob_testbuggy_tactical_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Testbuggy Tactical Red",null) },
            { "paintjob_testbuggy_tactical_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Testbuggy Tactical White",null) },
            { "paintjob_testbuggy_vibrant_purple", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Testbuggy Vibrant Purple",null) },
            { "paintjob_type6_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Type 6 Blackfriday 1",null) },
            { "paintjob_type6_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Type 6 Lrpo Azure",null) },
            { "paintjob_type6_militaire_sand", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Type 6 Militaire Sand",null) },
            { "paintjob_type6_tactical_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Type 6 Tactical Blue",null) },
            { "paintjob_type6_vibrant_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Type 6 Vibrant Blue",null) },
            { "paintjob_type6_vibrant_yellow", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Type 6 Vibrant Yellow",null) },
            { "paintjob_type7_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Type 7 Black Friday 1",null) },
            { "paintjob_type7_salvage_03", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Type 7 Salvage 3",null) },
            { "paintjob_type7_tactical_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Type 7 Tactical White",null) },
            { "paintjob_type9_mechanist_04", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Type 9 Mechanist 4",null) },
            { "paintjob_type9_military_fullmetal_cobalt", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Type 9 Military Full Metal Cobalt",null) },
            { "paintjob_type9_military_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Type 9 Military Lrpo Azure",null) },
            { "paintjob_type9_military_metallic2_chrome", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Type 9 Military Metallic 2 Chrome",null) },
            { "paintjob_type9_military_militaire_forest_green", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Type 9 Military Militaire Forest Green",null) },
            { "paintjob_type9_military_tactical_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Type 9 Military Tactical Red",null) },
            { "paintjob_type9_military_vibrant_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Type 9 Military Vibrant Blue",null) },
            { "paintjob_type9_salvage_03", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Type 9 Salvage 3",null) },
            { "paintjob_type9_salvage_06", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Type 9 Salvage 6",null) },
            { "paintjob_type9_spring_04", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Type 9 Spring 4",null) },
            { "paintjob_type9_vibrant_orange", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Type 9 Vibrant Orange",null) },
            { "paintjob_typex_2_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Typex 2 Lrpo Azure",null) },
            { "paintjob_typex_3_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Typex 3 Lrpo Azure",null) },
            { "paintjob_typex_festive_silver", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Typex Festive Silver",null) },
            { "paintjob_typex_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Typex Lrpo Azure",null) },
            { "paintjob_viper_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Viper Blackfriday 1",null) },
            { "paintjob_viper_default_03", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Viper Default 3",null) },
            { "paintjob_viper_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Viper Lrpo Azure",null) },
            { "paintjob_viper_merc", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Viper Merc",null) },
            { "paintjob_viper_mkiv_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Viper Mk IV Black Friday 1",null) },
            { "paintjob_viper_mkiv_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Viper Mkiv Lrpo Azure",null) },
            { "paintjob_viper_stripe1_02", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Viper Stripe 1 2",null) },
            { "paintjob_viper_vibrant_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Viper Vibrant Blue",null) },
            { "paintjob_viper_vibrant_green", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Viper Vibrant Green",null) },
            { "paintjob_viper_vibrant_orange", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Viper Vibrant Orange",null) },
            { "paintjob_viper_vibrant_purple", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Viper Vibrant Purple",null) },
            { "paintjob_viper_vibrant_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Viper Vibrant Red",null) },
            { "paintjob_viper_vibrant_yellow", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Viper Vibrant Yellow",null) },
            { "paintjob_vulture_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Vulture Black Friday 1",null) },
            { "paintjob_vulture_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Vulture Lrpo Azure",null) },
            { "paintjob_vulture_metallic_chrome", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Vulture Metallic Chrome",null) },
            { "paintjob_vulture_militaire_desert_sand", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Vulture Militaire Desert Sand",null) },
            { "paintjob_vulture_synth_orange", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Vulture Synth Orange",null) },
            { "paintjob_anaconda_corrosive_05", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Anaconda Corrosive 5",null) },
            { "paintjob_anaconda_lavecon_lavecon", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Anaconda Lavecon Lavecon",null) },
            { "paintjob_anaconda_metallic2_gold", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Anaconda Metallic 2 Gold",null) },
            { "paintjob_anaconda_squadron_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Anaconda Squadron Black",null) },
            { "paintjob_anaconda_squadron_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Anaconda Squadron Blue",null) },
            { "paintjob_anaconda_squadron_green", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Anaconda Squadron Green",null) },
            { "paintjob_anaconda_squadron_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Anaconda Squadron Red",null) },
            { "paintjob_anaconda_tactical_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Anaconda Tactical Grey",null) },
            { "paintjob_anaconda_tactical_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Anaconda Tactical Red",null) },
            { "paintjob_anaconda_tactical_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Anaconda Tactical White",null) },
            { "paintjob_asp_halloween01_05", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Asp Halloween 1 5",null) },
            { "paintjob_asp_lavecon_lavecon", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Asp Lavecon Lavecon",null) },
            { "paintjob_asp_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Asp Lrpo Azure",null) },
            { "paintjob_asp_metallic2_gold", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Asp Metallic 2 Gold",null) },
            { "paintjob_asp_operator_green", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Asp Operator Green",null) },
            { "paintjob_asp_operator_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Asp Operator Red",null) },
            { "paintjob_asp_squadron_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Asp Squadron Black",null) },
            { "paintjob_asp_squadron_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Asp Squadron Blue",null) },
            { "paintjob_asp_stripe1_04", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Asp Stripe 1 4",null) },
            { "paintjob_asp_vibrant_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Asp Vibrant Blue",null) },
            { "paintjob_asp_vibrant_orange", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Asp Vibrant Orange",null) },
            { "paintjob_belugaliner_corporatefleet_fleeta", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Belugaliner Corporatefleet Fleeta",null) },
            { "paintjob_cobramkiii_horizons_desert", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Cobra MK III Horizons Desert",null) },
            { "paintjob_cobramkiii_horizons_lunar", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Cobra MK III Horizons Lunar",null) },
            { "paintjob_cobramkiii_horizons_polar", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Cobra MK III Horizons Polar",null) },
            { "paintjob_cobramkiii_stripe1_03", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Cobra MK III Stripe 1 3",null) },
            { "paintjob_cobramkiii_tactical_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Cobra Mk III Tactical Grey",null) },
            { "paintjob_cobramkiii_tactical_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Cobra MK III Tactical White",null) },
            { "paintjob_cobramkiii_vibrant_orange", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Cobra Mk III Vibrant Orange",null) },
            { "paintjob_cobramkiii_yogscast_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Cobra MK III Yogscast 1",null) },
            { "paintjob_cobramkiii_stripe2_03", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Cobramkiii Stripe 2 3",null) },
            { "paintjob_cutter_tactical_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Cutter Tactical White",null) },
            { "paintjob_diamondback_tactical_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Diamondback Tactical Blue",null) },
            { "paintjob_diamondback_tactical_brown", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Diamondback Tactical Brown",null) },
            { "paintjob_diamondback_tactical_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Diamondback Tactical White",null) },
            { "paintjob_diamondbackxl_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Diamondbackxl Blackfriday 1",null) },
            { "paintjob_diamondbackxl_tactical_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Diamondbackxl Tactical Blue",null) },
            { "paintjob_diamondbackxl_tactical_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Diamondbackxl Tactical White",null) },
            { "paintjob_diamondbackxl_vibrant_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Diamondbackxl Vibrant Blue",null) },
            { "paintjob_dolphin_corporatefleet_fleeta", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Dolphin Corporatefleet Fleeta",null) },
            { "paintjob_dolphin_vibrant_yellow", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Dolphin Vibrant Yellow",null) },
            { "paintjob_eagle_tactical_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Eagle Tactical Blue",null) },
            { "paintjob_eagle_tactical_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Eagle Tactical White",null) },
            { "paintjob_empire_courier_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Empire Courier Blackfriday 1",null) },
            { "paintjob_empire_courier_metallic2_gold", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Empire Courier Metallic 2 Gold",null) },
            { "paintjob_empire_fighter_tactical_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Empire Fighter Tactical White",null) },
            { "paintjob_empire_fighter_vibrant_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Empire Fighter Vibrant Blue",null) },
            { "paintjob_empiretrader_tactical_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Empiretrader Tactical White",null) },
            { "paintjob_feddropship_tactical_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Feddropship Tactical Grey",null) },
            { "paintjob_federation_corvette_colourgeo_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Federation Corvette Colourgeo Red",null) },
            { "paintjob_federation_corvette_predator_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Federation Corvette Predator Blue",null) },
            { "paintjob_federation_corvette_tactical_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Federation Corvette Tactical White",null) },
            { "paintjob_federation_corvette_vibrant_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Federation Corvette Vibrant Blue",null) },
            { "paintjob_federation_fighter_tactical_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Federation Fighter Tactical White",null) },
            { "paintjob_federation_fighter_vibrant_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Federation Fighter Vibrant Blue",null) },
            { "paintjob_federation_gunship_tactical_brown", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Federation Gunship Tactical Brown",null) },
            { "paintjob_federation_gunship_tactical_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Federation Gunship Tactical White",null) },
            { "paintjob_ferdelance_tactical_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Ferdelance Tactical White",null) },
            { "paintjob_ferdelance_vibrant_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Ferdelance Vibrant Blue",null) },
            { "paintjob_ferdelance_vibrant_yellow", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Ferdelance Vibrant Yellow",null) },
            { "paintjob_hauler_doublestripe_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Hauler Doublestripe 1",null) },
            { "paintjob_hauler_doublestripe_02", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Hauler Doublestripe 2",null) },
            { "paintjob_independant_trader_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Independant Trader Blackfriday 1",null) },
            { "paintjob_indfighter_tactical_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Indfighter Tactical White",null) },
            { "paintjob_krait_mkii_egypt_02", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Krait Mkii Egypt 2",null) },
            { "paintjob_krait_mkii_vibrant_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Krait Mkii Vibrant Red",null) },
            { "paintjob_orca_militaire_desert_sand", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Orca Militaire Desert Sand",null) },
            { "paintjob_orca_vibrant_yellow", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Orca Vibrant Yellow",null) },
            { "paintjob_python_corrosive_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Python Corrosive 1",null) },
            { "paintjob_python_corrosive_06", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Python Corrosive 6",null) },
            { "paintjob_python_horus1_02", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Python Horus 1 2",null) },
            { "paintjob_python_horus2_03", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Python Horus 2 3",null) },
            { "paintjob_python_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Python Lrpo Azure",null) },
            { "paintjob_python_luminous_stripe_02", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Python Luminous Stripe 2",null) },
            { "paintjob_python_tactical_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Python Tactical White",null) },
            { "paintjob_sidewinder_doublestripe_07", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Sidewinder Doublestripe 7",null) },
            { "paintjob_sidewinder_gold_wireframe_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Sidewinder Gold Wireframe 1",null) },
            { "paintjob_sidewinder_militaire_forest_green", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Sidewinder Militaire Forest Green",null) },
            { "paintjob_sidewinder_specialeffect_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Sidewinder Specialeffect 1",null) },
            { "paintjob_sidewinder_thirds_06", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Sidewinder Thirds 6",null) },
            { "paintjob_sidewinder_vibrant_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Sidewinder Vibrant Red",null) },
            { "paintjob_testbuggy_chase_06", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob SRV Chase 6",null) },
            { "paintjob_testbuggy_destination_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob SRV Destination Blue",null) },
            { "paintjob_testbuggy_luminous_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob SRV Luminous Blue",null) },
            { "paintjob_testbuggy_luminous_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob SRV Luminous Red",null) },
            { "paintjob_testbuggy_metallic2_gold", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob SRV Metallic 2 Gold",null) },
            { "paintjob_testbuggy_militaire_earth_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob SRV Militaire Earth Red",null) },
            { "paintjob_testbuggy_militaire_earth_yellow", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob SRV Militaire Earth Yellow",null) },
            { "paintjob_testbuggy_tactical_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob SRV Tactical Blue",null) },
            { "paintjob_testbuggy_vibrant_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob SRV Vibrant Blue",null) },
            { "paintjob_testbuggy_vibrant_orange", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob SRV Vibrant Orange",null) },
            { "paintjob_testbuggy_vibrant_yellow", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob SRV Vibrant Yellow",null) },
            { "paintjob_type6_tactical_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Type 6 Tactical White",null) },
            { "paintjob_type7_vibrant_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Type 7 Vibrant Blue",null) },
            { "paintjob_type9_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Type 9 Blackfriday 1",null) },
            { "paintjob_type9_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Type 9 Lrpo Azure",null) },
            { "paintjob_type9_military_iridescentblack_02", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Type 9 Military Iridescent black 2",null) },
            { "paintjob_type9_vibrant_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Type 9 Vibrant Blue",null) },
            { "paintjob_typex_military_tactical_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Typex Military Tactical Grey",null) },
            { "paintjob_typex_military_tactical_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Typex Military Tactical White",null) },
            { "paintjob_typex_operator_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Typex Operator Red",null) },
            { "paintjob_viper_flag_norway_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Viper Flag Norway 1",null) },
            { "paintjob_viper_mkiv_militaire_sand", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Viper MK IV Militaire Sand",null) },
            { "paintjob_viper_mkiv_squadron_black", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Viper MK IV Squadron Black",null) },
            { "paintjob_viper_mkiv_squadron_orange", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Viper MK IV Squadron Orange",null) },
            { "paintjob_viper_mkiv_tactical_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Viper MK IV Tactical Blue",null) },
            { "paintjob_viper_mkiv_tactical_brown", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Viper MK IV Tactical Brown",null) },
            { "paintjob_viper_mkiv_tactical_green", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Viper MK IV Tactical Green",null) },
            { "paintjob_viper_mkiv_tactical_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Viper MK IV Tactical Grey",null) },
            { "paintjob_viper_mkiv_tactical_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Viper MK IV Tactical White",null) },
            { "paintjob_vulture_tactical_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Vulture Tactical Blue",null) },
            { "paintjob_vulture_tactical_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Vulture Tactical White",null) },
            { "paintjob_diamondbackxl_tactical_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Diamondbackxl Tactical Grey",null) },
            { "paintjob_python_tactical_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Python Tactical Grey",null) },
            { "paintjob_krait_light_tactical_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Krait Light Tactical Grey",null) },
            { "paintjob_cutter_militaire_earth_yellow", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Cutter Militaire Earth Yellow",null) },
            { "paintjob_anaconda_fullmetal_cobalt", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Anaconda Fullmetal Cobalt",null) },

            { "nameplate_expedition02_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Nameplate Expedition 2 Grey",null) },
            { "paintjob_adder_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Adder Lrpo Azure",null) },
            { "paintjob_adder_vibrant_orange", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Adder Vibrant Orange",null) },
            { "paintjob_anaconda_horus1_02", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Anaconda Horus 1 2",null) },
            { "paintjob_anaconda_horus1_03", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Anaconda Horus 1 3",null) },
            { "paintjob_anaconda_horus2_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Anaconda Horus 2 1",null) },
            { "paintjob_anaconda_icarus_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Anaconda Icarus Grey",null) },
            { "paintjob_anaconda_iridescentblack_02", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Anaconda Iridescentblack 2",null) },
            { "paintjob_anaconda_lowlighteffect_01_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Anaconda Lowlighteffect 1 1",null) },
            { "paintjob_anaconda_militaire_forest_green", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Anaconda Militaire Forest Green",null) },
            { "paintjob_anaconda_militaire_sand", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Anaconda Militaire Sand",null) },
            { "paintjob_anaconda_prestige_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Anaconda Prestige Blue",null) },
            { "paintjob_anaconda_prestige_green", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Anaconda Prestige Green",null) },
            { "paintjob_anaconda_prestige_purple", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Anaconda Prestige Purple",null) },
            { "paintjob_anaconda_prestige_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Anaconda Prestige Red",null) },
            { "paintjob_anaconda_pulse2_green", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Anaconda Pulse 2 Green",null) },
            { "paintjob_anaconda_war_orange", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Anaconda War Orange",null) },
            { "paintjob_asp_icarus_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Asp Icarus Grey",null) },
            { "paintjob_asp_iridescentblack_04", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Asp Iridescentblack 4",null) },
            { "paintjob_belugaliner_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Belugaliner Blackfriday 1",null) },
            { "paintjob_belugaliner_ember_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Belugaliner Ember Blue",null) },
            { "paintjob_cobramkiv_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Cobramkiv Lrpo Azure",null) },
            { "paintjob_cutter_gradient2_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Cutter Gradient 2 Red",null) },
            { "paintjob_cutter_iridescentblack_05", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Cutter Iridescentblack 5",null) },
            { "paintjob_cutter_synth_orange", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Cutter Synth Orange",null) },
            { "paintjob_cutter_tactical_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Cutter Tactical Blue",null) },
            { "paintjob_cutter_vibrant_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Cutter Vibrant Red",null) },
            { "paintjob_cutter_war_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Cutter War Blue",null) },
            { "paintjob_diamondback_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Diamond Back Black Friday 1",null) },
            { "paintjob_diamondback_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Diamondback Lrpo Azure",null) },
            { "paintjob_diamondbackxl_vibrant_orange", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Diamondbackxl Vibrant Orange",null) },
            { "paintjob_dolphin_vibrant_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Dolphin Vibrant Blue",null) },
            { "paintjob_eagle_aerial_display_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Eagle Aerial Display Red",null) },
            { "paintjob_eagle_stripe1_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Eagle Stripe 1 1",null) },
            { "paintjob_empire_courier_iridescenthighcolour_02", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Empire Courier Iridescenthighcolour 2",null) },
            { "paintjob_empire_courier_tactical_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Empire Courier Tactical White",null) },
            { "paintjob_empiretrader_slipstream_orange", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Empiretrader Slipstream Orange",null) },
            { "paintjob_feddropship_militaire_earth_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Feddropship Militaire Earth Red",null) },
            { "paintjob_federation_corvette_colourgeo_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Federation Corvette Colourgeo Grey",null) },
            { "paintjob_federation_corvette_razormetal_silver", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Federation Corvette Razormetal Silver",null) },
            { "paintjob_federation_corvette_tactical_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Federation Corvette Tactical Grey",null) },
            { "paintjob_federation_corvette_tactical_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Federation Corvette Tactical Red",null) },
            { "paintjob_federation_corvette_vibrant_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Federation Corvette Vibrant Red",null) },
            { "paintjob_federation_gunship_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Federation Gunship Blackfriday 1",null) },
            { "paintjob_federation_gunship_militarystripe_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Federation Gunship Militarystripe Red",null) },
            { "paintjob_ferdelance_razormetal_copper", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Ferdelance Razormetal Copper",null) },
            { "paintjob_ferdelance_slipstream_orange", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Ferdelance Slipstream Orange",null) },
            { "paintjob_hauler_tactical_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Hauler Tactical Red",null) },
            { "paintjob_hauler_vibrant_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Hauler Vibrant Blue",null) },
            { "paintjob_krait_light_lowlighteffect_01_06", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Krait Light Lowlighteffect 1 6",null) },
            { "paintjob_krait_light_turbulence_06", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Krait Light Turbulence 6",null) },
            { "paintjob_krait_mkii_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Krait Mkii Blackfriday 1",null) },
            { "paintjob_krait_mkii_egypt_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Krait Mkii Egypt 1",null) },
            { "paintjob_krait_mkii_horus1_02", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Krait Mkii Horus 1 2",null) },
            { "paintjob_krait_mkii_horus1_03", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Krait Mkii Horus 1 3",null) },
            { "paintjob_krait_mkii_tactical_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paint Job Krait Mk II Tactical Blue",null) },
            { "paintjob_krait_mkii_trims_greyorange", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Krait Mkii Trims Greyorange",null) },
            { "paintjob_krait_mkii_vibrant_orange", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Krait Mkii Vibrant Orange",null) },
            { "paintjob_mamba_tactical_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Mamba Tactical White",null) },
            { "paintjob_orca_corporate1_corporate1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Orca Corporate 1 Corporate 1",null) },
            { "paintjob_orca_geometric_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Orca Geometric Blue",null) },
            { "paintjob_python_egypt_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Python Egypt 1",null) },
            { "paintjob_python_horus2_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Python Horus 2 1",null) },
            { "paintjob_python_lowlighteffect_01_03", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Python Lowlighteffect 1 3",null) },
            { "paintjob_python_salvage_06", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Python Salvage 6",null) },
            { "paintjob_python_squadron_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Python Squadron Blue",null) },
            { "paintjob_python_squadron_gold", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Python Squadron Gold",null) },
            { "paintjob_python_squadron_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Python Squadron Red",null) },
            { "paintjob_python_tactical_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Python Tactical Red",null) },
            { "paintjob_type6_foss_orangewhite", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Type 6 Foss Orangewhite",null) },
            { "paintjob_type6_foss_whitered", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Type 6 Foss Whitered",null) },
            { "paintjob_type6_iridescentblack_03", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Type 6 Iridescentblack 3",null) },
            { "paintjob_type7_lrpo_azure", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Type 7 Lrpo Azure",null) },
            { "paintjob_type7_militaire_earth_yellow", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Type 7 Militaire Earth Yellow",null) },
            { "paintjob_type7_turbulence_06", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Type 7 Turbulence 6",null) },
            { "paintjob_type9_military_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Type 9 Military Blackfriday 1",null) },
            { "paintjob_type9_military_vibrant_orange", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Type 9 Military Vibrant Orange",null) },
            { "paintjob_type9_tactical_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Type 9 Tactical Grey",null) },
            { "paintjob_type9_turbulence_03", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Type 9 Turbulence 3",null) },
            { "paintjob_typex_2_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Typex 2 Blackfriday 1",null) },
            { "paintjob_typex_3_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Typex 3 Blackfriday 1",null) },
            { "paintjob_typex_3_military_militaire_forest_green", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Typex 3 Military Militaire Forest Green",null) },
            { "paintjob_typex_3_military_tactical_grey", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Typex 3 Military Tactical Grey",null) },
            { "paintjob_typex_3_military_vibrant_yellow", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Typex 3 Military Vibrant Yellow",null) },
            { "paintjob_typex_3_trims_greyorange", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Typex 3 Trims Greyorange",null) },
            { "paintjob_typex_blackfriday_01", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Typex Blackfriday 1",null) },
            { "paintjob_viper_mkiv_slipstream_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Viper Mkiv Slipstream Blue",null) },
            { "paintjob_viper_predator_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Paintjob Viper Predator Blue",null) },
            { "python_shipkit1_bumper1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Shipkit 1 Bumper 1",null) },
            { "python_shipkit1_bumper2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Shipkit 1 Bumper 2",null) },
            { "python_shipkit1_bumper3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Shipkit 1 Bumper 3",null) },
            { "python_shipkit1_bumper4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Shipkit 1 Bumper 4",null) },
            { "python_shipkit1_spoiler1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Shipkit 1 Spoiler 1",null) },
            { "python_shipkit1_spoiler2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Shipkit 1 Spoiler 2",null) },
            { "python_shipkit1_spoiler3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Shipkit 1 Spoiler 3",null) },
            { "python_shipkit1_spoiler4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Shipkit 1 Spoiler 4",null) },
            { "python_shipkit1_tail1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Shipkit 1 Tail 1",null) },
            { "python_shipkit1_tail2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Shipkit 1 Tail 2",null) },
            { "python_shipkit1_tail3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Shipkit 1 Tail 3",null) },
            { "python_shipkit1_tail4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Shipkit 1 Tail 4",null) },
            { "python_shipkit1_wings1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Shipkit 1 Wings 1",null) },
            { "python_shipkit1_wings2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Shipkit 1 Wings 2",null) },
            { "python_shipkit1_wings3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Shipkit 1 Wings 3",null) },
            { "python_shipkit1_wings4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Shipkit 1 Wings 4",null) },
            { "python_shipkit2raider_bumper1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Shipkit 2 Raider Bumper 1",null) },
            { "python_shipkit2raider_bumper3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Shipkit 2 Raider Bumper 3",null) },
            { "python_shipkit2raider_spoiler1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Shipkit 2 Raider Spoiler 1",null) },
            { "python_shipkit2raider_spoiler2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Shipkit 2 Raider Spoiler 2",null) },
            { "python_shipkit2raider_tail1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Shipkit 2 Raider Tail 1",null) },
            { "python_shipkit2raider_tail3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Shipkit 2 Raider Tail 3",null) },
            { "python_shipkit2raider_wings2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Shipkit 2 Raider Wings 2",null) },
            { "python_shipkit2raider_wings3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Shipkit 2 Raider Wings 3",null) },
            { "python_nx_strike_spoiler1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Nx Strike Spoiler 1",null) },
            { "python_nx_strike_wings1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Nx Strike Wings 1",null) },
            { "python_nx_strike_bumper1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Python Nx Strike Bumper 1",null) },
            { "sidewinder_shipkit1_bumper1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Sidewinder Shipkit 1 Bumper 1",null) },
            { "sidewinder_shipkit1_bumper2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Sidewinder Shipkit 1 Bumper 2",null) },
            { "sidewinder_shipkit1_bumper4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Sidewinder Shipkit 1 Bumper 4",null) },
            { "sidewinder_shipkit1_spoiler1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Sidewinder Shipkit 1 Spoiler 1",null) },
            { "sidewinder_shipkit1_spoiler3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Sidewinder Shipkit 1 Spoiler 3",null) },
            { "sidewinder_shipkit1_tail1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Sidewinder Shipkit 1 Tail 1",null) },
            { "sidewinder_shipkit1_tail3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Sidewinder Shipkit 1 Tail 3",null) },
            { "sidewinder_shipkit1_tail4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Sidewinder Shipkit 1 Tail 4",null) },
            { "sidewinder_shipkit1_wings2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Sidewinder Shipkit 1 Wings 2",null) },
            { "sidewinder_shipkit1_wings3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Sidewinder Shipkit 1 Wings 3",null) },
            { "sidewinder_shipkit1_wings4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Sidewinder Shipkit 1 Wings 4",null) },
            { "string_lights_coloured", new ShipModule(999999941,ShipModule.ModuleTypes.VanityType,0,0,"String Lights Coloured",null) },
            { "string_lights_thargoidprobe", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"String Lights Thargoid probe",null) },
            { "string_lights_warm_white", new ShipModule(999999944,ShipModule.ModuleTypes.VanityType,0,0,"String Lights Warm White",null) },
            { "string_lights_skull", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"String Lights Skull",null) },
            { "type6_shipkit1_bumper1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Type 6 Shipkit 1 Bumper 1",null) },
            { "type6_shipkit1_spoiler3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Type 6 Shipkit 1 Spoiler 3",null) },
            { "type6_shipkit1_wings1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Type 6 Shipkit 1 Wings 1",null) },
            { "type9_military_shipkit1_bumper4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Type 9 Military Ship Kit 1 Bumper 4",null) },
            { "type9_military_shipkit1_spoiler3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Type 9 Military Ship Kit 1 Spoiler 3",null) },
            { "type9_military_shipkit1_wings3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Type 9 Military Ship Kit 1 Wings 3",null) },
            { "type9_military_shipkit1_bumper3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Type 9 Military Shipkit 1 Bumper 3",null) },
            { "type9_military_shipkit1_spoiler2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Type 9 Military Shipkit 1 Spoiler 2",null) },
            { "typex_3_shipkit1_bumper3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Typex 3 Shipkit 1 Bumper 3",null) },
            { "typex_3_shipkit1_spoiler3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Typex 3 Shipkit 1 Spoiler 3",null) },
            { "typex_3_shipkit1_wings4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Typex 3 Shipkit 1 Wings 4",null) },
            { "viper_shipkit1_bumper4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Viper Shipkit 1 Bumper 4",null) },
            { "viper_shipkit1_spoiler4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Viper Shipkit 1 Spoiler 4",null) },
            { "viper_shipkit1_tail4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Viper Shipkit 1 Tail 4",null) },
            { "viper_shipkit1_wings4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Viper Shipkit 1 Wings 4",null) },
            { "voicepack_verity", new ShipModule(999999901,ShipModule.ModuleTypes.VanityType,0,0,"Voice Pack Verity",null) },
            { "voicepack_alix", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Voicepack Alix",null) },
            { "voicepack_amelie", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Voicepack Amelie",null) },
            { "voicepack_archer", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Voicepack Archer",null) },
            { "voicepack_carina", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Voicepack Carina",null) },
            { "voicepack_celeste", new ShipModule(999999904,ShipModule.ModuleTypes.VanityType,0,0,"Voicepack Celeste",null) },
            { "voicepack_eden", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Voicepack Eden",null) },
            { "voicepack_gerhard", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Voicepack Gerhard",null) },
            { "voicepack_jefferson", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Voicepack Jefferson",null) },
            { "voicepack_leo", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Voicepack Leo",null) },
            { "voicepack_luciana", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Voicepack Luciana",null) },
            { "voicepack_victor", new ShipModule(999999902,ShipModule.ModuleTypes.VanityType,0,0,"Voicepack Victor",null) },
            { "vulture_shipkit1_bumper1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Vulture Shipkit 1 Bumper 1",null) },
            { "vulture_shipkit1_spoiler3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Vulture Shipkit 1 Spoiler 3",null) },
            { "vulture_shipkit1_spoiler4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Vulture Shipkit 1 Spoiler 4",null) },
            { "vulture_shipkit1_tail1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Vulture Shipkit 1 Tail 1",null) },
            { "vulture_shipkit1_wings2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Vulture Shipkit 1 Wings 2",null) },
            { "weaponcustomisation_blue", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Weapon Customisation Blue",null) },
            { "weaponcustomisation_cyan", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Weapon Customisation Cyan",null) },
            { "weaponcustomisation_green", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Weapon Customisation Green",null) },
            { "weaponcustomisation_pink", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Weapon Customisation Pink",null) },
            { "weaponcustomisation_purple", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Weapon Customisation Purple",null) },
            { "weaponcustomisation_red", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Weapon Customisation Red",null) },
            { "weaponcustomisation_white", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Weapon Customisation White",null) },
            { "weaponcustomisation_yellow", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Weapon Customisation Yellow",null) },

            { "krait_mkii_shipkit1_tail4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Mkii Shipkit 1 Tail 4",null) },
            { "cutter_shipkit1_bumper1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Cutter Shipkit 1 Bumper 1",null) },
            { "type6_shipkit1_spoiler2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Type 6 Shipkit 1 Spoiler 2",null) },
            { "type6_shipkit1_wings4", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Type 6 Shipkit 1 Wings 4",null) },
            { "type6_shipkit1_wings3", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Type 6 Shipkit 1 Wings 3",null) },
            { "empire_courier_shipkit1_bumper1", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Empire Courier Shipkit 1 Bumper 1",null) },
            { "federation_corvette_shipkit1_wings2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Federation Corvette Shipkit 1 Wings 2",null) },
            { "krait_light_shipkit1_tail2", new ShipModule(-1,ShipModule.ModuleTypes.VanityType,0,0,"Krait Light Shipkit 1 Tail 2",null) },

            { "paint", new ShipModule(-1,ShipModule.ModuleTypes.WearAndTearType,0,0,"Paint",null) },
            { "all", new ShipModule(-1,ShipModule.ModuleTypes.WearAndTearType,0,0,"Repair All",null) },
            { "hull", new ShipModule(-1,ShipModule.ModuleTypes.WearAndTearType,0,0,"Repair All",null) },
            { "wear", new ShipModule(-1,ShipModule.ModuleTypes.WearAndTearType,0,0,"Wear",null) },
        };
        #endregion

        #region Synth Modules

        static private Dictionary<string, ShipModule> synthesisedmodules = new Dictionary<string, ShipModule>();        // ones made by edd

        #endregion

    }
}
