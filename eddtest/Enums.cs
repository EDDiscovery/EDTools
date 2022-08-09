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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EDDTest
{
    public static class Enums
    {
        // EDD: scanforenums stdenums   . *.cs  located at c:\code\eddiscovery
        static public void ScanForEnums(string enums, FileInfo[] files)
        {
            var elist = ReadEnums(enums,true);
            Console.WriteLine($"Enums read {elist.Count}");

            foreach ( var f in files)
            {
                string[] lines = File.ReadAllLines(f.FullName);
                Console.WriteLine($"{f.FullName} {lines.Length}");
                foreach ( var l in lines)
                {
                    List<string> update = new List<string>();
                    foreach( var kvp in elist)
                    {
                        int pos = 0;
                        int indexof = 0;

                        while( (indexof = l.IndexOf(kvp.Key,pos)) >= 0) // may be shorter aliases to it before we reach the identifier
                        {
                            int endindex = indexof + kvp.Key.Length;
                            if (endindex == l.Length || l[endindex].IsLetterOrDigitOrUnderscore() == false)
                            {
                                update.Add(kvp.Key);
                                break;
                            }

                            pos = endindex;
                        }
                    }

                    foreach( var x in update)
                    {
                        elist[x] = new Tuple<string, int>(elist[x].Item1, elist[x].Item2 + 1);

                    }
                }
            }

            string retlist = $"Report at " + DateTime.Now.ToStringZulu() + Environment.NewLine;

            foreach (var kvp in elist)
            {
                if ( kvp.Value.Item2 == 0 )
                {
                    retlist += $"Enum symbol {kvp.Key} {kvp.Value.Item1}  : Referenced {kvp.Value.Item2}" + Environment.NewLine;
                }
            }

            File.WriteAllText("report.txt", retlist);
        }

        static public Dictionary<string, Tuple<string,int>> ReadEnums(string enums, bool returnid = false)
        {
            if (enums != null)
            {
                if (enums == "stdenums")
                {
                    enums = @"c:\code\eddiscovery\eddiscovery\translations\eddiscoverytranslations.cs;" +
                        @"c:\code\eddiscovery\elitedangerouscore\elitedangerous\elitedangerous\translations.cs;" +
                        @"c:\code\eddiscovery\extendedcontrols\extendedcontrols\forms\translationids.cs;" +
                        @"C:\Code\EDDiscovery\ActionLanguage\ActionLanguage\ActionEditing\TranslationIDs.cs;" +
                        @"C:\Code\EDDiscovery\ExtendedControls\ExtendedForms\TranslationIDs.cs";
                }

                var enumerations = new Dictionary<string, Tuple<string,int>>();
                string[] files = enums.Split(";");
                string classname = "";

                foreach (var f in files)
                {
                    string[] lines = File.ReadAllLines(f).Where(x => x.Length > 0).ToArray();
                    bool inenum = false;
                    foreach (var l in lines)
                    {
                        string tl = l.Trim();
                        if (inenum == false && (tl.Contains("public enum") || tl.Contains("internal enum")))
                        {
                            inenum = true;
                            classname = tl.Substring(tl.IndexOf("enum") + 4).Trim();
                        }
                        else if (inenum == true && tl.Equals("}"))
                            inenum = false;
                        else if (inenum == true && tl.HasChars() && !tl.StartsWith("/") && !tl.StartsWith("{"))
                        {
                            StringParser s = new StringParser(tl);
                            string e = s.NextWord(",\\");
                            if (returnid)
                                e = classname + "." + e;
                            enumerations[e] = new Tuple<string,int>(s.LineLeft,0);
                        }
                    }
                }

                return enumerations;
            }

            return null;
        }
    }

}

