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
        static public string Process(string language, string txpath, int searchdepth,
                                    string language2,
                                    string options
            )            // overall index of items
        {
            BaseUtils.Translator primary = BaseUtils.Translator.Instance;
            primary.LoadTranslation(language, System.Globalization.CultureInfo.CurrentCulture, new string[] { txpath }, searchdepth, Path.GetTempPath(), loadorgenglish: true, loadfile: true);

            if (!primary.Translating)
            {
                Console.WriteLine("Primary translation did not load " + language);
                return "";
            }

            BaseUtils.Translator secondary = new BaseUtils.Translator();
            if (language2 != null)
            {
                secondary.LoadTranslation(language2, System.Globalization.CultureInfo.CurrentCulture, new string[] { txpath }, searchdepth, @"c:\code", loadorgenglish: true, loadfile: true);

                if (!secondary.Translating)
                {
                    Console.WriteLine("Secondary translation did not load " + language2);
                    return "";
                }
            }

            string totalret = "";

            string section = "";

            string filetowrite = "";

            List<StreamWriter> filelist = new List<StreamWriter>();
            StreamWriter batchfile = null;

            bool hasdotted = false;

            foreach (string id in primary.EnumerateKeys)
            {
                string ret = "";

                string orgfile = primary.GetOriginalFile(id);
                FileInfo fi = new FileInfo(orgfile);
                if (filetowrite == null || !filetowrite.Equals(fi.Name))
                {
                    //ret += Environment.NewLine + "WRITETOFILE " + filetowrite + Environment.NewLine;

                    filetowrite = fi.Name;

                    if (secondary.Translating)
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

                        batchfile.WriteLine("copy " + txname + " " + txorgfile);
                    }
                    else
                    {
                        filelist.Add(new StreamWriter(Path.Combine(".", filetowrite), false, Encoding.UTF8));

                        if (filelist.Count > 1)
                            filelist[0].WriteLine(Environment.NewLine + "include " + filetowrite);
                    }
                }

                string idtouse = id;

                StringParser sp = new StringParser(id);
                string front = sp.NextWord(".:");
                if (front != section)
                {
                    if (hasdotted || sp.IsChar('.'))
                        ret += Environment.NewLine + "SECTION " + front + Environment.NewLine + Environment.NewLine;
                    if (sp.IsChar('.'))
                        hasdotted = true;
                    section = front;
                }

                if (hasdotted && sp.IsChar('.'))
                    idtouse = sp.LineLeft; ;

                string primaryorgeng = primary.GetOriginalEnglish(id);
                string translation = primary.GetTranslation(id);            // default is from the first file
                System.Diagnostics.Debug.WriteLine($"Org {id} = `{primaryorgeng}` `{translation}`");
                bool secondarydef = false;

                if (secondary.Translating)
                {
                    if (secondary.IsDefined(id))
                    {
                        secondarydef = true;                                // we have a secondary def of the id
                        translation = secondary.GetTranslation(id);
                        if ( translation != null )
                            VerifyFormatting(primaryorgeng, translation, id);
                        secondary.UnDefine(id);                             // remove id so its not reported as missing
                        System.Diagnostics.Debug.WriteLine($"Found {id} removed");
                    }
                    else
                    {
                        translation = null;                                 // meaning not present
                    }
                }

                ret += idtouse + ": " + primaryorgeng.AlwaysQuoteString().EscapeControlChars();        // output is id : orgeng

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
                    if (containsnonspacedigits)
                        totalret += id + " in " + primary.GetOriginalFile(id) + ":" + primary.GetOriginalLine(id) + " Not defined by secondary" + Environment.NewLine;

                    ret += " @";
                }
                else
                {
                    ret += " => " + translation.AlwaysQuoteString().EscapeControlChars();       // place translated string into file again
                }

                ret += Environment.NewLine;


                //  totalret += ret;
                filelist.Last().Write(ret);
            }

            if (secondary.Translating)
            {
                foreach (string id in secondary.EnumerateKeys)
                {
                    totalret += "**************** Secondary defines " + id + " in " + secondary.GetOriginalFile(id) + ":" + secondary.GetOriginalLine(id) + " but primary does not" + Environment.NewLine;
                }
            }

            foreach (var f in filelist)
                f.Close();

            if (batchfile != null)
                batchfile.Close();

            return totalret;
        }

        static public void VerifyFormatting(string eng, string trans, string id)
        {
            int pos = 0;
            int post = 0;
            bool bad = false;
            while ((pos = eng.IndexOf("{", pos)) != -1)
            {
                bad = true;

                int endpos = eng.IndexOf("}", pos);
                if ( endpos != -1 )
                {
                    post = trans.IndexOf("{", post);
                    if ( post != -1 )
                    {
                        int endpost = trans.IndexOf("}", post);
                        if ( endpost != -1 )
                        {
                            string endform = eng.Substring(pos, endpos - pos + 1);
                            string transform = trans.Substring(post, endpost - post + 1);
                            bad = !endform.Equals(transform);
                        }
                    }
                }

                pos++;
                post++;
            }

            int semicolons = 0;
            for (int i = 0; i < eng.Length; i++)
                semicolons += eng[i] == ';' ? 1 : 0;

            if ( semicolons == 2 )
            {
                bad = true;

                int semitrans = 0;
                for (int i = 0; i < trans.Length; i++)
                    semitrans += trans[i] == ';' ? 1 : 0;

                if ( semitrans == semicolons )
                {
                    string e = eng.Substring(eng.LastIndexOf(';'));
                    string t = trans.Substring(trans.LastIndexOf(';'));
                    bad = !e.Equals(t);
                }

            }

            if ( bad )
            {
                Console.WriteLine($"Bad formatting {id}: {eng} -> {trans}");
            }
        }
    }
}

