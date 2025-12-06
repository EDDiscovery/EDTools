/*
 * Copyright © 2015 - 2024 robbyxp @ github.com
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EDDTest
{
    public partial class ItemModulesEDSY
    {
        Dictionary<string, string> PropertiesToEDD = new Dictionary<string, string>
        {
            ["mtype"] = null,
            ["name"] = null,
            ["fdid"] = null,
            ["fdname"] = null,
            ["eddbid"] = null,
            ["namekey"] = null,
            ["hidden"] = null,
            ["limit"] = null,
            ["tag"] = null,
            ["passive"] = null,
            ["reserved"] = null,
            ["noblueprints"] = null,
            ["powerlock"] = null,
            ["noundersize"] = null,
            ["sco"] = null,
            ["mlctype"] = null, // already in name (multi control type)
            ["noexpeffects"] = null,
            ["unlimit"] = null,
            ["unlimitcount"] = null,
            ["mats"] = null,
            ["special"] = null,

            ["integ"] = "Integrity",
            ["cost"] = "Cost",
            ["mount"] = "Mount",
            ["class"] = "Class",
            ["rating"] = "Rating",
            ["mass"] = "Mass",
            ["boottime"] = "BootTime",
            ["pwrdraw"] = "PowerDraw",
            ["distdraw"] = "DistributorDraw",
            ["rounds"] = "Rounds",
            ["jitter"] = "Jitter",
            ["hullbst"] = "HullStrengthBonus",

            ["jamdur"] = "Time", // s
            ["ecmdur"] = "Time",
            ["hsdur"] = "Time",
            ["duration"] = "Time",
            ["emgcylife"] = "Time",
            ["limpettime"] = "Time",
            ["scantime"] = "Time",
            ["barrierdur"] = "Time",

            ["maximumrng"] = "Range",
            ["lpactrng"] = "Range",
            ["ecmrng"] = "Range",
            ["barrierrng"] = "Range",
            ["scanrng"] = "Range",
            ["maxrng"] = "Range",

            ["cargocap"] = "Size",      // t
            ["fuelcap"] = "Size",       // t

            ["bins"] = "Capacity",
            ["vslots"] = "Capacity",

            ["vcount"] = "Rebuilds",

            ["shotspd"] = "Speed",      // m/s
            ["maxspd"] = "Speed",
            //here
            ["fsdoptmass"] = "OptMass",
            ["genoptmass"] = "OptMass",
            ["engoptmass"] = "OptMass",

            ["engmaxmass"] = "MaxMass",
            ["genmaxmass"] = "MaxMass",
            ["engmaxmass"] = "MaxMass",

            ["engminmass"] = "MinMass",
            ["genminmass"] = "MinMass",
            ["engminmass"] = "MinMass",

            ["ecmcool"] = "ReloadTime",     // s
            ["rldtime"] = "ReloadTime",
            ["barriercool"] = "ReloadTime",

            ["maxangle"] = "Angle",     // deg
            ["scanangle"] = "Angle",
            ["facinglim"] = "Angle",

            ["engheat"] = "ThermalLoad",
            ["fsdheat"] = "ThermalLoad",
            ["thmload"] = "ThermalLoad",
            ["scbheat"] = "ThermalLoad",
            ["ecmheat"] = "ThermalLoad",  //units/sec
            ["thmdrain"] = "ThermalDrain",

            ["afmrepcap"] = "Ammo",
            ["ammomax"] = "Ammo",

            ["ammocost"] = "AmmoCost",
            ["ammoclip"] = "Clip",
            ["damage"] = "Damage",
            ["dps"] = "DPS",
            ["dmgfall"] = "Falloff",

            ["fuelxfer"] = "FuelTransfer",
            ["missile"] = "MissileType",
            ["cabincls"] = "CabinClass",

            ["spinup"] = "SCBSpinUp",
            ["scbdur"] = "SCBDuration",
            ["shieldrnfps"] = "ShieldReinforcement",

            ["genminmul"] = "MinStrength",      // shields
            ["genoptmul"] = "OptStrength",
            ["genmaxmul"] = "MaxStrength",
            ["genrate"] = "RegenRate",
            ["genpwr"] = "MWPerUnit",

            ["ecmpwr"] = "ActivePower", // MW/use


            ["brcdmg"] = "BreachDamage", // damage to target modules
            ["brcpct"] = "BreachModuleDamageAfterBreach",
            ["minbrc"] = "BreachMin",
            ["maxbrc"] = "BreachMax",

            ["thmwgt"] = "ThermalProportionDamage",
            ["kinwgt"] = "KineticProportionDamage",
            ["expwgt"] = "ExplosiveProportionDamage",
            ["abswgt"] = "AbsoluteProportionDamage",
            ["cauwgt"] = "CausticPorportionDamage",
            ["axewgt"] = "AXPorportionDamage",

            ["agzresist"] = "GuardianModuleResistance",

            ["repairrtg"] = "RepairCostPerMat",
            ["repaircon"] = "RateOfRepairConsumption",
            ["bstrof"] = "BurstRateOfFire",
            ["bstsize"] = "BurstSize",
            ["timerng"] = "TargetMaxTime", // sec to intercept
            ["rof"] = "RateOfFire",
            ["bstint"] = "BurstInterval",
            ["dmgmul"] = "DamageMultiplierFullCharge",
            ["pierce"] = "ArmourPiercing",

            ["barrierpwr"] = "MWPerSec",

            ["expres"] = "ExplosiveResistance",           // shield booster, hull reinforcer, shields (also used armour)
            ["kinres"] = "KineticResistance",
            ["thmres"] = "ThermalResistance",
            ["caures"] = "CausticResistance",
            ["axeres"] = "AXResistance",

            ["bgenrate"] = "BrokenRegenRate",
            ["shieldrnf"] = "AdditionalReinforcement",
            ["shieldbst"] = "ShieldReinforcement",
            ["hullrnf"] = "HullReinforcement",

            ["engoptmul"] = "EngineOptMultiplier",
            ["engminmul"] = "EngineMinMultiplier",
            ["engmaxmul"] = "EngineMaxMultiplier",

            ["fuelpower"] = "PowerConstant",
            ["fuelmul"] = "LinearConstant",
            ["maxfuel"] = "MaxFuelPerJump",

            ["pwrbst"] = "PowerBonus",

            ["syschg"] = "SystemsRechargeRate",
            ["syscap"] = "SystemsCapacity",

            ["engchg"] = "EngineRechargeRate",
            ["engcap"] = "EngineCapacity",

            ["wepchg"] = "WeaponsRechargeRate",
            ["wepcap"] = "WeaponsCapacity",


            ["scospd"] = "SCOSpeedIncrease",
            ["scoacc"] = "SCOAccelerationRate",
            ["scoheat"] = "SCOHeatGenerationRate",
            ["scoconint"] = "SCOControlInterference",
            ["scofuel"] = "SCOFuelDuringOvercharge",

            ["typemis"] = "TypicalEmission",
            ["pwrcap"] = "PowerGen",
            ["heateff"] = "HeatEfficiency",
            ["scooprate"] = "RefillRate",
            ["maxlimpet"] = "Limpets",
            ["targetrng"] = "TargetRange",
            ["hacktime"] = "HackTime",
            ["mincargo"] = "MinCargo",
            ["maxcargo"] = "MaxCargo",
            ["cabincap"] = "Passengers",
            ["jumpbst"] = "AdditionalRange",
            ["dmgprot"] = "Protection",
            ["integ"] = "Integrity",
            ["multispd"] = "MultiTargetSpeed",

            ["lmprepcap"] = "MaxRepairMaterialCapacity",
            ["minebonus"] = "MineBonus",

            ["minmulspd"] = "MinimumSpeedModifier",
            ["optmulspd"] = "OptimalSpeedModifier",
            ["maxmulspd"] = "MaximumSpeedModifier",

            ["minmulacc"] = "MinimumAccelerationModifier",
            ["optmulacc"] = "OptimalAccelerationModifier",
            ["maxmulacc"] = "MaximumAccelerationModifier",

            ["minmulrot"] = "MinimumRotationModifier",
            ["optmulrot"] = "OptimalRotationModifier",
            ["maxmulrot"] = "MaximumRotationModifier",

            ["proberad"] = "ProbeRadius"
        };


        bool ProcessData(long edsyfid, string esdyfdname, string edsyname, string parameters, ref string textout, bool checkname = true)
        {
            int lineno = -1;

            if (edsyfid > 0)       // find by fid if its there
            {
                string fids = "(" + edsyfid.ToStringInvariant() + ",";
                lineno = Array.FindIndex(itemmodules, x => x.Contains(fids));
            }

            if (lineno == -1)      // if can't find
            {
                //System.Diagnostics.Debug.WriteLine($"Find {fdname} with unknown FID");

                string fdnamef = esdyfdname.AlwaysQuoteString();
                lineno = Array.FindIndex(itemmodules, x => x.ContainsIIC(fdnamef));     // try fdname quoted

                if (lineno == -1)
                {
                    lineno = Array.FindIndex(itemmodules, x => x.ContainsIIC(edsyname));    // else try text
                }
                else
                {
                    textout += $"FID missing {edsyfid} in module {esdyfdname} should be {edsyfid}\r\n";
                }
            }

            if (lineno >= 0 )
            {
                StringParser sp = new StringParser(itemmodules[lineno]);

                string modulefdname;
                if (sp.IsCharMoveOn('{') && (modulefdname = sp.NextQuotedWord()) != null && sp.IsCharMoveOn(',') && sp.IsStringMoveOn("new ShipModule("))
                {
                    int replacepos = sp.Position;

                    long? fidread = sp.NextLongComma(",");    // fid
                    string edtype = sp.NextWordComma();  // type
                    string name = sp.NextQuotedWord();

                    if (fidread.HasValue && edtype != null && name != null && sp.IsCharMoveOn(')'))
                    {
                        if (!checkname || modulefdname.EqualsIIC(esdyfdname))
                        {
                            edsyname = edsyname.Replace("-", " ");      // Multi-Cannon
                            name = name.Replace("-", " ");      // type-6

                            if (name.Length < edsyname.Length || !edsyname.EqualsIIC(name.Substring(0, edsyname.Length)))
                            {
                                // don't turn diff on - human needed
                                textout += $"Difference name {edsyfid} {esdyfdname} EDSY: '{edsyname}' vs ItemModules.cs: '{name}'\r\n";
                            }

                            string lineshouldbe = $"{{ {parameters} }} }},";
                            bool diff = sp.LineLeft != lineshouldbe;

                            if (diff)
                            {
                                string msg = $"\r\n{lineno + 1} Difference Properties {edsyfid} {esdyfdname}\r\n `{itemmodules[lineno].Trim()}`\r\n`{lineshouldbe}`\r\n";
                                textout += msg;
                                System.Diagnostics.Debug.Write(msg);

                                itemmodules[lineno] = itemmodules[lineno].Left(replacepos) + $"{fidread.Value},{edtype},{name.AlwaysQuoteString()}){lineshouldbe}";
                            }

                            return true;
                        }
                        else
                        {
                            textout += $"Error EDSY does not agree with fdname of itemsmodule {edsyfid} {esdyfdname} : {itemmodules[lineno]}\r\n";
                        }
                    }
                    else
                    {
                        textout += $"Error in form (front part) {edsyfid} {esdyfdname} : {itemmodules[lineno]}\r\n";
                    }
                }
                else
                {
                    textout += $"Error in form (front part) {edsyfid} {esdyfdname} : {itemmodules[lineno]}\r\n";
                }
            }
            else
            {
                esdyfdname = esdyfdname.ToLower();
                string moduletype = esdyfdname.Contains("_grade1") ? "LightweightAlloy" :
                                    esdyfdname.Contains("_grade2") ? "ReinforcedAlloy" :
                                    esdyfdname.Contains("_grade3") ? "MilitaryGradeComposite" :
                                    esdyfdname.Contains("_mirrored") ? "MirroredSurfaceComposite" :
                                    esdyfdname.Contains("_reactive") ? "ReactiveSurfaceComposite" :
                                    "Unknown";

                textout += $"MODULE NOT IN Modules : {{ \"{esdyfdname}\", new ShipModule({edsyfid}, ShipModule.ModuleTypes.{moduletype}, \"{edsyname}\") }},\r\n";
            }

            return false;
        }

        string[] itemmodules = null;

    }
}

