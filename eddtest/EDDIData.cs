/*
 * Copyright © 2023 - 2023 robbyxp @ github.com
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

using EliteDangerousCore;

namespace EDDTest
{
    static public class EDDIData
    {

        static public void Process()
        {
            foreach( var mod in EDDI.Modules)
            {
                if (ItemData.ShipModuleExists(mod.fdname))
                {
                    var edm = ItemData.GetShipModuleProperties(mod.fdname);
                    if (edm.ModuleID != mod.id)
                    {
                        string i = edm.Info == null ? "null": $"\"{edm.Info}\"";

                        System.Diagnostics.Debug.WriteLine($"{{ \"{mod.fdname.ToLowerInvariant()}\", new ShipModule({mod.id}, {edm.Mass}, {edm.Power}, {i}, \"{edm.ModName}\", \"{edm.ModType}\" ) }},");
                        mod.id = edm.ModuleID;
                    }
                }
                else
                {

                    System.Diagnostics.Debug.WriteLine($"{{ \"{mod.fdname.ToLowerInvariant()}\", new ShipModule({mod.id}, 0, 0, \"\", \"{mod.descr.SplitCapsWordFull()}\", \"\" ) }},");
                }

            }

            System.Diagnostics.Debug.WriteLine($"//Modules");
            foreach (var m in ItemData.modules)
            {
                System.Diagnostics.Debug.WriteLine($"       {{ \"{m.Key}\", new ShipModule({m.Value}) }},");
            }
            System.Diagnostics.Debug.WriteLine($"//Other");
            foreach (var m in ItemData.othermodules)
            {
                System.Diagnostics.Debug.WriteLine($"       {{ \"{m.Key}\", new ShipModule({m.Value}) }},");
            }
        }

    }
}

