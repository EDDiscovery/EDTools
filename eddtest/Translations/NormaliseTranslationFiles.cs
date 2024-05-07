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

            //List<string> secondarykeys = secondary.EnumerateKeys.ToList();    foreach( var k in secondarykeys) {  System.Diagnostics.Debug.WriteLine($"Secondary {k} = {secondary.GetTranslation(k)}"); }

            List<string> primarykeys = primary.EnumerateKeys.ToList();

            string[] secondaryfilelist = secondary?.FileList();

            foreach (string id in primarykeys)
            {
                //System.Diagnostics.Debug.WriteLine($"Primary  {primary.GetOriginalFile(id)}:{primary.GetOriginalLine(id)}: {id} `{primary.GetOriginalEnglish(id)}` `{primary.GetTranslation(id)}`");

                if (id.Contains("DataGridViewStarResults"))
                {

                }

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

                    // if we have a secondary of the same name
                    // here we move the secondary translation to the primary array, ready for output later.

                    if (secondary.IsDefined(secid))
                    {
                        // if secondary translation is not null, i.e set

                        if (secondary.GetTranslation(secid) != null)     
                        {
                            string sectranslation = secondary.GetTranslation(secid);

                            // check formatting

                            string res = VerifyFormattingClass.VerifyFormatting(secondary.GetOriginalFile(secid), secondary.GetOriginalLine(secid),
                                                            primary.GetOriginalEnglish(secid), sectranslation, secid);
                            if (res != null)
                            {
                                System.Diagnostics.Debug.WriteLine(res);
                                reporttext += res + Environment.NewLine;
                            }

                            // see if duplication
                            // if we have another key with the same text, and its still in primary (! bug here), redefine to it

                            string otherkey = secondary.FindTranslation(sectranslation,secondaryfilelist[0],secondary.GetOriginalFile(secid),true);
                            if (otherkey != secid && primary.IsDefined(otherkey))       
                            {
                                System.Diagnostics.Debug.WriteLine($"Secondary key {secid} value already defined in {otherkey}");
                               // System.Diagnostics.Debug.WriteLine($".. {secondary.GetTranslation(secid)} vs {secondary.GetTranslation(otherkey)}");

                                //if (!secondary.GetOriginalEnglish(secid).EqualsIIC(secondary.GetOriginalEnglish(otherkey)))       // removed check for english diff
                                //{
                                    //System.Diagnostics.Debug.WriteLine($".. English! {secid} `{secondary.GetOriginalEnglish(secid)}` vs {otherkey} `{secondary.GetOriginalEnglish(otherkey)}`");
                                //}

                                // SET primary translation to reference of previous
                                primary.ReDefine(secid, $"==={otherkey}");
                            }
                            else
                            {
                                // SET primary translation to secondary

                                primary.ReDefine(secid, sectranslation);
                                reporttext += $"Secondary {secid} translation '{sectranslation.EscapeControlChars()}'" + Environment.NewLine;
                            }

                        }
                        else
                        {
                            // its a null
                            reporttext += $"Secondary {secid} not defined " + Environment.NewLine;
                        }
                    }
                    else
                    {
                        // SET primary translation to null , meaning no translation, so it will come out as @
                        primary.ReDefine(secid, null);       
                        reporttext += $"Secondary {secid} not found" + Environment.NewLine;
                        //System.Diagnostics.Debug.WriteLine($".. not found in secondary");
                    }
                }
            }


            // dump any enums not found
            if (enumerations != null)
            {
                foreach (var kvp in enumerations)
                {
                    if (kvp.Value.Item2 == 0)
                        reporttext += $"Enum symbol {kvp.Key} is not present in translation file" + Environment.NewLine;
                }
            }

            // now dump to files

            if (secondary.Translating)
            {
                string currentfilename = null, currentsectionname = "";
                bool hasdotted = false; // this records if we are in a Section

                List<string> filename = new List<string>();
                List<StringBuilder> fileoutputs = new List<StringBuilder>();

                JSONFormatter englishfile = new QuickJSON.JSONFormatter();        // file holding keys->english
                englishfile.Object().LF().Object("Main").LF();
                JSONFormatter foreignfile = new QuickJSON.JSONFormatter();        // file holding keys->foreign translation, or "" if not
                foreignfile.Object().LF().Object("Main").LF();

                // we are writing out primary keys, secondary translations have moved to them

                foreach (string id in primarykeys)
                {
                    string primaryfilename = primary.GetOriginalFile(id);

                    // transmute filename to foreign name
                    if (currentfilename == null || primaryfilename != currentfilename)
                    {
                        currentfilename = primaryfilename;
                        fileoutputs.Add(new StringBuilder());
                        string nerfname = currentfilename.Replace(language, language2);
                        nerfname = nerfname.Replace(language.Substring(0, language.IndexOf("-")), language2.Substring(0, language2.IndexOf("-")));
                        filename.Add(nerfname);
                        System.Diagnostics.Debug.WriteLine($"Output file {nerfname}");
                    }

                    // copy as we may alter
                    string idtouse = id;

                    StringParser sp = new StringParser(id);
                    string idsectionname = sp.NextWord(".:");       // grab front section name
                    if (idsectionname != currentsectionname)        // if section changed, we need to emit 
                    {
                        if (hasdotted || sp.IsChar('.'))            // need to distinguish start without dots from a dotted section
                        {
                            currentsectionname = idsectionname;

                            //System.Diagnostics.Debug.WriteLine($"Switch to {front}");
                            fileoutputs.Last().Append(Environment.NewLine + "SECTION " + idsectionname + Environment.NewLine + Environment.NewLine);

                            englishfile.Close().LF();
                            englishfile.Object(idsectionname.SplitCapsWordFull()).LF(); // open new section
                            foreignfile.Close().LF();
                            foreignfile.Object(idsectionname.SplitCapsWordFull()).LF(); 
                        }

                        if (sp.IsChar('.'))
                            hasdotted = true;
                    }

                    if (hasdotted && sp.IsChar('.'))        // cut to dot
                        idtouse = sp.LineLeft;

                    fileoutputs.Last().Append(idtouse);     // output id, colon, primary english text
                    fileoutputs.Last().Append(": ");
                    fileoutputs.Last().Append(primary.GetOriginalEnglish(id).EscapeControlChars().AlwaysQuoteString());

                    string secondarytranslation = primary.GetTranslation(id);    // pick up secondary translation, moved to primary slot by above code

                    if (secondarytranslation == null)       // null, its an @
                    {
                        fileoutputs.Last().Append(" @");
                        englishfile.V(currentsectionname + idtouse, primary.GetOriginalEnglish(id).EscapeControlCharsFull());
                        foreignfile.V(currentsectionname + idtouse, "");
                    }
                    else if (secondarytranslation.StartsWith("==="))    // reference
                    {
                        string keyid = secondarytranslation.Substring(3);

                        if (primary.IsDefined(keyid))           // double check its there..
                        {
                            fileoutputs.Last().Append(" = ");
                            fileoutputs.Last().Append(keyid.EscapeControlCharsFull());
                            englishfile.V(currentsectionname + idtouse, primary.GetOriginalEnglish(id).EscapeControlCharsFull());
                            secondarytranslation = primary.GetTranslation(keyid);
                            foreignfile.V(currentsectionname + idtouse, secondarytranslation.EscapeControlCharsFull());
                        }
                        else
                        {
                            fileoutputs.Last().Append(" @");
                            reporttext += $"Reference error {id} is referencing {keyid} this is not present in primary" + Environment.NewLine;
                            System.Diagnostics.Debug.WriteLine($"Reference error {id} is referencing {keyid} this is not present in primary");
                        }
                    }
                    else
                    {
                        // else its a full translation

                        fileoutputs.Last().Append(" => ");
                        fileoutputs.Last().Append(secondarytranslation.EscapeControlChars().AlwaysQuoteString());
                        englishfile.V(currentsectionname + idtouse, primary.GetOriginalEnglish(id).EscapeControlCharsFull());
                        foreignfile.V(currentsectionname + idtouse, secondarytranslation.EscapeControlCharsFull());
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
                File.WriteAllText($"crowdin-english.json", englishoutput);      // this is the keyfile -> main language crowdin file
                File.WriteAllText($"crowdin-{language2}.json", foreignoutput);      // this is the keyfile -> current translation file
            }

            File.WriteAllText("report.txt", reporttext);

            return reporttext;
        }


    }

}

