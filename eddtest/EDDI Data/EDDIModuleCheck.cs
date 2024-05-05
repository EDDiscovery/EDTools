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
using QuickJSON;
using System.Linq;

namespace EDDTest
{
    static public partial class EDDIData
    {
        static public void CheckModulesvsEDDI()
        {
            foreach (var mod in EDDI.GetModules())
            {
                if (ItemData.TryGetShipModule(mod.fdname, out ItemData.ShipModule edm, false))
                {
                    if (edm.ModuleID != mod.id)
                    {
                        string i = edm.Info == null ? "null" : $"\"{edm.Info}\"";

                        System.Diagnostics.Debug.WriteLine($"!!{{ \"{mod.fdname.ToLowerInvariant()}\", new ShipModule({mod.id}, {edm.Mass}, {edm.Power}, {i}, \"{edm.EnglishModName}\", \"{edm.ModType}\" ) }},");
                        edm.ModuleID = (int)mod.id;
                    }
                }
                else
                {

                    System.Diagnostics.Debug.WriteLine($"{{ \"{mod.fdname.ToLowerInvariant()}\", new ShipModule({mod.id}, 0, 0, \"\", \"{mod.descr.SplitCapsWordFull()}\", \"\" ) }},");
                }

            }


            JArray modread = JArray.Parse(BaseUtils.FileHelpers.TryReadAllTextFromFile(@"c:\code\mods.json"),out string err, JToken.ParseOptions.None);
            foreach( JObject jo in modread)
            {
                System.Diagnostics.Debug.WriteLine($"{{ \"{jo["Name"].Str().ToLowerInvariant()}\", new ShipModule({jo["id"].Long()}, 1, 1, \"\", \"{jo["Name"].Str().SplitCapsWordFull()}\", ShipModule.ModuleTypes.FrameShiftDrive ) }},");

            }
        }

    }
}

