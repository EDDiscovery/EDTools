/*
 * Copyright 2015 - 2025 robbyxp @ github.com
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
    public static class NormaliseTranslationFilesMKII
    {
        // usage:
        // report on example state: normalisetranslate c:\code\eddiscovery\eddiscovery\translations 2 example-ex
        // compare translations: normalisetranslate c:\code\eddiscovery\eddiscovery\translations 2 example-ex francais-fr

        static public string ProcessNew(string language, string txpath, int searchdepth, string[] language2)
        {
            string reporttext = $"Report at " + DateTime.Now.ToStringZulu() + Environment.NewLine;

            BaseUtils.TranslatorMKII primary = new TranslatorMKII();
            primary.LoadTranslation(language, System.Globalization.CultureInfo.CurrentCulture, new string[] { txpath }, searchdepth, Path.GetTempPath(), true);

            if (!primary.Translating)
            {
                Console.WriteLine("Primary translation did not load " + language);
                return "";
            }

            List<string> primarykeys = primary.EnumerateKeys.ToList();

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

            foreach (string foreignlang in language2.EmptyIfNull())
            {
                BaseUtils.TranslatorMKII secondary = new BaseUtils.TranslatorMKII();
                secondary.LoadTranslation(foreignlang, System.Globalization.CultureInfo.CurrentCulture, new string[] { txpath }, searchdepth, @"c:\code", true);

                if (!secondary.Translating)
                {
                    Console.WriteLine("Secondary translation did not load " + foreignlang);
                    continue;
                }
                else
                    Console.WriteLine("Secondary translation loaded " + foreignlang);

                // first we transfer the translation from secondary to primary

                foreach (string primid in primarykeys)
                {
                    // if we have a secondary of the same name
                    // here we move the secondary translation to the primary array, ready for output later.

                    string orgenglish = primary.GetOriginalEnglish(primid);
                    string secid = primid;
                    if (!secondary.IsDefined(secid))        // we may have shifted to sha already, see if its there, use that to pick up the info to transfer to primary
                        secid = orgenglish.CalcSha8();

                    if (secondary.IsDefined(secid))
                    {
                        // if secondary translation is not null, i.e set

                        if (secondary.GetTranslation(secid) != null)
                        {
                            string sectranslation = secondary.GetTranslation(secid);

                            // check formatting

                            string res = VerifyFormattingClass.VerifyFormatting(secondary.GetOriginalFile(secid), secondary.GetOriginalLine(secid),
                                                            primary.GetOriginalEnglish(primid), sectranslation, secid);
                            if (res != null)
                            {
                                System.Diagnostics.Debug.WriteLine(res);
                                Console.WriteLine(res);
                                Console.WriteLine("Press key to continue");
                                Console.ReadKey();
                                reporttext += res + Environment.NewLine;
                            }

                            primary.ReDefine(primid, sectranslation);
                            reporttext += $"Secondary {primid} translation '{sectranslation.EscapeControlChars()}'" + Environment.NewLine;
                        }
                        else
                        {
                            // its a null
                            reporttext += $"Secondary {primid} !!! translation not defined " + Environment.NewLine;
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

                // we are writing out primary keys, secondary translations have moved to them

                string currentfilename = null;

                List<string> filename = new List<string>();                         // filenames created
                List<StringBuilder> fileoutputs = new List<StringBuilder>();        // with stringbuilder 

                Dictionary<string, string> hashtoenglish = new Dictionary<string, string>();

                int outputfileindex = 0;

                foreach (string id in primarykeys)
                {
                    string primaryfilename = primary.GetOriginalFile(id);

                    // transmute filename to foreign name
                    if (currentfilename == null || !primaryfilename.EqualsIIC(currentfilename))
                    {
                        string nerfname = primaryfilename.Replace(language, foreignlang);

                        int alreadyexists = filename.FindIndex(x => x.EqualsIIC(nerfname));
                        if (alreadyexists >= 0)
                        {
                            outputfileindex = alreadyexists;
                            System.Diagnostics.Debug.WriteLine($"Continue with previous output file {nerfname} {outputfileindex}");
                        }
                        else
                        {
                            outputfileindex = fileoutputs.Count;
                            fileoutputs.Add(new StringBuilder());
                            nerfname = nerfname.Replace(language.Substring(0, language.IndexOf("-")), foreignlang.Substring(0, foreignlang.IndexOf("-")));
                            filename.Add(nerfname);
                            System.Diagnostics.Debug.WriteLine($"Changed to new output file {nerfname} {outputfileindex}");
                        }

                        currentfilename = primaryfilename;
                    }

                    // these are captured blank lines, includes, // comments, SECTION comments

                    if (id.StartsWith("COMMENTBLANK:"))
                    {
                        string txt = primary.GetOriginalEnglish(id);
                        if (txt.StartsWith("include ", StringComparison.InvariantCultureIgnoreCase))
                        {
                            txt = txt.Replace(language.Substring(0, language.IndexOf("-")), foreignlang.Substring(0, foreignlang.IndexOf("-")));
                        }

                         fileoutputs[outputfileindex].Append(txt);
                        //System.Diagnostics.Debug.WriteLine($"{primary.GetOriginalFile(id)}:{primary.GetOriginalLine(id)} {txt}");
                    }
                    else
                    {
                        string orgenglish = primary.GetOriginalEnglish(id);

                        string shatouse = orgenglish.CalcSha8();

                        if (hashtoenglish.ContainsKey(shatouse))        // have we used that SHA before, if so, we are being too short.
                        {
                            if (hashtoenglish[shatouse] != orgenglish)
                            {
                                Console.WriteLine($"Clash of IDs {id} {shatouse} between `{hashtoenglish[shatouse]}` vs `{orgenglish}`");
                            }
                            else
                            {
                               // Console.WriteLine($"Repeat of identical English text {id} {orgsha} `{orgenglish}`");
                            }
                        }
                        hashtoenglish[shatouse] = orgenglish;

                        fileoutputs[outputfileindex].Append(shatouse);     // output id, colon, primary english text
                        fileoutputs[outputfileindex].Append(": ");
                        fileoutputs[outputfileindex].Append(orgenglish.EscapeControlChars().AlwaysQuoteString());

                        string secondarytranslation = primary.GetTranslation(id);    // pick up secondary translation, moved to primary slot by above code

                        if (secondarytranslation == null)       // null, its an @
                        {
                            fileoutputs[outputfileindex].Append(" @");
                        }
                        else
                        {
                            // else its a full translation

                            fileoutputs[outputfileindex].Append(" => ");
                            fileoutputs[outputfileindex].Append(secondarytranslation.EscapeControlChars().AlwaysQuoteString());
                        }
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
            }

            if ( (language2?.Length??0) == 0)
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
