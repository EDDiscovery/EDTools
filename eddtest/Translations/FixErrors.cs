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
    public static class FixErrors
    {
        // fix leakage of definitions due to error in normalisetranslate mk II

        static public string Process(string language)
        {
            string reporttext = $"Report at " + DateTime.Now.ToStringZulu() + Environment.NewLine;

            BaseUtils.TranslatorMkII primary = new TranslatorMkII();
            primary.LoadTranslation(language, System.Globalization.CultureInfo.CurrentCulture, new string[] { @"c:\code\eddiscovery\eddiscovery\translations" }, 2, Path.GetTempPath(), null, true, true);

            if (!primary.Translating)
            {
                Console.WriteLine("Primary translation did not load " + language);
                return "";
            }

            BaseUtils.Translator oldtranslator = new Translator();
            oldtranslator.LoadTranslation(language, System.Globalization.CultureInfo.CurrentCulture, new string[] { @"c:\code\eddiscovery2\eddiscovery\translations" }, 2, null, true);

            if (!oldtranslator.Translating)
            {
                Console.WriteLine("Primary translation did not load " + language);
                return "";
            }

            List<string> primarykeys = primary.EnumerateKeys.ToList();

            List<string> oldenglish = oldtranslator.originalenglish.Values.ToList();

            // lets check if english text differs in primary

            foreach (string id in primarykeys)
            {
                if (!TranslatorMkII.IsSourceID(id))
                {
                    primary.TryGetOriginalEnglish(id, out string english);      // get the english from the new file

                    if (english == "Interdicted")
                    {
                    }

                    // see if we have IDs for it in original file

                    var idinoldfile = oldtranslator.originalenglish.Where(kvp => kvp.Value.Contains(english, StringComparison.InvariantCultureIgnoreCase)).Select(kvp => kvp.Key).ToList();

                    if (idinoldfile.Count >= 1)
                    {
                        string translation = null;

                        foreach (var oldid in idinoldfile)      // find a translation, some may be null, status for instance had 6 instances in the old file
                        {
                            translation = oldtranslator.GetTranslation(oldid);
                            if (translation != null)
                                break;
                        }
                        //System.Diagnostics.Debug.WriteLine($"{id} {english} is in old file as {idinoldfile[0]} translated {translation}");

                        primary.TryGetValue(id, out string newtranslation);

                        if (newtranslation != translation)
                        {
                            if (translation == null)
                            {
                                System.Diagnostics.Debug.WriteLine($" ERROR : {id} `{english}` `{newtranslation}` different to {idinoldfile[0]} MARKED AS NULL");
                                primary.ReDefine(id, null);
                            }
                            else 
                            {
                                primary.ReDefine(id, translation);
                                //if ((int)newtranslation[0] < 0x2000)
                                //{
                                //    primary.ReDefine(id, translation);
                                //    //System.Diagnostics.Debug.WriteLine($" ERROR : {id} {english} {newtranslation} different to {idinoldfile[0]} {translation}");
                                //}
                            }
                        }

                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"{id} {english} NOT in old file");
                    }
                }
            }

            NormaliseTranslationFilesMKII.WriteTranslatorFiles(primary);

            File.WriteAllText("report.txt", reporttext);

            return reporttext;

        }


    }
}

