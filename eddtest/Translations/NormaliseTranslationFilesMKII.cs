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
        // compare translations: normalisetranslatemkii c:\code\eddiscovery\eddiscovery\translations 2 example-ex francais-fr
        // normalisetranslatemkii c:\code\eddiscovery\eddiscovery\translations 2 example-ex example-ex francais-fr chinese-zh deutsch-de italiano-it polski-pl portugues-pt-br russian-ru spanish-es
        // normalisetranslatemkii c:\code\eddiscovery\eddiscovery\translations 2 example-ex deutsch-de 

        static public string ProcessNew(string language, string txpath, int searchdepth, string[] language2)
        {
            string reporttext = $"Report at " + DateTime.Now.ToStringZulu() + Environment.NewLine;

            BaseUtils.TranslatorMkII primary = new TranslatorMkII();
            primary.LoadTranslation(language, System.Globalization.CultureInfo.CurrentCulture, new string[] { txpath }, searchdepth, Path.GetTempPath(), null, true, true);

            if (!primary.Translating)
            {
                Console.WriteLine("Primary translation did not load " + language);
                return "";
            }

            List<string> primarykeys = primary.EnumerateKeys.ToList();

            // lets check if english text differs in primary

            //foreach (string id in primarykeys)
            //{
            //    //System.Diagnostics.Debug.WriteLine($"Primary  {primary.GetOriginalFile(id)}:{primary.GetOriginalLine(id)}: {id} `{primary.GetOriginalEnglish(id)}` `{primary.GetTranslation(id)}`");

            //    if (!id.StartsWith("COMMENTBLANK:") &&
            //                primary.GetTranslation(id) != null && primary.GetTranslation(id) != primary.GetOriginalEnglish(id))        // not @, and not the same
            //    {
            //        System.Diagnostics.Debug.WriteLine($"*** Primary translation not same as english {primary.GetOriginalFile(id)} : {primary.GetOriginalLine(id)} : {id} : {primary.GetOriginalEnglish(id)} : {primary.GetTranslation(id)}");
            //        reporttext += $"*** Primary translation not same as english {primary.GetOriginalFile(id)} : {primary.GetOriginalLine(id)} : {id} : {primary.GetOriginalEnglish(id)} : {primary.GetTranslation(id)}" + Environment.NewLine;
            //    }
            //}

            foreach (string foreignlang in language2.EmptyIfNull())
            {
                BaseUtils.TranslatorMkII secondary = new BaseUtils.TranslatorMkII();
                secondary.LoadTranslation(foreignlang, System.Globalization.CultureInfo.CurrentCulture, new string[] { txpath }, searchdepth, @"c:\code", null, true, true);

                if (!secondary.Translating)
                {
                    Console.WriteLine("Secondary translation did not load " + foreignlang);
                    continue;
                }
                else
                    Console.WriteLine("Secondary translation loaded " + foreignlang);

                // first we transfer the translation from secondary to primary

                foreach (string id in primarykeys)
                {
                    // if we have a secondary of the same name
                    // here we move the secondary translation to the primary array, ready for output later.

                    if (!TranslatorMkII.IsSourceID(id))
                    {
                        if (secondary.TryGetValue(id, out string sectranslation) && sectranslation != null) // if we have a defined ID in the secondary
                        {
                            secondary.TryGetSource(id, out string secfile, out int seclineno);
                            primary.TryGetOriginalEnglish(id, out string orgenglish);

                            // check formatting
                            string res = VerifyFormattingClass.VerifyFormatting(secfile, seclineno, orgenglish, sectranslation, id);
                            if (res != null)
                            {
                                System.Diagnostics.Debug.WriteLine(res);
                                Console.WriteLine(res);
                                //Console.WriteLine("Press key to continue");
                                //Console.ReadKey();
                                reporttext += res + Environment.NewLine;
                            }

                            primary.ReDefine(id, sectranslation);
                            reporttext += $"Secondary {id} translation '{sectranslation.EscapeControlChars()}'" + Environment.NewLine;
                        }
                        else
                        {
                            // SET primary translation to null , meaning no translation, so it will come out as @
                            primary.ReDefine(id, null);
                            reporttext += $"Secondary {id} not found" + Environment.NewLine;
                            //System.Diagnostics.Debug.WriteLine($".. not found in secondary");
                        }
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
                    primary.TryGetSource(id, out string primaryfilename, out int _);
                    primary.TryGetValue(id, out string translation);             // this has the translation in it, or for comments the text line. May be null if not defined

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

                    if (TranslatorMkII.IsSourceID(id))
                    {
                        if (translation.StartsWith("include ", StringComparison.InvariantCultureIgnoreCase))
                        {
                            translation = translation.Replace(language.Substring(0, language.IndexOf("-")), foreignlang.Substring(0, foreignlang.IndexOf("-")));
                        }

                        fileoutputs[outputfileindex].Append(translation);
                        fileoutputs[outputfileindex].Append(Environment.NewLine);

                        //System.Diagnostics.Debug.WriteLine($"{primary.GetOriginalFile(id)}:{primary.GetOriginalLine(id)} {txt}");
                    }
                    else
                    {
                        primary.TryGetOriginalEnglish(id, out string orgenglish);

                        // Manual Fixups

                        orgenglish = orgenglish.ReplaceIfEndsWith(": ").ReplaceIfEndsWith(":");

                        if (translation != null)
                        {
                            translation = translation.ReplaceIfEndsWith(": ").ReplaceIfEndsWith(":");
                        }

                        // end

                        string shatouse = orgenglish.CalcSha8();

                        bool repeated = false;

                        if (hashtoenglish.ContainsKey(shatouse))        // have we used that SHA before, if so, we are being too short.
                        {
                            if (hashtoenglish[shatouse] != orgenglish)
                            {
                                Console.WriteLine($"Clash of IDs {id} {shatouse} between `{hashtoenglish[shatouse]}` vs `{orgenglish}`");
                            }
                            else
                            {
                                repeated = true;
                                reporttext += $"Repeat of identical English text {id} `{orgenglish}`" +Environment.NewLine;
                            }
                        }

                        if (!repeated)
                        {
                            hashtoenglish[shatouse] = orgenglish;

                            fileoutputs[outputfileindex].Append(shatouse);     // output id, colon, primary english text
                            fileoutputs[outputfileindex].Append(": ");
                            fileoutputs[outputfileindex].Append(orgenglish.EscapeControlChars().AlwaysQuoteString());

                            if (translation == null)       // null, its an @
                            {
                                fileoutputs[outputfileindex].Append(" @");
                            }
                            else
                            {
                                // else its a full translation

                                fileoutputs[outputfileindex].Append(" => ");
                                fileoutputs[outputfileindex].Append(translation.EscapeControlChars().AlwaysQuoteString());
                            }
                            fileoutputs[outputfileindex].Append(Environment.NewLine);
                        }
                    }

                }

                bool overwrite = true;

                if (overwrite)
                {
                    //now overwrite them all
                    for (int i = 0; i < fileoutputs.Count; i++)
                    {
                        string contents = fileoutputs[i].ToString();
                        File.WriteAllText(filename[i], contents, Encoding.UTF8);
                        reporttext += $"Writing contents to {filename[i]}" + Environment.NewLine;
                    }
                }

                // to check reread - not needed

                //BaseUtils.TranslatorMkII secondaryreread = new BaseUtils.TranslatorMkII();
                //secondaryreread.LoadTranslation(foreignlang, System.Globalization.CultureInfo.CurrentCulture, new string[] { txpath }, searchdepth, @"c:\code", $"reread-{foreignlang}.log", true, true);

                //if (!secondaryreread.Translating)
                //{
                //    Console.WriteLine("Secondary translation did not reload after change" + foreignlang);
                //}
                //else
                //{
                //    int read1 = secondary.EnumerateKeys.Count();
                //    int read2 = secondaryreread.EnumerateKeys.Count();
                //    if ( read1 != read2 )
                //        Console.WriteLine("Secondary reread has different number of keys " + foreignlang);
                //    else
                //        Console.WriteLine("Secondary translation reloaded " + foreignlang);
                //}

            }

            //if ( (language2?.Length??0) == 0)
            //{
            //    foreach (string id in primarykeys)
            //    {
            //        reporttext += $"{id} in {primary.GetOriginalFile(id)} : {primary.GetOriginalLine(id)} : org '{primary.GetOriginalEnglish(id)}' : tx '{primary.GetTranslation(id)}'" + Environment.NewLine;
            //    }
            //}

            File.WriteAllText("report.txt", reporttext);

            return reporttext;

        }


        public static void WriteInfo(TranslatorMkII primary)
        {
            List<string> primarykeys = primary.EnumerateKeys.ToList();

            List<string> reordered = new List<string>();
            foreach (var x in primarykeys)
            {
                if (!TranslatorMkII.IsSourceID(x))
                {
                    primary.TryGetOriginalEnglish(x, out string v);
                    if ((v.EndsWith(":") && primary.IsDefinedEnglish(v.Substring(0, v.Length - 1)) ||
                        (v.EndsWith(": ") && primary.IsDefinedEnglish(v.Substring(0, v.Length - 2)))
                        ))
                    {
                        reordered.Add(v);
                    }
                }
            }


            reordered.Sort();
            string sl = string.Join(Environment.NewLine, reordered);
            FileHelpers.TryWriteToFile(@"c:\code\keys.txt", sl);

            List<string> coloned = new List<string>();
            foreach (var x in primarykeys)
            {
                if (!TranslatorMkII.IsSourceID(x))
                {
                    primary.TryGetOriginalEnglish(x, out string v);
                    if (v.EndsWith(":") || v.EndsWith(": "))
                    {
                        coloned.Add(v);
                    }
                }
            }

            coloned.Sort();
            sl = string.Join(Environment.NewLine, coloned);
            FileHelpers.TryWriteToFile(@"c:\code\keyscolon.txt", sl);
        }

        // demo writing out primary files

        public static void WriteTranslatorFiles(TranslatorMkII primary)
        {
            List<StringBuilder> fileoutputs = new List<StringBuilder>();        // with stringbuilder 
            string currentfilename = null;
            List<string> filename = new List<string>();                         // filenames created
            int outputfileindex = 0;

            var primarykeys = primary.EnumerateKeys.ToList();       // reload, may have deleted

            foreach (string id in primarykeys)
            {
                primary.TryGetSource(id, out string primaryfilename, out int _);
                primary.TryGetValue(id, out string translation);             // this has the translation in it, or for comments the text line. May be null if not defined

                if (currentfilename == null || !primaryfilename.EqualsIIC(currentfilename))
                {
                    string nerfname = primaryfilename;

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
                        filename.Add(nerfname);
                        System.Diagnostics.Debug.WriteLine($"Changed to new output file {nerfname} {outputfileindex}");
                    }

                    currentfilename = primaryfilename;
                }

                if (TranslatorMkII.IsSourceID(id))
                {
                    fileoutputs[outputfileindex].Append(translation);
                    fileoutputs[outputfileindex].Append(Environment.NewLine);

                    //System.Diagnostics.Debug.WriteLine($"{primary.GetOriginalFile(id)}:{primary.GetOriginalLine(id)} {txt}");
                }
                else
                {
                    primary.TryGetOriginalEnglish(id, out string orgenglish);

                    string shatouse = orgenglish.CalcSha8();

                    fileoutputs[outputfileindex].Append(shatouse);     // output id, colon, primary english text
                    fileoutputs[outputfileindex].Append(": ");
                    fileoutputs[outputfileindex].Append(orgenglish.EscapeControlChars().AlwaysQuoteString());

                    if (translation == null)       // null, its an @
                    {
                        fileoutputs[outputfileindex].Append(" @");
                    }
                    else
                    {
                        // else its a full translation

                        fileoutputs[outputfileindex].Append(" => ");
                        fileoutputs[outputfileindex].Append(translation.EscapeControlChars().AlwaysQuoteString());
                    }
                    fileoutputs[outputfileindex].Append(Environment.NewLine);
                }
            }

            for (int i = 0; i < fileoutputs.Count; i++)
            {
                string contents = fileoutputs[i].ToString();
                File.WriteAllText(filename[i], contents, Encoding.UTF8);
            }
        }

    }
}

