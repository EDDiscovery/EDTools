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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EDDTest
{
    public static class Enums
    {
        // EDD: scanforenums enumfile;enumfile;..  c:\code\eddiscovery *.cs  
        // EDD: scanforenums eddiscoveryrootfolder c:\code\eddiscovery *.cs     - uses std enum translator files
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
                        elist[x] = new Tuple<string,string, int>(elist[x].Item1, elist[x].Item2, elist[x].Item3 + 1);

                    }
                }
            }

            string retlist = $"Report at " + DateTime.Now.ToStringZulu() + Environment.NewLine;

            foreach (var kvp in elist)
            {
                if ( kvp.Value.Item3 == 0 )
                {
                    retlist += $"Enum symbol {kvp.Key} {kvp.Value.Item1}:{kvp.Value.Item2}  : Referenced {kvp.Value.Item3}" + Environment.NewLine;
                }
            }

            File.WriteAllText("report.txt", retlist);
        }

        static public Dictionary<string, Tuple<string,string,int>> ReadEnums(string enums, bool returnid = false)
        {
            if (enums != null)
            {
                if (Directory.Exists(enums))
                {
                    enums = 
                        enums + @"\eddiscovery\translations\eddiscoverytranslations.cs;" +
                        enums + @"\elitedangerouscore\elitedangerous\elitedangerous\translations.cs;" +
                        enums + @"\extendedcontrols\extendedcontrols\forms\translationids.cs;" +
                        enums + @"\ActionLanguage\ActionLanguage\ActionEditing\TranslationIDs.cs;" +
                        enums + @"\ExtendedControls\ExtendedForms\TranslationIDs.cs;" +
                        enums + @"\ActionLanguage\ActionLanguage\AddOnManager\EDDiscoveryTranslations.cs";
                }


                var enumerations = new Dictionary<string, Tuple<string, string, int>>();
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
                            enumerations[e] = new Tuple<string,string,int>(f, s.LineLeft,0);
                        }
                    }
                }

                return enumerations;
            }

            return null;
        }
    }

}

