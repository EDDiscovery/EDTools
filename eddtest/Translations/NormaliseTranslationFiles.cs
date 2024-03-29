﻿/*
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

        // you can scan for enums scanforenums  stdenums . *.cs to check if enums are in use

        static public string ProcessNew(string language, string txpath, int searchdepth,
                                    string language2,
                                    string renamefile,
                                    string enums
            )
        {
            string reporttext = $"Report at " + DateTime.Now.ToStringZulu() + Environment.NewLine;

            BaseUtils.Translator primary = BaseUtils.Translator.Instance;
            primary.LoadTranslation(language, System.Globalization.CultureInfo.CurrentCulture, new string[] { txpath }, searchdepth, Path.GetTempPath(), loadorgenglish: true, loadfile: true);


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
            if (renamefile != null)
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

            List<string> keys = primary.EnumerateKeys.ToList();

            string[] secondaryfilelist = secondary?.FileList();

            foreach (string id in keys)
            {
                //System.Diagnostics.Debug.WriteLine($"Primary  {primary.GetOriginalFile(id)}:{primary.GetOriginalLine(id)}: {id} `{primary.GetOriginalEnglish(id)}` `{primary.GetTranslation(id)}`");

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

                if (primary.GetTranslation(id) != null && primary.GetTranslation(id) != primary.GetOriginalEnglish(id))        // not @, and not the same
                {
                    System.Diagnostics.Debug.WriteLine($"*** Primary translation not same as english {primary.GetOriginalFile(id)} : {primary.GetOriginalLine(id)} : {id} : {primary.GetOriginalEnglish(id)} : {primary.GetTranslation(id)}");
                    reporttext += $"*** Primary translation not same as english {primary.GetOriginalFile(id)} : {primary.GetOriginalLine(id)} : {id} : {primary.GetOriginalEnglish(id)} : {primary.GetTranslation(id)}" + Environment.NewLine;
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
                        if (secondary.GetTranslation(secid) != null)     // if secondary has a key and its not null(meaning not known)
                        {
                            string sectranslation = secondary.GetTranslation(secid);
                            string res = VerifyFormattingClass.VerifyFormatting(secondary.GetOriginalFile(secid), secondary.GetOriginalLine(secid),
                                                            primary.GetOriginalEnglish(secid), sectranslation, secid);
                            if (res != null)
                            {
                                System.Diagnostics.Debug.WriteLine(res);
                                reporttext += res + Environment.NewLine;
                            }

                            string otherkey = secondary.FindTranslation(sectranslation,secondaryfilelist[0],secondary.GetOriginalFile(secid),true);
                            if (otherkey != secid)
                            {
                                //System.Diagnostics.Debug.WriteLine($"Secondary key {secid} value already defined in {otherkey}");
                               // System.Diagnostics.Debug.WriteLine($".. {secondary.GetTranslation(secid)} vs {secondary.GetTranslation(otherkey)}");
                                if (!secondary.GetOriginalEnglish(secid).EqualsIIC(secondary.GetOriginalEnglish(otherkey)))
                                {
                                    //System.Diagnostics.Debug.WriteLine($".. English! {secid} `{secondary.GetOriginalEnglish(secid)}` vs {otherkey} `{secondary.GetOriginalEnglish(otherkey)}`");
                                }
                     
                                primary.ReDefine(secid, $"==={otherkey}");
                            }
                            else
                            {
                                primary.ReDefine(secid, sectranslation);
                                reporttext += $"Secondary {secid} translation '{sectranslation.EscapeControlChars()}'" + Environment.NewLine;
                            }
                            //System.Diagnostics.Debug.WriteLine($".. altered to {sectranslation}");
                        }
                        else
                        {
                            // its a null
                            reporttext += $"Secondary {secid} not defined " + Environment.NewLine;
                        }
                    }
                    else
                    {
                        primary.ReDefine(secid, null);       // set to null, meaning no translation, so it will come out as @
                        reporttext += $"Secondary {secid} not found" + Environment.NewLine;
                        //System.Diagnostics.Debug.WriteLine($".. not found in secondary");
                    }
                }
            }

            if (enumerations != null)
            {
                foreach (var kvp in enumerations)
                {
                    if (kvp.Value.Item2 == 0)
                        reporttext += $"Enum symbol {kvp.Key} is not present in translation file" + Environment.NewLine;
                }
            }

            if (secondary.Translating)
            {
                string currentfilename = null, currentsectionname = "";
                bool hasdotted = false;

                List<string> filename = new List<string>();
                List<StringBuilder> fileoutputs = new List<StringBuilder>();

                QuickJSON.JSONFormatter englishfile = new QuickJSON.JSONFormatter();        // file holding keys->english
                englishfile.Object().LF().Object("Main").LF();
                QuickJSON.JSONFormatter foreignfile = new QuickJSON.JSONFormatter();        // file holding keys->foreign translation, or "" if not
                foreignfile.Object().LF().Object("Main").LF();

                foreach (string id in keys)
                {
                    string primaryfilename = primary.GetOriginalFile(id);
                    if (currentfilename == null || primaryfilename != currentfilename)
                    {
                        currentfilename = primaryfilename;
                        fileoutputs.Add(new StringBuilder());
                        string nerfname = currentfilename.Replace(language, language2);
                        nerfname = nerfname.Replace(language.Substring(0, language.IndexOf("-")), language2.Substring(0, language2.IndexOf("-")));
                        filename.Add(nerfname);
                        System.Diagnostics.Debug.WriteLine($"Output file {nerfname}");
                    }

                    string idtouse = id;

                    StringParser sp = new StringParser(id);
                    string front = sp.NextWord(".:");
                    if (front != currentsectionname)
                    {
                        if (hasdotted || sp.IsChar('.'))
                        {
                            currentsectionname = front;

                            //System.Diagnostics.Debug.WriteLine($"Switch to {front}");
                            fileoutputs.Last().Append(Environment.NewLine + "SECTION " + front + Environment.NewLine + Environment.NewLine);

                            englishfile.Close().LF();
                            englishfile.Object(front.SplitCapsWordFull()).LF(); // open new section
                            foreignfile.Close().LF();
                            foreignfile.Object(front.SplitCapsWordFull()).LF(); 
                        }

                        if (sp.IsChar('.'))
                            hasdotted = true;
                    }

                    if (hasdotted && sp.IsChar('.'))
                        idtouse = sp.LineLeft;

                    fileoutputs.Last().Append(idtouse);
                    fileoutputs.Last().Append(": ");
                    fileoutputs.Last().Append(primary.GetOriginalEnglish(id).EscapeControlChars().AlwaysQuoteString());
                    string translation = primary.GetTranslation(id);

                    if (translation == null)
                    {
                        fileoutputs.Last().Append(" @");
                        englishfile.V(currentsectionname + idtouse, primary.GetOriginalEnglish(id).EscapeControlCharsFull());
                        foreignfile.V(currentsectionname + idtouse, "");
                    }
                    else if (translation.StartsWith("==="))
                    {
                        fileoutputs.Last().Append(" = ");
                        string keyid = translation.Substring(3);
                        fileoutputs.Last().Append(keyid.EscapeControlCharsFull());
                        englishfile.V(currentsectionname + idtouse, primary.GetOriginalEnglish(id).EscapeControlCharsFull());
                        translation = primary.GetTranslation(keyid);
                        foreignfile.V(currentsectionname + idtouse, translation.EscapeControlCharsFull());
                    }
                    else
                    {
                        fileoutputs.Last().Append(" => ");
                        fileoutputs.Last().Append(translation.EscapeControlChars().AlwaysQuoteString());
                        englishfile.V(currentsectionname + idtouse, primary.GetOriginalEnglish(id).EscapeControlCharsFull());
                        foreignfile.V(currentsectionname + idtouse, translation.EscapeControlCharsFull());
                    }

                    englishfile.LF();
                    foreignfile.LF();
                    fileoutputs.Last().Append(Environment.NewLine);
                }

                // put includes in first file
                for (int i = 1; i < fileoutputs.Count; i++)
                {
                    fileoutputs[0].Append($"\r\ninclude {Path.GetFileName(filename[i])}\r\n");
                }

                // now overwrite them all
                for (int i = 0; i < fileoutputs.Count; i++)
                {
                    string contents = fileoutputs[i].ToString();
                    File.WriteAllText(filename[i], contents, Encoding.UTF8);
                }

                englishfile.Close();
                englishfile.LF();
                foreignfile.Close();
                foreignfile.LF();
                string englishoutput = englishfile.Get();
                string foreignoutput = foreignfile.Get();
                System.Diagnostics.Debug.Assert(JObject.Parse(englishoutput, out string errore, JToken.ParseOptions.CheckEOL) != null);
                System.Diagnostics.Debug.Assert(JObject.Parse(foreignoutput, out string errorf, JToken.ParseOptions.CheckEOL) != null);
                File.WriteAllText($"edd.json", englishoutput);      // this is the keyfile -> main language crowdin file
                File.WriteAllText("{language2}.json", foreignoutput);      // this is the keyfile -> current translation file
            }

            File.WriteAllText("report.txt", reporttext);

            return reporttext;
        }


    }

}

