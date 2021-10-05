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

using BaseUtils.JSON;
using System;
using System.Collections.Generic;
using System.IO;

namespace EDDTest
{
    static public class CoriolisEng
    {
        static private string ProcessModules(string json)
        {
            JObject jo = new JObject();
            jo = JObject.Parse(json);

            Dictionary<Tuple<string, int>, string> engresults = new Dictionary<Tuple<string, int>, string>();

            foreach( var top in jo )
            {
                JObject inner = top.Value.Object();
                var blueprints = inner["blueprints"] as JObject;

                foreach ( var b in blueprints)      // kvp
                {
                    string bname = b.Key;
                    JObject blueprint = b.Value as JObject;
                //    System.Diagnostics.Debug.WriteLine("Blueprint " + blueprint.Path);

                    var grades = blueprint["grades"] as JObject;

                    foreach (var g in grades)
                    {
                        //System.Diagnostics.Debug.WriteLine("Key " + g.Key + " value " + g.Value);
                        var engineers = g.Value["engineers"];
                        foreach (var e in engineers)
                        {
                            var eng = e.Str();
                            System.Diagnostics.Debug.WriteLine("B " + bname + " g:" + g.Key + " e:" + eng);
                            var keyvp = new Tuple<string, int>(inner.Name + "-" + bname, g.Key.InvariantParseInt(0));
                            if ( engresults.ContainsKey(keyvp))
                            {
                                engresults[keyvp] += "," + eng;
                            }
                            else
                            {
                                engresults[keyvp] = eng;
                            }
                        }
                    }
                }

            }

            string res = "";
            foreach( var vkp in engresults)
            {
                int dash = vkp.Key.Item1.IndexOf('-');
                res += "new EngineeringRecipe( \"" + vkp.Key.Item1.Substring(dash+1) + "\", \"?\", \"" + vkp.Key.Item1.Substring(0,dash) + 
                                "\", \"" + vkp.Key.Item2 + "\", \"" + vkp.Value + "\" )," + Environment.NewLine;
            }
            return res;
        }

        static public string ProcessEng(FileInfo[] allFiles)            // overall index of items
        {
            foreach (var f in allFiles)
            {
                if (f.FullName.Contains("modules",StringComparison.InvariantCultureIgnoreCase))
                {
                    return ProcessModules(File.ReadAllText(f.FullName));
                }
            }

            return "Not found";
        }

    }
}
