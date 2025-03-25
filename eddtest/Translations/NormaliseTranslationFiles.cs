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
        // report on example state: normalisetranslate c:\code\eddiscovery\eddiscovery\translations 2 example-ex
        // compare translations: normalisetranslate c:\code\eddiscovery\eddiscovery\translations 2 example-ex francais-fr
        // with a rename file: normalisetranslate c:\code\eddiscovery\eddiscovery\translations 2 example-ex francais-fr renamefile.txt
        // compare vs enumerations: normalisetranslate c:\code\eddiscovery\eddiscovery\translations 2 example-ex - - c:\code\eddiscovery

        // best to first check enums: scanforenums c:\code\eddiscovery  c:\code\eddiscovery *.cs
        // then check enums vs example: normalisetranslate c:\code\eddiscovery\eddiscovery\translations 2 example-ex - - c:\code\eddiscovery


        static public string ProcessNew(string language, string txpath, int searchdepth,
                                    string language2,
                                    string renamefile,
                                    string enums
            )
        {
            string reporttext = $"Report at " + DateTime.Now.ToStringZulu() + Environment.NewLine;

            BaseUtils.Translator primary = BaseUtils.Translator.Instance;
            primary.LoadTranslation(language, System.Globalization.CultureInfo.CurrentCulture, new string[] { txpath }, searchdepth, Path.GetTempPath(), true );


            if (!primary.Translating)
            {
                Console.WriteLine("Primary translation did not load " + language);
                return "";
            }

            BaseUtils.Translator secondary = new BaseUtils.Translator();
            if (language2 != null && language2 != "-")
            {
                secondary.LoadTranslation(language2, System.Globalization.CultureInfo.CurrentCulture, new string[] { txpath }, searchdepth, @"c:\code",  true);

                if (!secondary.Translating)
                {
                    Console.WriteLine("Secondary translation did not load " + language2);
                    return "";
                }
            }

            string[] renames = renamefile != null ? FileHelpers.TryReadAllLinesFromFile(renamefile) : null;

            List<string> primarykeys = primary.EnumerateKeys.ToList();
            //List<string> secondarykeys = secondary.EnumerateKeys.ToList();    foreach( var k in secondarykeys) {  System.Diagnostics.Debug.WriteLine($"Secondary {k} = {secondary.GetTranslation(k)}"); }

            var enumerations = Enums.ReadEnums(enums);

            if (enumerations != null)
            {
                foreach (string id in primarykeys)
                {
                    string idenum = id.Replace(".", "_");       // only safe direction, as some names have _ in them so can't go the other way

                    if (enumerations.ContainsKey(idenum))
                    {
                        if (enumerations[idenum].Item3 > 0)     // should always be zero, but if there is a typo on _ or . meaning it looks the same, it may repeat
                        {
                            reporttext += $"In primarykeys {id} is repeated" + Environment.NewLine;
                        }

                        enumerations[idenum] = new Tuple<string,string, int>(enumerations[idenum].Item1, enumerations[idenum].Item2, enumerations[idenum].Item3 + 1);
                    }
                    else
                    {
                        // these are not as enums, knock them out
                        if (id.Contains("PopOutInfo.") || id.Contains("JournalTypeEnum.") || id.Contains("MaterialCommodityMicroResourceType.") ||
                            id.Contains("ShipSlots.") || id.Contains("StationServices.") || id.Contains("ModuleTypeNames.") || id.Contains("ModulePartNames.") ||
                            id.Contains("PowerPlayStates.") || id.Contains("GovernmentTypes.") || id.Contains("Allegiances.") ||
                            id.Contains("FactionStates.") || id.Contains("Crimes.") || id.Contains("Services.") ||
                            id.Contains("StarportStates.") || id.Contains("StarportTypes.") || id.Contains("SecurityTypes.") || id.Contains("EconomyTypes."))
                        {

                        }
                        else
                            reporttext += $"Enumerations files do not contain {id} from primary key {idenum}" + Environment.NewLine;
                    }
                }

                foreach (var kvp in enumerations)       // see if any enumerations are not used
                {
                    if (kvp.Value.Item3 == 0)
                        reporttext += $"Enum symbol {kvp.Value.Item1}:{kvp.Key} is not present in translation file" + Environment.NewLine;
                }
            }

            // lets check if english text differs in primary

            foreach (string id in primarykeys)
            {
                //System.Diagnostics.Debug.WriteLine($"Primary  {primary.GetOriginalFile(id)}:{primary.GetOriginalLine(id)}: {id} `{primary.GetOriginalEnglish(id)}` `{primary.GetTranslation(id)}`");

                if (!id.StartsWith("COMMENTBLANK:") &&
                            primary.GetTranslation(id) != null && primary.GetTranslation(id) != primary.GetOriginalEnglish(id))        // not @, and not the same
                {
                    System.Diagnostics.Debug.WriteLine($"*** Primary translation not same as english {primary.GetOriginalFile(id)} : {primary.GetOriginalLine(id)} : {id} : {primary.GetOriginalEnglish(id)} : {primary.GetTranslation(id)}");
                    reporttext += $"*** Primary translation not same as english {primary.GetOriginalFile(id)} : {primary.GetOriginalLine(id)} : {id} : {primary.GetOriginalEnglish(id)} : {primary.GetTranslation(id)}" + Environment.NewLine;
                }
            }


            if (secondary.Translating)
            {
                // first we transfer the translation from secondary to primary

                foreach (string id in primarykeys)
                {
                    // if we have a secondary of the same name
                    // here we move the secondary translation to the primary array, ready for output later.
                    if (secondary.IsDefined(id))
                    {
                        // if secondary translation is not null, i.e set

                        if (secondary.GetTranslation(id) != null)
                        {
                            string sectranslation = secondary.GetTranslation(id);

                            // check formatting

                            string res = VerifyFormattingClass.VerifyFormatting(secondary.GetOriginalFile(id), secondary.GetOriginalLine(id),
                                                            primary.GetOriginalEnglish(id), sectranslation, id);
                            if (res != null)
                            {
                                System.Diagnostics.Debug.WriteLine(res);
                                Console.WriteLine(res);
                                Console.WriteLine("Press key to continue");
                                Console.ReadKey();
                                reporttext += res + Environment.NewLine;
                            }

                            primary.ReDefine(id, sectranslation);
                            reporttext += $"Secondary {id} translation '{sectranslation.EscapeControlChars()}'" + Environment.NewLine;
                        }
                        else
                        {
                            // its a null
                            reporttext += $"Secondary {id} !!! translation not defined " + Environment.NewLine;
                        }
                    }
                    else
                    {
                        // SET primary translation to null , meaning no translation, so it will come out as @
                        primary.ReDefine(id, null);
                        reporttext += $"Secondary {id} not found" + Environment.NewLine;
                        //System.Diagnostics.Debug.WriteLine($".. not found in secondary");
                    }
                }

                // next we see if we need to change the primary keys order/naming

                if ( renames != null)
                {
                    for(int i = 0; i < renames.Length; i++)
                    {
                        if (renames[i].Length > 0)
                        {
                            StringParser sp = new StringParser(renames[i]);
                            string from = sp.NextWord("|").Trim();
                            sp.MoveOn(1);
                            string to = sp.NextWord("|").Trim();
                            sp.MoveOn(1);
                            string file = sp.NextWord().Trim();

                            if (file == "*")
                                file = primary.GetOriginalFile(from);

                            if (primary.Rename(from, to, file))
                            {
                                int insertindex = 0;
                                int dot = to.IndexOf(".");
                                if (dot >= 0)
                                {
                                    string startpart = to.Substring(0, dot);
                                    int starti = primarykeys.StartsWith(startpart);
                                    if (starti > 0)
                                        insertindex = starti;
                                }

                                int pk = primarykeys.IndexOf(from);
                                primarykeys.RemoveAt(pk);
                                primarykeys.Insert(insertindex, to);
                                reporttext += $"Rename {from} to {to} in file {file}" + Environment.NewLine;

                            }
                        }
                    }
                }

                // now see if we can remove repeats

                string[] primaryfilelist = primary.FileList();

                foreach (string id in primarykeys)
                {
                    string translation = primary.GetTranslation(id);
                    if (translation != null)
                    {
                        string otherkey = primary.FindTranslation(translation, primaryfilelist[0], primary.GetOriginalFile(id), true);
                        if (otherkey != id)    // if we cound one
                        {
                            primary.ReDefine(id, $"==={otherkey}");
                            reporttext += $"Reference from {id} -> {otherkey}" + Environment.NewLine;
                        }
                    }
                }


                // we are writing out primary keys, secondary translations have moved to them

                string currentfilename = null, currentsectionname = "";
                bool hasdotted = false; // this records if we are in a Section

                List<string> filename = new List<string>();                         // filenames created
                List<StringBuilder> fileoutputs = new List<StringBuilder>();        // with stringbuilder 

                JSONFormatter englishfile = new QuickJSON.JSONFormatter();        // file holding keys->english
                englishfile.Object().LF().Object("Main").LF();
                JSONFormatter foreignfile = new QuickJSON.JSONFormatter();        // file holding keys->foreign translation, or "" if not
                foreignfile.Object().LF().Object("Main").LF();

                int outputfileindex = 0;

                foreach (string id in primarykeys)
                {
                    string primaryfilename = primary.GetOriginalFile(id);

                    // transmute filename to foreign name
                    if (currentfilename == null || !primaryfilename.EqualsIIC(currentfilename))
                    {
                        string nerfname = primaryfilename.Replace(language, language2);

                        int alreadyexists = filename.FindIndex(x=>x.EqualsIIC(nerfname));
                        if (alreadyexists >= 0)
                        {
                            outputfileindex = alreadyexists;
                            System.Diagnostics.Debug.WriteLine($"Continue with previous output file {nerfname} {outputfileindex}");
                        }
                        else
                        {
                            outputfileindex = fileoutputs.Count;
                            fileoutputs.Add(new StringBuilder());
                            nerfname = nerfname.Replace(language.Substring(0, language.IndexOf("-")), language2.Substring(0, language2.IndexOf("-")));
                            filename.Add(nerfname);
                            System.Diagnostics.Debug.WriteLine($"Changed to new output file {nerfname} {outputfileindex}");
                        }

                        currentfilename = primaryfilename;
                    }

                    // these are captured blank lines, includes, // comments

                    if (id.StartsWith("COMMENTBLANK:"))
                    {
                        string txt = primary.GetOriginalEnglish(id);
                        if (txt.StartsWith("include ", StringComparison.InvariantCultureIgnoreCase))
                        {
                            txt = txt.Replace(language.Substring(0,language.IndexOf("-")), language2.Substring(0, language2.IndexOf("-")));
                        }

                        fileoutputs[outputfileindex].Append(txt);
                        //System.Diagnostics.Debug.WriteLine($"{primary.GetOriginalFile(id)}:{primary.GetOriginalLine(id)} {txt}");
                    }
                    else
                    { 
                        // copy as we may alter
                        string idtouse = id;
                        //System.Diagnostics.Debug.WriteLine($"{primary.GetOriginalFile(id)}:{primary.GetOriginalLine(id)} : {id}");

                        StringParser sp = new StringParser(id);
                        string idsectionname = sp.NextWord(".:");       // grab front section name
                        if (idsectionname != currentsectionname)        // if section changed, we need to emit 
                        {
                            if (hasdotted || sp.IsChar('.'))            // need to distinguish start without dots from a dotted section
                            {
                                currentsectionname = idsectionname;

                                //System.Diagnostics.Debug.WriteLine($"Switch to {front}");
                                fileoutputs[outputfileindex].Append("SECTION " + idsectionname + Environment.NewLine + Environment.NewLine);

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

                        fileoutputs[outputfileindex].Append(idtouse);     // output id, colon, primary english text
                        fileoutputs[outputfileindex].Append(": ");
                        fileoutputs[outputfileindex].Append(primary.GetOriginalEnglish(id).EscapeControlChars().AlwaysQuoteString());

                        string secondarytranslation = primary.GetTranslation(id);    // pick up secondary translation, moved to primary slot by above code

                        if (secondarytranslation == null)       // null, its an @
                        {
                            fileoutputs[outputfileindex].Append(" @");
                            englishfile.V(currentsectionname + idtouse, primary.GetOriginalEnglish(id).EscapeControlCharsFull());
                            foreignfile.V(currentsectionname + idtouse, "");
                        }
                        else if (secondarytranslation.StartsWith("==="))    // reference
                        {
                            string keyid = secondarytranslation.Substring(3);

                            if (primary.IsDefined(keyid))           // double check its there..
                            {
                                fileoutputs[outputfileindex].Append(" = ");
                                fileoutputs[outputfileindex].Append(keyid.EscapeControlCharsFull());
                                englishfile.V(currentsectionname + idtouse, primary.GetOriginalEnglish(id).EscapeControlCharsFull());
                                secondarytranslation = primary.GetTranslation(keyid);
                                foreignfile.V(currentsectionname + idtouse, secondarytranslation.EscapeControlCharsFull());
                            }
                            else
                            {
                                fileoutputs[outputfileindex].Append(" @");
                                reporttext += $"Reference error {id} is referencing {keyid} this is not present in primary" + Environment.NewLine;
                                System.Diagnostics.Debug.WriteLine($"Reference error {id} is referencing {keyid} this is not present in primary");
                            }
                        }
                        else
                        {
                            // else its a full translation

                            fileoutputs[outputfileindex].Append(" => ");
                            fileoutputs[outputfileindex].Append(secondarytranslation.EscapeControlChars().AlwaysQuoteString());
                            englishfile.V(currentsectionname + idtouse, primary.GetOriginalEnglish(id).EscapeControlCharsFull());
                            foreignfile.V(currentsectionname + idtouse, secondarytranslation.EscapeControlCharsFull());
                        }

                        englishfile.LF();
                        foreignfile.LF();
                    }

                    fileoutputs[outputfileindex].Append(Environment.NewLine);
                }

                // now overwrite them all
                for (int i = 0; i < fileoutputs.Count; i++)
                {
                    string contents = fileoutputs[i].ToString();
                    File.WriteAllText(filename[i], contents, Encoding.UTF8);
                    reporttext += $"Writing contents to {filename[i]}" + Environment.NewLine;
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
            else
            {
                foreach (string id in primarykeys)
                {
                    reporttext += $"{id} in {primary.GetOriginalFile(id)} : {primary.GetOriginalLine(id)} : org '{primary.GetOriginalEnglish(id)}' : tx '{primary.GetTranslation(id)}'" + Environment.NewLine;
                }
            }

            File.WriteAllText("report.txt", reporttext);

            return reporttext;
        }


    }

}
