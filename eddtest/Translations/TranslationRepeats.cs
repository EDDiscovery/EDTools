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
    public static class TranslationFileRepeats
    {
        // translator repeat is withdrawn from now to hold in back pockets

        // usage:
        // report on example state: normalisetranslate c:\code\eddiscovery\eddiscovery\translations 2 example-ex - "NS NoOutput" c:\code\renames.lst stdenums
        // normalise normalisetranslate c:\code\eddiscovery\eddiscovery\translations 2 example-ex francais-fr "NS"

        // you can scan for enums scanforenums  stdenums . *.cs to check if enums are in use

        static public string Process(string txpath, int searchdepth)
        {
//            Translator[] languages = new Translator[2] { new Translator(), new Translator() };
//            languages[0].LoadTranslation("example-ex", System.Globalization.CultureInfo.CurrentCulture, new string[] { txpath }, searchdepth,loadorgenglish: true, loadfile: true);

//            if (!languages[0].Translating)
//            {
//                Console.WriteLine("Primary translation did not load ");
//                return "";
//            }

//            //languages[1].LoadTranslation("deutsch-de", System.Globalization.CultureInfo.CurrentCulture, new string[] { txpath }, searchdepth, loadorgenglish: true, loadfile: true);

//            Dictionary<string, string> repeats = new Dictionary<string, string>();

//            foreach (var kvp in languages[0].originalenglish)
//            {
//                if (languages[0].translations[kvp.Key] == null || languages[0].translations[kvp.Key][0] != Translator.RedirectChar)
//                {
//                    List<string> keyrepeats = new List<string>();
//                    foreach (var kvp2 in languages[0].originalenglish)
//                    {
//                        if (kvp.Value == kvp2.Value)
//                        {
//                            keyrepeats.Add(kvp2.Key);
//                        }
//                    }

//                    if (keyrepeats.Count > 1)
//                    {
//                       // System.Diagnostics.Debug.WriteLine($"{kvp.Key} repeats {keyrepeats.Count}");

//                        if (kvp.Key.IndexOf('.') == -1)
//                        {
//                            foreach (var name in keyrepeats)
//                            {
//                                if ( name != kvp.Key)
//                                    languages[0].translations[name] = Translator.RedirectChar + kvp.Key;
//                            }
//                        }
//                        else
//                        {
//                            string commonname = "Common." + kvp.Value.FirstAlphaNumericText();

//                            if (repeats.ContainsKey(commonname))
//                            {
//                                System.Diagnostics.Debug.WriteLine($"Common name repeat {kvp.Value}");
//;                               commonname = "Common." + kvp.Key;
//                            }

//                            foreach (var name in keyrepeats)
//                            {
//                                languages[0].translations[name] = Translator.RedirectChar + commonname;
//                            }

//                            repeats[commonname] = kvp.Value;
//                        }
//                    }
//                }
//            }

//            for (int i = 0; i < 1; i++)
//            {
//                languages[i].WriteFiles(@"c:\code");
              
//                string firstfile = languages[0].originalfile.Values.ToList().First();
//                string name = Path.GetFileNameWithoutExtension(firstfile);
//                firstfile = Path.Combine(@"c:\code", Path.GetFileName(firstfile));

//                File.AppendAllText(firstfile, $"{Environment.NewLine}Section Common{Environment.NewLine}");
//                foreach ( var kvp in repeats)
//                {
//                    File.AppendAllText(firstfile, $"{kvp.Key.ReplaceIfStartsWith("Common","")}: {kvp.Value.AlwaysQuoteString()} @{Environment.NewLine}");
//                }



//                File.AppendAllText(firstfile, $"{Environment.NewLine}include translation-{name}-uc.tlp {Environment.NewLine}include translation-{name}-je.tlp {Environment.NewLine}include translation-{name}-ed.tlp {Environment.NewLine}");
//            }

            return "";
        }
    }

}

