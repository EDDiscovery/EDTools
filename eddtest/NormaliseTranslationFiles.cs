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
    public static class NormaliseTranslationFiles
    {
        // usage:
        // report on example state: normalisetranslate c:\code\eddiscovery\eddiscovery\translations 2 example-ex - "NS NoOutput" c:\code\renames.lst stdenums
        // normalise normalisetranslate c:\code\eddiscovery\eddiscovery\translations 2 example-ex francais-fr "NS"

        static public string Process(string language, string txpath, int searchdepth,
                                    string language2, string options, 
                                    string renamefile,
                                    string enums
            )          
        {
            string reporttext = $"Report at " + DateTime.Now.ToStringZulu() + Environment.NewLine;

            BaseUtils.Translator primary = BaseUtils.Translator.Instance;
            primary.LoadTranslation(language, System.Globalization.CultureInfo.CurrentCulture, new string[] { txpath }, searchdepth, Path.GetTempPath(), loadorgenglish: true, loadfile: true);

            if (options == null)
                options = "";

            if (!primary.Translating)
            {
                Console.WriteLine("Primary translation did not load " + language);
                return "";
            }

            BaseUtils.Translator secondary = new BaseUtils.Translator();
            if (language2 != null && language2 != "-")
            {
                secondary.LoadTranslation(language2, System.Globalization.CultureInfo.CurrentCulture, new string[] { txpath }, searchdepth, @"c:\code", loadorgenglish: true, loadfile: true);

                if (!secondary.Translating)
                {
                    Console.WriteLine("Secondary translation did not load " + language2);
                    return "";
                }
            }

            string[] renamesfrom = null;
            string[] renamesto = null;
            if (renamefile != null )
            {
                if (File.Exists(renamefile))
                {
                    string[] lines = File.ReadAllLines(renamefile).Where(x => x.Length > 0).ToArray();
                    renamesfrom = lines.Select(x => x.Substring(0, x.IndexOf("|")).Trim()).ToArray();
                    renamesto = lines.Select(x => x.Substring(x.IndexOf("|") + 1).Trim()).ToArray();
                }
                else
                {
                    reporttext += $"Rename file {renamefile} missing" + Environment.NewLine;

                }
            }

            var enumerations = Enums.ReadEnums(enums);


            string section = "";

            string filetowrite = "";

            List<StreamWriter> filelist = options.Contains("Nooutput",StringComparison.InvariantCultureIgnoreCase) ? null: new List<StreamWriter>();
            StreamWriter batchfile = null;

            bool hasdotted = false;

            foreach (string id in primary.EnumerateKeys)
            {
                string translationline = "";

                string orgfile = primary.GetOriginalFile(id);
                FileInfo fi = new FileInfo(orgfile);
                if (filetowrite == null || !filetowrite.Equals(fi.Name))
                {
                    //ret += Environment.NewLine + "WRITETOFILE " + filetowrite + Environment.NewLine;

                    filetowrite = fi.Name;

                    if (filelist != null)
                    {
                        if (secondary.Translating)      // if going to secondary
                        {
                            string txname = filetowrite.Replace(language, language2);

                            if (txname.Equals(filetowrite))
                            {
                                txname = filetowrite.Replace(language.Left(language.IndexOf('-')), language2.Left(language2.IndexOf('-')));
                            }

                            filelist.Add(new StreamWriter(Path.Combine(".", txname), false, Encoding.UTF8));

                            if (filelist.Count > 1)
                                filelist[0].WriteLine(Environment.NewLine + "include " + txname);

                            string txorgfile = orgfile.Replace(language, language2);
                            if (txorgfile.Equals(orgfile))
                            {
                                txorgfile = orgfile.Replace(language.Left(language.IndexOf('-')), language2.Left(language2.IndexOf('-')));
                            }

                            if (batchfile == null)
                                batchfile = new StreamWriter("copyback.bat");

                            batchfile.WriteLine("move " + txname + " " + txorgfile);
                        }
                        else
                        {
                            filelist.Add(new StreamWriter(Path.Combine(".", filetowrite), false, Encoding.UTF8));

                            if (filelist.Count > 1)
                                filelist[0].WriteLine(Environment.NewLine + "include " + filetowrite);
                        }
                    }
                }

                string idtouse = id;

                StringParser sp = new StringParser(id);
                string front = sp.NextWord(".:");
                if (front != section)
                {
                    if (hasdotted || sp.IsChar('.'))
                        translationline += Environment.NewLine + "SECTION " + front + Environment.NewLine + Environment.NewLine;
                    if (sp.IsChar('.'))
                        hasdotted = true;
                    section = front;
                }

                if (hasdotted && sp.IsChar('.'))
                    idtouse = sp.LineLeft; 

                string primaryorgeng = primary.GetOriginalEnglish(id);
                string translation = primary.GetTranslation(id);            // default is from the first file
                                                                            // System.Diagnostics.Debug.WriteLine($"Org {id} = `{primaryorgeng}` `{translation}`");
                bool secondarydef = false;

                if (enumerations != null)
                {
                    string idenum = id.Replace(".", "_");
                    if (enumerations.ContainsKey(idenum))
                    {
                        System.Diagnostics.Debug.WriteLine($"Found {id} in enumerations");
                        enumerations[idenum] = new Tuple<string, int>(enumerations[idenum].Item1, enumerations[idenum].Item2 + 1);
                    }
                    else
                    {
                        // these are not as enums, knock them out
                        if (id.Contains("PopOutInfo.") || id.Contains("JournalTypeEnum.") || id.Contains("MaterialCommodityMicroResourceType."))
                        {

                        }
                        else
                            reporttext += $"Enumerations files do not contain {id}" + Environment.NewLine;
                    }

                }

                if (secondary.Translating)
                {
                    string secid = id;
                    if (!secondary.IsDefined(id) && renamesto != null)
                    {
                        int i = Array.FindIndex(renamesto, x => x == id);
                        if (i != -1)
                        {
                            reporttext += $"Rename {renamesfrom[i]} to {renamesto[i]}" + Environment.NewLine;
                            System.Diagnostics.Debug.WriteLine($"Rename {renamesfrom[i]} to {renamesto[i]}");
                            secid = renamesfrom[i];
                        }
                    }

                    if (secondary.IsDefined(secid))
                    {
                        secondarydef = true;                                // we have a secondary def of the id
                        translation = secondary.GetTranslation(secid);
                        if (translation != null)
                            VerifyFormatting(secondary.GetOriginalFile(secid), secondary.GetOriginalLine(secid), primaryorgeng, translation, secid);
                        secondary.UnDefine(secid);                             // remove id so its not reported as missing
                        System.Diagnostics.Debug.WriteLine($"Found {secid} removed");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Not Found {secid}");
                        translation = null;                                 // meaning not present
                    }
                }

                translationline += idtouse + ": " + primaryorgeng.EscapeControlChars().AlwaysQuoteString();        // output is id : orgeng

                if (translation == null ||                                  // no translation
                    (translation.Equals(primaryorgeng) && !secondarydef) ||          // or same text and its not secondary def
                    translation.IsEmpty() ||                                // or empty
                    (translation[0] == '<' && translation[translation.Length - 1] == '>'))  // or has those <> around it from the very first go at this parlava
                {
                    bool containsnonspacedigits = false;
                    foreach (var c in primaryorgeng)
                    {
                        if (!(char.IsPunctuation(c) || char.IsWhiteSpace(c) || char.IsNumber(c)))
                        {
                            containsnonspacedigits = true;
                            break;
                        }
                    }

                    if (secondary.Translating && containsnonspacedigits)
                    {
                        if (secondarydef)
                        {
                            if (!options.Contains("NS"))
                                reporttext += "Defined by secondary as @: " + id + " in " + primary.GetOriginalFile(id) + ":" + primary.GetOriginalLine(id) + Environment.NewLine;
                        }
                        else
                        {
                            reporttext += "Not defined by secondary: " + id + " in " + primary.GetOriginalFile(id) + ":" + primary.GetOriginalLine(id) + Environment.NewLine;
                        }
                    }

                    translationline += " @";
                }
                else
                {
                    translationline += " => " + translation.EscapeControlChars().AlwaysQuoteString();       // place translated string into file again
                                                                                                            //  reporttext += "Translated by secondary: " + id + " = '" + translation + "' in " + primary.GetOriginalFile(id) + ":" + primary.GetOriginalLine(id) + Environment.NewLine;
                }

                translationline += Environment.NewLine;


                //  totalret += ret;
                if ( filelist != null )
                    filelist.Last().Write(translationline);
            }

            if (secondary.Translating)
            {
                foreach (string id in secondary.EnumerateKeys)
                {
                    reporttext += "In secondary but not in primary: " + id + " in " + secondary.GetOriginalFile(id) + ":" + secondary.GetOriginalLine(id) + Environment.NewLine;
                }
            }

            if (filelist != null)
            {
                foreach (var f in filelist)
                    f.Close();
            }

            if (batchfile != null)
                batchfile.Close();

            if (enumerations != null)
            {
                foreach (var kvp in enumerations)
                {
                    if (kvp.Value.Item2 == 0)
                        reporttext += $"Enum symbol {kvp.Key} unused" + Environment.NewLine;
                }
            }


            File.WriteAllText("report.txt", reporttext);

            return reporttext;
        }

        static public void VerifyFormatting(string file, int line, string eng, string trans, string id)
        {
            int pos = 0;
            string bad = null;

            while ((pos = eng.IndexOf("{", pos)) != -1 && bad == null)      // go thru brackets of eng
            {
                int endpos = eng.IndexOf("}", pos);
                if (endpos != -1)
                {
                    string s = eng.Substring(pos, endpos - pos + 1);
                    int post = trans.IndexOf(s);

                    if (post == -1)       // if not found in string {n}
                        bad = "Bracket mismatch";
                }

                pos++;
            }

            pos = 0;
            while ((pos = trans.IndexOf("{", pos)) != -1 && bad == null)      // go thru brackets of trans
            {
                int endpos = trans.IndexOf("}", pos);
                if (endpos != -1)
                {
                    string s = trans.Substring(pos, endpos - pos + 1);
                    int post = eng.IndexOf(s);

                    if (post == -1)       // if not found in string {n}
                        bad = "Bracket mismatch (more in trans)";
                }

                pos++;
            }

            if (bad == null)
            {
                int engsemicolons = 0;
                for (int i = 0; i < eng.Length; i++)
                    engsemicolons += eng[i] == ';' ? 1 : 0;

                int foreignsemicolons = 0;
                for (int i = 0; i < trans.Length; i++)
                    foreignsemicolons += trans[i] == ';' ? 1 : 0;

                if (engsemicolons == 2)     // build format prefix;postfix;format
                {
                    if (foreignsemicolons == engsemicolons)
                    {
                        string e = eng.Substring(eng.LastIndexOf(';'));
                        string t = trans.Substring(trans.LastIndexOf(';'));
                        bad = e.Equals(t) ? null : "Field builder format difference";
                    }

                }
                else if (engsemicolons != foreignsemicolons)    // warn if different number
                {
                    bad = "Field builder semicolon count";
                }

            }

            if (bad != null)
            {
                Console.WriteLine($"{bad}: {id}: '{eng}' -> '{trans}' in {file}:{line}");
            }
        }
    }

}

